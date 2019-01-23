using DotNetty.Buffers;
using Netty.SuperSocket.RequestInfo;
using System.Threading.Tasks;

namespace Netty.SuperSocket.Example
{
    public class MySession : AppSession<MySession, StringRequestInfo>
    {
        public override async Task OnReceiveDataAsync(IByteBuffer buffer)
        {
            //var bytes = new byte[] { 0x01, 0x02 };
            //var buffer = e.ReadBytes(e.ReadableBytes);
             await Send(buffer); 
            await base.OnReceiveDataAsync(buffer);
        }
    }
}
