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
                result = Receive();
                if (result.EndsWith(PasswordPrompt))
                {
                    Send(password, waitTime);
                    result = Receive();
                    if (result.EndsWith(DefaulConfig.Success_Flag))
                    {
                        isLogin = true;
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
        /// 接收
        /// </summary>
        /// <returns>传回的数据</returns>
        public string Receive(int waitMs = 1500)
        {
            StringBuilder result = new StringBuilder();
            if (Connected && ns != null && ns.CanRead)
            {
                // 先给设备一点时间把数据吐出来，超时后不再干等
                int waited = 0;
                while (!ns.DataAvailable && waited < waitMs)
                {
                    Thread.Sleep(50);
                    waited += 50;
                }

                if (ns.DataAvailable)
                {
                    byte[] buff = new byte[BuffSize];
                    int numberOfRead;
                    do
                    {
                        try
                        {
                            numberOfRead = ns.Read(buff, 0, BuffSize);
                        }
                        catch (IOException)
                        {
                            // 读超时：返回已经读到的部分
                            break;
                        }

                        // 0 表示对端已关闭连接
                        if (numberOfRead <= 0) break;

                        result.Append(Encoding.ASCII.GetString(buff, 0, numberOfRead));
                    } while (ns.DataAvailable);
                }
            }
            string info = result.ToString().Trim();
            //处理一些情况
            ResultCheck(result);

            return result.ToString().Trim();
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
