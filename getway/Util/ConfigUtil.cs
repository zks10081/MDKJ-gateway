using getway.Model;
using System.IO;
using System.Text.Json;

namespace getway.Util
{
    internal class ConfigUtil
    {


        // 保存配置
        public static void Save(LoginModel config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DefaulConfig.ConfigPath, json);
        }

        // 读取配置
        public static LoginModel Load()
        {
            string ConfigPath = DefaulConfig.ConfigPath;
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<LoginModel>(json) ?? new LoginModel();
            }
            return new LoginModel(); // 如果没有配置文件，返回一个空对象
        }


    }
}
