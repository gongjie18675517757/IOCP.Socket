using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
namespace IOCP.SocketCore
{
    /// <summary>
    /// 远程客户端
    /// </summary>
    public class SocketClient
    {
        public SocketClient()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// 远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint { get; internal set; }

        /// <summary>
        /// 连接ID
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// 异步套接字
        /// </summary>
        internal SocketAsyncEventArgs SocketAsyncEvent { get; set; }

        /// <summary>
        /// 套接字对象
        /// </summary>
        internal Socket Socket { get;  set; }
    }
}
