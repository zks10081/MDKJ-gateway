using getway.Util;
using System.Diagnostics;

namespace getway.DB.TelnetConnect
{
    internal class Terimanl
    {

        private static Dictionary<string, TelnetRDP> TelnetPool = new Dictionary<string, TelnetRDP>();

        /// <summary>
        /// 连接认证
        /// </summary>
        /// <param name="server"></param>
        /// <param name="forceNew">是否强制刷新</param>
        /// <returns></returns>
        internal async Task<SocksTunnelConnection> ConnectTerminalAsync(ServerInfo server, bool forceNew = false)
        {
            //不强制刷新，并能从连接池拿到，则直接返回
            if (!forceNew && TryGetSessionForServer(server, out var key, out var telnet) && telnet != null)
            {

                return null;
            }

            telnet = new TelnetRDP();
            key = server.GetKey();
            telnet.ConnectTimeoutSeconds = DefaulConfig.ConnectTimeoutSeconds;


            SocksProxyEntry? socks = null;

            try
            {
                await telnet.ConnectTelnetAsync(server);
                TelnetPool.Add(key, telnet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("terminal connect failed", ex);
                Debug.WriteLine($"连接失败：{key}\n\n{ex.Message}", "连接错误");
            }



        }

        public bool TryGetSessionForServer(ServerInfo server, out string key, out TelnetRDP telnet)
        {
            string index = server.GetKey() ?? string.Empty;
            foreach (var kv in TelnetPool.OrderBy(x => x.Key))
            {
                if (kv.Key == index)
                {
                    key = kv.Key;
                    telnet = kv.Value;
                    return true;
                }
            }
            key = string.Empty;
            telnet = null;
            return false;
        }

    }

    public enum ServerHealthState { Unknown, Checking, Online, Offline }

    public enum ServerProtocol { RDP, SSH, Telnet }

    internal class ServerInfo : ViewModelBase
    {   // 主键，不参与绑定通知
        public int Id { get; set; }

        private string _name = "";
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private string _ip = "";
        public string IP { get => _ip; set => SetProperty(ref _ip, value); }

        private string _username = "";
        public string Username { get => _username; set => SetProperty(ref _username, value); }

        // 密码不需要 INPC（不直接绑定到 UI）
        public string Password { get; set; } = "";

        private int _port = 3389;
        public int Port
        {
            get => _port;
            set
            {
                if (_port == value) return;
                _port = value;
            }
        }

        public string GetKey()
        {
            return IP + Name;
        }

    }

    internal class SocksProxyEntry
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");

        /// <summary>显示名称，用于区分机房（如：北京机房、上海机房）</summary>
        public string Name { get; set; } = "";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 1080;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseTls { get; set; }
        public string TlsServerName { get; set; } = "";
        public string TlsPinnedSha256 { get; set; } = "";

        public string DisplayName => UseTls ? $"{Name} (TLS)" : Name;

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = System.Guid.NewGuid().ToString("N");
        }
    }

}
