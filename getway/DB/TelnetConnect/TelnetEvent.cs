using getway.Model;
using getway.Util;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace getway.DB.TelnetConnect
{
    class TelnetEvent
    {
        /// <summary>
        /// 查询板卡信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="slotid"></param>

        public static void QueryBoard(string key, int slotid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);

            telnet.Send("enable");
            string result = telnet.Receive();
            telnet.Send($"display board {slotid}" + Environment.NewLine);

            //telnet.Send("quit" + Environment.NewLine);
        }

        /// <summary>
        /// 查询sip用户注册信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="slotid"></param>
        /// <param name="borderid"></param>
        public static void QuerySipUser(string key, int slotid, int borderid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);

            telnet.Send("enable" + Environment.NewLine);
            string result = telnet.Receive();
            telnet.Send($"display sippstnuser reg-state {slotid}/{borderid}/0 {slotid}/{borderid}/63" + Environment.NewLine);
            //telnet.Send("quit" + Environment.NewLine);
        }
        /// <summary>
        /// 查询sip用户呼叫状态
        /// </summary>
        /// <param name="key"></param>
        /// <param name="slotid"></param>
        /// <param name="borderid"></param>
        public static void QuerySipUserCall(string key, int slotid, int borderid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);

            telnet.Send("enable" + Environment.NewLine);
            string result = telnet.Receive();
            //display sippstnuser call-state 0/1/0 0/1/63
            telnet.Send($"display sippstnuser call-state {slotid}/{borderid}/0 {slotid}/{borderid}/63" + Environment.NewLine);
            Thread.Sleep(100);
            telnet.Send($" " + Environment.NewLine);
            telnet.Send($" " + Environment.NewLine);
            //telnet.Send("quit" + Environment.NewLine);
        }

        /// <summary>
        /// 任意指令执行
        /// </summary>
        /// <param name="key"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static async Task<string> AnyCommand(string key, string command)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            if (telnet == null) return null;

            telnet.Send(command + Environment.NewLine);
            Thread.Sleep(300);
            string result = telnet.Receive();
            return result;
        }

        /// <summary>
        /// 重置权限
        /// </summary>
        /// <param name="key"></param>
        public static void ResetPerm(string key)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            if (telnet == null) return;

            string command = "quit";
            int out_flag = 0;
            do
            {
                telnet.Send(command + Environment.NewLine);
                Thread.Sleep(200);
                string result = telnet.Receive();

                if (result.EndsWith("dmkj#"))
                {
                    break;
                }
                if (result.Contains("(y/n)[n]"))//如果要退出连接，则终止
                {
                    telnet.Send("n" + Environment.NewLine);
                }
                if (out_flag > 4)//超过4次，就停止循环
                {
                    break;
                }
            }
            while (true);

        }

        /// <summary>
        /// 查询热线配置信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="boardport"></param>
        /// <param name="userphone"></param>
        public static void QueryHotLine(string key, string boardport, string userphone)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();
            telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
            result = telnet.Receive();

            telnet.Send($"display sippstnuser servicedata {boardport} telno {userphone}" + Environment.NewLine);

        }

        /// <summary>
        /// 修改sip用户号码
        /// </summary>
        /// <param name="key"></param>
        /// <param name="boardport"></param>
        /// <param name="olduserphone"></param>
        /// <param name="userphone"></param>
        public static void EditSipUserPhone(string key, string boardport, string olduserphone, string userphone)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();
            telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
            result = telnet.Receive();

            telnet.Send($"sippstnuser modify {boardport} mgid {olduserphone} telno {userphone}" + Environment.NewLine);

        }

        /// <summary>
        /// 配置摘机热线
        /// </summary>
        /// <param name="key"></param>
        /// <param name="boardport"></param>
        /// <param name="userphone"></param>
        /// <param name="hotlinephone"></param>
        /// <param name="hotlinetime"></param>
        public static void EditHotLine(string key, string boardport, string userphone, string hotlinephone, string hotlinetime)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;
            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();
            telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
            result = telnet.Receive();
            telnet.Send($"sippstnuser rightflag set {boardport} telno {userphone} hotline enable" + Environment.NewLine);
            Thread.Sleep(300);
            telnet.Receive();
            telnet.Send($"sippstnuser servicedata parameter set {boardport} telno {userphone} hottime {hotlinetime} hotlinenum {hotlinephone}" + Environment.NewLine);
            Thread.Sleep(300);
            telnet.Receive();

        }
        //---------------------------拨号规则操控--------------------------

        /// <summary>
        /// 修改拨号规则，先删除，添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="digitmap"></param>
        public static void DigitMap(string key, string digitmap)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"local-digitmap delete all" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send($"local-digitmap add {DefaulConfig.DigitMapName} normal {digitmap}" + Environment.NewLine);

        }

        /// <summary>
        /// 查询拨号计划
        /// </summary>
        /// <param name="key"></param>
        public static void DigitMapQueryAll(string key)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"display local-digitmap all" + Environment.NewLine);

        }

        /// <summary>
        /// 添加拨号计划
        /// </summary>
        /// <param name="key"></param>
        /// <param name="digitName"></param>
        /// <param name="ruletext"></param>
        public static void DigitMapAdd(string key, string ruletext)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"local-digitmap add {DefaulConfig.DigitMapName} normal {ruletext}" + Environment.NewLine);

        }

        /// <summary>
        /// 根据名称删除拨号计划
        /// </summary>
        /// <param name="key"></param>
        /// <param name="digitName"></param>
        public static void DigitMapDelet(string key, string digitName)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"local-digitmap delete name {digitName}" + Environment.NewLine);

        }

        /// <summary>
        /// 删除全部拨号计划
        /// </summary>
        /// <param name="key"></param>
        public static void DigitMapDeletAll(string key)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"local-digitmap delete all" + Environment.NewLine);

        }


        //---------------------------sip用户操控--------------------------

        /// <summary>
        /// 删除全部SIP用户
        /// </summary>
        /// <param name="key"></param>
        public static async Task SipUserDeleteAll(string key, string frameid, string slotid)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();
            telnet.Send("esl user" + Environment.NewLine);//进入esl user模式
            result = telnet.Receive();

            telnet.Send($"sippstnuser batdel {frameid}/{slotid}/0 {frameid}/{slotid}/63" + Environment.NewLine);

        }

        /// <summary>
        /// 添加用SIP户
        /// </summary>
        public static void SipUserAdd()
        {

        }





        //---------------------------保存配置--------------------------

        public static void SaveConfig(string key)
        {
            Telnet2 telnet = TcpConnect.GetTelnet(key);
            string result = string.Empty;

            telnet.Send("enable" + Environment.NewLine);
            result = telnet.Receive();
            telnet.Send("config" + Environment.NewLine);//进入config模式
            result = telnet.Receive();

            telnet.Send($"save" + Environment.NewLine);

        }



        /// <summary>
        /// 板卡信息：更新已有板卡对象的属性，靠 IpcUserModel/BorderModel 的 PropertyChanged 推送到界面。
        /// 不要用 new 出来的对象替换列表元素——那样列表本身不发通知，界面不会刷新。
        /// </summary>
        public static void BorderString(string result, ObservableCollection<BorderModel> borderList)
        {
            if (string.IsNullOrWhiteSpace(result)) return;

            var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // 跳过表头、分隔线和空行
                if (line.Contains("SlotID") || line.Contains("---") || string.IsNullOrWhiteSpace(line))
                    continue;

                // 使用正则提取数据（兼容 SlotID 3 这种只有数字的空行）
                var match = Regex.Match(line.Trim(), @"^(\d+)\s+(\S*)\s*(\S*)\s*(\S*)\s*(\S*)\s*(\S*)");

                if (!match.Success) continue;

                int slotNo = int.Parse(match.Groups[1].Value);
                string status = match.Groups[3].Value;

                //只有1-4才是板卡
                if (slotNo > 0 || slotNo < 5) continue;

                // 列表里的槽位是固定的，按槽位找现有对象更新即可
                BorderModel? model = borderList.FirstOrDefault(b => b.SlotNo == slotNo);
                if (model == null) continue;
                //状态变化则更新
                if (status.Equals(model.Status)) continue;

                //1.第一次激活，则进行更新
                //2.状态不同，进行更新
                //默认非激活，第一次激活更新，之后不同就是激活转非激活
                //3 号版是空，不过3号版无数据，不进判断


                model.BorderName = $"板卡{slotNo}";
                model.IsEnable = "Normal".Equals(status);//只有
                model.Status = status;
            }
        }



        /// <summary>
        /// 解析处理SIP用户注册信息
        /// SIP 用户信息：更新已存在对象的属性，靠 IpcUserModel 的 PropertyChanged 推送到界面。
        /// 不能用 new 出来的对象替换 List[index]——集合不发通知，界面就"收不到"数据。
        /// </summary>
        public static void SipUserRegString(string result, ObservableCollection<IpcUserModel> List)
        {
            if (string.IsNullOrWhiteSpace(result)) return;

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


            foreach (Match match in matches)
            {

                // 排除掉命令提示符那一行的干扰（双重保险）
                if (match.Value.StartsWith("enabledmkj") || match.Value.StartsWith("dmkj")) continue;

                // "0/1/63" 里第三个数才是用户序号（第二个数是板卡槽位，64 个用户会全挤到同一格）
                if (!int.TryParse(match.Groups[3].Value, out int index)) continue;
                if (index < 0 || index >= List.Count) continue;

                // 回显形如 "0 /1 /21  0  FailRegistered  8023"：第5组是注册状态，第6组是分机号
                string name = match.Groups[6].Value;
                string regState = match.Groups[5].Value;
                string fsp = $"{match.Groups[1].Value}/{match.Groups[2].Value}/{match.Groups[3].Value}";

                IpcUserModel model = List[index];

                // 内容没变就不写，避免每轮轮询刷一堆无意义的通知
                if (model.RegiteStatus == regState) continue;

                model.Name = string.IsNullOrEmpty(name) ? null : name;
                model.RegiteStatus = string.IsNullOrEmpty(regState) ? null : regState;
                model.FSP = fsp;
                model.Index = index;

                //根基注册状态和呼叫状态，计算设备状态
                model.SetBackgroundColorByState(model.RegiteStatus, model.CallStatus);
            }

        }

        /// <summary>
        /// 解析处理用户呼叫信息
        /// SIP 用户信息：更新已存在对象的属性，靠 IpcUserModel 的 PropertyChanged 推送到界面。
        /// </summary>
        public static void SipUserCallString(string result, ObservableCollection<IpcUserModel> List)
        {
            if (string.IsNullOrWhiteSpace(result)) return;

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
            // 逻辑：跳过命令提示符(如 enabledmkj#)，匹配 0  /1 /0    0    Connected        SecondaryConnected   8002  这种格式
            var pattern = @"(?<!enabledmkj#)\b(\d+)\s*/\s*(\d+)\s*/\s*(\d+)\s+(\d+)\s+(\S+)\s+(\S+)\s+(\d+)";

            var matches = Regex.Matches(result, pattern);


            foreach (Match match in matches)
            {

                // 排除掉命令提示符那一行的干扰（双重保险）
                if (match.Value.StartsWith("enabledmkj") || match.Value.StartsWith("dmkj")) continue;

                // "0/1/63" 里第三个数才是用户序号（第二个数是板卡槽位，64 个用户会全挤到同一格）
                if (!int.TryParse(match.Groups[3].Value, out int index)) continue;
                if (index < 0 || index >= List.Count) continue;

                // 回显形如 "0  /1 /0    0    Connected        SecondaryConnected   8002 "：共匹配7组，第5是呼叫状态，第7是分机号
                string name = match.Groups[7].Value;
                string CallState = match.Groups[5].Value;
                string fsp = $"{match.Groups[1].Value}/{match.Groups[2].Value}/{match.Groups[3].Value}";

                IpcUserModel model = List[index];

                // 内容没变就不写，避免每轮轮询刷一堆无意义的通知
                if (model.CallStatus == CallState) continue;

                model.Name = string.IsNullOrEmpty(name) ? null : name;
                model.CallStatus = string.IsNullOrEmpty(CallState) ? null : CallState;
                model.FSP = fsp;
                model.Index = index;

                //根基注册状态和呼叫状态，计算设备状态
                model.SetBackgroundColorByState(model.RegiteStatus, model.CallStatus);
            }

        }

        /// <summary>
        /// 解析 键值类型的内容
        /// 热线信息解析、拨号计划解析
        /// </summary>
        public static Dictionary<string, string> MapContentString(string result)
        {
            if (string.IsNullOrWhiteSpace(result)) return new Dictionary<string, string>();

            Dictionary<string, string> dic = new Dictionary<string, string>();

            var allMatches = Regex.Matches(result, @"^[\s-]*(\w[\w()-]*)\s*:\s*(\S+)", RegexOptions.Multiline);

            foreach (Match line in allMatches)
            {
                string key = line.Groups[1].Value;   // 如 hottime(s)、hotlinenum
                string value = line.Groups[2].Value; // 如 5、-

                dic[key] = value;
            }

            return dic;
        }

    }


}
