using System;
using System.Collections.Generic;
using System.Linq;
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
    /// AddGetwayView.xaml 的交互逻辑
    /// </summary>
    public partial class AddGetwayView : Window
    {
        public AddGetwayView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            DataContext = new AddGetwayView();
        }


    }


}
