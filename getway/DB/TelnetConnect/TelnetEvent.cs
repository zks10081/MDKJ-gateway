using getway.Model;
using System.IO;
using System.Text.RegularExpressions;

namespace getway.DB.TelnetConnect
{
    class TelnetEvent
    {

        public static void QueryBoard(string key)
        {
            StreamWriter writer = TcpConnect.GetWriter(key);
            Task.Run(() =>
            {
                Thread.Sleep(300);

                writer.WriteLine("root" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("mduadmin" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("n" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("enable" + Environment.NewLine);
                writer.Flush();

                try
                {

                    writer.WriteLine("display board 0" + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();

                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new ObjectDisposedException("网络异常");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw new InvalidOperationException("");
                }
            });
        }

        public static void QuerySipUser(string key)
        {
            StreamWriter writer = TcpConnect.GetWriter(key);

            Task.Run(() =>
            {
                Thread.Sleep(300);

                writer.WriteLine("root" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("mduadmin" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("n" + Environment.NewLine);
                writer.Flush();
                writer.WriteLine("enable" + Environment.NewLine);
                writer.Flush();

                try
                {

                    writer.WriteLine("display sippstnuser reg-state 0/1/0 0/1/63" + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();
                }
                catch (Exception)
                {

                    throw;
                }
            });
        }

        public static void command(string key, string command)
        {
            StreamWriter writer = TcpConnect.GetWriter(key);

            Task.Run(() =>
            {
                Thread.Sleep(300);

                try
                {

                    writer.WriteLine(command + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();
                    writer.WriteLine("n" + Environment.NewLine);
                    writer.Flush();
                }
                catch (Exception)
                {

                    throw;
                }
            });
        }




        public static List<BorderModel> BorderString(string result)
        {

            var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<BorderModel> borderList = new List<BorderModel>();

            foreach (var line in lines)
            {
                // 跳过表头、分隔线和空行
                if (line.Contains("SlotID") || line.Contains("---") || string.IsNullOrWhiteSpace(line))
                    continue;

                // 使用正则提取数据（兼容 SlotID 3 这种只有数字的空行）
                // 匹配逻辑：开头是数字，后面跟着可选的非空白字符块
                var match = Regex.Match(line.Trim(), @"^(\d+)\s+(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)");

                if (match.Success)
                {
                    BorderModel model = new BorderModel();

                    model.BorderName = string.IsNullOrEmpty(match.Groups[2].Value) ? null : match.Groups[2].Value;
                    model.SlotNo = int.Parse(match.Groups[1].Value);

                    borderList.Add(model);

                }
            }
            return borderList;

        }

        public static List<IpcUserModel> SipUserString(string result)
        {

            var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<IpcUserModel> List = new List<IpcUserModel>();
            // 1. 预定义清理非打印字符的正则（如 [37D 这种回退符乱码）
            // 正则1：删除所有真实的 ESC 控制字符（\u001b 等）
            var escRegex = new Regex(@"[\x1b\x00-\x1f\x7f]+");
            // 正则2：删除残留的字面 ANSI 序列（如 [37D、[2J 等）
            var ansiRegex = new Regex(@"\[\d+[A-Za-z]");

            foreach (var line in lines)
            {

                string cleanLine = line;

                // 2. 直接跳过包含分页提示的行
                if (cleanLine.Contains("More") && cleanLine.Contains("Press 'Q'"))
                {
                    continue;
                }

                // 3. 跳过表头、分隔线和空行
                if (cleanLine.Contains("---") || cleanLine.Contains("F  /S /P") || string.IsNullOrWhiteSpace(cleanLine))
                {
                    continue;
                }

                // 4. 关键步骤：清理掉所有不可见的转义字符/乱码，防止数据粘在一起
                // 全局清洗整个文本
                cleanLine = escRegex.Replace(cleanLine, string.Empty);
                cleanLine = ansiRegex.Replace(cleanLine, string.Empty);

                // 清理后，45 和 46 的行就会变成干净的 "0  /1 /45   0        FailRegistered   8047"

                // 5. 使用正则提取数据
                // 匹配逻辑：数字/数字/数字  空格 数字  空格 字母单词  空格 数字
                var match = Regex.Match(cleanLine.Trim(), @"(\d+)\s*/\s*(\d+)\s*/\s*(\d+)\s+(\d+)\s+(\S+)\s+(\d+)");

                if (match.Success)
                {

                    IpcUserModel model = new IpcUserModel();

                    model.Name = string.IsNullOrEmpty(match.Groups[5].Value) ? null : match.Groups[5].Value;
                    model.State = string.IsNullOrEmpty(match.Groups[6].Value) ? null : match.Groups[6].Value;
                    model.FSP = $"{match.Groups[1].Value}/{match.Groups[2].Value}/{match.Groups[3].Value}";

                    List.Add(model);

                }
            }
            return List;

        }
    }
}
