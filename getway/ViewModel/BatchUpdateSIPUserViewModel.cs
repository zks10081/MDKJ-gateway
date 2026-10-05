using DMGatewayDemo.Util;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using RadiantPi.Telnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace getway.ViewModel
{

    internal class BatchUpdateSIPUserViewModel : ViewModelBase
    {

        private string _StartPhonet;
        public string StartPhone { get => _StartPhonet; set => SetProperty(ref _StartPhonet, value); }
        private string _ExcludePhone;
        public string ExcludePhone { get => _ExcludePhone; set => SetProperty(ref _ExcludePhone, value); }

        public bool _IsAddSuccess = true;
        public string Key;
        public string BoardId;

        public BatchUpdateSIPUserViewModel() {

        }

        public BatchUpdateSIPUserViewModel(string board)
        {
            Key = DefaulConfig.GetBaseKey();
            BoardId = board;

        }


        public ICommand BatchUpdate => new ViewModelCommand(async param =>
        {
            //校验输入内容
            if (!ConfigUtil.IsPhoneNum(StartPhone))
            {
                MessageView.ShowWaring("起始号码格式错误！");
            }

            //参数初始化
            int index = int.Parse(StartPhone);
            string key = Key;
            string boardName = BoardId;
            string result = string.Empty;
            List<int> UserList = new List<int>();
            List<string> ExcludeList = ExcludePhone.Split(",").ToList();
            List<string> CommandList = new List<string>();

            //标记
            int count = 0;
            int addCount = 0;
            string starFlagNo = string.Empty;//起始标记
            string starFlagName = string.Empty;//起始标记

            //处理要生成的用户指令
            while (count<64)
            {
                addCount++;
                string name = index.ToString();

                //如果标记为空，则赋值
                if (string.IsNullOrEmpty(starFlagName))
                {
                    starFlagNo = count.ToString();
                    starFlagName = name;
                }

                //在需要剔除的编号和添加结尾，进行添加指令处理
                if (ExcludeList.Contains(name)|| count == 63)
                {
                    continue;
                    string countStr = count.ToString();
                    if (addCount == 1)//如果一条单独添加
                    {
                        string commandContent = $"sippstnuser add 0/{boardName}/{countStr} 0 telno {name}" + Environment.NewLine;
                        CommandList.Add(commandContent);
                    }
                    else if(addCount > 1)//大于则批量添加
                    {
                        string commandContent = $"sippstnuser batadd 0/{boardName}/{starFlagNo} 0/{boardName}/{countStr} 0 telno {starFlagName} step 1" + Environment.NewLine;
                        CommandList.Add(commandContent);
                    }

                    //标记重置
                    starFlagName = string.Empty;
                    addCount = 0;
                }

                //自增
                index++;
                count++;
            }


            //批量删除用户
            await TelnetEvent.SipUserDeleteAll(key, "0", "1");
            result = TcpConnect.Receive(key);

            if (result.Contains("Username or Domain invalid") || result.Contains("System is busy"))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                });
                return;

            }

            if (result.Contains("Command processing is completed") || result.Contains("users has been deleted") || result.Contains("successfully"))
            {
                //删除成功
                Console.WriteLine("删除完成");
            }
            //重置权限
            TelnetEvent.ResetPerm(key);


            //执行生成指令
            await SIPUserAddCommand(key, CommandList);

            //如果成功，关闭窗口
            if (_IsAddSuccess)
            {
                CloseWindows(param);
            }

        });


        public async Task SIPUserAddCommand(string key,List<string> list)
        {
            await Task.Run(() =>
            {

                Telnet2 telnet = TcpConnect.GetTelnet(key);
                string result = string.Empty;

                telnet.Send("enable" + Environment.NewLine);
                result = telnet.Receive();
                telnet.Send("config" + Environment.NewLine);//进入config模式
                result = telnet.Receive();
                telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
                result = telnet.Receive();

                foreach (string item in list)
                {
                    telnet.Send(item);
                    _IsAddSuccess = checkResult(key);
                    if (!_IsAddSuccess) break;
                }

                TelnetEvent.ResetPerm(key);
            });


        }

        public bool checkResult(string key)
        {
            bool ischeck = true;
            string result = string.Empty;
            int end = 0;
            do
            {
                result = TcpConnect.Receive(key);

                if (result.Contains("User data has been added successfully"))
                {
                    ischeck = false;
                    //ConfirmButtonText = $"编辑中({cnt}/64)";
                }


                // Failure: Port 0/2/0 will be assigned telephone number 7088, but the telephone number has been occupied by another port
                if (result.Contains("the telephone number has been occupied by another port"))
                {
                    string x = string.Empty;
                    var match = Regex.Match(result, @"telephone number\s+(\d+)");

                    if (match.Success) x = match.Groups[1].Value; // 7088

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageView.ShowWaring($"警告：{x}已其他端口占用，请修改号码");
                    });
                    return false;
                }
                end++;
                if (end > 5)//超过5次则判断超时
                {
                    MessageView.ShowWaring($"警告：接口超时！请重新添加");
                    return false;
                }
                Thread.Sleep(300);
            }
            while (ischeck);

            return true;

        }


        // 取消按钮
        public ICommand ColseBatchUpdate => new ViewModelCommand(param =>
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
