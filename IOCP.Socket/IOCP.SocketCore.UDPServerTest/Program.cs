using IOCP.SocketCore.UdpSocketServer;
using System;

namespace IOCP.SocketCore.UDPServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpServer udpServer = new UdpServer(1024);
            udpServer.ReceiveData += UdpServer_OnReceiveData;
            udpServer.ServerStartRun += UdpServer_ServerStartRun;
            udpServer.Start(8088);

            Console.ReadLine();

            udpServer.Stop();

            udpServer.Start(8088);

            Console.ReadLine();

            udpServer.Stop();

            Console.ReadLine();

        }

        private static void UdpServer_ServerStartRun(object sender, EventArgs e)
        {
            UdpServer udpServer = (UdpServer)sender;
            Console.WriteLine($"{udpServer.LocalEndPoint}\tstart..");
        }

        private static void UdpServer_OnReceiveData(object sender, ReceiveDataEventArgs e)
        {
            UdpServer udpServer = (UdpServer)sender;
            udpServer.SendAsync(e.EndPoint, e.Data);
        }
    }
}
