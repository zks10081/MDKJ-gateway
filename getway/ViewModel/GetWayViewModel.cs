using CommunityToolkit.Mvvm.Input;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;

namespace getway.ViewModel
{
    class GetWayViewMode : ViewModelBase
    {

        // 网关不可达时的兜底等待上限，避免一次切换把这一轮流程挂住
        private const int ConnectTimeoutMs = 8000;
        private const int QueryTimeoutMs = 15000;

        private List<GetWayModel> _GetWayList = new List<GetWayModel>();
        public List<GetWayModel> GetWayList { get => _GetWayList; set => SetProperty(ref _GetWayList, value); }

        public AsyncRelayCommand AddConnectCommand { get; set; }
        public ICommand QueryBoardCommand { get; set; }
        public ICommand QuerySIPUserCommand { get; set; }

        public ICommand QueryCommand { get; set; }

        // 日志缓冲：界面绑定的是 ReadContent 字符串，追加内容后必须主动通知刷新
        private readonly StringBuilder _readContent = new StringBuilder();
        public string ReadContent => _readContent.ToString();

        private void AppendLog(string text)
        {
            _readContent.AppendLine(text);
            OnPropertyChanged(nameof(ReadContent));
        }

        private string _AnyCommandString;
        public string AnyCommandString { get => _AnyCommandString; set => SetProperty(ref _AnyCommandString, value); }

        // 子 ViewModel：由父 ViewModel 持有，在连接建立后再注入 key
        public IpcItemViewModel IpcItemVM { get; }

        private string _GetWayIp = string.Empty;
        public string GetWayIp
        {
            get => _GetWayIp;
            set
            {
                if (SetProperty(ref _GetWayIp, value) && !string.IsNullOrEmpty(value))
                {
                    _ = SwitchGetWayAsync(value);
                }
            }
        }

        // 当前已连上的 telnet 标识，空表示未连接
        private string _currentKey = string.Empty;

        // 每次切换换一个 token，被覆盖的旧切换不再写回界面
        private CancellationTokenSource? _switchCts;

        public GetWayViewMode()
        {
            // 子 ViewModel 先创建，此时没有 key 也不会取数据
            IpcItemVM = new IpcItemViewModel();

            AddConnectCommand = new AsyncRelayCommand(AddConnect);
            QueryBoardCommand = new AsyncRelayCommand(QueryBoard);
            QuerySIPUserCommand = new AsyncRelayCommand(QuerySipUser);
            QueryCommand = new AsyncRelayCommand(QueryAnyCommand);

            _ = InitAsync();
        }

        //加载网关列表，并默认连上第一个网关
        private async Task InitAsync()
        {
            try
            {
                GetWayList = await Task.Run(GetWayDB.QueryGetWayList);
            }
            catch (Exception ex)
            {
                GetWayList = new List<GetWayModel>();
                AppendLog("网关列表加载失败：" + ex.Message);
                return;
            }

            var first = GetWayList.FirstOrDefault();
            if (first == null)
            {
                AppendLog("未查询到网关");
                return;
            }

            // 赋值即触发切换，保证列表高亮与已连网关始终一致
            //GetWayIp = first.IP;
        }

        /// <summary>
        /// 建立/切换网关 telnet 连接，成功后重查板卡与 SIP 用户
        /// </summary>
        private async Task SwitchGetWayAsync(string ip)
        {
            _switchCts?.Cancel();
            _switchCts?.Dispose();
            var cts = new CancellationTokenSource();
            _switchCts = cts;

            string key = ip + "root";
            AppendLog($"正在连接网关 {ip} ...");

            try
            {
                var connectTask = Task.Run(() => TcpConnect.AddTelnet(key, ip, "root", "mduadmin"));
                if (await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs)) != connectTask)
                {
                    if (!cts.IsCancellationRequested) AppendLog($"网关 {ip} 连接超时");
                    return;
                }

                Telnet2? telnet2 = await connectTask;
                if (cts.IsCancellationRequested) return;

                if (telnet2 == null)
                {
                    AppendLog($"网关 {ip} 连接失败");
                    return;
                }

                _currentKey = key;
                DefaulConfig.Now_Telnet_key = key;
                IpcItemVM.SetIp(ip);

                var refreshTask = IpcItemVM.RefreshAllAsync(cts.Token);
                if (await Task.WhenAny(refreshTask, Task.Delay(QueryTimeoutMs)) == refreshTask)
                {
                    await refreshTask;
                }
                if (cts.IsCancellationRequested) return;

                AppendLog($"网关 {ip} 已连接：板卡 {IpcItemVM.BorderList.Count} 个，SIP 用户 {IpcItemVM.IpcUserList.Count} 个");
            }
            catch (Exception ex)
            {
                AppendLog($"网关 {ip} 切换失败：{ex.Message}");
            }
        }

        //测试连接：重连当前网关并刷新数据
        public async Task AddConnect()
        {
            if (string.IsNullOrEmpty(GetWayIp))
            {
                AppendLog("未选择网关");
                return;
            }

            await SwitchGetWayAsync(GetWayIp);
        }

        //查询板卡
        public async Task QueryBoard()
        {
            //if (!IsConnected()) return;
            IpcItemVM.RefreshBoardAsync();
        }

        //查询sip用户
        public async Task QuerySipUser()
        {
            //if (!IsConnected()) return;
            IpcItemVM.RefreshIpcAsync();
        }

        //任意命令
        public async Task QueryAnyCommand()
        {
            //if (!IsConnected()) return;
            string command = _AnyCommandString;

            string key = _currentKey;
            string result = await Task.Run(() => TelnetEvent.AnyCommand(key, command ?? string.Empty));

            TelnetRadiantPi radiantPi = new TelnetRadiantPi();
            radiantPi.RadianPiConnect(command);

            AppendLog(string.IsNullOrEmpty(result) ? "无回显" : result);
        }

        private bool IsConnected()
        {
            if (TcpConnect.GetTelnet(_currentKey)?.Connected != true)
            {
                AppendLog("网关未连接");
                return false;
            }
            return true;
        }


        //---------------------radaintpi测试--------------------------
        private TelnetMonitorService _monitor;
        private readonly DispatcherTimer _uiTimer;
        private CancellationTokenSource _cts;
        private Task _connectTask;
        private readonly List<string> _logBuffer = new();



    }
}
