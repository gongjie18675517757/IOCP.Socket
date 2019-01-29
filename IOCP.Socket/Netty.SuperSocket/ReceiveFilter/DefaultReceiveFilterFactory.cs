using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 默认协议工厂
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    /// <typeparam name="TRequestInfo"></typeparam>
    public class DefaultReceiveFilterFactory<TSession, TRequestInfo> : IReceiveFilterFactory<TSession, TRequestInfo>
         where TRequestInfo : IRequestInfo
        where TSession : AppSession<TSession, TRequestInfo>, new()
    {
        private readonly IReceiveFilter<TRequestInfo> receiveFilter;

        public DefaultReceiveFilterFactory(IReceiveFilter<TRequestInfo> receiveFilter)
        {
            this.receiveFilter = receiveFilter;
        }

        public IReceiveFilter<TRequestInfo> CreateFilter(TSession session) => receiveFilter;

    }
}
