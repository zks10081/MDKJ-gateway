using System.IO;
using System.Net.Sockets;
using System.Text;

namespace getway.DB.TelnetConnect
{
    internal sealed class TelnetRDP : IDisposable
    {
        // ── 事件 ──────────────────────────────────────────────────────────────────
        public event Action<string>? DataReceived;
        public event Action? Connected;
        public event Action? Disconnected;

        // ── Telnet ────────────────────────────────────────────────────────────────
        private TcpClient? _telnetClient;
        private Stream? _telnetStream;
        private SocksTunnelConnection? _telnetTunnel;

        // 公共状态
        private CancellationTokenSource? _readCts;
        private bool _disposed;
        private int _terminalCols = 80;
        private int _terminalRows = 24;

        public bool IsConnected { get; private set; }


        /// <summary>连接超时（秒），默认 15 s。</summary>
        public int ConnectTimeoutSeconds { get; set; } = 15;

        // 自动登录凭据
        private string _telnetUsername = "";
        private string _telnetPassword = "";
        private readonly TelnetProtocolHandler _telnetProtocol = new();

        internal async Task ConnectTelnetAsync(ServerInfo server)
            => ConnectTelnetAsync(server.IP, server.Port, server.Name, server.Password);
        public async Task ConnectTelnetAsync(string ip, int prot, string username, string pasaword)
        {
            _readCts = new CancellationTokenSource();

            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(ConnectTimeoutSeconds));

            _telnetUsername = username ?? "";
            _telnetPassword = pasaword ?? "";
            _telnetProtocol.Reset();

            if (ip.Length < 0)
            {
                _telnetTunnel = await Socks5Helper.ConnectAsync(ip, prot, username, pasaword, timeoutCts.Token)
                    .ConfigureAwait(false);
                _telnetClient = _telnetTunnel.Client;
                _telnetStream = _telnetTunnel.Stream;
            }
            else
            {
                _telnetClient = new TcpClient();
                await _telnetClient.ConnectAsync(ip, prot, timeoutCts.Token).ConfigureAwait(false);
                Socks5Helper.ConfigureTcpClient(_telnetClient);
                _telnetStream = _telnetClient.GetStream();
            }

            IsConnected = true;
            Connected?.Invoke();

            _ = Task.Run(() => ReadTelnetLoop(_readCts!.Token));
        }


        private void ReadTelnetLoop(CancellationToken ct)
        {
            var buffer = new byte[4096];
            var promptBuf = new System.Text.StringBuilder(512);

            try
            {
                while (!ct.IsCancellationRequested && _telnetStream != null && IsConnected)
                {
                    int count = _telnetStream.Read(buffer, 0, buffer.Length);
                    if (count <= 0) break;
                    // 增量处理 Telnet IAC 协商，避免 TCP 分片导致协商状态丢失
                    var (printable, response) = _telnetProtocol.Process(buffer, count);

                    if (response.Count > 0)
                    {
                        var resp = response.ToArray();
                        _telnetStream.Write(resp, 0, resp.Length);
                    }
                    if (_telnetProtocol.ConsumeWindowSizeRequest())
                    {
                        SendTelnetWindowSize();
                    }

                    if (printable.Count > 0)
                    {
                        var data = Encoding.UTF8.GetString(printable.ToArray());
                        DataReceived?.Invoke(data);
                    }


                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                //AppLogger.Error("telnet read error", ex);
            }
            finally
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    Disconnected?.Invoke();
                }
            }
        }

        public void SendInput(string data)
        {
            if (!IsConnected) return;

            try
            {
                if (_telnetStream != null)
                {
                    // 将 \x7f (DEL/xterm 默认退格) 映射为 \x08 (BS/Ctrl+H)，
                    // 确保 Cisco/Huawei 等网络设备无论经过何种路径（直连或 SOCKS 跳转）
                    // 都能正确识别退格键（许多设备 VTY 线路只接受 \x08）。
                    data = data.Replace('\x7f', '\x08');
                    var bytes = Encoding.UTF8.GetBytes(data);
                    _telnetStream.Write(bytes, 0, bytes.Length);
                    _telnetStream.Flush();
                }
            }
            catch (Exception ex)
            {
                //AppLogger.Error("terminal send input error", ex);
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void SendTelnetWindowSize()
        {
            if (_telnetStream == null || !_telnetProtocol.CanSendWindowSize)
                return;

            try
            {
                var bytes = TelnetProtocolHandler.CreateWindowSizeCommand(_terminalCols, _terminalRows);
                _telnetStream.Write(bytes, 0, bytes.Length);
                _telnetStream.Flush();
            }
            catch (Exception ex)
            {
                //AppLogger.Warn($"telnet resize ignored: {ex.Message}");
            }
        }

        private sealed class TelnetProtocolHandler
        {
            private const byte IAC = 0xFF;
            private const byte WILL = 0xFB;
            private const byte WONT = 0xFC;
            private const byte DO = 0xFD;
            private const byte DONT = 0xFE;
            private const byte SB = 0xFA;
            private const byte SE = 0xF0;
            private const byte SGA = 0x03;
            private const byte ECHO = 0x01;
            private const byte NAWS = 0x1F;

            private State _state = State.Data;
            private byte _pendingCommand;
            private bool _windowSizePending;

            public bool CanSendWindowSize { get; private set; }

            private enum State
            {
                Data,
                SawIac,
                NeedOption,
                SubNegotiation,
                SubNegotiationSawIac
            }

            public void Reset()
            {
                _state = State.Data;
                _pendingCommand = 0;
                _windowSizePending = false;
                CanSendWindowSize = false;
            }

            public (List<byte> printable, List<byte> response) Process(byte[] buf, int count)
            {
                var printable = new List<byte>(count);
                var response = new List<byte>();

                for (int i = 0; i < count; i++)
                {
                    byte b = buf[i];
                    switch (_state)
                    {
                        case State.Data:
                            if (b == IAC)
                                _state = State.SawIac;
                            else
                                printable.Add(b);
                            break;

                        case State.SawIac:
                            if (b == IAC)
                            {
                                printable.Add(IAC);
                                _state = State.Data;
                            }
                            else if (b is WILL or WONT or DO or DONT)
                            {
                                _pendingCommand = b;
                                _state = State.NeedOption;
                            }
                            else if (b == SB)
                            {
                                _state = State.SubNegotiation;
                            }
                            else
                            {
                                _state = State.Data;
                            }
                            break;

                        case State.NeedOption:
                            AppendNegotiationReply(response, _pendingCommand, b);
                            _pendingCommand = 0;
                            _state = State.Data;
                            break;

                        case State.SubNegotiation:
                            if (b == IAC)
                                _state = State.SubNegotiationSawIac;
                            break;

                        case State.SubNegotiationSawIac:
                            _state = b == SE ? State.Data : State.SubNegotiation;
                            break;
                    }
                }

                return (printable, response);
            }

            public bool ConsumeWindowSizeRequest()
            {
                if (!_windowSizePending)
                    return false;
                _windowSizePending = false;
                return true;
            }

            public static byte[] CreateWindowSizeCommand(int cols, int rows)
            {
                cols = Math.Clamp(cols, 1, 65535);
                rows = Math.Clamp(rows, 1, 65535);

                var bytes = new List<byte>(9) { IAC, SB, NAWS };
                AppendEscapedByte(bytes, (byte)(cols >> 8));
                AppendEscapedByte(bytes, (byte)cols);
                AppendEscapedByte(bytes, (byte)(rows >> 8));
                AppendEscapedByte(bytes, (byte)rows);
                bytes.Add(IAC);
                bytes.Add(SE);
                return bytes.ToArray();
            }

            private void AppendNegotiationReply(List<byte> response, byte command, byte option)
            {
                switch (command)
                {
                    case WILL:
                        response.AddRange(option is ECHO or SGA ? new byte[] { IAC, DO, option } : new byte[] { IAC, DONT, option });
                        break;
                    case DO:
                        if (option == SGA || option == NAWS)
                        {
                            response.AddRange(new byte[] { IAC, WILL, option });
                            if (option == NAWS)
                            {
                                CanSendWindowSize = true;
                                _windowSizePending = true;
                            }
                        }
                        else
                        {
                            response.AddRange(new byte[] { IAC, WONT, option });
                        }
                        break;
                    case WONT:
                    case DONT:
                        if (option == NAWS)
                        {
                            CanSendWindowSize = false;
                            _windowSizePending = false;
                        }
                        break;
                }
            }

            private static void AppendEscapedByte(List<byte> buffer, byte value)
            {
                buffer.Add(value);
                if (value == IAC)
                    buffer.Add(IAC);
            }
        }
    }



}
