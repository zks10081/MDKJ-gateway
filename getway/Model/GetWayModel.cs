using getway.Util;

namespace getway.Model
{
    internal class GetWayModel : ViewModelBase
    {

        private int _FrameId;
        public int FrameId { get => _FrameId; set => SetProperty(ref _FrameId, value); }

        private string _IP;
        public string IP { get => _IP; set => SetProperty(ref _IP, value); }

    }
}
