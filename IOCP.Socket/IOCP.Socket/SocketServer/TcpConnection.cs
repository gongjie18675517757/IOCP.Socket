using System;
using System.Net;
using System.Net.Sockets;
namespace IOCP.SocketCore.SocketServer
{
    /// <summary>
    /// TCP客户端连接
    /// </summary>
    public class TcpConnection
    {
        private readonly ITcpServer tcpServer;
        private bool connectioned=true;

        /// <summary>
        /// 连接被断开
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// 收到数据
        /// </summary>
        public event EventHandler ReceiveDataed;



        internal Socket Socket { get; set; }
        internal SocketAsyncEventArgs ReceiveSocketAsync { get; set; }

        /// <summary>
        /// 远程终结点
        /// </summary>
        public EndPoint RemoteEndPoint => Socket?.RemoteEndPoint;

        /// <summary>
        /// 本地终结点
        /// </summary>
        public EndPoint LocalEndPoint => Socket?.LocalEndPoint;

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool Connectioned => connectioned;

        internal TcpConnection(ITcpServer  tcpServer)
        {
            this.tcpServer = tcpServer;
            tcpServer.Disconnected += TcpServer_Disconnected;
            tcpServer.ReceiveData += TcpServer_ReceiveDataed;
        }

        private void TcpServer_ReceiveDataed(object sender, TcpReceiveDataArges e)
        {
            ReceiveDataed?.Invoke(this, e);
        }

        private void TcpServer_Disconnected(object sender, TcpConnectionedArges e)
        {
            Disconnected?.Invoke(this, e);
        }


        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes"></param>
        public void Send(byte[] bytes) => tcpServer.SendAsync(this, bytes);
    }

}
