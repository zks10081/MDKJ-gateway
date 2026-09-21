using System.Text;

namespace getway.DB.TelnetConnect
{
    class TelnetProtocol
    {
        // Telnet 控制字节常量
        private const byte IAC = 0xFF; // Interpret As Command
        private const byte DO = 0xFD;
        private const byte DONT = 0xFE;
        private const byte WILL = 0xFB;
        private const byte WONT = 0xFC;
        private const byte SB = 0xFA; // Subnegotiation Begin
        private const byte SE = 0xF0; // Subnegotiation End

        // 常见选项码
        private const byte OPT_ECHO = 0x01;
        private const byte OPT_SGA = 0x03; // Suppress Go Ahead
        private const byte OPT_TERM_TYPE = 0x18;
        private const byte OPT_NAWS = 0x1F; // Negotiate About Window Size

        private readonly TelnetTransport _transport;
        private readonly Encoding _encoding;
        private readonly List<byte> _pendingData = new List<byte>();

        // 事件：解析后的纯文本数据（已去除 Telnet 控制字节）
        public event Action<string> TextReceived;

        public TelnetProtocol(TelnetTransport transport, Encoding encoding = null)
        {
            _transport = transport;
            _encoding = encoding ?? Encoding.ASCII;
            _transport.DataReceived += OnRawData;
        }

        /// <summary>
        /// 收到原始字节后，解析 Telnet 控制序列
        /// </summary>
        private void OnRawData(byte[] buffer, int count)
        {
            for (int i = 0; i < count; i++)
            {
                byte b = buffer[i];

                if (b == IAC)
                {
                    // 进入控制序列解析
                    if (i + 1 < count)
                    {
                        byte cmd = buffer[i + 1];

                        if (cmd == IAC)
                        {
                            // 数据中的 0xFF 需要双写转义
                            _pendingData.Add(IAC);
                            i++; // 跳过第二个 0xFF
                        }
                        else if (cmd == DO || cmd == DONT || cmd == WILL || cmd == WONT)
                        {
                            if (i + 2 < count)
                            {
                                byte option = buffer[i + 2];
                                HandleNegotiation(cmd, option);
                                i += 2; // 跳过 cmd + option
                            }
                        }
                        else if (cmd == SB)
                        {
                            // 子协商：跳过直到 SE
                            i += 2;
                            while (i < count && buffer[i] != SE) i++;
                        }
                    }
                }
                else
                {
                    _pendingData.Add(b);
                }
            }

            // 将累积的纯文本数据抛出
            if (_pendingData.Count > 0)
            {
                string text = _encoding.GetString(_pendingData.ToArray());
                _pendingData.Clear();
                TextReceived?.Invoke(text);
            }
        }

        /// <summary>
        /// 处理选项协商：自动响应 DO/WILL 等
        /// </summary>
        private async void HandleNegotiation(byte cmd, byte option)
        {
            byte[] response;

            switch (cmd)
            {
                case DO:   // 服务器请求我们启用某选项
                    response = new byte[] { IAC, WILL, option };
                    break;
                case DONT: // 服务器请求我们禁用某选项
                    response = new byte[] { IAC, WONT, option };
                    break;
                case WILL: // 服务器声明它要启用某选项
                    response = new byte[] { IAC, DO, option };
                    break;
                case WONT: // 服务器声明它要禁用某选项
                    response = new byte[] { IAC, DONT, option };
                    break;
                default:
                    return;
            }

            await _transport.SendAsync(response);
        }

        /// <summary>
        /// 发送命令（自动追加 \r\n）
        /// </summary>
        public Task SendCommandAsync(string command)
        {
            return _transport.SendStringAsync(command + "\r\n", _encoding);
        }
    }
}
