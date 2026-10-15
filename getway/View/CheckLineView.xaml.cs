using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace getway.View
{
    /// <summary>
    /// CheckLineView.xaml 的交互逻辑
    /// </summary>
    public partial class CheckLineView : Window
    {
        public CheckLineView(string fsp, string phone_num)
        {
            InitializeComponent();
            this.DataContext = new CheckLineViewModel(fsp, phone_num);
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
