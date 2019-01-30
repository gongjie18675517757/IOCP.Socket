using Microsoft.Extensions.Options;
using Netty.SuperSocket.Config;
using Netty.SuperSocket.ReceiveFilter;
using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.Example
{
    public class MyServer : APPServer<MySession, StringRequestInfo>
    {
        public MyServer(IOptions<ServerConfig> options) : base(options, new TerminatorReceiveFilterFactory<MySession>("#####"))
        {
        }
    }
}
