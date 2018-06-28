using System;

namespace IOCP.SocketCore.UdpSocketServer
{
    internal interface IUdpServer : IServer
    {
        event EventHandler<ReceiveDataEventArgs> ReceiveData;
    }
}
