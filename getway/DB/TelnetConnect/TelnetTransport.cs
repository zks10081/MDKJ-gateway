using System.Net.Sockets;
using System.Text;

namespace getway.DB.TelnetConnect
{
    class TelnetTransport
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private byte[] _readBuffer = new byte[4096];

        // 事件：收到原始数据
        public event Action<byte[], int> DataReceived;
        // 事件：连接断开
        public event Action Disconnected;

        public bool IsConnected => _client?.Connected ?? false;

        /// <summary>
        /// 异步连接到 Telnet 服务器
        /// </summary>
        public async Task ConnectAsync(string host, int port = 23, int timeoutMs = 5000)
        {
            _cts = new CancellationTokenSource();
            _client = new TcpClient();

            // 带超时的连接
            var connectTask = _client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeoutMs, _cts.Token);

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                throw new TimeoutException($"连接 {host}:{port} 超时");

            await connectTask; // 确保异常能抛出
            _stream = _client.GetStream();
            _stream.ReadTimeout = 10000;
            _stream.WriteTimeout = 10000;

            // 启动后台监听循环
            _ = ListenLoopAsync();
        }

        /// <summary>
        /// 后台持续监听服务端返回的数据
        /// </summary>
        private async Task ListenLoopAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested && _stream != null)
                {
                    int bytesRead = await _stream.ReadAsync(
                        _readBuffer, 0, _readBuffer.Length, _cts.Token);

                    if (bytesRead > 0)
                        DataReceived?.Invoke(_readBuffer, bytesRead);
                    else
                        break; // 连接关闭
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"监听异常: {ex.Message}");
            }
            finally
            {
                Disconnected?.Invoke();
            }
        }

        /// <summary>
        /// 发送原始字节
        /// </summary>
        public async Task SendAsync(byte[] data)
        {
            if (_stream == null) throw new InvalidOperationException("未连接");
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        /// <summary>
        /// 发送字符串（自动转字节）
        /// </summary>
        public Task SendStringAsync(string text, Encoding encoding = null)
        {
            encoding ??= Encoding.ASCII;
            return SendAsync(encoding.GetBytes(text));
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _stream?.Dispose();
            _client?.Close();
        }
    }
}
