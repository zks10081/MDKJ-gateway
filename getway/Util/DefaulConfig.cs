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



        public static string Now_Telnet_key = "";
        // Telnet用户名和密码
        public const string BaseUsername = "queryboard"; // 身兼多职：查询业务版，修改热线，批量删除SIP用户，批量增加SIP用户
        public const string BasePassword = "dm13456";
        public const string QuerySIPUserRegStateUsername = "queryreg";
        public const string QuerySIPUserRegStatePassword = "dm13456";
        public const string QuerySIPUserCallStateUsername_1 = "querycall01";
        public const string QuerySIPUserCallStatePassword_1 = "dm13456";
        public const string QuerySIPUserCallStateUsername_2 = "querycall02";
        public const string QuerySIPUserCallStatePassword_2 = "dm13456";
        public const string QuerySIPUserHotLineUsername = "queryhotline";
        public const string QuerySIPUserHotLinePassword = "dm13456";


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
