using getway.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace getway.ViewModel
{
    class MainVlewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public object pageContent {  get; set; }

        //命令属性
        public ICommand NavCommand { get; set; }

        public MainVlewModel() {
            NavCommand = new Command(doNavPage);
        }


        private void doNavPage(object paramter)
        {
            pageContent = paramter;
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs("pageContent"));
        }
    }
}
