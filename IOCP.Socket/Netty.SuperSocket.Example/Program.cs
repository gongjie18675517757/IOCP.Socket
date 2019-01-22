using AspectCore.Extensions.DependencyInjection;
using AspectCore.Injector;
using DotNetty.Buffers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Netty.SuperSocket.Config;
using Netty.SuperSocket.RequestInfo;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Netty.SuperSocket.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", true, true);
                    configHost.AddEnvironmentVariables(prefix: "ASPNETCORE_");
                    if (args != null)
                        configHost.AddCommandLine(args);
                })
                   .ConfigureAppConfiguration((hostContext, configApp) =>
                   {
                       configApp.SetBasePath(Directory.GetCurrentDirectory());
                       configApp.AddJsonFile("appsettings.json", optional: true);
                       configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                       configApp.AddEnvironmentVariables();
                       if (args != null)
                           configApp.AddCommandLine(args);
                   })
                   .ConfigureServices((hostContext, services) =>
                   {
                       services.AddHostedService<SocketServer>();
                       services.AddOptions();
                       services.Configure<ServerConfig>(hostContext.Configuration.GetSection("ServerConfig"));
                       services.AddTransient<MyServer>();
                   })
                   .UseServiceProviderFactory(new ServiceProviderFactory())
                   .UseConsoleLifetime()
                   .ConfigureLogging((HostBuilderContext hostContext, ILoggingBuilder configLogging) =>
                   {
                       configLogging.AddConsole();
                       if (hostContext.HostingEnvironment.EnvironmentName == EnvironmentName.Development)
                           configLogging.AddDebug();
                   });

            await builder.RunConsoleAsync();
        }
    }

    public class MySession : AppSession<MySession, StringRequestInfo>
    {
        public override async Task OnReceiveDataAsync(IByteBuffer buffer)
        {
            //var bytes = new byte[] { 0x01, 0x02 };
            //var buffer = e.ReadBytes(e.ReadableBytes);
             await Send(buffer); 
            await base.OnReceiveDataAsync(buffer);
        }
    }

    public class MyServer : APPServer<MySession, StringRequestInfo>
    {
        public MyServer(IOptions<ServerConfig> options) : base(options)
        {
        }
    }

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
            foreach (var item in myServer.LocalAddress)
            {
                logger.LogInformation(item.ToString());
            }

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

        private void E_Received(object sender, IByteBuffer e)
        {
            //var session = sender as MySession;
            //var bytes = new byte[] { 0x01, 0x02 };
            //var buffer = e.ReadBytes(e.ReadableBytes);
            //session.Send(bytes, 0, bytes.Length);
        }
    }
    public class ServiceProviderFactory : IServiceProviderFactory<IServiceResolver>
    {
        public IServiceResolver CreateBuilder(IServiceCollection services)
        {
            var container = services.ToServiceContainer();
            return container.Build();
        }

        public IServiceProvider CreateServiceProvider(IServiceResolver containerBuilder)
        {
            return containerBuilder;
        }
    }
}
