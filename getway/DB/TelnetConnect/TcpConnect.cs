using System.Net.Sockets;

namespace getway.DB.TelnetConnect
{
    class TcpConnect
    {

        public List<TcpClient> Clients;
        private static Dictionary<string, Dictionary<string, Object>> pool = new Dictionary<string, System.Collections.Generic.Dictionary<string, Object>>();

        private static Dictionary<string, Telnet2> TelnetPool = new Dictionary<string, Telnet2>();

        /// <summary>
        /// 建立连接，并加入池内
        /// </summary>
        /// <param name="key"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static Telnet2? AddTelnet(String key, string host, string name, string password)
        {
            if (TelnetPool.ContainsKey(key))
            {
                Telnet2 exist = TelnetPool[key];
                if (exist != null && exist.Connected) return exist;
                TelnetPool.Remove(key);
            }

            Telnet2 telnet2 = new Telnet2();
            telnet2.Connect(host, 23, name, password);

            // 登录失败不入池，返回 null 让调用方感知
            if (!telnet2.isLogin)
            {
                telnet2.Close();
                return null;
            }

            TelnetPool[key] = telnet2;
            return telnet2;

        }
        /// <summary>
        /// 在连接池获取Telnet连接
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Telnet2 GetTelnet(String key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                return TelnetPool[key];
            }
            return null;

        }

        /// <summary>
        /// 根据key获得对应连接的结果
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Receive(String key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                return TelnetPool[key].Receive();
            }
            return String.Empty;
        }

        public static void CloseTelnetByIp(string ip)
        {
            foreach (string key in TelnetPool.Keys)
            {
                if (key.Contains(ip))
                {
                    TelnetPool[key].Close();
                }
            }
        }

        /// <summary>
        /// 根据key删除对于telnet连接
        /// </summary>
        /// <param name="key"></param>
        public static void CloseTenlet(string key)
        {
            if (TelnetPool.ContainsKey(key)) TelnetPool[key].Close();
        }

        /// <summary>
        /// 删除连接池所有连接
        /// </summary>
        /// <param name="key"></param>
        public static void CloseAll()
        {
            foreach (var item in TelnetPool.Keys)
            {
                TelnetPool[item].Close();
            }
        }




    }
}
