using getway.Util;

namespace getway.Model
{
    internal class IpcUserModel : ViewModelBase
    {

        private string _Name;
        public string Name { get => _Name; set => SetProperty(ref _Name, value); }

        /// <summary>
        /// 0 ：离线
        /// 1 ：振铃
        /// 2 ：摘机
        /// 3 ：通话中
        /// 4 ：锁定
        /// 5 ：空闲
        /// </summary>
        private string _Status = "-1";
        public string Status
        {
            get => _Status;
            set
            {
                string status = value ?? string.Empty;
                if (SetProperty(ref _Status, status))
                {
                    // 未知状态兜底成离线色，避免字典取不到直接抛异常
                    BackgroundColor = DefaulConfig.SipUserStatusDict.TryGetValue(status, out string color)
                        ? color
                        : DefaulConfig.SIPUserBackgroundColorOfUnOnline;
                }
            }
        }

        private string _BackgroundColor;
        public string BackgroundColor { get => _BackgroundColor; set => SetProperty(ref _BackgroundColor, value); }


        // 为了方便界面展示，可以加一个组合属性
        private string _FSP;
        public string FSP { get => _FSP; set => SetProperty(ref _FSP, value); }

        public int Index;

        //注册状态
        public string Regite;

        //呼叫状态
        public string call;

        public void SetBackgroundColorByState(string regState, string callState)
        {
            // 轮询 reg-state 时拿不到呼叫状态，这里只按注册状态判定：激活=空闲，其余都算离线
            if (string.IsNullOrEmpty(callState))
            {
                Status = DefaulConfig.Active.Equals(regState) ? "5" : "0";
                return;
            }
            // 振铃（注册状态为：激活，且呼叫状态为：正在振铃、正在回铃）
            if (DefaulConfig.Active.Equals(regState) && (DefaulConfig.Ringing.Equals(callState) || DefaulConfig.Ringback.Equals(callState)))
            {
                Status = "1";   // Status 的 setter 会自动带上对应颜色
            }
            // 摘机 （注册状态为：激活，且呼叫状态为：拨号中）
            else if (DefaulConfig.Active.Equals(regState) && DefaulConfig.Dialing.Equals(callState))
            {
                Status = "2";
            }
            // 通话中 （注册状态为：激活，且呼叫状态为：请求建立呼叫中、建立连接中、通话已建立、正在释放连接）
            else if ((DefaulConfig.Active.Equals(regState)) && (DefaulConfig.Establishing.Equals(callState) || DefaulConfig.Connecting.Equals(callState) || DefaulConfig.Connected.Equals(callState) || DefaulConfig.Disconnecting.Equals(callState)))
            {
                Status = "3";
            }
            // 锁定（注册状态为：激活，且呼叫状态为： 用户锁定）
            else if (DefaulConfig.Active.Equals(regState) && DefaulConfig.Locked.Equals(callState))
            {
                Status = "4";
            }
            // 空闲（注册状态为：激活，且呼叫状态为：空闲）
            else if (DefaulConfig.Active.Equals(regState) && DefaulConfig.Idle.Equals(callState))
            {
                Status = "5";
            }
            // 注册失败，没有匹配状态则失败
            else
            {
                Status = "0";
            }
        }


    }
}
