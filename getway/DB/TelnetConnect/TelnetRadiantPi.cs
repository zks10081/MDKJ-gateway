using RadiantPi.Telnet;

namespace getway.DB.TelnetConnect
{
    internal class TelnetRadiantPi
    {
        public async Task RadianPiConnect(string cmd)
        {

            // 1. 初始化客户端并连接设备
            using TelnetClient client = new("192.168.1.221", 23);

            // 2. 注册实时消息接收事件（核心：用于监听设备状态）
            client.MessageReceived += (sender, args) =>
            {
                // 当设备主动推送状态，或响应查询命令时，会实时触发此事件
                Console.WriteLine($"[实时状态]: {args.Message}");
            };
            // 3. 可选：设备握手验证
            client.ValidateConnectionAsync = async (client, reader, writer) =>
            {
                var welcomeMsg = await reader.ReadLineAsync();
                Console.WriteLine($"设备欢迎信息: {welcomeMsg}");
            };

            await client.ConnectAsync();
            Console.WriteLine("已成功连接设备，开始监听...");

            // 4. 模拟定时发送查询命令（低延迟查询）
            while (true)
            {
                // 向设备发送查询状态的指令（如 AT 指令或 CLI 命令）
                await client.SendAsync(cmd + "\r\n");

                // 注意：这里不需要 Sleep 等待响应，响应数据会由 MessageReceived 事件实时捕获
                await Task.Delay(5000);
            }


        }

    }
}
