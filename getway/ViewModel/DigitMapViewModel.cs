using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.Pg;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    class DigitMapViewModel : ViewModelBase
    {
        private string _DigtiMapContent;
        public string DigtiMapContent { get => _DigtiMapContent; set => SetProperty(ref _DigtiMapContent, value); }

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
            string result = string.Empty;

            //清空拨号规则
            TelnetEvent.DigitMapDeletAll(key);
            TelnetEvent.ResetPerm(key);

            //添加新的拨号规则
            TelnetEvent.DigitMapAdd(key, DigtiMapContent);

            await Task.Run(() =>
            {
                int outCount = 0;

                while (true)
                {
                    result = TcpConnect.Receive(key);
                    if (result == null) break;
                    //超过3次则直接跳过
                    if (outCount >= 3) break;


                    if (result.Contains("System is busy, please retry after a while"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                        });
                        break;
                    }
                    else if (result.Contains("Username or Domain invalid"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowWaring("服务器异常，请重试");
                        });
                        break;
                    }
                    else if (result.Contains("Failure: The new digitmap name conflict"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowFail("拨号计划已存在，请重新输入！");
                        });
                        break;
                    }
                    else if (result.Contains("The digitmap value is invalid"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowFail("拨号计划不符合规则，请重新输入！");
                        });
                        break;
                    }
                    else if (result.Contains(" Command:"))
                    {
                        //保存成功
                        Console.WriteLine("修改拨号计划完成");
                        DigitMapDB.UpdateDigitMapByIp(DefaulConfig.GetwayIp, DigtiMapContent);//向数据库更新
                        break;

                    }
                    else
                    {
                        outCount++;
                    }
                }

            });


            //权限复位
            TelnetEvent.ResetPerm(key);

            CloseWindows(param);

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
