using CommunityToolkit.Mvvm.Input;
using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace getway.ViewModel
{
    class GetWayViewMode : ViewModelBase
    {

        private readonly Random _random = new Random();
        public List<GetWayIpInfo> GetWayList { get; set; } = new List<GetWayIpInfo>();

        public string nowNetwordIp { get; set; }

        public AsyncRelayCommand AddConnectCommand { get; set; }
        public ICommand QueryBoardCommand { get; set; }
        public ICommand QuerySIPUserCommand { get; set; }

        public ICommand QueryCommand { get; set; }

        private string _ReadContent;
        public string ReadContent { get => _ReadContent; set => SetProperty(ref _ReadContent, value); }

        public GetWayViewMode()
        {
            InitGetWayList();
            nowNetwordIp = "192.168.1.210";
            ReadContent = "";

            AddConnectCommand = new AsyncRelayCommand(AddConnect);
            QueryBoardCommand = new Command(QueryBoard);
            QueryBoardCommand = new Command(QuerySipUser);
            QueryCommand = new Command(QueryAnyCommand);



        }
        public void InitGetWayList()
        {
            for (int i = 0; i < 4; i++)
            {
                String randomIp = $"{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}.{_random.Next(256)}";
                GetWayList.Add(new GetWayIpInfo() { ip = randomIp });
            }
        }

        //测试连接
        public async Task AddConnect()
        {
            string username = "admin";
            string host = nowNetwordIp;
            string key = username + host;
            StringBuilder resultBuild = new StringBuilder();
            string result = "";
            //Dictionary<string, object> poolItem = await TcpConnect.Add(key, host);


            //StreamReader reader = (StreamReader)poolItem["reader"];
            //ReadContent = reader.ReadLine();

            Telnet2 telnet2 = new Telnet2();
            resultBuild.AppendLine("登录连接：");
            result = telnet2.Connect(nowNetwordIp, 23, "root", "mduadmin");
            resultBuild.AppendLine(result);

            if (result.EndsWith("Error"))
            {
                return;
            }

            //resultBuild.AppendLine("查询板卡：");
            //telnet2.Send("enable");
            //telnet2.Send("display board 0" + Environment.NewLine);
            //result = telnet2.Receive();
            //List<BorderModel> BoederList = TelnetEvent.BorderString(result);
            //resultBuild.AppendLine(result + Environment.NewLine);

            resultBuild.AppendLine("查询SIP用户：");
            telnet2.Send("enable");
            telnet2.Send("display sippstnuser reg-state 0/1/0 0/1/63" + Environment.NewLine);
            result = telnet2.Receive();
            List<IpcUserModel> BoederList = TelnetEvent.SipUserString(result);

            resultBuild.AppendLine(result + Environment.NewLine);


            ReadContent = resultBuild.ToString().Trim();
        }

        //查询板卡
        public void QueryBoard(object paramter)
        {
            string username = "admin";
            string host = nowNetwordIp;
            string key = username + host;

            TelnetEvent.QueryBoard(key);


            StreamReader reader = TcpConnect.GetReader(key);
            ReadContent = reader.ReadLine();

        }

        //查询sip用户
        public void QuerySipUser(object paramter)
        {
            string username = "admin";
            string host = nowNetwordIp;
            string key = username + host;

            TelnetEvent.QuerySipUser(key);


            StreamReader reader = TcpConnect.GetReader(key);
            ReadContent = reader.ReadLine();


        }

        //任意命令
        public void QueryAnyCommand(object paramter)
        {
            string username = "admin";
            string host = nowNetwordIp;
            string key = username + host;
            string command = (string)paramter;

            TelnetEvent.command(key, command);


            StreamReader reader = TcpConnect.GetReader(key);
            ReadContent = reader.ReadLine();
        }



        public class GetWayIpInfo
        {
            public String ip { get; set; }
        }
    }
}
