using CommunityToolkit.Mvvm.Input;
using getway.Base;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Text;
using System.Windows.Input;

namespace getway.ViewModel
{
    class GetWayViewMode : ViewModelBase
    {

        public List<GetWayModel> GetWayList { get; set; } = new List<GetWayModel>();

        public string nowNetwordIp { get; set; } = string.Empty;

        public AsyncRelayCommand AddConnectCommand { get; set; }
        public ICommand QueryBoardCommand { get; set; }
        public ICommand QuerySIPUserCommand { get; set; }

        public ICommand QueryCommand { get; set; }

        private string _ReadContent;
        public string ReadContent { get => _ReadContent; set => SetProperty(ref _ReadContent, value); }

        // 子 ViewModel：由父 ViewModel 持有，在连接建立后再注入 key
        public IpcItemViewModel IpcItemVM { get; }

        private string _GetWayIp = string.Empty;
        public string GetWayIp
        {
            get => _GetWayIp; set
            {
                if (_GetWayIp != value) // 值检查
                {
                    _GetWayIp = value;
                    OnPropertyChanged(nameof(GetWayIp));
                    if (!string.IsNullOrEmpty(_GetWayIp))
                    {
                        //调用切换网关
                        SwitchGetWay(_GetWayIp);
                    }
                }
            }
        }

        public GetWayViewMode()
        {
            ReadContent = "";

            // 1. 先创建子 ViewModel（此时还没有数据，不会取数据）
            IpcItemVM = new IpcItemViewModel();

            // 2. 加载网关列表
            InitGetWayList();

            // 3. 建立连接 -> 写入 Now_Telnet_key -> 再让子 ViewModel 取数据
            nowNetwordIp = "192.168.1.210";
            SwitchGetWay(nowNetwordIp);

            AddConnectCommand = new AsyncRelayCommand(AddConnect);
            QueryBoardCommand = new Command(QueryBoard);
            QuerySIPUserCommand = new Command(QuerySipUser);
            QueryCommand = new Command(QueryAnyCommand);



        }

        /// <summary>
        /// 建立/切换网关 telnet 连接，连接成功后刷新子 ViewModel 数据
        /// </summary>
        private void SwitchGetWay(string ip)
        {
            nowNetwordIp = ip;
            string key = ip + "root";

            Telnet2? telnet2 = TcpConnect.AddTelnet(key, ip);
            if (telnet2 == null)
            {
                ReadContent = $"网关 {ip} 连接失败";
                return;
            }

            DefaulConfig.Now_Telnet_key = key;
            IpcItemVM.SetKey(key);
        }

        public void InitGetWayList()
        {
            try
            {
                GetWayList = GetWayDB.QueryGetWayList();
            }
            catch (Exception ex)
            {
                GetWayList = new List<GetWayModel>();
                ReadContent = "网关列表加载失败：" + ex.Message;
            }
        }

        //测试连接
        public async Task AddConnect()
        {
            string host = nowNetwordIp;
            StringBuilder resultBuild = new StringBuilder();
            resultBuild.AppendLine("登录连接：");

            Telnet2? telnet2 = await Task.Run(() => TcpConnect.AddTelnet(host + "root", host));

            if (telnet2 == null)
            {
                resultBuild.AppendLine($"{host} 连接失败");
                ReadContent = resultBuild.ToString().Trim();
                return;
            }

            DefaulConfig.Now_Telnet_key = host + "root";
            resultBuild.AppendLine($"{host} 连接成功");

            // 连接成功后才让子 ViewModel 取板卡与 SIP 用户数据
            await Task.Run(() => IpcItemVM.SetKey(DefaulConfig.Now_Telnet_key));

            ReadContent = resultBuild.ToString().Trim();
        }

        //查询板卡
        public void QueryBoard(object paramter)
        {
            IpcItemVM.initBorderList();
        }

        //查询sip用户
        public void QuerySipUser(object paramter)
        {
            IpcItemVM.initIpcList();
        }

        //任意命令
        public void QueryAnyCommand(object paramter)
        {
            string command = paramter as string ?? string.Empty;
            if (string.IsNullOrEmpty(DefaulConfig.Now_Telnet_key))
            {
                ReadContent = "网关未连接";
                return;
            }

            string result = TelnetEvent.AnyCommand(DefaulConfig.Now_Telnet_key, command);
            ReadContent = string.IsNullOrEmpty(result) ? "无回显" : result;
        }



        public class GetWayIpInfo
        {
            public String ip { get; set; }
        }
    }
}
