using IOCP.SocketCore;
using System;

namespace IOCP.SocketCore.TCPServerTest
{
    class Program: SocketClient
    { 
        static void Main(string[] args)
        {
            var tcpServer = new TcpServer(10240);
            tcpServer.NewConnection += TcpServer_NewConnection1;
            tcpServer.Disconnected += TcpServer_Disconnected1;
            tcpServer.ServerStartRun += TcpServer_ServerStartRun;
            tcpServer.ServerStopRun += TcpServer_ServerStopRun;
            tcpServer.ReceiveData += TcpServer_ReceiveData1;
            tcpServer.Start(8088);
            Console.ReadLine();
        }

        private static void TcpServer_ServerStopRun(object sender, EventArgs e)
        {
            Log($"stop..{((TcpServer)sender).LocalEndPoint}");
        }

        private static void TcpServer_ReceiveData1(object sender, ReceiveDataArges e)
        {
            (sender as TcpServer).SendAsync(e.Client, e.Data);
            //Log($"recevie\t{e.Data.Length}");
            var body = System.Text.Encoding.UTF8.GetString(e.Data);
        }

        private static void TcpServer_Disconnected1(object sender, SocketClient e)
        {
            //Log($"{e.Id}\t{e.RemoteEndPoint}\tDisconnected");
        }

        private static void TcpServer_NewConnection1(object sender, SocketClient e)
        {
            //Log($"{e.Id}\t{e.RemoteEndPoint}\tconnectioned");
        } 
        private static void TcpServer_ServerStartRun(object sender, EventArgs e)
        {
            //Log($"run..{((TcpServer)sender).LocalEndPoint}");
        } 
        static void Log(object msg)
        {
            Console.WriteLine($"{DateTime.Now}\t{msg}");
        }
    }
}
