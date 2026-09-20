using getway.Base;
using System.ComponentModel;
using System.Windows.Input;

namespace getway.ViewModel
{
    class MainVlewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public object PageContent { get; set; }

        //命令属性
        public ICommand NavCommand { get; set; }

        public MainVlewModel()
        {
            NavCommand = new Command(DoNavPage);
        }


        private void DoNavPage(object paramter)
        {
            PageContent = paramter;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PageContent"));
        }
    }
}
