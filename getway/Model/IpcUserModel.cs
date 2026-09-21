using getway.Util;

namespace getway.Model
{
    internal class IpcUserModel : ViewModelBase
    {

        private String _Name;
        public string Name { get => _Name; set => SetProperty(ref _Name, value); }

        private string _State;
        public string State { get => _State; set => SetProperty(ref _State, value); }


        // 为了方便界面展示，可以加一个组合属性
        private string _FSP;
        public string FSP { get => _FSP; set => SetProperty(ref _FSP, value); }


    }
}
