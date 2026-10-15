using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    class DigitMapViewModel : ViewModelBase
    {
        private string _DigtiMapContent;
        private string _ActionContent;
        private bool _IsEdit = true;
        public string DigtiMapContent { get => _DigtiMapContent; set => SetProperty(ref _DigtiMapContent, value); }
        public string ActionContent { get => _ActionContent; set => SetProperty(ref _ActionContent, value); }
        public bool IsEdit { get => _IsEdit; set => SetProperty(ref _IsEdit, value); }

        public string old_DigtiMapContent;
        public string key;

        //父级方法
        private readonly ILogSink? _logSink;
        private void Log(string text) => _logSink?.AppendLog(text);


        public DigitMapViewModel()
        {
            key = DefaulConfig.GetBaseKey();

        }
        public DigitMapViewModel(string digitMapContent, ILogSink _logSink)
        {
            _DigtiMapContent = digitMapContent;
            key = DefaulConfig.GetBaseKey();


            //记录旧值
            old_DigtiMapContent = digitMapContent;
        }


        public ICommand UpdateInfo => new ViewModelCommand(async param =>
        {
            if (string.IsNullOrEmpty(DigtiMapContent))
            {
                MessageView.ShowFail("拨号计划内容不能为空，请重新输入！");
                DigtiMapContent = old_DigtiMapContent;
                return;

            }

            DigtiMapContent = DigtiMapContent.Replace(" ", ""); // 去除空格
            DigtiMapContent = DigtiMapContent.ToUpper(); // 变大写

            if (!ConfigUtil.IsDigitMap(DigtiMapContent))
            {
                MessageView.ShowFail("拨号计划不符合规则，请重新输入！");
                DigtiMapContent = old_DigtiMapContent;
                return;
            }

            IsEdit = false;
            ActionContent = "修改中，请稍等！";
            string result = string.Empty;
            bool issuccess = false;
            bool otherFail = false;

            //清空拨号规则
            TelnetEvent.DigitMapDeletAll(key);

            TelnetEvent.ResetPerm(key);

            //添加新的拨号规则
            TelnetEvent.DigitMapAdd(key, DigtiMapContent);

            await Task.Run(() =>
            {
                //计时
                var sw = Stopwatch.StartNew();
                int outCount = 0;

                while (true && sw.ElapsedMilliseconds < 5000)
                {
                    result = TcpConnect.Receive(key);
                    if (DefaulConfig.IsDebug)
                    {
                        Debug.WriteLine($"耗时：{sw.ElapsedMilliseconds}ms，拨号计划修改：{result}");
                    }

                    if (result.Contains("System is busy, please retry after a while"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                        });
                        otherFail = true;
                        break;
                    }
                    else if (result.Contains("Username or Domain invalid"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowWaring("服务器异常，请重试");
                        });
                        otherFail = true;
                        break;
                    }
                    else if (result.Contains("Failure: The new digitmap name conflict"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowFail("拨号计划已存在，请重新输入！");
                        });
                        otherFail = true;
                        break;
                    }
                    else if (result.Contains("The digitmap value is invalid"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowFail("拨号计划不符合规则，请重新输入！");
                        });
                        otherFail = true;
                        break;
                    }
                    else if (result.Contains("Command:"))
                    {
                        //保存成功
                        Console.WriteLine("修改拨号计划完成");
                        DigitMapDB.UpdateDigitMapByIp(DefaulConfig.GetwayIp, DigtiMapContent);//向数据库更新
                        issuccess = true;
                        break;

                    }
                    Thread.Sleep(300);
                }
                //权限复位
                TelnetEvent.ResetPerm(key);

            });


            IsEdit = true;
            ActionContent = string.Empty;
            if (issuccess)
            {
                MessageView.ShowSuccess("修改成功！");
                CloseWindows(param);
            }
            else
            {
                if (otherFail) MessageView.ShowWaring("修改拨号计划超时");
                DigtiMapContent = old_DigtiMapContent;
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
