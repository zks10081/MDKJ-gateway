using DMGatewayDemo.Util;
using getway.DB.TelnetConnect;
using getway.Util;
using getway.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    class SaveConfigViewModel : ViewModelBase
    {


        private string _TipText;
        public string TipText { get => _TipText; set => SetProperty(ref _TipText, value); }

        public int SaveStatus;
        public string key;

        public SaveConfigViewModel()
        {
            _TipText = "点击“确定”保存配置";
            SaveStatus = 0;
            key = DefaulConfig.GetBaseKey();
        }

        public ICommand SaveInfo => new ViewModelCommand(async param => {
            
            SaveStatus = 1;
            _TipText = "保存配置需要几分钟，请等待";
            string result = string.Empty;

            TelnetEvent.SaveConfig(key);

            await Task.Run(() =>
            {
                string line = "";
                while (true)
                {
                    result = TcpConnect.Receive(key);
                    if (result == null) break;


                    if (result.Contains("System is busy, please retry"))
                    {
                        SaveStatus = 2;
                        _TipText = "保存失败，请稍后重试！";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowSuccess("系统忙碌中，请稍后重试！");
                        });
                        break;
                    }

                    //  语音网关在执行 save 命令时会保存系统配置文件和数据库文件到闪存中
                    //      1. 首先会保存配置文件，再保存数据库文件
                    //      2. 无论是否执行修改操作，配置文件都会被重新保存，但是数据库文件会判断是否发生修改
                    //      3. 所以，对数据库的保存状态进行判断即可【未修改无需保存，已修改保存成功】
                    //  语音网关在执行 save data 命令时会保存数据库文件到闪存中
                    //      1. 数据库文件会判断是否发生修改
                    //      2. 所以，对数据库的保存状态进行判断即可【未修改无需保存，已修改保存成功】
                    // --------------------------------------------------------------------------------------------------
                    // --------------------------------------------------------------------------------------------------
                    // control board is saved   保存成功
                    // no need                  无需保存
                    if (line.Contains("control board is saved") || line.Contains("no need"))
                    {
                        SaveStatus = 2;
                        _TipText = "配置保存成功";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageView.ShowSuccess("配置保存成功");
                        });
                        break;
                    }
                }
            });

            if(SaveStatus == 3)
            {
                CloseWindows(param);
            }


        });

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

    }
}
