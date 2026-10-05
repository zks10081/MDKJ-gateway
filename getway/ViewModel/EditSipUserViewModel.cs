using DMGatewayDemo.Util;
using getway.Base;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace getway.ViewModel
{
    internal class EditSipUserViewModel : ViewModelBase
    {
        private string _BoardPort;
        public string BoardPort { get => _BoardPort; set => SetProperty(ref _BoardPort, value); }
        private string _UserPhone;
        public string UserPhone { get => _UserPhone; set => SetProperty(ref _UserPhone, value); }
        private string _HotLinePhone;
        public string HotLinePhone { get => _HotLinePhone; set => SetProperty(ref _HotLinePhone, value); }
        private string _HotLineTime;
        public string HotLineTime { get => _HotLineTime; set => SetProperty(ref _HotLineTime, value); }


        private string oldTelno;
        private string oldHotlineTime;
        private string oldHotLinePhone;


        private string key;

        public EditSipUserViewModel() { }
        public EditSipUserViewModel(string boardport, string userphone, string hotlinephone, string hotlinetime) {
            _BoardPort = boardport;
            _UserPhone = userphone;
            _HotLinePhone = hotlinephone;   
            _HotLineTime = hotlinetime;

            //用于记录值是否更新
            oldTelno = userphone;
            oldHotLinePhone = hotlinephone;
            oldHotlineTime = hotlinetime;

            key = DefaulConfig.GetBaseKey();
        }

        public ICommand EditSipUserHotline => new ViewModelCommand(async param =>
        {
            string result = string.Empty;
            bool isSuccess = true;
            //无变化则直接退出,或HotLinePhone的值特殊情况
            if ( oldHotLinePhone == HotLinePhone && oldHotlineTime == HotLineTime)
            {
                if ((oldHotLinePhone == "" && HotLinePhone == "-") || (oldHotLinePhone == "-" && HotLinePhone == "")) CloseWindows(param);
                if(oldTelno == UserPhone) CloseWindows(param);
            }

            //修改用户号码
            if(oldTelno != UserPhone)
            {
                //校验格式
                if (!ConfigUtil.IsPhoneNum(UserPhone))
                {
                    UserPhone = oldTelno;
                    MessageView.ShowWaring("用户号码格式错误");
                    return;
                }

                TelnetEvent.EditSipUserPhone(key, BoardPort, oldTelno, UserPhone);
                result = TcpConnect.Receive(key);
                checkReult(result);

                if (!isSuccess)
                {
                    return;
                }
            }


            //修改热线信息
            if (oldHotLinePhone != HotLinePhone || oldHotlineTime != HotLineTime)
            {
                //校验格式
                if (!ConfigUtil.IsPhoneNum(HotLinePhone))
                {
                    UserPhone = oldTelno;
                    MessageView.ShowWaring("热线号码格式错误");
                    return;
                }
                if (!ConfigUtil.IsHotTime(HotLineTime))
                {
                    UserPhone = oldTelno;
                    MessageView.ShowWaring("请输入正确的热线时间，范围是 1-60 秒");
                    return;
                }

                TelnetEvent.EditHotLine(key, BoardPort, UserPhone,HotLinePhone,HotLineTime);


                result = TcpConnect.Receive(key);
                checkReult(result);

                if (!isSuccess)
                {
                    return;
                }

                //成功则关闭窗口
                CloseWindows(param);


            }

        });

        public bool checkReult(string result)
        {
            // 修改用户结束条件的结束条件
            if (result.Contains("modified successfully"))
            {
                if (DefaulConfig.IsDebug)
                {
                    Console.WriteLine("修改成功");
                }
                return true;
            }
            // 编辑热线的结束条件
            if (result.Contains("Unknown command, the error locates at"))
            {
                if (DefaulConfig.IsDebug)
                {
                    Console.WriteLine("修改成功");
                }
                return true;
            }

            // Failure: Port 0/2/0 will be assigned telephone number 7088, but the telephone number has been occupied by another port
            if (result.Contains("but the telephone number has been occupied by another port"))
            {

                string x = string.Empty;
                var match = Regex.Match(result, @"telephone number\s+(\d+)");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring($"警告：{x}已其他端口占用，请修改号码");
                });
            }

            if (result.Contains("System is busy, please retry after a while"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                });
            }

            if (result.Contains("Failure:"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                });
            }

            if (result.Contains("Username or Domain invalid!"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("服务器异常，请重试");
                });
            }
            return false;


        }

        // 取消按钮
        public ICommand ColseGetWay => new ViewModelCommand(param =>
        {
            CloseWindows(param);
        });

        public void CloseWindows(object paramter)
        {
            Window window = (Window)paramter;
            window.Close();
        }

        /// <summary>
        /// 线路检测
        /// </summary>
        public ICommand CheckLineCommand => new ViewModelCommand(param =>
        {
            Window window = (Window)param;
            window.Close();

            //new CheckLineView(IP, FSP, Telno).ShowDialog();
        });

    }
}
