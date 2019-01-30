using Netty.SuperSocket.RequestInfo;
using System.Text;

namespace Netty.SuperSocket.ReceiveFilter
{
    /// <summary>
    /// 结束符协议工厂
    /// </summary>
    /// <typeparam name="TSession"></typeparam>
    public class TerminatorReceiveFilterFactory<TSession> : IReceiveFilterFactory<TSession, StringRequestInfo>
        where TSession : AppSession<TSession, StringRequestInfo>, new()
    {
        private readonly IReceiveFilter<StringRequestInfo> receiveFilter;

        public TerminatorReceiveFilterFactory() : this("##")
        {

        }
        public TerminatorReceiveFilterFactory(string terminator) : this(Encoding.UTF8, terminator)
        {

        }

        public TerminatorReceiveFilterFactory(Encoding encoding, string terminator)
            : this(new TerminatorReceiveFilter(encoding, terminator))
        {

        }

        public TerminatorReceiveFilterFactory(IReceiveFilter<StringRequestInfo> receiveFilter)
        {
            this.receiveFilter = receiveFilter;
        }

        public IReceiveFilter<StringRequestInfo> CreateFilter(TSession session)
        {
            return receiveFilter;
        }
    }
}
