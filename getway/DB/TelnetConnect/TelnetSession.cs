using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace getway.DB.TelnetConnect
{
    class TelnetSession
    {
        private readonly TelnetProtocol _protocol;
        private readonly ConcurrentQueue<string> _responseBuffer = new();
        private readonly SemaphoreSlim _responseSignal = new(0);
        private CancellationTokenSource _sessionCts;

        public bool IsLoggedIn { get; private set; }

        public TelnetSession(TelnetProtocol protocol)
        {
            _protocol = protocol;
            _protocol.TextReceived += OnTextReceived;
        }

        /// <summary>
        /// 登录：发送用户名和密码，等待提示符
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password,
            string promptPattern = @"[#$>]", int timeoutMs = 10000)
        {
            _sessionCts = new CancellationTokenSource();

            // 等待登录提示
            string banner = await WaitForTextAsync("login:", "Login:", timeoutMs);
            if (banner == null) return false;

            // 发送用户名
            await _protocol.SendCommandAsync(username);

            // 等待密码提示
            string pwdPrompt = await WaitForTextAsync("password:", "Password:", timeoutMs);
            if (pwdPrompt == null) return false;

            // 发送密码
            await _protocol.SendCommandAsync(password);

            // 等待命令提示符（如 # $ >）
            string prompt = await WaitForRegexAsync(promptPattern, timeoutMs);
            IsLoggedIn = prompt != null;
            return IsLoggedIn;
        }

        /// <summary>
        /// 执行命令并返回完整响应
        /// </summary>
        public async Task<string> ExecuteCommandAsync(string command,
            string promptPattern = @"[#$>]", int timeoutMs = 15000)
        {
            if (!IsLoggedIn) throw new InvalidOperationException("未登录");

            // 清空缓冲区
            _responseBuffer.Clear();

            // 发送命令
            await _protocol.SendCommandAsync(command);

            // 等待直到出现提示符（表示命令执行完毕）
            var fullResponse = await WaitForRegexAsync(promptPattern, timeoutMs);
            return fullResponse ?? "";
        }

        /// <summary>
        /// 收到服务端文本后，放入缓冲区并释放等待信号
        /// </summary>
        private void OnTextReceived(string text)
        {
            _responseBuffer.Enqueue(text);
            _responseSignal.Release();
        }

        /// <summary>
        /// 等待特定文本出现
        /// </summary>
        private async Task<string> WaitForTextAsync(string expected,
            string altExpected = null, int timeoutMs = 10000)
        {
            var accumulated = "";
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                // 非阻塞尝试取数据
                if (_responseBuffer.TryDequeue(out string chunk))
                {
                    accumulated += chunk;
                    if (accumulated.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       (altExpected != null && accumulated.IndexOf(altExpected, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return accumulated;
                    }
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            return null;
        }

        /// <summary>
        /// 等待正则匹配
        /// </summary>
        private async Task<string> WaitForRegexAsync(string pattern, int timeoutMs = 10000)
        {
            var accumulated = "";
            var regex = new Regex(pattern, RegexOptions.Compiled);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                if (_responseBuffer.TryDequeue(out string chunk))
                {
                    accumulated += chunk;
                    if (regex.IsMatch(accumulated))
                        return accumulated;
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            return null;
        }

        public void Dispose()
        {
            _sessionCts?.Cancel();
            _responseSignal.Dispose();
        }
    }
}
