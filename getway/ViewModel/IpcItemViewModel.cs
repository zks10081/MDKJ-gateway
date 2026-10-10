using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using getway.View;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
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



        //板卡，sip用户注册、呼叫状态查询线程状态
        private bool _isBorder = false;
        private bool _isSipUserReg = false;
        private bool _isSipUserCall = false;
        private bool _isFirstQuery = true;//初次查询，要进行状态显示
        private bool _isSipQueryStatus = true;
        private bool _isSipUserCallStatus = false;
        private bool _isSipUserRegStatus = false;

        private EditorSipUserView sipUserView;
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
        private void ChangBorderShow(string action) => _logSink?.ChangBorderShow(action);


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

        // 改选中项并触发用户列表刷新；token 用于丢弃过期结果
        private void SetSelectBorder(BorderModel? border, CancellationToken token)
        {
            if (SetProperty(ref _SelectBorder, border))
            {
                if (border == null) return;
                // 停掉上一块板卡的轮询循环，再按新的槽位重新拉数据
                BoardNo = border?.SlotNo.ToString() ?? string.Empty;
                //切换卡板选择状态
                SelectFlag(border);
                //开启卡板显示
                ChangBorderShow("open");

                //重置sip状态查询的线程标记
                InitQueryTag();
                //初始化回显
                InitFirst();
                //初始状态为false，后续重复进入跳过
                Log($"切换板卡：{BoardNo}");
                if (_isSipQueryStatus)
                {
                    _isSipQueryStatus = false;
                    //当sip查询结束，才进行新的查询
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            if (!_isSipUserCallStatus && !_isSipUserRegStatus)
                            {
                                Log($"查询板卡：{BoardNo}");
                                //重启新的板卡sip查询
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    RefreshIpcAsync();
                                });
                                _isSipQueryStatus = true;
                                break;
                            }
                        }
                    });
                }
            }
        }

        //由父 ViewModel调用方法
        public void SetIp(string ip) => IP = ip ?? string.Empty;
        public void InitFirst() => _isFirstQuery = true;

        //重查板卡与 SIP 用户
        public async Task RefreshAllAsync(CancellationToken token = default)
        {
            RefreshBoardAsync();
            //await IpcRefresh;
            //RefreshIpcAsync();
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
                bool firstFlag = true;
                while (!_cts.Token.IsCancellationRequested && _isBorder)
                {
                    try
                    {
                        //查询开始前，把读取内容清空
                        string result = TcpConnect.Receive(key);

                        //开始查询
                        TelnetEvent.QueryBoard(key, 0);
                        await Task.Delay(1000, _cts.Token);

                        //处理查询结果
                        result = TcpConnect.Receive(key);
                        TelnetEvent.BorderString(result, BorderList);

                        //if (firstFlag)
                        //{
                        //    firstFlag = false;
                        //    continue;
                        //}
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
                        if (!_isSipUserCall && !_isSipUserReg)
                        {
                            QuerStatus(4, "查询错误");
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

            //当线程标记和占用状态，都false，才可以开新线程
            if (!_isSipUserReg && !_isSipUserRegStatus)
            {
                //起线程，后台循环查询sip用户注册状态
                _isSipUserReg = true;
                _isSipUserRegStatus = true;
                Task.Run(async () =>
                {
                    string name = DefaulConfig.QuerySIPUserRegStateUsername;
                    string password = DefaulConfig.QuerySIPUserRegStatePassword;
                    string key = ip + name;
                    StringBuilder ReandCache = new StringBuilder();

                    if (TcpConnect.AddTelnet(ip + name, ip, name, password) == null)
                    {
                        Log($"SIP 注册状态查询连接失败：{ip}");
                        _isSipUserReg = false;
                        _isSipUserCallStatus = false;
                        return;
                    }

                    while (!_cts.Token.IsCancellationRequested && _isSipUserReg)
                    {
                        //计时
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            //循环查取前，清空读取内容
                            string result = TcpConnect.Receive(key);
                            //查询sip用户注册数据
                            TelnetEvent.QuerySipUser(key, 0, slotNo);

                            int emptyCount = 0;
                            while (!token.IsCancellationRequested && sw.ElapsedMilliseconds < TotalTimeoutMs)
                            {
                                result = TcpConnect.Receive(key);

                                //查询失败，终止查询
                                if (result.Contains("Failure"))
                                {
                                    throw new Exception(result);
                                }

                                // 设备这次没吐数据：连续几次都没有就认为这条命令回显结束
                                if (string.IsNullOrEmpty(result))
                                {
                                    if (++emptyCount >= DefaulConfig.MaxEmptyCount) break;
                                    Thread.Sleep(200);
                                    continue;
                                }

                                emptyCount = 0;
                                TelnetEvent.SipUserRegString(result, IpcUserList);
                                ReandCache.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，内容：\r{result}");
                                if (DefaulConfig.Debug_ShowTelnet)
                                {
                                    Log($"注册状态回显（{sw.ElapsedMilliseconds} ms）：{result}");
                                }

                                // 回显里出现命令提示符（dmkj...#）说明这条命令已经执行完
                                if (ConfigUtil.IsCommandEndFlag(result) && sw.ElapsedMilliseconds > 500) break;

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
                            string msg = ex.Message.Trim().Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace("dmkj#", "");
                            Log($"SIP 呼叫状态查询出错：{msg}");
                            _isSipUserReg = false;
                            break;
                        }
                    }

                    Log($"SIP 注册状态查询结束：{ip}");
                    _isSipUserRegStatus = false;
                });
            }

            //当线程标记和占用状态，都false，才可以开新线程
            if (!_isSipUserCall && !_isSipUserCallStatus)
            {
                //起线程，后台循环查询sip用户呼叫状态
                _isSipUserCall = true;
                _isSipUserCallStatus = true;
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
                        _isSipUserCallStatus = false;
                        return;
                    }

                    while (!_cts.Token.IsCancellationRequested && _isSipUserCall)
                    {
                        try
                        {
                            //计时
                            var sw = Stopwatch.StartNew();
                            //循环查取前，清空读取内容
                            string result = TcpConnect.Receive(key);
                            //查询呼叫状态数据
                            TelnetEvent.QuerySipUserCall(key, 0, slotNo);

                            int emptyCount = 0;
                            while (!token.IsCancellationRequested && sw.ElapsedMilliseconds < TotalTimeoutMs)
                            {
                                result = TcpConnect.Receive(key);

                                //查询失败，终止查询
                                if (result.Contains("Failure"))
                                {
                                    throw new Exception(result);
                                }

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
                                if (ConfigUtil.IsCommandEndFlag(result) && sw.ElapsedMilliseconds > 500) break;

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
                            string msg = ex.Message.Trim().Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace("dmkj#", "");
                            Log($"SIP 呼叫状态查询出错：{msg}");
                            _isSipUserCall = false;
                            break;
                        }
                    }

                    Log($"SIP 呼叫状态查询结束：{ip}");
                    _isSipUserCallStatus = false;
                });
            }

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

        //------------------处理数据方法--------------------

        //选择板卡运行方法
        private void SelectBorderExecute(object parameter)
        {
            if (parameter is BorderModel border && border.IsEnable)
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
                IpcUserList.Add(new IpcUserModel() { Name = Convert.ToString(8002 + i), Status = "0", FSP = $"0/1/{i}" });
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

        public void SelectFlag(BorderModel model)
        {
            foreach (BorderModel item in BorderList)
            {
                item.IsSelect = false;
            }
            if (model == null) return;
            model.IsSelect = true;
        }

        //---------------------编辑热线--------------------------



        public void QueryHotLine(object paramter)
        {
            string phoneNum = (string)paramter;
            string ip = IP;
            IpcUserModel model = getIpcUser(phoneNum);

            Dictionary<string, string> dic = HotLineDB.QueryHotLine(model.FSP, ip);
            model.HotlinePhone = dic.GetValueOrDefault("hotnum", "");
            model.HotlineTime = dic.GetValueOrDefault("hottime", "");

            sipUserView = new EditorSipUserView(model.FSP, phoneNum, model.HotlinePhone, model.HotlineTime, IpcUserList);
            sipUserView.ShowDialog();
        }

        public void QueryHotLineTelnet(object paramter)
        {
            string result = string.Empty;
            string key = DefaulConfig.GetBaseKey();
            string phoneNum = (string)paramter;
            IpcUserModel model = getIpcUser(phoneNum);

            TelnetEvent.QueryHotLine(key, model.FSP, phoneNum);
            Thread.Sleep(300);
            result = TcpConnect.Receive(key, "dmkj(config-esl-user)#", 5000);
            Dictionary<string, string> dic = TelnetEvent.MapContentString(result);
            model.HotlinePhone = dic.GetValueOrDefault("hotlinenum", "");
            model.HotlineTime = dic.GetValueOrDefault("hottime(s)", "");

            //重置权限
            TelnetEvent.ResetPerm(key);

            if (string.IsNullOrEmpty(result)) Log($"查询热线无回显：{phoneNum}");
            if (model == null) Log($"用户列表中未找到：{phoneNum}");



            sipUserView = new EditorSipUserView(BoardNo, phoneNum, model.HotlinePhone, model.HotlineTime, IpcUserList);
            sipUserView.ShowDialog();

        }


    }
}
