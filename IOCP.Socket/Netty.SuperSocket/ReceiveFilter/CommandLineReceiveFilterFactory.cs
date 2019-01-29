using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 命令行协议工厂
    /// </summary> 
    public class CommandLineReceiveFilterFactory<TSession> : IReceiveFilterFactory<TSession, StringRequestInfo>
        where TSession : AppSession<TSession, StringRequestInfo>, new()
    {
        public IReceiveFilter<StringRequestInfo> CreateFilter(TSession session) => new CommandLineReceiveFilter();
    }
}
