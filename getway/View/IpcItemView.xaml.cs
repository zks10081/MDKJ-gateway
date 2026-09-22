using System.Windows.Controls;

namespace getway.View
{
    /// <summary>
    /// IpcItemView.xaml 的交互逻辑
    /// </summary>
    public partial class IpcItemView : UserControl
    {
        public IpcItemView()
        {
            InitializeComponent();
            // DataContext 由父视图（GetWayView）通过绑定注入 IpcItemViewModel，
            // 不再在这里 new，避免子 ViewModel 先于父 ViewModel 创建
        }
    }
}
