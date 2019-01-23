using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Netty.SuperSocket.Config;
using System;
using System.IO;
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
                       var x = hostContext.Configuration.GetSection("ServerConfig").ToString();
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
