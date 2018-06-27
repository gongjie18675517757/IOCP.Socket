using System;

namespace IOCP.SocketCore.TCPServer.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var  tcpServer = new SocketServer.TcpServer();
            tcpServer.NewConnection += TcpServer_NewConnection;
            tcpServer.ServerStartRun += TcpServer_ServerStartRun;
            tcpServer.ReceiveData += TcpServer_ReceiveData;
            tcpServer.Start(8088);

            Console.ReadLine();

        }

        private static void TcpServer_ReceiveData(object sender, SocketServer.TcpReceiveDataArges e)
        {
          //  Log($"recevie\t{e.Data.Length}");
            e.Connection.Send(e.Data);
        }

        private static void TcpServer_ServerStartRun(object sender, EventArgs e)
        {
            Log($"run..{((SocketServer.TcpServer)sender).LocalEndPoint}");
        }

        private static void TcpServer_NewConnection(object sender, SocketServer.TcpConnectionedArges e)
        {
          //  Log($"{e.Connection.RemoteEndPoint}\tconnectioned");
        }

        static void Log(object msg)
        {
            Console.WriteLine($"{DateTime.Now}\t{msg}");
        }
    }
}
