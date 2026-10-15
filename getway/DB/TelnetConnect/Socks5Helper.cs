using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace getway.DB.TelnetConnect
{
    internal sealed class SocksTunnelConnection : IDisposable, IAsyncDisposable
    {

        public SocksTunnelConnection(TcpClient client, Stream stream)
        {
            Client = client;
            Stream = stream;
        }
        public TcpClient Client { get; }
        public Stream Stream { get; }

        public void Dispose()
        {
            try { Stream.Dispose(); } catch { }
            try { Client.Dispose(); } catch { }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Stream is IAsyncDisposable disposable)
                    await disposable.DisposeAsync();
                else
                    Stream.Dispose();
                Stream.Dispose();
            }
            catch { }
            try { Client.Dispose(); } catch { }
        }
    }

    internal sealed class Socks5Helper
    {

        public static async Task<SocksTunnelConnection> ConnectAsync(string ip, int prot, string username, string pasaword, CancellationToken ct)
        {
            TcpClient tcp = new TcpClient();

            try
            {
                await tcp.ConnectAsync(ip, prot, ct);
                ConfigureTcpClient(tcp);

                Stream transport = tcp.GetStream();
                transport = await UpgradeToTlsAsync(ip, transport, ct);

                await AuthenticateAndConnectAsync(
                transport, ip, prot, username, pasaword, ct);

                return new SocksTunnelConnection(tcp, transport);
            }
            catch (Exception)
            {
                tcp.Dispose();
                throw;
            }
        }

        // TLS 升级的核心入口
        private static async Task<Stream> UpgradeToTlsAsync(string ip, Stream baseStream, CancellationToken ct)
        {
            SslStream sslStream = new SslStream(
                baseStream, leaveInnerStreamOpen: false, (_, certificate, _, policyErrors) => ValidateServerCertificate(ip, certificate, policyErrors));

            await sslStream.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = ip,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }, ct);

            return sslStream;
        }

        //TLS 证书验证回调函数
        public static bool ValidateServerCertificate(string ip, X509Certificate? certificate, SslPolicyErrors policyErrors)
        {
            if (certificate == null) return false;

            var pinned = NormalizeFingerprint(ip);
            if (!string.IsNullOrEmpty(pinned))
            {
                using var cer2 = new X509Certificate(certificate);
                var actul = NormalizeFingerprint(cer2.GetCertHashString(HashAlgorithmName.SHA256));
                return string.Equals(pinned, actul, StringComparison.OrdinalIgnoreCase);//比较忽略大小写
            }
            return policyErrors == SslPolicyErrors.None;
        }

        public static async Task AuthenticateAndConnectAsync(Stream stream, string host, int port, string username, string password, CancellationToken ct)
        {
            bool hasAuth = !string.IsNullOrEmpty(username);

            //0x05	SOCKS 版本号	固定为 5，代表 SOCKSv5 协议
            //0x01    命令码(CMD)   0x01 = CONNECT, 0x02 = BIND, 0x03 = UDP ASSOCIATE
            //0x00    保留字段(RSV)  固定为 0，预留供未来扩展
            byte[] greet = hasAuth ? [0x05, 0x02, 0x00, 0x02] : [0x05, 0x01, 0x00];
            await stream.WriteAsync(greet, ct);

            var greetResp = await ReadExactlyAsync(stream, 2, ct);

            if (greetResp[0] != 0x05)
                throw new Exception("SOCKS5 服务器响应版本错误");
            if (greetResp[1] == 0x02)
            {
                if (!hasAuth)
                    throw new Exception("代理要求认证但未配置用户名");

                var userBytes = Encoding.UTF8.GetBytes(username);
                var passBytes = Encoding.UTF8.GetBytes(password);
                if (userBytes.Length > byte.MaxValue || passBytes.Length > byte.MaxValue)
                    throw new Exception("SOCKS5 用户名或密码过长");
                var auth = new byte[3 + userBytes.Length + passBytes.Length];
                auth[0] = 0x01;
                auth[1] = (byte)userBytes.Length;
                userBytes.CopyTo(auth, 2);
                auth[2 + userBytes.Length] = (byte)passBytes.Length;
                passBytes.CopyTo(auth, 3 + userBytes.Length);

                await stream.WriteAsync(auth, ct);

                var authResp = await ReadExactlyAsync(stream, 2, ct);
                if (authResp[0] != 0x00)
                    throw new Exception("SOCKS5 认证失败（用户名或密码错误）");

            }
            else if (greetResp[1] != 0x00)
            {
                throw new Exception($"SOCKS5 无可用认证方式 (0x{greetResp[1]:X2})");
            }

            byte[] connectReq = BuildConnectRequest(host, port);
            await stream.WriteAsync(connectReq, ct);

            var rep = await ReadExactlyAsync(stream, 4, ct);
            if (rep[1] != 0x00)
                throw new Exception($"SOCKS5 CONNECT 失败：{DescribeReplyCode(rep[1])} (0x{rep[1]:X2})");

            await ConsumeBoundAddressAsync(stream, rep[3], ct);
        }

        //读取
        private static async Task<byte[]> ReadExactlyAsync(Stream stream, int count, CancellationToken ct)
        {
            var buf = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int n = await stream.ReadAsync(buf.AsMemory(offset, count - offset), ct);
                if (n == 0)
                    throw new EndOfStreamException("连接已关闭（读取 SOCKS5 响应时）");
                offset += n;
            }
            return buf;
        }


        private static string NormalizeFingerprint(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToUpperInvariant(ch));
            }
            return sb.ToString();
        }



        /// <summary>统一启用低延迟和 TCP 保活，短暂空闲后也能更快发现已断链路。</summary>
        internal static void ConfigureTcpClient(TcpClient tcp)
        {
            tcp.NoDelay = true;
            try
            {
                tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                tcp.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
            }
            catch (Exception ex) when (ex is SocketException or PlatformNotSupportedException or ArgumentException)
            {
                // 旧系统可能不支持精细 keepalive 选项，NoDelay 仍然有效。
                //AppLogger.Warn($"configure TCP keepalive failed: {ex.Message}");
            }
        }

        private static byte[] BuildConnectRequest(string host, int port)
        {
            if (IPAddress.TryParse(host, out var ip))
            {
                byte atyp;
                byte[] addrBytes;
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    atyp = 0x01;
                    addrBytes = ip.GetAddressBytes();
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    atyp = 0x04;
                    addrBytes = ip.GetAddressBytes();
                }
                else
                {
                    throw new Exception("不支持的 IP 地址类型");
                }

                var req = new byte[4 + addrBytes.Length + 2];
                req[0] = 0x05;
                req[1] = 0x01;
                req[2] = 0x00;
                req[3] = atyp;
                addrBytes.CopyTo(req, 4);
                req[^2] = (byte)(port >> 8);
                req[^1] = (byte)port;
                return req;
            }

            var hostBytes = Encoding.UTF8.GetBytes(host);
            if (hostBytes.Length == 0 || hostBytes.Length > byte.MaxValue)
                throw new Exception("目标主机名长度无效");

            var domainReq = new byte[7 + hostBytes.Length];
            domainReq[0] = 0x05;
            domainReq[1] = 0x01;
            domainReq[2] = 0x00;
            domainReq[3] = 0x03;
            domainReq[4] = (byte)hostBytes.Length;
            hostBytes.CopyTo(domainReq, 5);
            domainReq[^2] = (byte)(port >> 8);
            domainReq[^1] = (byte)port;
            return domainReq;
        }

        private static async Task ConsumeBoundAddressAsync(
        Stream stream,
        byte atyp,
        CancellationToken ct)
        {
            switch (atyp)
            {
                case 0x01:
                    await ReadExactlyAsync(stream, 6, ct);
                    break;
                case 0x04:
                    await ReadExactlyAsync(stream, 18, ct);
                    break;
                case 0x03:
                    var len = await ReadExactlyAsync(stream, 1, ct);
                    await ReadExactlyAsync(stream, len[0] + 2, ct);
                    break;
                default:
                    throw new Exception($"SOCKS5 返回了未知地址类型 0x{atyp:X2}");
            }
        }

        private static string DescribeReplyCode(byte code)
    => code switch
    {
        0x01 => "普通服务器故障",
        0x02 => "规则集禁止连接",
        0x03 => "网络不可达",
        0x04 => "主机不可达",
        0x05 => "连接被拒绝",
        0x06 => "TTL 已过期",
        0x07 => "命令不支持",
        0x08 => "地址类型不支持",
        _ => "未知错误"
    };

    }
}
