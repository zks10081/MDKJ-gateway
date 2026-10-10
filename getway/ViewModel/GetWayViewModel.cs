using CommunityToolkit.Mvvm.Input;
using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using getway.View;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace getway.ViewModel
{
    class GetWayViewMode : ViewModelBase, ILogSink
    {

        // 网关不可达时的兜底等待上限，避免一次切换把这一轮流程挂住
        private const int ConnectTimeoutMs = 8000;
        private const int QueryTimeoutMs = 15000;

        private ObservableCollection<GetWayModel> _GetWayList = new ObservableCollection<GetWayModel>();
        public ObservableCollection<GetWayModel> GetWayList { get => _GetWayList; set => SetProperty(ref _GetWayList, value); }

        public AsyncRelayCommand AddConnectCommand { get; set; }
        public ICommand QueryBoardCommand { get; set; }
        public ICommand QuerySIPUserCommand { get; set; }
        public ICommand QueryCommand { get; set; }
        public ICommand ResetPermCommand { get; set; }

        public ICommand AddGetwayCommand { get; set; }//添加网关
        public ICommand DeleteGetwayCommand { get; set; }//删除网关
        public ICommand bohaoCommand { get; set; }//拨号
        public ICommand BatchUpdateSipUserCommad { get; set; }
        public ICommand DigitMapCommand { get; set; }
        public ICommand SaveConfigCommand { get; set; }



        private string _AnyCommandString;
        private string _QueryStatusIcon;
        private string _QueryStatusColor;
        private string _QueryStatusText;
        //操控按钮显示
        private string _DebugShow;
        private string _GetwayShow;
        private string _BorderShow;

        public string AnyCommandString { get => _AnyCommandString; set => SetProperty(ref _AnyCommandString, value); }
        public string QueryStatusIcon { get => _QueryStatusIcon; set => SetProperty(ref _QueryStatusIcon, value); }
        public string QueryStatusColor { get => _QueryStatusColor; set => SetProperty(ref _QueryStatusColor, value); }
        public string QueryStatusText { get => _QueryStatusText; set => SetProperty(ref _QueryStatusText, value); }
        public string DebugShow { get => _DebugShow; set => SetProperty(ref _DebugShow, value); }
        public string GetwayShow { get => _GetwayShow; set => SetProperty(ref _GetwayShow, value); }
        public string BorderShow { get => _BorderShow; set => SetProperty(ref _BorderShow, value); }



        // 日志缓冲：界面绑定的是 ReadContent 字符串，追加内容后必须主动通知刷新
        // 子 ViewModel 会从后台线程写日志，所以读写都要加锁
        private readonly StringBuilder _readContent = new StringBuilder();
        private readonly object _logLock = new object();

        // 日志上限：超过后丢掉最前面的一半，避免长时间轮询把内存撑爆
        private const int MaxLogLength = 20000;

        public string ReadContent
        {
            get { lock (_logLock) { return _readContent.ToString(); } }
        }

        // ILogSink：子 ViewModel（IpcItemViewModel）也走这里写日志
        public void AppendLog(string text)
        {
            lock (_logLock)
            {
                _readContent.AppendLine(text);
                if (_readContent.Length > MaxLogLength)
                {
                    _readContent.Remove(0, _readContent.Length - MaxLogLength / 2);
                }
            }

            // 后台线程触发的 PropertyChanged，WPF 绑定会自动封送到 UI 线程
            OnPropertyChanged(nameof(ReadContent));
        }


        // 子 ViewModel：由父 ViewModel 持有，在连接建立后再注入 key
        public IpcItemViewModel IpcItemVM { get; }

        private readonly Random _random = new Random();

        private string _GetWayIp = string.Empty;
        public string GetWayIp
        {
            get => _GetWayIp;
            set
            {
                if (SetProperty(ref _GetWayIp, value) && !string.IsNullOrEmpty(value))
                {
                    DefaulConfig.GetwayIp = value;
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
            // 把 this 作为日志出口注入，子页面的日志就写进同一个 ReadContent
            IpcItemVM = new IpcItemViewModel(this);

            AddConnectCommand = new AsyncRelayCommand(AddConnect);
            QueryBoardCommand = new AsyncRelayCommand(QueryBoard);
            QuerySIPUserCommand = new AsyncRelayCommand(QuerySipUser);
            QueryCommand = new AsyncRelayCommand(QueryAnyCommand);
            ResetPermCommand = new AsyncRelayCommand(ResetPerm);

            AddGetwayCommand = new Command(AddGetway);
            DeleteGetwayCommand = new Command(DeleteGetway);
            BatchUpdateSipUserCommad = new ViewModelCommand(BatchUpdateSipUser);
            DigitMapCommand = new ViewModelCommand(DigitMap);
            SaveConfigCommand = new ViewModelCommand(SaveConfig); ;

            _ = InitAsync();
            QueryStatusFun(0, "");//初始化查询状态显示
            InitShow();
        }

        /// <summary>
        /// 查询状态显示
        /// 0：网关未连接，1：板卡未激活，2：加载中，3：加载成功，4：连接超时
        /// </summary>
        /// <param name="status"></param>
        /// <param name="tip"></param>
        public void QueryStatusFun(int status, string tip)
        {
            if (status == 0 || status == 1)
            {
                QueryStatusIcon = "\ue8f2";
                QueryStatusColor = "#ffa500";
            }
            else if (status == 2)
            {
                QueryStatusIcon = "\ue632";
                QueryStatusColor = "#cbcccc";

            }
            else if (status == 3)
            {
                QueryStatusIcon = "\ue615";
                QueryStatusColor = "#008000";
            }
            else if (status == 4)
            {
                QueryStatusIcon = "\ue8f2";
                QueryStatusColor = "#ff0000";
            }
            QueryStatusText = string.IsNullOrEmpty(tip) ? "网关未连接" : tip;
        }

        /// <summary>
        /// 加载网关列表
        /// </summary>
        /// <returns></returns>
        private async Task InitAsync()
        {
            try
            {
                GetWayList = new ObservableCollection<GetWayModel>(await Task.Run(GetWayDB.QueryGetWayList));
            }
            catch (Exception ex)
            {
                GetWayList = new ObservableCollection<GetWayModel>();
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

            string key = DefaulConfig.GetBaseKey();
            AppendLog($"正在连接网关 {ip} ...");

            try
            {
                //清空连接池
                TcpConnect.CloseAll();
                //终止所有查询
                IpcItemVM.InitQueryTag();
                //板卡和sip都恢复默认状态
                IpcItemVM.SelectBorder = null;
                IpcItemVM.SelectFlag(null);

                //调整显示
                InitShow();
                GetwayShow = "Visible";

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
                IpcItemVM.InitFirst();//初始化状态回显

                var refreshTask = IpcItemVM.RefreshAllAsync(cts.Token);
                if (await Task.WhenAny(refreshTask, Task.Delay(QueryTimeoutMs)) == refreshTask)
                {
                    await refreshTask;
                }
                if (cts.IsCancellationRequested) return;

                AppendLog($"网关 {ip} 已连接：板卡 {IpcItemVM.BorderList.Count} 个，SIP 用户 {IpcItemVM.IpcUserList.Count} 个");
                QueryStatusFun(1, "板卡未激活");
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
            //IpcItemVM.RefreshIpcAsync();
            // 该方法内部有 Thread.Sleep 等待回显，放到线程池跑，不要把 UI 线程卡住
            await Task.Run(() => IpcItemVM.selectSipUserCallStatus());
        }

        //任意命令
        public async Task QueryAnyCommand()
        {
            //if (!IsConnected()) return;
            string command = _AnyCommandString;

            string key = _currentKey;
            string result = await Task.Run(() => TelnetEvent.AnyCommand(key, command ?? string.Empty));


            AppendLog(string.IsNullOrEmpty(result) ? "无回显" : result);
        }

        //重置权限
        public async Task ResetPerm()
        {
            string key = _currentKey;
            string command = "quit";
            int out_flag = 0;
            do
            {
                string result = await Task.Run(() => TelnetEvent.AnyCommand(key, command ?? string.Empty));
                AppendLog(string.IsNullOrEmpty(result) ? "无回显" : result);
                if (result.EndsWith("dmkj#"))
                {
                    break;
                }
                if (result.Contains("(y/n)[n]"))//如果要退出连接，则终止
                {
                    TelnetEvent.AnyCommand(key, "n" ?? string.Empty);
                }
                if (out_flag > 4)//超过4次，就停止循环
                {
                    break;
                }
            }
            while (true);
        }
        public void InitGetWayList()
        {
            for (int i = 0; i < 4; i++)
            {
                String randomIp = $"{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}";

            }
        }


        //---------------------网关操控--------------------------


        public void AddGetway(object paramter)
        {
            AddGetwayView addGetway = new AddGetwayView(GetWayList);
            addGetway.Show();

        }

        public void DeleteGetway(object paramter)
        {
            if (string.IsNullOrEmpty(GetWayIp))
            {
                AppendLog("未选择网关");
                return;
            }
            bool result = MessageView.ShowSelect("删除网关", "是否确认删除？");
            if (result)
            {
                GetWayDB.DeleteOne(GetWayIp);

                for (int i = 0; i < GetWayList.Count; i++)
                {
                    if (GetWayList[i].Equals(GetWayIp))
                    {
                        GetWayList.RemoveAt(i);



                        //删除对应ip的连接
                        TcpConnect.CloseTelnetByIp(GetWayIp);
                    }
                }

                //MessageView.ShowSuccess($"网关 {GetWayIp} 已删除!");
            }

        }


        //---------------------批量修改sip用户--------------------------

        public void BatchUpdateSipUser(object param)
        {
            string BoardNo = IpcItemVM.BoardNo;

            BatchUpdateSIPUserView batchUpdateView = new BatchUpdateSIPUserView(this, BoardNo);
            batchUpdateView.ShowDialog();
        }



        //---------------------配置拨号计划--------------------------
        public void DigitMap(object param)
        {
            string ip = GetWayIp;
            string digitMapContent = DigitMapDB.getDigitMapByIp(ip);

            DigitMapView digitalMapView = new DigitMapView(digitMapContent, this);
            digitalMapView.Show();

        }
        public void DigitMapTelnet(object param)
        {

            string result = string.Empty;
            string key = DefaulConfig.GetBaseKey();


            TelnetEvent.DigitMapQueryAll(key);
            result = TcpConnect.Receive(key);
            Dictionary<string, string> dic = TelnetEvent.MapContentString(result);

            string digitMapContent = dic["Body"];

            DigitMapView digitalMapView = new DigitMapView(digitMapContent, this);
            digitalMapView.Show();
        }


        //---------------------保存配置--------------------------

        public void SaveConfig(object param)
        {
            SaveConfigView saveConfigView = new SaveConfigView();
            saveConfigView.ShowDialog();
        }

        //------------------------方法---------------------------------

        public void InitShow()
        {
            DebugShow = "Collapsed";
            GetwayShow = "Collapsed";
            BorderShow = "Collapsed";
        }

        /// <summary>
        /// 改变板卡选中显示
        /// </summary>
        /// <param name="action">open 开启显示，close 关闭显示</param>
        public void ChangBorderShow(string action)
        {
            if ("open".Equals(action))
            {
                BorderShow = "Visible";
            }
            else if ("open".Equals(action))
            {
                BorderShow = "Collapsed";
            }
        }

        public void QuerySipUserDataSwitch(string str)
        {
            if ("open".Equals(str))
            {
                //初始化回显
                IpcItemVM.InitFirst();
                //重启sip查询
                IpcItemVM.RefreshIpcAsync();
            }
            else if ("open".Equals(str))
            {
                //终止所有查询
                IpcItemVM.InitQueryTag();
            }
        }

    }
}
