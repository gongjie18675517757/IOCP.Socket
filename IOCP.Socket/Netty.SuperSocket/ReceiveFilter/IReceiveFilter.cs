using DotNetty.Buffers;
using Netty.SuperSocket.RequestInfo;
using System.Threading.Tasks;

namespace Netty.SuperSocket.ReceiveFilter
{
    public interface IReceiveFilter<TRequestInfo> where TRequestInfo : IRequestInfo
    {
        Task<TRequestInfo> Filter(IByteBuffer  byteBuffer); 
    }
}
