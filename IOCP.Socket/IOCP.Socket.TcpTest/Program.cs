using System;
using System.Buffers;
using System.Text;
using IOCP.Socket.Core;
namespace IOCP.Socket.TcpTest
{
    class Program
    {
        static void Main(string[] args)
        {
            APPServer<AppSession> tcpServer = new APPServer<AppSession>();
            tcpServer.NewConnection += TcpServer_NewConnection;
            tcpServer.ServerStartRun += TcpServer_ServerStartRun;
            tcpServer.Start();

            Console.ReadLine();
        }

        private static void TcpServer_ServerStartRun(object sender, EventArgs e)
        {
            var server = sender as APPServer<AppSession>;
            Console.WriteLine(server.LocalEndPoint);
        }

        private static void TcpServer_NewConnection(object sender, AppSession e)
        {
            var server = sender as APPServer<AppSession>;
            Console.WriteLine($"新连接\tcount={server.ConnectionCount}");
            e.Received += E_Received;
            e.Closed += delegate
             {
                 e.Received -= E_Received;
                 Console.WriteLine($"连接断开\tcount={server.ConnectionCount}");
             };
        }

        private static void E_Received(object sender, EventArgs e)
        {
            var client = sender as AppSession;
            client.PipeReader.ReadAsync().AsTask().ContinueWith(t =>
            {
                var readResult = t.Result;
                client.PipeReader.AdvanceTo(readResult.Buffer.End);
                //var str = Encoding.Default.GetString(readResult.Buffer.ToArray());
                //Console.WriteLine(str);
                client.SendAsync(readResult.Buffer.ToArray());
            });
        }
    }
}
