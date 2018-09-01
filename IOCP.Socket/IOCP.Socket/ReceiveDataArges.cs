namespace IOCP.SocketCore
{
    /// <summary>
    /// 收到的数据
    /// </summary>
    public class ReceiveDataArges
    { 
        public ReceiveDataArges(SocketClient client, byte[] data)
        {
            Client = client;
            Data = data;
        }

        public SocketClient Client { get; set; }
        public byte[] Data { get; set; }
    } 
}
