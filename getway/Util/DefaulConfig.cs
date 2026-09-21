using System.IO;

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

    public enum GatewayUIState
    {
        Loading,      // 连接中
        Online,       // 在线
        Timeout,      // 超时
        Offline       // 离线
    }

    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        RD = 1,
        SGA = 3
    }
}
