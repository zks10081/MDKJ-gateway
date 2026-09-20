using getway.ViewModel;
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
            this.DataContext = new IpcItemViewModel();
        }
    }
}
