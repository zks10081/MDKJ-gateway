using getway.Base;
using getway.Util;
using System.Net.Sockets;

namespace getway.DB.TelnetConnect
{
    class TcpConnect
    {

        public List<TcpClient> Clients;
        private static Dictionary<string, Dictionary<string, Object>> pool = new Dictionary<string, System.Collections.Generic.Dictionary<string, Object>>();

        public static Dictionary<string, Telnet2> TelnetPool = new Dictionary<string, Telnet2>();

        public static LogTlenet log = new LogTlenet(DefaulConfig.TelnetLogPath, "telnet");

        /// <summary>
        /// 建立连接，并加入池内
        /// </summary>
        /// <param name="key"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public static Telnet2? AddTelnet(String key, string host, string name, string password)
        {
            return AddTelnet(key, host, name, password, false);
        }
        public static Telnet2? AddTelnet(String key, string host, string name, string password, bool isHeart)
        {
            if (TelnetPool.ContainsKey(key))
            {
                Telnet2 exist = TelnetPool[key];
                if (exist != null && exist.Connected && exist.IsConnected) return exist;
                TelnetPool.Remove(key);
            }

            Telnet2 telnet2 = new Telnet2(log);
            telnet2.Connect(host, 23, name, password, isHeart);

            // 登录失败不入池，返回 null 让调用方感知
            if (!telnet2.IsConnected)
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
            // 连接不存在或已断开时直接给空串，交给调用方按"没有数据"处理
            if (TelnetPool.TryGetValue(key, out Telnet2? telnet) && telnet != null && telnet.Connected)
            {
                return telnet.Receive();
            }
            return String.Empty;
        }
        public static string Receive(String key, string terminator, int timeoutMs)
        {
            // 连接不存在或已断开时直接给空串，交给调用方按"没有数据"处理
            if (TelnetPool.TryGetValue(key, out Telnet2? telnet) && telnet != null && telnet.Connected)
            {
                return telnet.Receive(terminator, timeoutMs);
            }
            return String.Empty;
        }
        public static string Receive2(String key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                return TelnetPool[key].Receive2();
            }
            return String.Empty;
        }

        public static void ClearReceiveBuffer(String key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                TelnetPool[key].ClearReceiveBuffer();
            }
        }
        /// <summary>
        /// 连接占用状态
        /// </summary>
        /// <param name="key"></param>
        /// <param name="status">1：占用，0：未占用</param>
        public static void UpdateTake(String key, int status)
        {
            if (TelnetPool.ContainsKey(key))
            {
                TelnetPool[key].UpdateTake(status);
            }
        }

        public static void CloseTelnetByIp(string ip)
        {
            foreach (string key in TelnetPool.Keys.ToList())
            {
                if (key.Contains(ip))
                {
                    TelnetPool[key].Close();
                    TelnetPool.Remove(key);
                }
            }
        }



        /// <summary>
        /// 根据key删除对于telnet连接
        /// </summary>
        /// <param name="key"></param>
        public static void CloseTenlet(string key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                TelnetPool[key].Close();
                TelnetPool.Remove(key);
            }
        }

        /// <summary>
        /// 删除连接池所有连接
        /// </summary>
        /// <param name="key"></param>
        public static async Task CloseAll()
        {
            foreach (var item in TelnetPool.Keys)
            {
                if (TelnetPool[item].HeartbeatTaskStatus())
                {
                    await TelnetPool[item].DisposeAsync();
                }
                else
                {
                    TelnetPool[item].Close();
                }
            }
            TelnetPool.Clear();
        }

        public static void AppendLog(string key, string text)
        {
            if (TelnetPool.ContainsKey(key))
            {
                TelnetPool[key].AppendLog(text);
            }

        }




    }
}
