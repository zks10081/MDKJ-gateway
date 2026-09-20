using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace getway.Controls
{
    /// <summary>
    /// ProgressRing.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressRing : UserControl
    {
        public List<MarkInfo> MarkList { get; set; } = new List<MarkInfo>();

        public Brush ForeColor { get; set; } = Brushes.Orange;


        public String Title
        {
            get { return (String)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(String), typeof(ProgressRing), new PropertyMetadata("Title"));




        public double Value
        {
            get { return (double)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register("MyProperty", typeof(double), typeof(ProgressRing),
                new PropertyMetadata(0.0, new PropertyChangedCallback(OnValueChanaged)));

        public static void OnValueChanaged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //处理value发生变化
            (d as ProgressRing).Refresh();
        }

        private void Refresh()
        {
            var count = Math.Ceiling(Value / 100 * 360 / 8);
            for (int i = 0; i < MarkList.Count; i++)
            {
                if (i < count)
                {
                    MarkList[i].Color = ForeColor;
                }
                else
                {
                    MarkList[i].Color = Brushes.LightGray;
                }
            }
        }


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

    public class MarkInfo : INotifyPropertyChanged
    {
        public double Angle { get; set; }
        private Brush _color = Brushes.LightGray;


        public Brush Color
        {
            get { return _color; }
            set
            {
                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
