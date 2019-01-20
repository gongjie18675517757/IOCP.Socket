using Netty.SuperSocket.RequestInfo;
using System.Threading.Tasks;

namespace Netty.SuperSocket.Command
{
    /// <summary>
    /// 命令接口
    /// </summary>
    /// <typeparam name="TSession">会话类型</typeparam>
    /// <typeparam name="TRequest">请求类型</typeparam>
    public interface ICommand<TSession, TRequest>
        where TRequest : IRequestInfo
        where TSession : AppSession<TSession, TRequest>, new()
    {
        Task ExecuteCommand(TSession session, TRequest request);
    }
}
