using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace getway.View
{
    /// <summary>
    /// MessageView.xaml 的交互逻辑
    /// </summary>
    public partial class MessageView : Window, INotifyPropertyChanged
    {

        // 注意：C# 字符串里要用 Unicode 转义，写成 "&#xe61c;" 只会原样显示成文本
        public static Dictionary<Message_Type, String> _iconTextList = new Dictionary<Message_Type, string>()
        {
            {Message_Type.Message_Success,"\ue61c"},
            {Message_Type.Message_Fail,"\ue616"},
            {Message_Type.Message_Waring,"\ue614"},
            {Message_Type.Message_Info,"\ue626"},

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

        private string _OkShow = "Collapsed";
        public string OkShow { get => _OkShow; set => SetProperty(ref _OkShow, value); }

        private string _CanclShow = "Collapsed";
        public string CanclShow { get => _CanclShow; set => SetProperty(ref _CanclShow, value); }


        // 不叫 Title：避免隐藏 Window.Title 这个依赖属性
        public string MsgTitle
        {
            get => _Title; set => SetProperty(ref _Title, value);
        }
        public string Info { get => _Info; set => SetProperty(ref _Info, value); }

        public string iconText { get => _iconText; set => SetProperty(ref _iconText, value); }

        public Brush IconColor { get => _iconColort; set => SetProperty(ref _iconColort, value); }

        public MessageView()
        {
            InitializeComponent();
            // 页面上的 {Binding} 都以窗口自身为源，缺了这行所有绑定都拿不到值
            DataContext = this;
        }

        /// <summary>
        /// 弹窗页面
        /// </summary>
        /// <returns></returns>
        public static bool Show(string title, string message, Message_Type type, int buttonShow = 1)
        {
            MessageView model = new MessageView();
            model.MsgTitle = title;
            model.Info = message;
            model.Type = type;
            model.iconText = _iconTextList[type];
            model.IconColor = _iconColorList[type];
            if (buttonShow > 0)
            {
                model.OkShow = "Visible";
            }
            if (buttonShow > 1)
            {
                model.CanclShow = "Visible";
            }
            return model.ShowDialog() == true;
        }
        /// <summary>
        /// 成功确定弹窗
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ShowSuccess(string message)
        {
            return Show("成功", message, Message_Type.Message_Success);
        }

        /// <summary>
        /// 失败确定弹窗
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ShowFail(string message)
        {
            return Show("错误", message, Message_Type.Message_Fail);
        }

        /// <summary>
        /// 警告确定弹窗
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ShowWaring(string message)
        {
            return Show("警告", message, Message_Type.Message_Waring);
        }

        /// <summary>
        /// 选择弹窗
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool ShowSelect(string title, string message)
        {
            return Show(title, message, Message_Type.Message_Info, 2);
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // 返回 true，并自动关闭窗口
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // 返回 false，并自动关闭窗口
        }


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
