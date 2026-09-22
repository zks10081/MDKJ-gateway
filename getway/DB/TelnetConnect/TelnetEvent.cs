using getway.Model;
using System.Text.RegularExpressions;

namespace getway.DB.TelnetConnect
{
    class TelnetEvent
    {

        public static List<BorderModel> QueryBoard(string key, int slotid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            if (telnet == null) return null;

            telnet.Send("enable");
            telnet.Send($"display board {slotid}" + Environment.NewLine);
            Thread.Sleep(300);
            string result = telnet.Receive();
            List<BorderModel> List = BorderString(result);
            return List;
        }

        public static List<IpcUserModel> QuerySipUser(string key, int slotid, int borderid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            if (telnet == null) return null;

            telnet.Send("enable");
            telnet.Send($"display sippstnuser reg-state {slotid}/{borderid}/0 {slotid}/{borderid}/63" + Environment.NewLine);
            Thread.Sleep(300);
            string result = telnet.Receive();
            List<IpcUserModel> List = SipUserString(result);
            return List;
        }

        public static string AnyCommand(string key, string command)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            if (telnet == null) return null;

            telnet.Send(command + Environment.NewLine);
            Thread.Sleep(300);
            string result = telnet.Receive();
            return result;
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
            //result = Regex.Replace(result, @"----\s*More.*?----", "");
            result = Regex.Replace(result, @"----\s*More\s*\(.*?\)\s*----\s*", "");

            //直接把数据里的超长空格替换为单个空格，并去除首尾空格
            result = Regex.Replace(result, @"\s+", " ").Trim();

            // 4. 使用正则提取所有符合条件的数据
            // 逻辑：跳过命令提示符(如 enabledmkj#)，匹配 0 /1 /63  0  FailRegistered  8002 这种格式
            var pattern = @"(?<!enabledmkj#)\b(\d+)\s*/\s*(\d+)\s*/\s*(\d+)\s+(\d+)\s+(\S+)\s+(\d+)";

            var matches = Regex.Matches(result, pattern);

            List<IpcUserModel> List = new List<IpcUserModel>();

            foreach (Match match in matches)
            {

                // 排除掉命令提示符那一行的干扰（双重保险）
                if (match.Value.StartsWith("enabledmkj") || match.Value.StartsWith("dmkj")) continue;

                IpcUserModel model = new IpcUserModel();

                model.Name = string.IsNullOrEmpty(match.Groups[5].Value) ? null : match.Groups[5].Value;
                model.State = string.IsNullOrEmpty(match.Groups[6].Value) ? null : match.Groups[6].Value;
                model.FSP = $"{match.Groups[1].Value}/{match.Groups[2].Value}/{match.Groups[3].Value}";

                List.Add(model);
            }
            return List;

        }
    }
}
