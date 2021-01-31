
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using RelayConnectionManager.App;

using System.Threading;
using System.Threading.Tasks;

namespace RelayConnectionManager.App.Extensions
{
    public static class ServiceBaseLiveTimeHostExtension
    {
        public static IHostBuilder UseServiceBaseLifetime(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((hostContext,
                services) => services.AddSingleton<IHostLifetime,
                ServiceBaseLifeTime>());
        }

        public static Task RunAsServiceAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
        {
            return hostBuilder
                .UseServiceBaseLifetime()
                .Build()
                .RunAsync(cancellationToken);
        }
    }
}
