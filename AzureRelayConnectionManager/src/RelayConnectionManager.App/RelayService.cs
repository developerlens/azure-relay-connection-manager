using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    public class RelayService : IHostedService, IDisposable
    {
        private readonly ILogger<RelayService> Logger;
        private readonly IConfiguration AppConfiguration;
        private readonly IHybridConnectionReverseProxy HybridConnectionReverseProxy;

        public RelayService(ILogger<RelayService> logger, IConfiguration appConfiguration, IHybridConnectionReverseProxy hybridConnectionReverseProxy)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AppConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            HybridConnectionReverseProxy = hybridConnectionReverseProxy ?? throw new ArgumentNullException(nameof(hybridConnectionReverseProxy));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var configFilePath = AppConfiguration.GetValue<string>("Config:FilePath");
                var configFileName = AppConfiguration.GetValue<string>("Config:FileName");

                if (string.IsNullOrEmpty(configFilePath) && string.IsNullOrEmpty(configFileName))
                {
                    Logger.LogError("Could Not Load Json Config File");
                }

                if (File.Exists(Path.Combine(configFilePath, configFileName)))
                {
                    var filePath = Path.Combine(configFilePath, configFileName);
                    var entry = JsonConvert.DeserializeObject<RelayEntry>(File.ReadAllText(filePath));

                    if (entry != null && entry.RelayItems.Count > 0)
                    {
                        var relayItems = entry.RelayItems;

                        Parallel.ForEach(relayItems, async relayItem =>
                        {
                            if (!string.IsNullOrEmpty(relayItem.EntityPath)
                            && !string.IsNullOrEmpty(relayItem.ForwardTo)
                            && !string.IsNullOrEmpty(relayItem.Key)
                            && !string.IsNullOrEmpty(relayItem.KeyName)
                            && !string.IsNullOrEmpty(relayItem.RelayNamespace))
                            {
                                await HybridConnectionReverseProxy.OpenAsync(new ConnectionListener()
                                {
                                    Key = relayItem.Key,
                                    KeyName = relayItem.KeyName,
                                    Namespace = relayItem.RelayNamespace,
                                    Path = relayItem.EntityPath,
                                    TargetUrl = relayItem.ForwardTo
                                }, cancellationToken);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString(), ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                var configFilePath = AppConfiguration.GetValue<string>("Config:FilePath");
                var configFileName = AppConfiguration.GetValue<string>("Config:FileName");

                if (string.IsNullOrEmpty(configFilePath) && string.IsNullOrEmpty(configFileName))
                {
                    Logger.LogError("Could Not Load Json Config File");
                }

                if (File.Exists(Path.Combine(configFilePath, configFileName)))
                {
                    var filePath = Path.Combine(configFilePath, configFileName);
                    var entry = JsonConvert.DeserializeObject<RelayEntry>(File.ReadAllText(filePath));

                    if (entry != null && entry.RelayItems.Count > 0)
                    {
                        var relayItems = entry.RelayItems;

                        Parallel.ForEach(relayItems, async relayItem =>
                        {
                            if (!string.IsNullOrEmpty(relayItem.EntityPath)
                            && !string.IsNullOrEmpty(relayItem.ForwardTo)
                            && !string.IsNullOrEmpty(relayItem.Key)
                            && !string.IsNullOrEmpty(relayItem.KeyName)
                            && !string.IsNullOrEmpty(relayItem.RelayNamespace))
                            {
                                await HybridConnectionReverseProxy.CloseAsync(new ConnectionListener()
                                {
                                    Key = relayItem.Key,
                                    KeyName = relayItem.KeyName,
                                    Namespace = relayItem.RelayNamespace,
                                    Path = relayItem.EntityPath,
                                    TargetUrl = relayItem.ForwardTo
                                }, cancellationToken);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString(), ex);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BGTaskRunnerService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
