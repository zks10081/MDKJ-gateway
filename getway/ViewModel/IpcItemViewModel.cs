using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Windows.Input;

namespace getway.ViewModel
{
    class IpcItemViewModel : ViewModelBase
    {

        private List<IpcUserModel> _IpcUserList = new List<IpcUserModel>();
        public List<IpcUserModel> IpcUserList { get => _IpcUserList; set => SetProperty(ref _IpcUserList, value); }

        private List<BorderModel> _BorderList = new List<BorderModel>();
        public List<BorderModel> BorderList { get => _BorderList; set => SetProperty(ref _BorderList, value); }

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
                //_ipcRefresh = RefreshIpcAsync();
                RefreshIpcAsync();
            }
        }

        private bool _isBorder = false;

        private bool _isSipUserReg = false;

        private bool _isSipUserCall = false;
        private CancellationTokenSource _cts;

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
            string name = DefaulConfig.BaseUsername;
            string password = DefaulConfig.BasePassword;
            string key = ip + name;

            TcpConnect.AddTelnet(ip + name, ip, name, password);

            TelnetEvent.QueryBoard(key, 0);

            string result = TcpConnect.Receive(key);
            TelnetEvent.BorderString(result, BorderList);

            //Task.Run(async () =>
            //{
            //    string name = DefaulConfig.BaseUsername;
            //    string password = DefaulConfig.BasePassword;
            //    string key = ip + name;

            //    TcpConnect.AddTelnet(ip + name, ip, name, password);


            //    while (!_cts.Token.IsCancellationRequested && _isBorder)
            //    {
            //        try
            //        {
            //            TelnetEvent.QueryBoard(key, 0);
            //            await Task.Delay(1000, _cts.Token);

            //            string result = TcpConnect.Receive(key);
            //            TelnetEvent.BorderString(result, BorderList);

            //            await Task.Delay(4000, _cts.Token);
            //        }
            //        catch (OperationCanceledException) { break; }
            //        catch (Exception ex)
            //        {
            //            //Dispatcher.Invoke(() => AppendLog($"轮询出错: {ex.Message}"));
            //            await Task.Delay(5000, _cts.Token);
            //        }
            //    }
            //});

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

            //if (_isSipUserReg) return;
            //_isSipUserReg = true;

            string name = DefaulConfig.QuerySIPUserRegStateUsername;
            string password = DefaulConfig.QuerySIPUserRegStatePassword;
            string key = ip + name;

            TcpConnect.AddTelnet(ip + name, ip, name, password);

            try
            {
                //查询sip用户注册数据
                TelnetEvent.QuerySipUser(key, 0, slotNo);


                //await Task.Delay(5000, _cts.Token);

                //处理查询数据
                string result = String.Empty;
                do
                {
                    result = TcpConnect.Receive(key);
                    TelnetEvent.SipUserString(result, IpcUserList);

                } while (!String.IsNullOrEmpty(result));

            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e);
            }
            catch (Exception ex)
            {
                //Dispatcher.Invoke(() => AppendLog($"轮询出错: {ex.Message}"));
                //await Task.Delay(5000, token);
            }

            //await Task.Run(async () =>
            //{
            //    while (!token.IsCancellationRequested && _isSipUserReg)
            //    {
            //        try
            //        {
            //            //查询sip用户注册数据
            //            TelnetEvent.QuerySipUser(key, 0, slotNo);


            //            await Task.Delay(5000, token);

            //            //处理查询数据
            //            string result = String.Empty;
            //            do
            //            {
            //                result = TcpConnect.Receive(key);
            //                TelnetEvent.BorderString(result, BorderList);

            //            } while (String.IsNullOrEmpty(result));

            //        }
            //        catch (OperationCanceledException) { break; }
            //        catch (Exception ex)
            //        {
            //            //Dispatcher.Invoke(() => AppendLog($"轮询出错: {ex.Message}"));
            //            await Task.Delay(5000, token);
            //        }
            //    }
            //});



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
                IpcUserList.Add(new IpcUserModel() { Name = Convert.ToString(8002 + i), State = "0", FSP = $"0/1/{i}" });
            }
        }
    }
}
