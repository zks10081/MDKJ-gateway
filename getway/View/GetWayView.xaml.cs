using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace getway.View
{
    /// <summary>
    /// GetWayView.xaml 的交互逻辑
    /// </summary>
    public partial class GetWayView : UserControl
    {
        private DispatcherTimer _timer;
        public GetWayView()
        {
            InitializeComponent();
            DataContext = new GetWayViewMode();
            //定时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000); // 间隔1000毫秒
            _timer.Tick += Timer_Tick; // 绑定定时触发的事件

            _timer.Start(); // 启动定时器
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // 每秒更新一次时间显示
            tbTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 最小化
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            //SystemCommands.MinimizeWindow(this);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
