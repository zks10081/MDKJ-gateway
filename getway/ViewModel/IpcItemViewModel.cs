using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Windows.Input;

namespace getway.ViewModel
{
    class IpcItemViewModel : ViewModelBase
    {

        private List<IpcUserModel> _IpcUserList = new List<IpcUserModel>();
        public List<IpcUserModel> IpcUserList { get => _IpcUserList; set => SetProperty(ref _IpcUserList, value); }

        private List<BorderModel> _BorderList = new List<BorderModel>();
        public List<BorderModel> BorderList { get => _BorderList; set => SetProperty(ref _BorderList, value); }

        private BorderModel? _SelectBorder;
        public BorderModel? SelectBorder
        {
            get => _SelectBorder;
            set => SetSelectBorder(value, CancellationToken.None);
        }

        // 改选中项并触发用户列表刷新；token 用于丢弃过期结果
        private void SetSelectBorder(BorderModel? border, CancellationToken token)
        {
            if (SetProperty(ref _SelectBorder, border))
            {
                _ipcRefresh = RefreshIpcAsync(token);
            }
        }

        // 最近一次用户列表刷新任务，供父 ViewModel 等待
        private Task _ipcRefresh = Task.CompletedTask;
        public Task IpcRefresh => _ipcRefresh;

        // telnet 连接标识，由父 ViewModel（GetWayViewMode）在连接建立后注入
        public string Key { get; private set; } = string.Empty;

        public ICommand SelectBorderCommand { get; }

        public IpcItemViewModel()
        {
            // 构造时只做最小化初始化，不取数据：此时父 ViewModel 还没建立连接
            SelectBorderCommand = new Command(SelectBorderExecute);
            //生成临时数据
            //TempBorderInfo();
            //TempSipUserInfo();
        }

        /// <summary>
        /// 由父 ViewModel 在 telnet 连接成功后注入，取数据前必须已有 key
        /// </summary>
        public void SetKey(string key) => Key = key ?? string.Empty;

        //重查板卡与 SIP 用户
        public async Task RefreshAllAsync(CancellationToken token = default)
        {
            await RefreshBoardAsync(token);
            await IpcRefresh;
        }

        //获取卡框板槽信息
        public async Task RefreshBoardAsync(CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(Key))
            {
                BorderList = new List<BorderModel>();
                SelectBorder = null;
                return;
            }

            string key = Key;
            var boards = await Task.Run(() => TelnetEvent.QueryBoard(key, 0));
            // 切换网关期间旧结果不能覆盖新网关数据
            if (token.IsCancellationRequested) return;

            BorderList = boards ?? new List<BorderModel>();
            var first = BorderList.FirstOrDefault();
            if (first == null)
            {
                IpcUserList = new List<IpcUserModel>();
            }
            SetSelectBorder(first, token);
        }

        //获取sip用户注册数据
        public async Task RefreshIpcAsync(CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(Key) || _SelectBorder == null)
            {
                IpcUserList = new List<IpcUserModel>();
                return;
            }

            string key = Key;
            int slotNo = _SelectBorder.SlotNo;
            //var users = await Task.Run(() => TelnetEvent.QuerySipUser(key, 0, slotNo));
            var users = TelnetEvent.QuerySipUser(key, 0, slotNo);
            if (token.IsCancellationRequested) return;

            IpcUserList = users ?? new List<IpcUserModel>();
        }

        //选择板卡运行方法
        private void SelectBorderExecute(object parameter)
        {
            if (parameter is BorderModel border)
            {
                SelectBorder = border;
            }
        }

        public void TempBorderInfo()
        {

            BorderList.Clear();
            for (int i = 1; i <= 4; i++)
            {
                BorderList.Add(new BorderModel() { BorderName = $"板卡{i}", SlotNo = i });
            }
        }

        public void TempSipUserInfo()
        {

            IpcUserList.Clear();
            for (int i = 0; i < 64; i++)
            {
                IpcUserList.Add(new IpcUserModel() { Name = Convert.ToString(8002 + i), State = "0", FSP = $"0/1/{i}" });
            }
        }
    }
}
