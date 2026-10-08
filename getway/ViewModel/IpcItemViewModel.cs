using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using getway.View;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace getway.ViewModel
{
    class IpcItemViewModel : ViewModelBase
    {

        // 必须是 ObservableCollection：条款逐个更新/增删才能推给界面
        private ObservableCollection<IpcUserModel> _IpcUserList = new ObservableCollection<IpcUserModel>();
        public ObservableCollection<IpcUserModel> IpcUserList { get => _IpcUserList; set => SetProperty(ref _IpcUserList, value); }

        private ObservableCollection<BorderModel> _BorderList = new ObservableCollection<BorderModel>();
        public ObservableCollection<BorderModel> BorderList { get => _BorderList; set => SetProperty(ref _BorderList, value); }

        public string BoardNo { get; set; }
        private BorderModel? _SelectBorder;
        public BorderModel? SelectBorder
        {
            get => _SelectBorder;
            set => SetSelectBorder(value, CancellationToken.None);
        }





        // 改选中项并触发用户列表刷新；token 用于丢弃过期结果
        private void SetSelectBorder(BorderModel? border, CancellationToken token)
        {
            if (SetProperty(ref _SelectBorder, border))
            {
                // 停掉上一块板卡的轮询循环，再按新的槽位重新拉数据
                // （RefreshIpcAsync 用 _isSipUserReg 做开关，这里置 false 旧循环会自行退出）
                _isSipUserReg = false;
                BoardNo = border.SlotNo.ToString();
                RefreshIpcAsync();
            }
        }

        EditorSipUserView sipUserView;
        //板卡，sip用户注册、呼叫状态查询线程状态
        private bool _isBorder = false;
        private bool _isSipUserReg = false;
        private bool _isSipUserCall = false;
        private bool _isFirstQuery = true;//初次查询，要进行状态显示

        private CancellationTokenSource _cts;
        private readonly Random _random = new Random();

        // 最近一次用户列表刷新任务，供父 ViewModel 等待
        private Task _ipcRefresh = Task.CompletedTask;
        public Task IpcRefresh => _ipcRefresh;

        // telnet 连接标识，由父 ViewModel（GetWayViewMode）在连接建立后注入
        public string IP { get; private set; } = string.Empty;

        public ICommand SelectBorderCommand { get; set; }
        public ICommand EditchSipUserCommand { get; set; }

        // 日志出口：由父 ViewModel（GetWayViewMode）注入，未注入时静默丢弃
        private readonly ILogSink? _logSink;

        /// <summary>写一行日志；未注入日志出口时什么都不做</summary>
        private void Log(string text) => _logSink?.AppendLog(text);
        private void QuerStatus(int status, string tip) => _logSink?.QueryStatusFun(status, tip);


        public IpcItemViewModel(ILogSink? logSink = null)
        {
            _logSink = logSink;

            // 构造时只做最小化初始化，不取数据：此时父 ViewModel 还没建立连接
            SelectBorderCommand = new Command(SelectBorderExecute);
            EditchSipUserCommand = new ViewModelCommand(QueryHotLine);

            _cts = new CancellationTokenSource();
            //生成临时数据
            TempBorderInfo();
            TempSipUserInfo();
        }

        /// <summary>
        /// 由父 ViewModel 在 telnet 连接成功后注入，取数据前必须已有 key
        /// </summary>
        public void SetIp(string ip) => IP = ip ?? string.Empty;
        public void InitFirst() => _isFirstQuery = true;

        //重查板卡与 SIP 用户
        public async Task RefreshAllAsync(CancellationToken token = default)
        {
            RefreshBoardAsync();
            //await IpcRefresh;
            RefreshIpcAsync();
        }

        /// <summary>
        /// 所有线程终止
        /// </summary>
        public void InitQueryTag()
        {
            _isBorder = false;
            _isSipUserReg = false;
            _isSipUserCall = false;

        }

        /// <summary>
        /// 获取卡框板槽信息
        /// </summary>
        public void RefreshBoardAsync()
        {
            if (string.IsNullOrEmpty(IP))
            {
                TempBorderInfo();
                SelectBorder = null;
                return;
            }
            if (_isBorder) return;
            _isBorder = true;
            string ip = IP;

            Task.Run(async () =>
            {
                string name = DefaulConfig.BaseUsername;
                string password = DefaulConfig.BasePassword;
                string key = ip + name;

                if (TcpConnect.AddTelnet(ip + name, ip, name, password) == null)
                {
                    Log($"板卡查询连接失败：{ip}");
                    _isBorder = false;
                    return;
                }

                Log($"开始查询板卡：{ip}");

                while (!_cts.Token.IsCancellationRequested && _isBorder)
                {
                    try
                    {
                        TelnetEvent.QueryBoard(key, 0);
                        await Task.Delay(1000, _cts.Token);

                        string result = TcpConnect.Receive(key);
                        TelnetEvent.BorderString(result, BorderList);

                        await Task.Delay(4000, _cts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        Log($"板卡轮询出错：{ex.Message}");
                        await Task.Delay(5000, _cts.Token);
                    }
                }
            });

            // 切换网关期间旧结果不能覆盖新网关数据
            if (_cts.Token.IsCancellationRequested) return;



        }

        /// <summary>
        /// 获取sip用户注册数据
        /// </summary>
        public void RefreshIpcAsync(CancellationToken token = default)
        {

            if (string.IsNullOrEmpty(IP) || _SelectBorder == null)
            {
                TempSipUserInfo();
                return;
            }

            //数据赋值
            string ip = IP;
            int slotNo = _SelectBorder.SlotNo;
            // 本次查询的兜底总时长，超时后无条件退出
            const int TotalTimeoutMs = 10000;

            if (_isSipUserReg || _isSipUserCall) return;

            //第一次加载，进行查询状态回显
            bool isFirstReg = false;
            bool isFirstCall = false;
            if (_isFirstQuery)
            {
                QuerStatus(2, "加载中");
                //起线程，检查加载完成
                Task.Run(async () =>
                {
                    int out_flag = 0;
                    while (true)
                    {
                        if (isFirstCall && isFirstReg)
                        {
                            QuerStatus(3, "加载成功");
                            break;
                        }
                        if (out_flag > 40)
                        {
                            QuerStatus(4, "连接超时");
                            break;
                        }
                        out_flag++;
                        await Task.Delay(200, _cts.Token);
                    }

                });

                _isFirstQuery = false;
            }

            //起线程，后台循环查询sip用户注册状态
            _isSipUserReg = true;
            Task.Run(async () =>
            {
                string name = DefaulConfig.QuerySIPUserRegStateUsername;
                string password = DefaulConfig.QuerySIPUserRegStatePassword;
                string key = ip + name;

                if (TcpConnect.AddTelnet(ip + name, ip, name, password) == null)
                {
                    Log($"SIP 注册状态查询连接失败：{ip}");
                    _isSipUserReg = false;
                    return;
                }

                while (!_cts.Token.IsCancellationRequested && _isSipUserReg)
                {

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        //查询sip用户注册数据
                        TelnetEvent.QuerySipUser(key, 0, slotNo);

                        int emptyCount = 0;
                        while (!token.IsCancellationRequested && sw.ElapsedMilliseconds < TotalTimeoutMs)
                        {
                            string result = TcpConnect.Receive(key);

                            // 设备这次没吐数据：连续几次都没有就认为这条命令回显结束
                            if (string.IsNullOrEmpty(result))
                            {
                                if (++emptyCount >= DefaulConfig.MaxEmptyCount) break;
                                Thread.Sleep(200);
                                continue;
                            }

                            emptyCount = 0;
                            TelnetEvent.SipUserRegString(result, IpcUserList);
                            Log($"注册状态回显（{sw.ElapsedMilliseconds} ms）：{result}");
                            if (DefaulConfig.Debug_ShowTelnet)
                            {
                                Log($"注册状态回显（{sw.ElapsedMilliseconds} ms）：{result}");
                            }

                            // 回显里出现命令提示符（dmkj...#）说明这条命令已经执行完
                            if (ConfigUtil.IsCommandEndFlag(result)) break;

                            Thread.Sleep(200);

                        }

                        isFirstReg = true;
                        Log($"SIP 注册状态查询成功：耗时{sw.ElapsedMilliseconds} ms");

                        //本次查询时间3s后，开始下一次查询
                        await Task.Run(() =>
                        {
                            while (true)
                            {
                                if (sw.ElapsedMilliseconds > 3000)
                                {
                                    break;
                                }
                                Thread.Sleep(100);
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        Log($"SIP 呼叫状态查询出错：{ex.Message}");
                        _isSipUserReg = false;
                        break;
                    }
                }


            });

            //起线程，后台循环查询sip用户呼叫状态
            _isSipUserCall = true;
            Task.Run(async () =>
            {

                //获取配置信息
                string name = DefaulConfig.QuerySIPUserCallStateUsername_1;
                string password = DefaulConfig.QuerySIPUserCallStatePassword_1;
                string key = ip + name;
                //添加telnet连接
                if (TcpConnect.AddTelnet(ip + name, ip, name, password) == null)
                {
                    Log($"SIP 呼叫状态查询连接失败：{ip}");
                    _isSipUserCall = false;
                    return;
                }

                while (!_cts.Token.IsCancellationRequested && _isSipUserCall)
                {

                    try
                    {
                        var sw = Stopwatch.StartNew();
                        //查询呼叫状态数据
                        TelnetEvent.QuerySipUserCall(key, 0, slotNo);

                        int emptyCount = 0;
                        while (!token.IsCancellationRequested && sw.ElapsedMilliseconds < TotalTimeoutMs)
                        {
                            string result = TcpConnect.Receive(key);

                            // 设备这次没吐数据：连续几次都没有就认为这条命令回显结束
                            if (string.IsNullOrEmpty(result))
                            {
                                if (++emptyCount >= DefaulConfig.MaxEmptyCount) break;
                                Thread.Sleep(200);
                                continue;
                            }

                            emptyCount = 0;
                            TelnetEvent.SipUserCallString(result, IpcUserList);
                            if (DefaulConfig.Debug_ShowTelnet)
                            {
                                Log($"呼叫状态回显（{sw.ElapsedMilliseconds} ms）：{result}");
                            }

                            // 回显里出现命令提示符（dmkj...#）说明这条命令已经执行完
                            if (ConfigUtil.IsCommandEndFlag(result)) break;

                            Thread.Sleep(200);
                        }

                        isFirstCall = true;
                        Log($"SIP 呼叫状态查询结束：耗时 {sw.ElapsedMilliseconds} ms");

                        //本次查询时间3s后，开始下一次查询
                        await Task.Run(() =>
                        {
                            while (true)
                            {
                                if (sw.ElapsedMilliseconds > 3000)
                                {
                                    break;
                                }
                                Thread.Sleep(100);
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        Log($"SIP 呼叫状态查询出错：{ex.Message}");
                        _isSipUserCall = false;
                        break;
                    }
                }
            });

            //Log($"开始查询板卡 {slotNo} 的 SIP 用户状态");


            if (_cts.Token.IsCancellationRequested) return;
        }


        /// <summary>
        /// 查询一次 SIP 用户呼叫状态。内含 Thread.Sleep，调用方要放到后台线程执行。
        /// </summary>
        public void selectSipUserCallStatus(CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(IP))
            {
                Log("网关未连接，无法查询 SIP 呼叫状态");
                return;
            }


            string ip = IP;
            int slotNo = _SelectBorder?.SlotNo ?? 1;

            var sw = Stopwatch.StartNew();

            // 本次查询的兜底总时长，超时后无条件退出
            const int TotalTimeoutMs = 10000;
            const int MaxEmptyCount = 3;

            try
            {
                string name = DefaulConfig.QuerySIPUserCallStateUsername_1;
                string password = DefaulConfig.QuerySIPUserCallStatePassword_1;
                string key = ip + name;

                if (TcpConnect.AddTelnet(key, ip, name, password) == null)
                {
                    Log($"SIP 呼叫状态查询连接失败：{ip}");
                    return;
                }

                TelnetEvent.QuerySipUserCall(key, 0, slotNo);

                int emptyCount = 0;
                while (!token.IsCancellationRequested && sw.ElapsedMilliseconds < TotalTimeoutMs)
                {
                    string result = TcpConnect.Receive(key);

                    // 设备这次没吐数据：连续几次都没有就认为这条命令回显结束
                    if (string.IsNullOrEmpty(result))
                    {
                        if (++emptyCount >= MaxEmptyCount) break;
                        Thread.Sleep(200);
                        continue;
                    }

                    emptyCount = 0;
                    TelnetEvent.SipUserCallString(result, IpcUserList);
                    Log($"呼叫状态回显（{sw.ElapsedMilliseconds} ms）：{result}");

                    // 回显里出现命令提示符（dmkj...#）说明这条命令已经执行完
                    if (ConfigUtil.IsCommandEndFlag(result)) break;

                    Thread.Sleep(200);
                }

                Log($"SIP 呼叫状态查询结束：耗时 {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Log($"SIP 呼叫状态查询出错：{ex.Message}");
            }
        }

        //选择板卡运行方法
        private void SelectBorderExecute(object parameter)
        {
            if (parameter is BorderModel border)
            {
                SelectBorder = border;
            }
        }

        public void TempBorderInfo()
        {

            BorderList.Clear();
            for (int i = 1; i <= 4; i++)
            {
                BorderList.Add(new BorderModel() { BorderName = $"板卡{i}", SlotNo = i, IsEnable = false });
            }
        }

        public void TempSipUserInfo()
        {

            IpcUserList.Clear();
            for (int i = 0; i < 64; i++)
            {
                string status = _random.Next(6).ToString();
                IpcUserList.Add(new IpcUserModel() { Name = Convert.ToString(8002 + i), Status = status, FSP = $"0/1/{i}" });
            }
            string a = "";
        }

        public IpcUserModel getIpcUser(string phoneNum)
        {
            if (string.IsNullOrEmpty(phoneNum)) return null;

            foreach (IpcUserModel item in IpcUserList)
            {
                if (phoneNum.Equals(item.Name))
                {
                    return item;
                }
            }
            return null;
        }

        //---------------------编辑热线--------------------------


        public void QueryHotLine(object paramter)
        {
            string result = string.Empty;
            string key = DefaulConfig.GetBaseKey();
            string phoneNum = (string)paramter;
            string hotlineNum = string.Empty;
            string hotlinetime = string.Empty;


            TelnetEvent.QueryHotLine(DefaulConfig.FrameId.ToString(), BoardNo, phoneNum);
            result = TcpConnect.Receive(key);
            IpcUserModel model = getIpcUser(phoneNum);
            Dictionary<string, string> dic = TelnetEvent.MapContentString(result);
            model.HotlinePhone = dic["hotlinenum"];
            model.HotlineTime = dic["hottime"];

            if (string.IsNullOrEmpty(result)) Log($"查询热线无回显：{phoneNum}");
            if (model == null) Log($"用户列表中未找到：{phoneNum}");



            sipUserView = new EditorSipUserView(BoardNo, phoneNum, hotlineNum, hotlinetime);
            sipUserView.ShowDialog();

        }


    }
}
