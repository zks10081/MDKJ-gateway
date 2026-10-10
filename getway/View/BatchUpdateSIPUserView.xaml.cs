using getway.Base;
using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace getway.View
{
    /// <summary>
    /// BatchUpdateSIPUserView.xaml 的交互逻辑
    /// </summary>
    public partial class BatchUpdateSIPUserView : Window
    {
        public BatchUpdateSIPUserView(ILogSink _logSink, string board)
        {
            InitializeComponent();
            DataContext = new BatchUpdateSIPUserViewModel(_logSink, board);
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
