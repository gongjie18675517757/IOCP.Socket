using IOCP.Socket.Core.RequestInfo;
using System.Threading.Tasks;

namespace IOCP.Socket.Core.Command
{
    /// <summary>
    /// 命令接口
    /// </summary>
    /// <typeparam name="TSession">会话类型</typeparam>
    /// <typeparam name="TRequest">请求类型</typeparam>
    public interface ICommand<TSession,TRequest> 
        where TRequest:IRequestInfo
        where TSession:AppSession<TRequest>
    {
        Task ExecuteCommand(TSession session, TRequest request);
    }
}
