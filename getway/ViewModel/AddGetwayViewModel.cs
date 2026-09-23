using DMGatewayDemo.Util;
using getway.DB.Pg;
using getway.Model;
using getway.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace getway.ViewModel
{
    class AddGetwayViewModel : ViewModelBase
    {
        private string _GetwayIp;

        ObservableCollection<string> _NowGatewayList;


        public string GetwayIp { get => _GetwayIp; set => SetProperty(ref _GetwayIp, value); }

        public AddGetwayViewModel(ObservableCollection<string> list)
        {
            _NowGatewayList = list;
        }

        public ICommand AddGetWayCommand => new ViewModelCommand(param =>
        {
            if (!ConfigUtil.IsIP(_GetwayIp) || String.IsNullOrEmpty(_GetwayIp))
            {
                GetWayModel model = new GetWayModel();

            }
            foreach (var item in _NowGatewayList)
            {
                if (item == _GetwayIp)
                {
                    //DMMessageBox.ShowWaring("IP 地址为 " + InputGatewayIP + " 的网关已存在");
                    return;
                }
            }

            GetWayDB.AddGatewayModel(new GetWayModel
            {
                IP = _GetwayIp,
                FrameId = DefaulConfig.FrameId,
            });
            _NowGatewayList.Add(_GetwayIp);

            Window window = (Window)param;
            window.Close();

        });


        // 取消按钮
        public ICommand ColseGetWay => new ViewModelCommand(param =>
        {
            Window window = (Window)param;
            window.Close();
        });

    }
}
