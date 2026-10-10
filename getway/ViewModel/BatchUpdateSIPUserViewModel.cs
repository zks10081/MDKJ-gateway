using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{

    internal class BatchUpdateSIPUserViewModel : ViewModelBase
    {

        private string _StartPhonet;
        private string _ExcludePhone;
        private string _ActionContent;
        public string StartPhone { get => _StartPhonet; set => SetProperty(ref _StartPhonet, value); }
        public string ExcludePhone { get => _ExcludePhone; set => SetProperty(ref _ExcludePhone, value); }
        public string ActionContent { get => _ActionContent; set => SetProperty(ref _ActionContent, value); }

        public ILogSink _logSink;
        private void Log(string text) => _logSink?.AppendLog(text);
        private void QuerySipUserDataSwitch(string str) => _logSink?.QuerySipUserDataSwitch(str);

        public int _IsAddSuccess = 0;
        public string Key;
        public string BoardId;
        public int addIndex = 0;

        public BatchUpdateSIPUserViewModel()
        {

        }

        public BatchUpdateSIPUserViewModel(ILogSink logSink, string board)
        {
            _logSink = logSink;
            Key = DefaulConfig.GetBaseKey();
            BoardId = board;

        }


        public ICommand BatchUpdate => new ViewModelCommand(async param =>
        {
            //校验输入内容
            if (!ConfigUtil.IsPhoneNum(StartPhone))
            {
                MessageView.ShowWaring("起始号码格式错误！");
                return;
            }
            if (string.IsNullOrEmpty(BoardId))
            {

                MessageView.ShowWaring("未选择板卡！");
                return;
            }

            //参数初始化
            int index = int.Parse(StartPhone);
            string key = Key;
            string boardName = BoardId;
            string result = string.Empty;
            List<int> UserList = new List<int>();
            ExcludePhone = ExcludePhone.Replace("，", ",");
            List<string> ExcludeList = ExcludePhone.Split(",").ToList();
            List<string> CommandList = new List<string>();
            List<int> countList = new List<int>();

            StringBuilder ReadCache = new StringBuilder();

            //标记
            int count = 0;
            int addCount = 0;
            string starFlagNo = count.ToString();//起始标记
            string starFlagName = index.ToString();//起始标记
            ActionContent = $"正在处理数据。。。";

            int last_index = 0;
            int last_count = 0;

            //处理要生成的用户指令
            ReadCache.AppendLine("----------------处理要生成的用户指令------------------");
            while (count < 64)
            {
                string name = index.ToString();

                //如果标记为空，则赋值
                if (string.IsNullOrEmpty(starFlagName))
                {
                    starFlagNo = count.ToString();
                    starFlagName = name;
                }

                //在需要剔除的编号和添加结尾，进行添加指令处理，
                //addcount：
                if (ExcludeList.Contains(name) || count == 63)
                {
                    if (count == 63) last_count = count;

                    if (addCount == 1)//如果一条单独添加
                    {
                        string commandContent = $"sippstnuser add 0/{boardName}/{starFlagNo} 0 telno {starFlagName}";
                        CommandList.Add(commandContent);
                        countList.Add(count);
                        ReadCache.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，添加指令：\r{commandContent}");
                    }
                    else if (addCount > 1)//大于则批量添加
                    {
                        string commandContent = $"sippstnuser batadd 0/{boardName}/{starFlagNo} 0/{boardName}/{last_count} 0 telno {starFlagName} step 1";
                        CommandList.Add(commandContent);
                        countList.Add(count);
                        ReadCache.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，添加指令：\r{commandContent}");
                    }

                    if (count == 63) break;

                    //标记重置
                    starFlagName = string.Empty;
                    addCount = 0;
                }
                else
                {
                    addCount++;
                    last_count = count;
                    count++;
                }


                //自增
                last_index = index;
                index++;
            }


            ActionContent = $"正在初始化用户数据。。。";
            QuerySipUserDataSwitch("close");
            ReadCache.AppendLine("----------------批量删除用户------------------");
            //批量删除用户
            bool deletflag = await SipUserDelete(key, ReadCache);
            if (!deletflag)
            {
                MessageView.ShowWaring("初始化用户数据超时，请重试！");
                return;
            }



            ActionContent = $"正在添加用户。。。";
            ReadCache.AppendLine("----------------执行生成指令------------------");
            //执行生成指令
            SIPUserAddCommand(key, CommandList, countList, ReadCache);

            //获取状态显示
            await Task.Run(() =>
            {
                while (true)
                {
                    if (_IsAddSuccess != 0) break;//

                    ActionContent = $"正在添加用户({addIndex}/64)";
                    Thread.Sleep(300);
                    addIndex++;
                    if (addIndex > 63) addIndex = 63;
                }
                if (_IsAddSuccess == 1)
                {
                    ActionContent = $"正在添加用户(64/64)";

                }
                else
                {
                    ActionContent = $"添加识别，请重试！";
                }
            });

            //如果成功，关闭窗口
            if (_IsAddSuccess == 1)
            {
                QuerySipUserDataSwitch("open");
                CloseWindows(param);
            }
            string log = ReadCache.ToString();
            Debug.WriteLine($"日志打印：{log}");

        });

        public async Task<bool> SipUserDelete(string key, StringBuilder read, int TotalTimeoutMs = 40000)
        {
            bool isuccess = false;
            await Task.Run(async () =>
            {
                try
                {
                    //计时
                    var sw = Stopwatch.StartNew();
                    //执行命令前，清空读取内容
                    string result = TcpConnect.Receive(key);
                    //删除sip用户
                    TelnetEvent.SipUserDeleteAll(key, "0", "1");

                    int emptyCount = 0;
                    while (true && sw.ElapsedMilliseconds < TotalTimeoutMs)
                    {
                        result = TcpConnect.Receive(key);
                        read.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，内容：\r{result}");
                        Debug.WriteLine($"耗时：{sw.ElapsedMilliseconds}ms，删除sip结果：{result}");

                        if (result.Contains("Username or Domain invalid") || result.Contains("System is busy"))
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageView.ShowWaring("有未完成的操作，请稍后再继续,删除方法");
                            });
                            return;

                        }
                        else if (result.Contains("Command processing is completed") || result.Contains("users has been deleted") || result.Contains("successfully"))
                        {
                            //删除成功
                            Console.WriteLine("删除完成");
                            Log($"删除完成！,耗时{sw.ElapsedMilliseconds}ms");
                            isuccess = true;
                            break;
                        }
                        Thread.Sleep(300);
                    }


                    //重置权限
                    TelnetEvent.ResetPerm(key);

                }
                catch (Exception ex)
                {
                    Log($"SIP 用户删除异常：{ex.Message}");
                }

            });
            return isuccess;
        }


        public void SIPUserAddCommand(string key, List<string> list, List<int> countList, StringBuilder read, int TotalTimeoutMs = 10000)
        {
            Task.Run(() =>
            {

                Telnet2 telnet = TcpConnect.GetTelnet(key);
                string result = string.Empty;

                telnet.Send("enable" + Environment.NewLine);
                result = telnet.Receive();
                telnet.Send("config" + Environment.NewLine);//进入config模式
                result = telnet.Receive();
                telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
                result = telnet.Receive();


                int index = 0;
                int addsum = 0;
                bool issunccess = true;
                foreach (string item in list)
                {
                    try
                    {
                        //计时
                        var sw = Stopwatch.StartNew();
                        //执行命令前，清空读取内容
                        telnet.ClearReceiveBuffer();
                        //用户添加
                        telnet.Send(item);

                        //计算本次查询超时时间
                        int takeTime = countList[index] - addsum;
                        if (takeTime > 30)
                        {
                            TotalTimeoutMs = 60000;
                        }
                        else if (takeTime > 20)
                        {
                            TotalTimeoutMs = 40000;
                        }
                        else if (takeTime > 10)
                        {
                            TotalTimeoutMs = 20000;
                        }
                        else
                        {
                            TotalTimeoutMs = 5000;
                        }


                        int count = 0;
                        while (true && sw.ElapsedMilliseconds < TotalTimeoutMs)
                        {

                            count++;
                            result = TcpConnect.Receive(key);

                            Log($"执行命令{item}，第{count}次内容：{result}");
                            read.AppendLine($"时间：{DateTime.Now:HH:mm:ss.fff}，执行指令{item}，\r内容：\r{result}");
                            Debug.WriteLine($"执行命令{item}，结果：{result}");

                            if (result.Contains("Command processing is completed") || result.Contains("successfully"))
                            {
                                addIndex = countList[index] > addIndex ? countList[index] : addIndex;
                                Log($"执行成功！命令：{item},耗时{sw.ElapsedMilliseconds}ms");
                                break;
                            }
                            else if (result.Contains("the telephone number has been occupied by another port"))
                            {
                                // Failure: Port 0/2/0 will be assigned telephone number 7088, but the telephone number has been occupied by another port
                                string x = string.Empty;
                                var match = Regex.Match(result, @"telephone number\s+(\d+)");

                                if (match.Success) x = match.Groups[1].Value; // 7088

                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageView.ShowWaring($"警告：{x}已其他端口占用，请修改号码");
                                });
                                issunccess = false;
                                break;
                            }
                            else if (result.Contains("Username or Domain invalid") || result.Contains("System is busy"))
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageView.ShowWaring("有未完成的操作，请稍后再继续，添加方法");
                                });
                                issunccess = false;
                                break;

                            }

                            Thread.Sleep(300);

                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"SIP 用户删除异常：{ex.Message}");
                    }
                    addsum = countList[index];
                    index++;
                    //报错跳出循环
                    if (!issunccess) break;
                }

                _IsAddSuccess = issunccess ? 1 : 2;

                TelnetEvent.ResetPerm(key);
            });


        }

        public bool checkResult(string key, int index)
        {
            bool ischeck = true;
            string result = string.Empty;
            int end = 0;



            do
            {
                end++;

                if (end > 10)//超过5次则判断超时
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageView.ShowWaring($"警告：接口超时！请重新添加");
                    });
                    return false;
                }
                Thread.Sleep(300);
            }
            while (ischeck);

            return true;

        }


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
