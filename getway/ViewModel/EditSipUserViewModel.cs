using DMGatewayDemo.Util;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using getway.View;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    internal class EditSipUserViewModel : ViewModelBase
    {
        private string _FSP;
        public string FSP { get => _FSP; set => SetProperty(ref _FSP, value); }
        private string _UserPhone;
        public string UserPhone { get => _UserPhone; set => SetProperty(ref _UserPhone, value); }
        private string _HotLinePhone;
        public string HotLinePhone { get => _HotLinePhone; set => SetProperty(ref _HotLinePhone, value); }
        private string _HotLineTime;
        public string HotLineTime { get => _HotLineTime; set => SetProperty(ref _HotLineTime, value); }

        public ObservableCollection<IpcUserModel> IpcUserList;


        private string oldTelno;
        private string oldHotlineTime;
        private string oldHotLinePhone;


        private string key;

        public EditSipUserViewModel() { }
        public EditSipUserViewModel(string fsp, string userphone, string hotlinephone, string hotlinetime, ObservableCollection<IpcUserModel> list)
        {
            IpcUserList = list;
            _FSP = fsp;
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
            //无变化则直接退出,或HotLinePhone的值特殊情况
            if (oldHotLinePhone == HotLinePhone && oldHotlineTime == HotLineTime && oldTelno == UserPhone) CloseWindows(param);
            CloseWindows(param);

            //修改用户号码
            if (oldTelno != UserPhone)
            {
                bool isSuccess = false;
                //校验格式
                if (!ConfigUtil.IsPhoneNum(UserPhone))
                {
                    UserPhone = oldTelno;
                    MessageView.ShowWaring("用户号码格式错误");
                    return;
                }

                TelnetEvent.EditSipUserPhone(key, FSP, oldTelno, UserPhone);
                //result = TcpConnect.Receive(key, "dmkj(config-esl-user)#", 5000);
                await Task.Run(() =>
                {
                    //计时
                    var sw = Stopwatch.StartNew();
                    while (true || sw.ElapsedMilliseconds < 5000)
                    {
                        result = TcpConnect.Receive(key);

                        isSuccess = checkReult(result);

                        if (isSuccess) break;

                    }
                });

                if (!isSuccess)
                {
                    return;
                }
                else
                {   //将变化同步至数组
                    getIpcUser(oldTelno).Name = UserPhone;
                }

                TelnetEvent.ResetPerm(key);
            }


            //修改热线信息
            if (oldHotLinePhone != HotLinePhone || oldHotlineTime != HotLineTime)
            {
                bool isSuccess = false;
                //校验格式
                if (!ConfigUtil.IsPhoneNum(HotLinePhone) || HotLinePhone == "-")
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

                TelnetEvent.EditHotLine(key, FSP, UserPhone, HotLinePhone, HotLineTime);
                await Task.Run(() =>
                {
                    //计时
                    var sw = Stopwatch.StartNew();
                    while (true || sw.ElapsedMilliseconds < 5000)
                    {
                        result = TcpConnect.Receive(key);

                        isSuccess = checkReult(result);

                        if (isSuccess) break;

                    }
                });

                if (!isSuccess)
                {
                    return;
                }

                TelnetEvent.ResetPerm(key);

            }
            //成功则关闭窗口
            CloseWindows(param);

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
            else if (result.Contains("Unknown command, the error locates at"))
            {
                if (DefaulConfig.IsDebug)
                {
                    Console.WriteLine("修改成功");
                }
                return true;
            }

            // Failure: Port 0/2/0 will be assigned telephone number 7088, but the telephone number has been occupied by another port
            else if (result.Contains("but the telephone number has been occupied by another port"))
            {

                string x = string.Empty;
                var match = Regex.Match(result, @"telephone number\s+(\d+)");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring($"警告：{x}已其他端口占用，请修改号码");
                });
            }

            else if (result.Contains("System is busy, please retry after a while"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                });
            }

            else if (result.Contains("Failure:"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("有未完成的操作，请稍后再继续");
                });
            }

            else if (result.Contains("Username or Domain invalid!"))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageView.ShowWaring("服务器异常，请重试");
                });
            }
            else if (result.Contains("Command:"))
            {
                if (DefaulConfig.IsDebug)
                {
                    Console.WriteLine("修改成功");
                }
                return true;

            }
            return false;


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

        /// <summary>
        /// 线路检测
        /// </summary>
        public ICommand CheckLineCommand => new ViewModelCommand(param =>
        {
            Window window = (Window)param;
            window.Close();

            //new CheckLineView(IP, FSP, Telno).ShowDialog();
        });

        public IpcUserModel getIpcUser(string phoneNum)
        {
            if (string.IsNullOrEmpty(phoneNum)) return null;

            foreach (IpcUserModel item in IpcUserList)
            {
                if (phoneNum.Equals(item.Name))
                {
                    return item;
                }
            }
            return null;
        }

    }
}
