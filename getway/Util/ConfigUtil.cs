using getway.Model;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// 判断字符串是不是点分十进制的IP地址
        /// </summary>
        public static bool IsIP(string str)
        {
            if (str == null || str == "")
            {
                return false;
            }

            Regex rx = new Regex(@"^((2(5[0-5]|[0-4]\d))|[0-1]?\d{1,2})(\.((2(5[0-5]|[0-4]\d))|[0-1]?\d{1,2})){3}$");

            if (rx.IsMatch(str))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断字符串是否属于电话号码的规则
        ///     - 1. 1-24位字符串
        ///     - 2. 由 0-9 组成
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsPhoneNum(string str)
        {
            if (str == null || str == "")
            {
                return false;
            }

            Regex rx = new Regex(@"[\d#*]{1,24}");

            if (rx.IsMatch(str))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断字符串是否属于电话号码的规则
        ///     - 1. 1-60秒
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsHotTime(string str)
        {
            if (str == null || str == "")
            {
                return false;
            }

            Regex rx = new Regex(@"^(([1-9]|[1-5]\d)|60)");

            if (rx.IsMatch(str))
            {
                return true;
            }
            return false;
        }

        public static bool IsDigitMap(string str)
        {
            if (str == null || str == "")
            {
                return false;
            }

            Regex rx = new Regex(@"^(([1-9]|[1-5]\d)|60)");

            if (rx.IsMatch(str))
            {
                return true;
            }
            return false;

        }


    }
}
