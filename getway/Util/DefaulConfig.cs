using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace getway.Util
{
    public class DefaulConfig
    {

        public static string LoginPassword = "";

        // PGSQL 数据源
        public static string POSTGRESQL_PORT = "5432";
        public static string POSTGRESQL_USERNAME = "postgres";
        public static string POSTGRESQL_PASSWORD = "pg123456";
        public static string POSTGRESQL_DATABASE = "freeswitch";
        public static string DB_STRING = "";
        public static void SetDBString(string ip)
        {
            DB_STRING = $"Host={ip}; Port={POSTGRESQL_PORT}; User Id={POSTGRESQL_USERNAME}; Password={POSTGRESQL_PASSWORD}; Database={POSTGRESQL_DATABASE}";
        }

        // 配置文件保存在当前运行目录下
        public static string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    }
}
