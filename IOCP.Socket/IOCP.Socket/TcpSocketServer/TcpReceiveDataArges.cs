namespace IOCP.SocketCore.TcpSocketServer
{
    /// <summary>
    /// 收到的数据
    /// </summary>
    public class TcpReceiveDataArges : TcpConnectionedArges
    { 
        public TcpReceiveDataArges(TcpConnection connection,byte[] data) : base(connection)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
