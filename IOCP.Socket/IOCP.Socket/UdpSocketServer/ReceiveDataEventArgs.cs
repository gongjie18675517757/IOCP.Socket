using System;
using System.Net;

namespace IOCP.SocketCore.UdpSocketServer
{
    public class ReceiveDataEventArgs : EventArgs
    {
        public EndPoint EndPoint { get; set; }

        public byte[] Data { get; set; }
    }
}
