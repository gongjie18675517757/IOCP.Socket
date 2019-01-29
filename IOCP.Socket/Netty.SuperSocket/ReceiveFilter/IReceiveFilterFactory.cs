using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 协议工厂
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TRequestInfo"></typeparam>
    public interface IReceiveFilterFactory<TSession, TRequestInfo>
        where TRequestInfo : IRequestInfo
        where TSession : AppSession<TSession, TRequestInfo>, new()
    {
        IReceiveFilter<TRequestInfo> CreateFilter(TSession session);
    }
}
