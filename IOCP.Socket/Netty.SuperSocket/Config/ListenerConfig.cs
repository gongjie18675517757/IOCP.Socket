using System.Net;

namespace Netty.SuperSocket.Config
{
    /// <summary>
    /// 监听信息
    /// </summary>
    public class ListenerConfig
    {
        /// <summary>
        /// IP
        /// </summary>
        public string Ip { get; set; } = IPAddress.Any.ToString();

        /// <summary>
        /// 端口
        /// </summary>
        public int Post { get; set; } = 0;
    }
}
