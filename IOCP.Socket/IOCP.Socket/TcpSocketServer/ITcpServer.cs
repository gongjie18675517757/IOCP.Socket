using System;

namespace IOCP.SocketCore.TcpSocketServer
{
    internal interface ITcpServer:IServer
    {
        /// <summary>
        /// 已连接的数量
        /// </summary>
        int ConnectionCount { get; }

        /// <summary>
        /// 连接队列长度
        /// </summary>
        int Backlog { get; }  

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="bytes"></param>
        void SendAsync(TcpConnection connection, byte[] bytes);  

        /// <summary>
        /// 连接断开事件
        /// </summary>
        event EventHandler<TcpConnectionedArges> Disconnected;

        /// <summary>
        /// 收到数据
        /// </summary>
        event EventHandler<TcpReceiveDataArges> ReceiveData;
    }
}
