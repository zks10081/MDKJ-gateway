using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace getway.Util
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        //    => SetProperty(ref storage, value, true, propertyName);
        protected bool SetProperty<T>(ref T storage, T value, bool forceNew = false, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value) && !forceNew) return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}