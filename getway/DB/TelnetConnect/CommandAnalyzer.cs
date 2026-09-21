using System.Text.RegularExpressions;

namespace getway.DB.TelnetConnect
{
    class CommandAnalyzer
    {
        /// <summary>
        /// 解析接口状态（以华为/H3C 设备为例）
        /// 示例返回：
        /// Interface          PHY Protocol IP Address      Type
        /// GigabitEthernet0/0/1 up   up       192.168.1.1    static
        /// </summary>
        public static List<InterfaceInfo> ParseInterfaceStatus(string response)
        {
            var results = new List<InterfaceInfo>();
            var pattern = @"(\S+)\s+(up|down)\s+(up|down)\s+(\S+)";

            foreach (Match m in Regex.Matches(response, pattern, RegexOptions.IgnoreCase))
            {
                results.Add(new InterfaceInfo
                {
                    Name = m.Groups[1].Value,
                    PhyStatus = m.Groups[2].Value,
                    ProtoStatus = m.Groups[3].Value,
                    IpAddress = m.Groups[4].Value
                });
            }
            return results;
        }

        /// <summary>
        /// 解析 CPU/内存利用率
        /// </summary>
        public static DeviceHealth ParseDeviceHealth(string response)
        {
            var health = new DeviceHealth();

            var cpuMatch = Regex.Match(response, @"CPU\s+Usage[:\s]+(\d+)%");
            if (cpuMatch.Success)
                health.CpuUsage = int.Parse(cpuMatch.Groups[1].Value);

            var memMatch = Regex.Match(response, @"Memory\s+Usage[:\s]+(\d+)%");
            if (memMatch.Success)
                health.MemoryUsage = int.Parse(memMatch.Groups[1].Value);

            return health;
        }

        /// <summary>
        /// 判断命令是否执行成功
        /// </summary>
        public static bool IsSuccess(string response)
        {
            return !Regex.IsMatch(response, @"(Error|error|ERROR|Invalid|fail)", RegexOptions.Compiled);
        }
    }

    // 数据模型
    public class InterfaceInfo
    {
        public string Name { get; set; }
        public string PhyStatus { get; set; }
        public string ProtoStatus { get; set; }
        public string IpAddress { get; set; }
    }

    public class DeviceHealth
    {
        public int CpuUsage { get; set; }
        public int MemoryUsage { get; set; }
    }

}
