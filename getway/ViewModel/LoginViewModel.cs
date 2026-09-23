using getway.Base;
using getway.DB.Pg;
using getway.Model;
using getway.Util;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace getway.ViewModel
{
    class LoginViewModel : ViewModelBase
    {
        private string _Username;
        public string Username { get => _Username; set => SetProperty(ref _Username, value); }
        private bool _IsSave;
        public bool IsSave { get => _IsSave; set => SetProperty(ref _IsSave, value); }
        private string _Password;
        public string Password { get => _Password; set => SetProperty(ref _Password, value); }
        private string _IP;
        public string IP { get => _IP; set => SetProperty(ref _IP, value); }

        private bool _IsViewVisible = true;
        public bool IsViewVisible { get => _IsViewVisible; set => SetProperty(ref _IsViewVisible, value); }

        public bool IsLoginSuccess { get; set; }



        public ICommand LoginCommand { get; set; }

        public LoginViewModel() {

            LoginCommand = new Command(LoginAction);

            var config = ConfigUtil.Load();
            if (config.IsSave)
            {
                Username = config.Username;
                Password = config.Password;
                IP = config.IP;
                IsSave = config.IsSave;
            }

        }

        public static int SelectLogin(string username ,string password )
        {

            string sql = $"select count(*) from dm_login where login_user = '{username}' and login_pwd = '{password}'";
            DataTable dataTable = PgConnect.SelectAsync(sql);

            if (dataTable == null)
            {
                return -1;
            }
            DataRowCollection rows = dataTable.Rows;
            return int.Parse(rows[0]["count"].ToString());

        }



        public void LoginAction(object paramter)
        {
            if (_Username == "" || _Username == null)
            {
                MessageBox.Show("登录失败");
                return;
            }
            else if (_Password == "" || _Password == null)
            {
                MessageBox.Show("登录失败");
                return;
            }
            else if (_IP == "" || _IP == null || !ConfigUtil.IsIP(_IP))
            {
                MessageBox.Show("登录失败");
                return;
            }
            else
            {

                DefaulConfig.SetDBString(_IP);

                int count = SelectLogin(Username, Password);

                if (count <=0)
                {
                    Console.WriteLine("登录失败");
                    MessageBox.Show("登录失败");
                    return;

                }

                if (IsSave)
                {
                    var config = new LoginModel
                    {
                        Username = Username,
                        Password = Password,
                        IP = IP,
                        IsSave = true
                    };
                    ConfigUtil.Save(config);
                }
                else
                {
                    ConfigUtil.Save(new LoginModel());
                }

                // 登录成功
                IsViewVisible = false;
                DefaulConfig.LoginPassword = Password;
                // 获取当前登录窗口，并将其 DialogResult 设为 true
                var window = Application.Current.Windows.OfType<Login>().FirstOrDefault();
                if (window != null)
                {
                    window.DialogResult = true; // 这会自动关闭登录窗口
                }

            }


        }


    }
}
