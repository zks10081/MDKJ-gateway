namespace getway.Base
{
    /// <summary>
    /// 日志出口：子 ViewModel 通过它把日志写到持有日志显示区的父 ViewModel，
    /// 避免子 ViewModel 反向依赖具体的父类型。
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// 追加一行日志。实现方必须保证线程安全——调用方可能在后台线程里写。
        /// </summary>
        void AppendLog(string text);

        void QueryStatusFun(int status, string tip);

        void ChangBorderShow(string action);

        void QuerySipUserDataSwitch(string str);
    }
}
