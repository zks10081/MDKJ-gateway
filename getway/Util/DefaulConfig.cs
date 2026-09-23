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

        public static int FrameId = 12;
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

        //telnet参数配置
        public static string Success_Flag = "dmkj>";
        public static int Telnet_waitTime = 300;

        // SIP用户各种状态的颜色
        public const string SIPUserBackgroundColorOfRinging = "#ffff00"; // 振铃
        public const string SIPUserBackgroundColorOfDialing = "#ffc0cb"; // 摘机
        public const string SIPUserBackgroundColorOfCalling = "#90ee90"; // 通话中
        public const string SIPUserBackgroundColorOfLocked = "#213740"; // 锁定
        public const string SIPUserBackgroundColorOfIdle = "#87ceff"; // 空闲
        public const string SIPUserBackgroundColorOfUnOnline = "#bebebe"; // 注册失败，离线
        // SIP用户的6种注册状态
        public const string Initializing = "Initializing"; // 初始态
        public const string Registering = "Registering"; // 正在注册中
        public const string FailRegistered = "FailRegistered"; // 注册失败
        public const string UnRegistering = "UnRegistering"; // 正在取消注册
        public const string Deactive = "Deactive"; // 去激活
        public const string Active = "Active"; // 激活态
        // SIP用户的9种呼叫状态
        public const string Idle = "Idle"; // 空闲
        public const string Dialing = "Dialing"; // 拨号中
        public const string Establishing = "Establishing"; // 请求建立呼叫中
        public const string Ringing = "Ringing"; // 正在振铃
        public const string Ringback = "Ringback"; // 正在回铃
        public const string Connecting = "Connecting"; // 建立连接中
        public const string Connected = "Connected"; // 通话已建立
        public const string Disconnecting = "Disconnecting"; // 正在释放连接
        public const string Locked = "Locked"; // 用户锁定


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
