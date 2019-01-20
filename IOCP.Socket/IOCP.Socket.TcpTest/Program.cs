using System.ComponentModel.Design;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IOCP.Socket.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCP.Socket.TcpTest
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
}
