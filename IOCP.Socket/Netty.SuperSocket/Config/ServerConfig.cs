using System.Collections.Generic;

namespace Netty.SuperSocket.Config
{
    /// <summary>
    /// 服务配置
    /// </summary>
    public class ServerConfig
    {
        ///// <summary>
        ///// 监听模式
        ///// </summary>
        //public ListenMode Mode { get; set; } = ListenMode.Tcp;

        /// <summary>
        /// Appept线程数量
        /// </summary>
        public int ParentGroupCount { get; set; } = 1;

        /// <summary>
        /// 工作线程数量
        /// </summary>
        public int ChildGroupCount { get; set; } = 5;

        /// <summary>
        /// 监听信息
        /// </summary>
        public IEnumerable<ListenerConfig> Listeners { get; set; } = new ListenerConfig[] { new ListenerConfig() };

        /// <summary>
        ///  监听队列的大小
        /// </summary>
        public int ListenBacklog { get; set; } = 5;

        /// <summary>
        /// 发送超时
        /// </summary>
        public int SendTimeOut { get; internal set; } = 10;

        /// <summary>
        /// 可允许连接的最大连接数
        /// </summary>
        public int MaxConnectionNumber { get; set; } = 100;

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        public int ReceiveBufferSize { get; set; } = 1024;

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        public int SendBufferSize { get; set; } = 1024;

        /// <summary>
        /// 是否记录命令执行的记录
        /// </summary>
        public bool LogCommand { get; set; } = true;

        /// <summary>
        /// 是否记录session的基本活动，如连接和断开
        /// </summary>
        public bool LogBasicSessionActivity { get; set; } = true;

        /// <summary>
        /// 是否记录所有Socket异常和错误
        /// </summary>
        public bool LogAllSocketException { get; set; } = true;

        /// <summary>
        /// 是否定时清空空闲会话，默认值是 false
        /// </summary>
        public bool ClearIdleSession { get; set; } = false;

        /// <summary>
        /// 清空空闲会话的时间间隔, 默认值是120, 单位为秒
        /// </summary>
        public int ClearIdleSessionInterval { get; set; } = 120;

        /// <summary>
        /// 会话空闲超时时间; 当此会话空闲时间超过此值，同时clearIdleSession被配置成true时，此会话将会被关闭; 默认值为300，单位为秒
        /// </summary>
        public int IdleSessionTimeOut { get; set; }

        /// <summary>
        /// 最大允许的请求长度，默认值为1024
        /// </summary>
        public int MaxRequestLength { get; set; }

        /// <summary>
        /// 文本的默认编码，默认值是 ASCII
        /// </summary>
        public string TextEncoding { get; set; }
    }
}
