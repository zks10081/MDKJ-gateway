using System.IO;
using System.Net.Sockets;
using System.Text;

namespace getway.DB.TelnetConnect
{
    class TcpConnect
    {

        public List<TcpClient> Clients;
        private static Dictionary<string, Dictionary<string, Object>> pool = new Dictionary<string, System.Collections.Generic.Dictionary<string, Object>>();

        private static Dictionary<string, Telnet2> TelnetPool = new Dictionary<string, Telnet2>();

        public static Telnet2? AddTelnet(String key, string host)
        {
            if (TelnetPool.ContainsKey(key))
            {
                Telnet2 exist = TelnetPool[key];
                if (exist != null && exist.Connected) return exist;
                TelnetPool.Remove(key);
            }

            Telnet2 telnet2 = new Telnet2();
            telnet2.Connect(host, 23, "root", "mduadmin");

            // 登录失败不入池，返回 null 让调用方感知
            if (!telnet2.isLogin)
            {
                telnet2.Close();
                return null;
            }

            TelnetPool[key] = telnet2;
            return telnet2;

        }
        public static Telnet2 GetTelnet(String key)
        {
            if (TelnetPool.ContainsKey(key))
            {
                return TelnetPool[key];
            }
            return null;

        }

        public static void CloseTenlet(string key)
        {
            if (TelnetPool.ContainsKey(key)) TelnetPool[key].Close();
        }

        public static void CloseAll(string key)
        {
            foreach (var item in TelnetPool.Keys)
            {
                TelnetPool[item].Close();
            }
        }


        public static async Task<Dictionary<string, object>> Add(String key, string host)
        {
            if (pool.ContainsKey(key)) return pool[key];

            int port = 23;
            CancellationTokenSource _cts = new CancellationTokenSource();

            TcpClient tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(5000, _cts.Token);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                throw new TimeoutException($"连接 {host}:{port} 超时");

            await connectTask; // 确保异常能抛出
            Dictionary<string, Object> poolItem = new Dictionary<string, object>();
            try
            {
                NetworkStream stream = tcp.GetStream();
                StreamWriter writer = new StreamWriter(stream);
                StreamReader reader = new StreamReader(stream);

                poolItem.Add("stream", stream);
                poolItem.Add("writer", writer);
                poolItem.Add("reader", reader);

                pool.Add(key, poolItem);

            }
            catch (Exception)
            {

                throw;
            }

            return poolItem;

        }

        public static Dictionary<string, Object> GetPoolItem(string key)
        {
            if (pool.ContainsKey(key)) return pool[key];
            return null;

        }

        public static StreamWriter GetWriter(string key)
        {
            if (pool.ContainsKey(key))
                return (StreamWriter)pool[key]["writer"];
            return null;
        }
        public static NetworkStream GetStream(string key)
        {
            if (pool.ContainsKey(key))
                return (NetworkStream)pool[key]["stream"];
            return null;
        }
        public static StreamReader GetReader(string key)
        {
            if (pool.ContainsKey(key))
                return (StreamReader)pool[key]["reader"];
            return null;
        }

        /// <summary>
        /// 发送原始字节
        /// </summary>
        public async Task<StreamReader> SendAsync(string key, byte[] data)
        {
            if (pool.ContainsKey(key))
            {
                NetworkStream _stream = (NetworkStream)pool[key]["stream"];
                if (_stream == null) throw new InvalidOperationException("未连接");
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();

                return (StreamReader)pool[key]["reader"];
            }
            return null;
        }

        /// <summary>
        /// 发送字符串（自动转字节）
        /// </summary>
        public Task<StreamReader> SendStringAsync(string key, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.ASCII;
            return SendAsync(key, encoding.GetBytes(text));
        }


        public void Dispose(string key)
        {
            Dictionary<string, Object> poolItem = (Dictionary<string, Object>)pool[key];
            if (poolItem == null) return;

            NetworkStream stream = (NetworkStream)poolItem["stream"];
            StreamReader reader = (StreamReader)poolItem["reader"];
            StreamWriter writer = (StreamWriter)poolItem["writer"];

            stream.Close();
            reader.Close();
            writer.Close();

            pool.Remove(key);
        }

        public void DisposeAll()
        {
            foreach (string key in pool.Keys)
            {
                Dispose(key);
            }
        }

    }
}
