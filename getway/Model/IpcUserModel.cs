using getway.Util;

namespace getway.Model
{
    internal class IpcUserModel : ViewModelBase
    {

        private String _Name;
        public string Name { get => _Name; set => SetProperty(ref _Name, value); }

        private string _State;
        public string State
        {
            get => _State;
            set
            {
                if (SetProperty(ref _State, value))
                {
                    OnPropertyChanged(nameof(IsOnline));
                }
            }
        }

        // 注册状态文本为 "Registered" 才算在线，FailRegistered 等一律离线
        public bool IsOnline => _State == "Registered";


        // 为了方便界面展示，可以加一个组合属性
        private string _FSP;
        public string FSP { get => _FSP; set => SetProperty(ref _FSP, value); }


    }
}
