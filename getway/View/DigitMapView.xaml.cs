using getway.Base;
using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace getway.View
{
    /// <summary>
    /// DigitMapView.xaml 的交互逻辑
    /// </summary>
    public partial class DigitMapView : Window
    {
        public DigitMapView()
        {
            InitializeComponent();
            this.DataContext = new DigitMapViewModel();
        }
        public DigitMapView(string digitMapContent, ILogSink _logSink)
        {
            InitializeComponent();
            this.DataContext = new DigitMapViewModel(digitMapContent, _logSink);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 输入框获得焦点时全选，方便直接改写
        /// </summary>
        private void Input_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
