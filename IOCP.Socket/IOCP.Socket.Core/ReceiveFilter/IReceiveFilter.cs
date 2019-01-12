using IOCP.Socket.Core.RequestInfo;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace IOCP.Socket.Core.ReceiveFilter
{
    public interface IReceiveFilter<TRequestInfo> where TRequestInfo : IRequestInfo
    {
        Task<TRequestInfo> Filter(PipeReader pipeReader); 
    }
}
