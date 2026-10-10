using System.Windows.Controls;
using System.Windows.Input;

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

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && !item.IsSelected)
            {
                // 手动设置选中状态
                item.IsSelected = true;

                // 确保键盘焦点也同步过来（支持后续键盘上下键切换）
                item.Focus();
            }
        }
    }
}
