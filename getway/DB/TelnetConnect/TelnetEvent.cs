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


            // 1. 删除真实 ESC 控制字符
            result = Regex.Replace(result, @"[\x1b\x00-\x1f\x7f]+", "");

            // 2. 删除字面 ANSI 序列，例如 [37D
            result = Regex.Replace(result, @"\[\d+[A-Za-z]", "");

            // 3. 删除分页提示，例如 ---- More ( Press 'Q' to break ) ----
            result = Regex.Replace(result, @"----\s*More.*?----", "");

            var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<IpcUserModel> List = new List<IpcUserModel>();

            foreach (var line in lines)
            {

                // 4. 跳过表头、分隔线和空行
                if (line.Contains("---") || line.Contains("F  /S /P") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                // 5. 使用正则提取数据
                // 匹配逻辑：数字/数字/数字  空格 数字  空格 字母单词  空格 数字
                var match = Regex.Match(line.Trim(), @"(\d+)\s*/\s*(\d+)\s*/\s*(\d+)\s+(\d+)\s+(\S+)\s+(\d+)");

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
