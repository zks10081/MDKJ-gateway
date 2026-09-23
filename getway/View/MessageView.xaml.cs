using DMGatewayDemo.Util;
using getway.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace getway.View
{
    /// <summary>
    /// MessageView.xaml 的交互逻辑
    /// </summary>
    public partial class MessageView : Window,  INotifyPropertyChanged
    {

        public static Dictionary<Message_Type, String> _iconTextList = new Dictionary<Message_Type, string>()
        {
            {Message_Type.Message_Success,""},
            {Message_Type.Message_Fail,""},
            {Message_Type.Message_Waring,""},
            {Message_Type.Message_Info,""},

        };
        public static Dictionary<Message_Type, Brush> _iconColorList = new Dictionary<Message_Type, Brush>()
        {
            {Message_Type.Message_Success,Brushes.Green},
            {Message_Type.Message_Fail,Brushes.Red},
            {Message_Type.Message_Waring,Brushes.Orange},
            {Message_Type.Message_Info,Brushes.Blue},

        };

        private string _Title;
        private string _Info;
        private string _iconText;
        private Brush _iconColort;

        private Message_Type Type;


        public string Title { get => _Title; set => SetProperty(ref _Title, value); 
        public string Info { get => _Info; set => SetProperty(ref _Info, value); }

        public string iconText { get => _iconText; set => SetProperty(ref _iconText, value); }

        public Brush iconColor { get => _iconColort; set => SetProperty(ref _iconColort, value); }

        public MessageView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 弹窗页面
        /// </summary>
        /// <returns></returns>
        public static bool Show(string title,string message, Message_Type type)
        {
            MessageView model = new MessageView();
            model.Title = title;
            model.Info = message;
            model.Type = type;
            model.iconText = _iconTextList[type];
            model.iconColor = _iconColorList[type];
            return (bool)model.ShowDialog();
        }

        public static bool ShowSuccess(string message)
        {
            return Show("成功", message, Message_Type.Message_Success);
        }

        public static bool ShowFail(string message)
        {
            return Show("失败", message, Message_Type.Message_Fail);
        }


        /// <summary>
        /// 关闭
        /// </summary>
        public ICommand ColseGetWay => new ViewModelCommand(param =>
        {
            Window window = (Window)param;
            window.Close();
        });


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }


    }

    public enum Message_Type
    {
        Message_Success,      // 成功
        Message_Fail,       // 失敗
        Message_Waring,      // 警告
        Message_Info       // 信息
    }



}
