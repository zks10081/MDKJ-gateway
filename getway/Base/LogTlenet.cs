using getway.Util;
using System.IO;
using System.Text;
using System.Threading.Channels;

namespace getway.Base
{
    internal class LogTlenet
    {
        //日志
        private readonly StringBuilder _readContent = new StringBuilder();
        private readonly Channel<string> _channel = Channel.CreateBounded<string>(1000);
        private Task _writerTask;
        private readonly object _logLock = new object();

        private string _LogPath;
        private string _LogName;

        public LogTlenet(string path, string name)
        {
            _LogPath = path;
            _LogName = name;


            if (!File.Exists(DefaulConfig.TelnetLogPath))
            {
                Directory.CreateDirectory(DefaulConfig.TelnetLogPath);
            }
            _writerTask = ProcessLogsAsync();
        }

        public void AppendLog(string text)
        {
            _channel.Writer.TryWrite(text);
        }

        private async Task ProcessLogsAsync()
        {
            var filePath = Path.Combine(_LogPath, $"{_LogName}_{DateTime.Now:yyyyMMdd}.log");
            await using var writer = new StreamWriter(filePath, append: true);

            await foreach (var entry in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    await writer.WriteLineAsync(entry);
                    await writer.FlushAsync();
                }
                catch { /* 磁盘满等异常不阻塞主流程 */ }
            }
        }

        public async Task StopAsync()
        {
            _channel.Writer.Complete();
            if (_writerTask != null) await _writerTask;
        }

    }
}
