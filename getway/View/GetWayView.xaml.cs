using getway.ViewModel;
using System.Windows.Controls;

namespace getway.View
{
    /// <summary>
    /// GetWayView.xaml 的交互逻辑
    /// </summary>
    public partial class GetWayView : UserControl
    {
        public GetWayView()
        {
            InitializeComponent();
            DataContext = new GetWayViewMode();
        }
    }
}
