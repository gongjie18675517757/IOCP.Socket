using IOCP.Socket.Core;
using IOCP.Socket.Core.Config;
using IOCP.Socket.Core.RequestInfo;
using Microsoft.Extensions.Options;

namespace IOCP.Socket.TcpTest
{
    public class MyServer : APPServer<MySession, StringRequestInfo>
    {
        public MyServer(IOptions<ServerConfig> options) : base(options)
        {
        }
    }
}
