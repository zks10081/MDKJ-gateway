using getway.Base;
using getway.DB.TelnetConnect;
using getway.Model;
using getway.Util;
using System.Collections.Generic;
using System.Linq;
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
            get => _SelectBorder; set
            {
                if (_SelectBorder != value) // 值检查
                {
                    _SelectBorder = value;
                    OnPropertyChanged(nameof(SelectBorder));
                    if (_SelectBorder != null)
                    {
                        initIpcList();
                    }
                }
            }
        }

        // telnet 连接标识，由父 ViewModel（GetWayViewMode）在连接建立后注入
        public string Key { get; private set; } = string.Empty;

        public ICommand SelectBorderCommand { get; }

        public IpcItemViewModel()
        {
            // 构造时只做最小化初始化，不取数据：此时父 ViewModel 还没建立连接
            SelectBorderCommand = new Command(SelectBorderExecute);
        }

        /// <summary>
        /// 由父 ViewModel 在 telnet 连接成功后调用，保证子 ViewModel 不会先于父 ViewModel 取数据
        /// </summary>
        public void SetKey(string key)
        {
            Key = key ?? string.Empty;
            initBorderList();   // 内部给 SelectBorder 赋值时会自动刷新 SIP 用户列表
            if (_SelectBorder == null)
            {
                initIpcList();
            }
        }

        //获取sip用户注册数据
        public void initIpcList()
        {
            if (string.IsNullOrEmpty(Key) || _SelectBorder == null)
            {
                IpcUserList = new List<IpcUserModel>();
                return;
            }

            IpcUserList = TelnetEvent.QuerySipUser(Key, 0, _SelectBorder.SlotNo) ?? new List<IpcUserModel>();
        }

        //获取卡框板槽信息
        public void initBorderList()
        {
            if (string.IsNullOrEmpty(Key))
            {
                BorderList = new List<BorderModel>();
                SelectBorder = null;
                return;
            }

            BorderList = TelnetEvent.QueryBoard(Key, 0) ?? new List<BorderModel>();
            SelectBorder = BorderList.FirstOrDefault();
        }

        //选择板卡运行方法
        private void SelectBorderExecute(object parameter)
        {
            if (parameter is BorderModel border)
            {
                SelectBorder = border;
            }
        }
    }
}
