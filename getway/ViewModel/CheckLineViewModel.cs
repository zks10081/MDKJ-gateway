using DMGatewayDemo.Util;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    class CheckLineViewModel : ViewModelBase
    {

        private string _FSP;
        private string _PhoneNum;
        private string _ActionContent;
        private bool _IsEdit = true;
        public string FSP { get => _FSP; set => SetProperty(ref _FSP, value); }
        public string PhoneNum { get => _PhoneNum; set => SetProperty(ref _PhoneNum, value); }
        public string ActionContent { get => _ActionContent; set => SetProperty(ref _ActionContent, value); }
        public bool IsEdit { get => _IsEdit; set => SetProperty(ref _IsEdit, value); }

        public string key;


        public CheckLineViewModel(string fsp, string phone_num)
        {
            key = DefaulConfig.GetBaseKey();
            _FSP = fsp;
            _PhoneNum = phone_num;
        }

        public ICommand CheckLineCommand => new ViewModelCommand(async param =>
        {

            string result = string.Empty;
            bool isSuccess = false;
            StringBuilder ReadCache = new StringBuilder();

            IsEdit = false;
            ActionContent = "检测中.....";
            TcpConnect.UpdateTake(key, 1);
            TelnetEvent.SipUerCheckLine(key, FSP);
            await Task.Run(() =>
            {
                //计时
                var sw = Stopwatch.StartNew();
                bool isWait = false;
                while (true || sw.ElapsedMilliseconds < 30000)
                {
                    result = TcpConnect.Receive(key);

                    ReadCache.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，内容：\r{result}");
                    if (DefaulConfig.IsDebug)
                    {
                        Debug.WriteLine($"耗时：{sw.ElapsedMilliseconds}ms，{PhoneNum}线路检测：{result}");
                    }

                    if (result.Contains("Line state")) isWait = true;

                    if (isWait)
                    {
                        isSuccess = result.Contains("dmkj(config-test)#");
                    }


                    if (isSuccess) break;

                    Thread.Sleep(500);
                }

            });

            result = ReadCache.ToString();
            int status = 0;
            if (result.Contains("Busy") || result.Contains("Board is testing"))
            {
                MessageView.Show("警告", $"系统正忙", Message_Type.Message_Waring);
            }
            else if (result.Contains("AB short"))
            {
                MessageView.Show("警告", $"{PhoneNum}电话线路出现短接", Message_Type.Message_Waring);
            }
            else if (result.Contains("AB all break off or no phone connceted"))
            {
                MessageView.Show("警告", $"{PhoneNum}电话线路没有接电话", Message_Type.Message_Waring);
            }
            else if (result.Contains("R-C network") && result.Contains("Normal"))
            {
                status = 1;
            }
            else
            {
                MessageView.Show("警告", $"{PhoneNum}电话线路出现未知异常", Message_Type.Message_Waring);
            }


            ActionContent = "";
            TelnetEvent.ResetPerm(key);
            TcpConnect.UpdateTake(key, 0);
            IsEdit = true;

            if (status == 1)
            {
                MessageView.Show("成功", $"{PhoneNum}电话线路正常", Message_Type.Message_Success);
                CloseWindows(param);
            }

        });





        // 取消按钮
        public ICommand Cancel => new ViewModelCommand(param =>
        {
            CloseWindows(param);
        });

        public void CloseWindows(object paramter)
        {
            Window window = (Window)paramter;
            window.Close();
        }
    }
}
