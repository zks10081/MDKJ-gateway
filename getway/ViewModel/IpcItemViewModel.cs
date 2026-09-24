using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Collections.ObjectModel;
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

                RefreshIpcAsync();
            }
        }

        private bool _isBorder = false;

        private bool _isSipUserReg = false;

        private bool _isSipUserCall = false;
        private CancellationTokenSource _cts;
        private readonly Random _random = new Random();

        // 最近一次用户列表刷新任务，供父 ViewModel 等待
        private Task _ipcRefresh = Task.CompletedTask;
        public Task IpcRefresh => _ipcRefresh;

        // telnet 连接标识，由父 ViewModel（GetWayViewMode）在连接建立后注入
        public string IP { get; private set; } = string.Empty;

        public ICommand SelectBorderCommand { get; }

        public IpcItemViewModel()
        {
            // 构造时只做最小化初始化，不取数据：此时父 ViewModel 还没建立连接
            SelectBorderCommand = new Command(SelectBorderExecute);
            _cts = new CancellationTokenSource();
            //生成临时数据
            TempBorderInfo();
            TempSipUserInfo();
        }

        /// <summary>
        /// 由父 ViewModel 在 telnet 连接成功后注入，取数据前必须已有 key
        /// </summary>
        public void SetIp(string ip) => IP = ip ?? string.Empty;

        //重查板卡与 SIP 用户
        public async Task RefreshAllAsync(CancellationToken token = default)
        {
            RefreshBoardAsync();
            //await IpcRefresh;
            RefreshIpcAsync();
        }

        //所有线程终止
        public void InitQueryTag()
        {
            _isBorder = false;
            _isSipUserReg = false;
            _isSipUserCall = false;

        }

        //获取卡框板槽信息
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

                TcpConnect.AddTelnet(ip + name, ip, name, password);


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
                        //Dispatcher.Invoke(() => AppendLog($"轮询出错: {ex.Message}"));
                        await Task.Delay(5000, _cts.Token);
                    }
                }
            });

            // 切换网关期间旧结果不能覆盖新网关数据
            if (_cts.Token.IsCancellationRequested) return;



        }

        //获取sip用户注册数据
        public void RefreshIpcAsync()
        {

            if (string.IsNullOrEmpty(IP) || _SelectBorder == null)
            {
                TempSipUserInfo();
                return;
            }

            string ip = IP;
            int slotNo = _SelectBorder.SlotNo;

            if (_isSipUserReg) return;
            _isSipUserReg = true;

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested && _isSipUserReg)
                {

                    string name = DefaulConfig.QuerySIPUserRegStateUsername;
                    string password = DefaulConfig.QuerySIPUserRegStatePassword;
                    string key = ip + name;

                    TcpConnect.AddTelnet(ip + name, ip, name, password);

                    try
                    {
                        //查询sip用户注册数据
                        TelnetEvent.QuerySipUser(key, 0, slotNo);
                        await Task.Delay(300, _cts.Token);

                        //处理查询数据
                        string result;
                        int loopCount = 0;
                        do
                        {
                            result = TcpConnect.Receive(key);
                            // 没有更多数据就直接结束，不要空转解析
                            if (string.IsNullOrEmpty(result)) break;

                            TelnetEvent.SipUserString(result, IpcUserList);
                            await Task.Delay(300, _cts.Token);

                        } while (++loopCount < 20);   // 兜底上限，设备持续吐数据时不会死循环


                        await Task.Delay(2000, _cts.Token);

                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        //Dispatcher.Invoke(() => AppendLog($"轮询出错: {ex.Message}"));
                        await Task.Delay(5000, _cts.Token);
                    }
                }
            });



            if (_cts.Token.IsCancellationRequested) return;
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
    }
}
