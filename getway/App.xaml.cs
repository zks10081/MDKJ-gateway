using getway.Util;
using getway.ViewModel;
using System.Configuration;
using System.Data;
using System.Windows;

namespace getway
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void ApplicationStartup(object sender, EventArgs e)
        {
            // 获取当前运行进程的主模块的名称（即执行文件的名称，比如myapp.exe）
            string MName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            // 从模块名称中移除扩展名（如.exe），得到不包含扩展名的文件名
            string PName = System.IO.Path.GetFileNameWithoutExtension(MName);
            // 获取所有与当前应用程序的文件名匹配的进程
            System.Diagnostics.Process[] myProcess = System.Diagnostics.Process.GetProcessesByName(PName);
            // 判断是否重复启动
            if (myProcess.Length > 1)
            {
                MessageBox.Show("软件已经启动成功，不可重复启动");
                Application.Current.Shutdown();
                return;
            }
            Login loginView = new Login();
            loginView.DataContext = new LoginViewModel();

            // 使用 ShowDialog() 弹出登录窗口，代码会停在这里，直到登录窗口关闭
            // 只有当 DialogResult == true 时，说明登录成功了
            if (loginView.ShowDialog() == true)
            {
                // 登录成功，显示主窗口
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                // 用户点了 X 关闭，或者验证失败关闭窗口，程序自动退出
            }
        }
    }

}
