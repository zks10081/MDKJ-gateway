using getway.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace getway.View
{
    /// <summary>
    /// GetWayView.xaml 的交互逻辑
    /// </summary>
    public partial class GetWayView : Window
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
            SystemCommands.MinimizeWindow(this);
        }
        /// <summary>
        /// 最大化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClickMaxWindowButton(object sender, RoutedEventArgs e)
        {
            int SipInfoSide = 0;
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MainTitleTextBlock.FontSize = 48;
                SipInfoSide = 98;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MainTitleTextBlock.FontSize = 64;
                SipInfoSide = 128;
            }

            if (DataContext is GetWayViewMode vm)
            {
                vm.SipInfoSide = SipInfoSide;
            }
        }

        //命令回显自动滚动
        public void ReadContent_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Readscroll.Tag.ToString() == "1") return;
            double verticalOffset = e.VerticalOffset;//滚动位置
            double extentHeight = e.ExtentHeight;//内容总高度
            double viewportHeight = e.ViewportHeight;//可视内容高度
            double scrollableHeight = extentHeight - viewportHeight;
            if (scrollableHeight >= verticalOffset + 10)
            {
                Readscroll.ScrollToVerticalOffset(scrollableHeight);
            }

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
