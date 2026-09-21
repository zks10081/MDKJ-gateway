using getway.Model;
using System.ComponentModel;

namespace getway.ViewModel
{
    class IpcItemViewModel : INotifyPropertyChanged
    {

        public List<IpcUserModel> IpcUserList { get; set; } = new List<IpcUserModel>();

        public IpcItemViewModel()
        {
            initIpcList();

        }



        public void initIpcList()
        {
            for (int i = 0; i < 64; i++)
            {
                IpcUserList.Add(new IpcUserModel() { Name = Convert.ToString(8002 + i), State = "0", FSP = "" });
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;


    }
}
