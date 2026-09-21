using System.Text;

namespace getway.DB.TelnetConnect
{
    class TestConnect
    {
        static async Task Main(string[] args)
        {
            // 1. 创建通信层
            var transport = new TelnetTransport();
            await transport.ConnectAsync("192.168.1.1", 23);

            // 2. 创建协议层
            var protocol = new TelnetProtocol(transport, Encoding.ASCII);

            // 3. 创建会话层
            var session = new TelnetSession(protocol);

            // 4. 登录
            bool loggedIn = await session.LoginAsync("admin", "password123");
            Console.WriteLine($"登录结果: {loggedIn}");

            // 5. 执行命令
            string response = await session.ExecuteCommandAsync("display interface brief");

            // 6. 分析结果
            var interfaces = CommandAnalyzer.ParseInterfaceStatus(response);
            foreach (var iface in interfaces)
            {
                Console.WriteLine($"{iface.Name}: PHY={iface.PhyStatus}, " +
                                  $"Proto={iface.ProtoStatus}, IP={iface.IpAddress}");
            }

            // 7. 清理
            session.Dispose();
            transport.Dispose();
        }
    }
}
