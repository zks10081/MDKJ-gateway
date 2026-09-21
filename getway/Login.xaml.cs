using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace getway
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        private LoginViewModel ViewModel => (LoginViewModel)DataContext;

        public Login()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();

            // PasswordBox 不支持绑定，手工与 ViewModel.Password 同步
            pwdBox.Password = ViewModel.Password ?? string.Empty;
            pwdBox.PasswordChanged += (_, _) => ViewModel.Password = pwdBox.Password;
        }

        /// <summary>
        /// 最小化
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Input_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            switch (sender)
            {
                case TextBox textBox:
                    textBox.SelectAll();
                    break;
                case PasswordBox passwordBox:
                    passwordBox.SelectAll();
                    break;
            }
        }
    }
}
