using System;
namespace IOCP.SocketCore.SocketServer
{
    public class TcpConnectionedArges : EventArgs
    {
        public TcpConnectionedArges(TcpConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// 新连接的客户端
        /// </summary>
        public TcpConnection Connection { get; internal set; }
    }
}
