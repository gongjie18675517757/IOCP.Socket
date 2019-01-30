using IOCP.Socket.Core.RequestInfo;
using System.Threading.Tasks;

namespace IOCP.Socket.Core.ReceiveFilter
{
    public interface IReceiveFilter<TRequestInfo> where TRequestInfo : IRequestInfo
    {
       
    }
}
