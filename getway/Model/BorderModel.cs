using getway.Util;

namespace getway.Model
{
    internal class BorderModel : ViewModelBase
    {

        private string _BorderName;
        public string BorderName { get => _BorderName; set => SetProperty(ref _BorderName, value); }

        private string _Status;
        public string Status { get => _Status; set => SetProperty(ref _Status, value); }


        private int _SlotNo;
        public int SlotNo { get => _SlotNo; set => SetProperty(ref _SlotNo, value); }

        private bool _IsEnable;
        public bool IsEnable { get => _IsEnable; set => SetProperty(ref _IsEnable, value); }

        private bool _IsSelect = false;
        public bool IsSelect { get => _IsSelect; set => SetProperty(ref _IsSelect, value); }




    }
}
