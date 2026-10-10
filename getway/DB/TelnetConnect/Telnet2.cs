using getway.Util;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace getway.DB.TelnetConnect
{
    class Telnet2
    {
        private TcpClient Client;
        private NetworkStream ns;
        private string m_LogonPrompt = "User name:";
        private string m_PasswordPrompt = "User password:";
        private string m_SystemTimeTip = "Are you sure to modify system time? (y/n)[n]:";
        private readonly int BuffSize = 1024 * 4;
        private CancellationTokenSource _cts;

        public bool isLogin { get; private set; }

        /// <summary>
        /// 登录输入用户名提示字符
        /// 默认值：ogin:
        /// </summary>
        public string LoginPrompt
        {
            set { m_LogonPrompt = value; }
            get { return m_LogonPrompt; }
        }
        /// <summary>
        /// 登录输入密码提示字符
        /// 默认值：assword:
        /// </summary>
        public string PasswordPrompt
        {
            set { m_PasswordPrompt = value; }
            get { return m_PasswordPrompt; }
        }
        /// <summary>
        /// 连接状态
        /// </summary>
        public bool Connected
        {
            get { return Client != null ? Client.Connected : false; }
        }

        /// <summary>
        /// 连接远端主机
        /// </summary>
        /// <param name="hostname">主机名称</param>
        /// <param name="port">端口</param>
        /// <returns>远端主机响应文本</returns>
        public string Connect(string hostname, int port)
        {
            string result = string.Empty;
            try
            {
                Client = new TcpClient();
                // 同步 Read/Write 的兜底超时，防止对端不发数据时 ns.Read 永久阻塞
                Client.ReceiveTimeout = 5000;
                Client.SendTimeout = 5000;
                // 同步等待连接结果，最多 3 秒，避免界面长时间假死
                var connectTask = Client.ConnectAsync(hostname, port);
                if (!connectTask.Wait(3000))
                {
                    throw new TimeoutException($"连接 {hostname}:{port} 超时");
                }

                ns = Client.GetStream();
                result = Negotiate();
            }
            catch (Exception e)
            {
                result = e.InnerException?.Message ?? e.Message;
            }

            return result;
        }
        /// <summary>
        /// 连接远端主机并登录
        /// </summary>
        /// <param name="hostname">主机名称</param>
        /// <param name="port">端口</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="waitTime">连接等待时间</param>
        /// <returns></returns>
        public string Connect(string hostname, int port, string username, string password, int waitTime = 300)
        {
            string result = string.Empty;
            //未连通则先建立 TCP 连接并完成 telnet 协商（拿到 "User name:" 提示）
            if (!Connected)
            {
                result = Connect(hostname, port);
            }


            if (Connected && result.EndsWith(LoginPrompt))
            {
                Send(username, waitTime);
                // 登录响应要给足时间，用比默认更长的读取窗口
                result = Receive(5000);
                if (result.EndsWith(PasswordPrompt))
                {
                    Send(password, waitTime);
                    result = Receive(5000);
                    if (result.EndsWith(DefaulConfig.Success_Flag))
                    {
                        isLogin = true;
                        Send("scroll 120", waitTime);
                    }
                    else
                    {
                        Client.Close();
                        result = "Logon Error";
                    }
                }
            }
            return result;
        }

        //处理特殊结果
        public void ResultCheck(StringBuilder info)
        {
            string result = info.ToString().Trim();
            int waitTime = DefaulConfig.Telnet_waitTime;
            while (true)
            {
                if (result.EndsWith("---- More ( Press 'Q' to break ) ----"))
                {
                    Send(" ", waitTime);
                    result = Receive();
                    info.AppendLine(result);
                }
                else if (result.EndsWith(m_SystemTimeTip))
                {
                    Send("y", waitTime);
                    result = Receive();
                    info.AppendLine(result);
                }
                else
                {
                    break;
                }
            }

        }
        /// <summary>
        /// 清空 NetworkStream 接收缓冲区中残留的旧数据
        /// </summary>
        public void ClearReceiveBuffer()
        {
            if (Client == null)
                return;
            if (ns == null || !ns.CanRead)
                return;

            byte[] dump = new byte[4096];

            // 设置一个很短的读取超时，防止最后一点数据读不到时无限阻塞
            int oldTimeout = ns.ReadTimeout;
            ns.ReadTimeout = 100;

            try
            {
                while (Client.Available > 0)
                {
                    int count = ns.Read(dump, 0, dump.Length);
                    if (count <= 0)
                        break;
                }
            }
            catch (IOException)
            {
                // 超时或连接异常时停止清理
            }
            finally
            {
                ns.ReadTimeout = oldTimeout;
            }
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <returns>传回的数据</returns>
        /// <summary>
        /// 接收设备回显。
        /// 约定：设备在 waitMs 内没有返回任何数据时返回 string.Empty（表示"这一轮没数据"），
        /// 而不是一直阻塞到抛超时异常——调用方要靠空串判断"查不到东西就退出"。
        /// </summary>
        /// <param name="waitMs">本次读取的等待上限（毫秒），第一次读超时即视为无数据</param>
        public string Receive(int waitMs = 1500)
        {
            StringBuilder result = new StringBuilder();
            if (Connected && ns != null && ns.CanRead)
            {
                byte[] buff = new byte[BuffSize];
                try
                {
                    // 关键：给本次读取设一个超时，读不到就不再等
                    ns.ReadTimeout = waitMs;
                    do
                    {
                        int numberOfRead = ns.Read(buff, 0, BuffSize);
                        // 读到 0 字节说明对端已关闭连接
                        if (numberOfRead == 0) break;
                        result.AppendFormat("{0}", Encoding.ASCII.GetString(buff, 0, numberOfRead));
                    } while (ns.DataAvailable);
                }
                catch (IOException)
                {
                    // 读取超时或连接被关闭：按"本次没有数据"处理
                }
                catch (ObjectDisposedException)
                {
                    // 连接已释放：同上
                }
            }

            string info = result.ToString().Trim();
            // 只有确实读到内容才去处理分页/系统时间提示，避免空内容时又空等一轮
            if (info.Length > 0)
            {
                ResultCheck(result);
            }

            return result.ToString().Trim();
        }

        public string Receive2(int waitMs = 1500)
        {
            StringBuilder result = new StringBuilder();
            string line = string.Empty;
            if (Connected && ns != null && ns.CanRead)
            {
                // 设置读取超时，单位毫秒，例如 3 秒
                ns.ReadTimeout = 3000;
                using var reader = new StreamReader(ns, Encoding.UTF8, leaveOpen: true);
                if (reader == null) return "";
                int lineCount = 0;
                while (true)
                {
                    if (reader == null) break;
                    line = reader.ReadLine();
                    if (line == null) break;

                    result.AppendLine(line);
                } while (true) ;
            }
            string info = result.ToString().Trim();
            //处理一些情况
            ResultCheck(result);

            return result.ToString().Trim();
        }

        public string Receive(string terminator, int timeoutMs)
        {
            ns.ReadTimeout = timeoutMs;
            ClearReceiveBuffer();

            byte[] buffer = new byte[1024];
            var received = new StringBuilder();
            int maxBytes = 1024 * 100; // 防止死循环

            try
            {
                while (received.Length < maxBytes)
                {
                    int bytesRead = ns.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    received.Append(chunk);

                    int index = received.ToString().IndexOf(terminator);
                    if (index >= 0)
                    {
                        // 返回终止符及之前的所有内容
                        return received.ToString(0, index + terminator.Length);
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                // 超时或对端无响应
                // 返回已收到的部分，或者抛出自定义异常
            }

            string result = received.ToString();
            if (string.IsNullOrEmpty(result))
                throw new TimeoutException($"读取超时，未收到终止符 '{terminator}'");

            //处理一些情况
            ResultCheck(received);

            return received.ToString().Trim();
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="cmd">指令</param>
        /// <param name="waitTime">等待超时时间（毫秒）</param>
        public void Send(string cmd, int waitTime = 100)
        {
            if (Connected && ns.CanWrite)
            {
                cmd += "\r\n";
                byte[] buff = Encoding.ASCII.GetBytes(cmd);
                ns.Write(buff, 0, buff.Length);
                System.Threading.Thread.Sleep(waitTime);
            }
        }

        public async Task SendAsync(string message, int waitTime = 100)
        {
            var data = Encoding.UTF8.GetBytes(message);

            // 先发 4 字节长度头（网络字节序/大端）
            var lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            await ns.WriteAsync(lengthPrefix, 0, 4);

            // 再发数据体
            await ns.WriteAsync(data, 0, data.Length);
        }

        /// <summary>
        /// 精确读取指定字节数（解决拆包问题）
        /// </summary>
        private async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new Exception("连接已关闭");
                totalRead += read;
            }
        }

        /// <summary>
        /// 接收一条完整消息
        /// </summary>
        public async Task<string> ReceiveAsync()
        {

            // 第一步：读 4 字节长度头
            var lengthBuffer = new byte[4];
            await ReadExactAsync(ns, lengthBuffer, 0, 4);
            int messageLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));

            // 第二步：精确读取消息体
            var dataBuffer = new byte[messageLength];
            await ReadExactAsync(ns, dataBuffer, 0, messageLength);

            return Encoding.UTF8.GetString(dataBuffer);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            if (Client != null)
            {
                Client.Close();
            }
        }

        /// <summary>
        /// 处理Telnet选项谈判，直到出现用户名输入提示
        /// </summary>
        /// <returns></returns>
        private string Negotiate()
        {
            string result = string.Empty;
            if (Connected)
            {
                while (true)
                {
                    byte[] rev = ReceiveBytes();
                    // 读不到任何数据说明对端已关闭，避免在这里死循环
                    if (rev.Length == 0) throw new Exception("未收到登录提示，连接已关闭");
                    result = Encoding.ASCII.GetString(rev).Trim();
                    if (result.EndsWith(LoginPrompt))
                    {
                        break;
                    }
                    int count = rev.Length / 3;
                    for (int i = 0; i < count; i++)
                    {
                        int iac = rev[i * 3];
                        int cmd = rev[i * 3 + 1];
                        int value = rev[i * 3 + 2];
                        if (((int)Verbs.IAC) != iac)
                        {
                            continue;
                        }
                        switch (cmd)
                        {
                            case (int)Verbs.DO:
                                ns.WriteByte((byte)iac);
                                ns.WriteByte(value == (int)Options.RD ? (byte)Verbs.WILL : (byte)Verbs.WONT);
                                ns.WriteByte((byte)value);
                                break;
                            case (int)Verbs.DONT:
                                ns.WriteByte((byte)iac);
                                ns.WriteByte((byte)Verbs.WONT);
                                ns.WriteByte((byte)value);
                                break;
                            case (int)Verbs.WILL:
                                ns.WriteByte((byte)iac);
                                ns.WriteByte(value == (int)Options.SGA ? (byte)Verbs.DO : (byte)Verbs.DONT);
                                ns.WriteByte((byte)value);
                                break;
                            case (int)Verbs.WONT:
                                ns.WriteByte((byte)iac);
                                ns.WriteByte((byte)Verbs.DONT);
                                ns.WriteByte((byte)value);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 读取字节流
        /// </summary>
        /// <returns></returns>
        private byte[] ReceiveBytes()
        {
            byte[] result = new byte[0];
            byte[] buff = new byte[BuffSize];
            int numberOfRead = 0;
            if (Connected && ns.CanRead)
            {
                numberOfRead = ns.Read(buff, 0, BuffSize);
                result = new byte[numberOfRead];
                Array.Copy(buff, result, numberOfRead);
            }
            return result;
        }

    }


}
