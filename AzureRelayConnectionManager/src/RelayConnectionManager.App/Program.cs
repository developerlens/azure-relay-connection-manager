using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RelayConnectionManager.App.Extensions;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var runAsService = !(Debugger.IsAttached || args.Contains("--console"));
            var hostBuilder = await CreateHostBuilder(args);

            if (runAsService)
            {
                await hostBuilder.RunAsServiceAsync();
            }
            else
            {
                await hostBuilder.RunConsoleAsync();
            }
        }

        public static Task<IHostBuilder> CreateHostBuilder(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var hostBuilder = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((hostingcontext, config) =>
                    {
                        config.AddJsonFile($"appsettings.json", true, true);
                        config.AddJsonFile($"appsettings.{env}.json", true, true);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddOptions();
                        services.AddHttpClient();
                        services.AddTransient<IHybridConnectionReverseProxy, HybridConnectionReverseProxy>();
                        services.AddHostedService<RelayService>();
                    });

            return Task.FromResult(hostBuilder);
        }
    }
}
