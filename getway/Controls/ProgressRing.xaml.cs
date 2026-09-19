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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace getway.Controls
{
    /// <summary>
    /// ProgressRing.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressRing : UserControl
    {
        public List<MarkInfo> MarkList { get; set; } = new List<MarkInfo>();
        public ProgressRing()
        {
            InitializeComponent();
            int count = 360 / 8;
            for (int i = 0; i < count; i++)
            {
                MarkList.Add(new MarkInfo() { Angle = i * 8 });
            }
        }
    }

    public class MarkInfo
    {
        public double Angle {  get; set; }
        public Brush color {  get; set; }
    }
}
