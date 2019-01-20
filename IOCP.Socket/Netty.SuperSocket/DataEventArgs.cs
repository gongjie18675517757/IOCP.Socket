using DotNetty.Buffers;
using Netty.SuperSocket.RequestInfo;
using System;

namespace Netty.SuperSocket
{
    public class DataEventArgs<TSession, TRequest> : EventArgs
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {
        public IByteBuffer ByteBuffer { get; set; }

        public TSession Session { get; set; }
    }
}
