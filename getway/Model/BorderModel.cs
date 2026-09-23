using getway.Util;

namespace getway.Model
{
    internal class BorderModel : ViewModelBase
    {

        private string _BorderName;
        public string BorderName { get => _BorderName; set => SetProperty(ref _BorderName, value); }


        private int _SlotNo;
        public int SlotNo { get => _SlotNo; set => SetProperty(ref _SlotNo, value); }

        private bool _IsEnable;
        public bool IsEnable { get => _IsEnable; set => SetProperty(ref _IsEnable, value); }



    }
}
