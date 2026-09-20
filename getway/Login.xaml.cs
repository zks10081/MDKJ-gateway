using getway.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace getway
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel();

        }

        /// <summary>
        /// 最小化
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            //this.WindowState = WindowState.Minimized;
            SystemCommands.MinimizeWindow(this);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void txtUser_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            txtUser.CaretIndex = txtUser.Text.Length;
        }

        private void password_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            password.CaretIndex = password.Text.Length;
        }

        private void ip_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ip.CaretIndex = ip.Text.Length;
        }
    }
}
