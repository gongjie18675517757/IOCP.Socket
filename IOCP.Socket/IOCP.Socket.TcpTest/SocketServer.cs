using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using IOCP.Socket.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCP.Socket.TcpTest
{
    public class SocketServer : BackgroundService
    {
        private readonly MyServer myServer;
        private readonly ILogger<SocketServer> logger;

        public SocketServer(MyServer myServer, ILogger<SocketServer> logger)
        {
            this.myServer = myServer;
            this.logger = logger;
            myServer.ServerStarted += MyServer_ServerStarted;
            myServer.NewSession += TcpServer_NewConnection1;
            myServer.ServerStarted += TcpServer_ServerStartRun1;
        }

        private void MyServer_ServerStarted(object sender, EventArgs e)
        {
            logger.LogInformation($"服务已启动!");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return myServer.RunAsync(stoppingToken);
        }

        private void TcpServer_ServerStartRun1(object sender, EventArgs e)
        {
            var server = sender as MyServer;
            Console.WriteLine(server.LocalEndPoint);
        }

        private void TcpServer_NewConnection1(object sender, MySession e)
        {
            var server = sender as MyServer;
            logger.LogInformation($"新连接\t{e.RemoteEndPoint}\tcount={server.ConnectionCount}");
            e.Received += E_Received;
            e.Closed += delegate
            {
                e.Received -= E_Received;
                logger.LogInformation($"连接断开\t{e.RemoteEndPoint}\tcount={server.ConnectionCount}");
            };
        }

        private void E_Received(object sender, DataEventArgs e)
        {
            var client = sender as MySession;
            client.SendAsync(e.Buffer,e.Index,e.Length);
        }
    }
}
