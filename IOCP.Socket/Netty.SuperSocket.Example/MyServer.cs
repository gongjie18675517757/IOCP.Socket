using Microsoft.Extensions.Options;
using Netty.SuperSocket.Config;
using Netty.SuperSocket.RequestInfo;

namespace Netty.SuperSocket.Example
{
    public class MyServer : APPServer<MySession, StringRequestInfo>
    {
        public MyServer(IOptions<ServerConfig> options) : base(options)
        {
        }
    }
}
