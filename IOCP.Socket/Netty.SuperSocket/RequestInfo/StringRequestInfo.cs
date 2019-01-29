namespace Netty.SuperSocket.RequestInfo
{
    /// <summary>
    /// 命令行请求
    /// </summary>
    public class StringRequestInfo : IRequestInfo
    {
        /// <summary>
        /// 键
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 命令内容
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string[] Args { get; set; }
    }
}
