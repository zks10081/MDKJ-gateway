using getway.Model;
using Microsoft.Extensions.Logging;
using RadiantPi.Telnet;
using System.Net.Sockets;
using System.Threading.Channels;

namespace getway.DB.TelnetConnect
{
    internal class TelnetMonitorService : IDisposable
    {
        private TelnetClient _client; // RadiantPi 的客户端
        private TcpClient _rawTcpClient;  // 用于保持低延迟的底层连接
        private ILogger? Logger { get; }
        // 4. 主机IP和端口（请根据你的实际情况赋值，例如从配置文件读取）

        private readonly string _host = "192.168.1.100";
        private readonly int _port = 23;

        // 有界通道，容量100，满了自动丢弃最旧数据，保证实时性
        private readonly Channel<RadiantPiModel> _channel = Channel.CreateBounded<RadiantPiModel>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = true,
                SingleReader = true
            });

        public ChannelReader<RadiantPiModel> Reader => _channel.Reader;

        public TelnetMonitorService(string ip, int port)
        {
            _host = ip;
            _port = port;

            _client = new TelnetClient(ip, port);

            _client.MessageReceived += (sender, args) =>
            {
                var status = new RadiantPiModel
                {
                    Timestamp = DateTime.Now,
                    RawMessage = args.Message,
                    ParsedStatus = ParseMessage(args.Message)
                };
                // 非阻塞写入，满了直接丢弃
                _channel.Writer.TryWrite(status);
            };
        }

        private string ParseMessage(string raw)
        {
            // 根据实际设备协议做解析，这里简单去空白
            return raw.Trim();
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            await _client.ConnectAsync().WaitAsync(ct);

            try
            {
                // RadiantPi 的 TelnetClient 内部封装了一个 TcpClient 对象
                // 我们需要先反射找到它内部的 _client 字段 (类型为 TcpClient)
                var tcpClientField = _client.GetType().GetField("_tcpClient",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (tcpClientField?.GetValue(_client) is System.Net.Sockets.TcpClient tcpClientInstance)
                {
                    // 禁用 TcpClient 级别的 Nagle 算法
                    tcpClientInstance.NoDelay = true;

                    // 继续深入反射，拿到 TcpClient 内部的底层 Socket
                    var socketField = tcpClientInstance.GetType().GetField("m_ConnectedStream",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (socketField?.GetValue(tcpClientInstance) is System.Net.Sockets.NetworkStream networkStream)
                    {
                        // 获取 NetworkStream 绑定的真实 Socket 并禁用 Nagle 算法
                        networkStream.Socket.NoDelay = true;
                    }
                }
                Logger?.LogTrace($"Nagle algorithm disabled for socket [{_host}]:{_port}].");
            }
            catch (Exception reflectionEx)
            {
                // 防止反射在极少数版本更新下失败影响主业务
                Logger?.LogError(reflectionEx, "Failed to disable Nagle algorithm via reflection.");
            }
        }

        public async Task SendCommandAsync(string command, CancellationToken ct = default)
        {
            await _client.SendAsync(command + "\r\n").WaitAsync(ct);
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _client?.Dispose();
        }
    }
}
