using Microsoft.Azure.Relay;
using Microsoft.Extensions.Logging;

using RelayConnectionManager.App.Extensions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    public class HybridConnectionReverseProxy : IHybridConnectionReverseProxy
    {
        private readonly IHttpClientFactory HttpClientFactory;
        private readonly ILogger<HybridConnectionReverseProxy> Logger;

        public HybridConnectionReverseProxy(ILogger<HybridConnectionReverseProxy> logger, IHttpClientFactory httpClientFactory)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task OpenAsync(ConnectionListener connectionListener, CancellationToken cancelToken)
        {
            var Listener = CreateListener(connectionListener);
            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(connectionListener.TargetUrl);
            client.DefaultRequestHeaders.ExpectContinue = false;
            Listener.RequestHandler = (context) => this.RequestHandler(context, Listener.Address.AbsolutePath.EnsureEndsWith("/"), connectionListener.TargetUrl);
            await Listener.OpenAsync(cancelToken);
            Logger.LogInformation($"Forwarding from {Listener.Address} to {client.BaseAddress}.");
            Logger.LogInformation("utcTime, request, statusCode, durationMs");
        }

        public Task CloseAsync(ConnectionListener connectionListener, CancellationToken cancelToken)
        {
            var Listener = CreateListener(connectionListener);
            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(connectionListener.TargetUrl);
            client.DefaultRequestHeaders.ExpectContinue = false;
            Listener.RequestHandler = (context) => this.RequestHandler(context, Listener.Address.AbsolutePath.EnsureEndsWith("/"), connectionListener.TargetUrl);
            return Listener.CloseAsync(cancelToken);
        }

        #region Helper Methods
        private HybridConnectionListener CreateListener(ConnectionListener connectionListener)
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(connectionListener.KeyName, connectionListener.Key);
            return new HybridConnectionListener(new Uri(string.Format("sb://{0}.servicebus.windows.net/{1}", connectionListener.Namespace, connectionListener.Path)), tokenProvider);
        }

        private async void RequestHandler(RelayedHttpListenerContext context, string connectionSubPath, string targetUrl)
        {
            DateTime startTimeUtc = DateTime.UtcNow;
            try
            {
                var client = HttpClientFactory.CreateClient();
                client.BaseAddress = new Uri(targetUrl);
                client.DefaultRequestHeaders.ExpectContinue = false;

                HttpRequestMessage requestMessage = CreateHttpRequestMessage(context, connectionSubPath);
                HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);
                await SendResponseAsync(context, responseMessage);
                await context.Response.CloseAsync();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e.GetType().Name}: {e.Message}");
                SendErrorResponse(e, context);
            }
            finally
            {
                LogRequest(startTimeUtc, context);
            }
        }

        private async Task SendResponseAsync(RelayedHttpListenerContext context, HttpResponseMessage responseMessage)
        {
            context.Response.StatusCode = responseMessage.StatusCode;
            context.Response.StatusDescription = responseMessage.ReasonPhrase;
            foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
            {
                if (string.Equals(header.Key, "Transfer-Encoding"))
                {
                    continue;
                }

                context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
            }

            var responseStream = await responseMessage.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(context.Response.OutputStream);
        }

        private void SendErrorResponse(Exception e, RelayedHttpListenerContext context)
        {
            context.Response.StatusCode = HttpStatusCode.InternalServerError;

#if DEBUG || INCLUDE_ERROR_DETAILS
            context.Response.StatusDescription = $"Internal Server Error: {e.GetType().FullName}: {e.Message}";
#endif
            context.Response.Close();
        }

        private HttpRequestMessage CreateHttpRequestMessage(RelayedHttpListenerContext context, string connectionSubPath)
        {
            var requestMessage = new HttpRequestMessage();
            if (context.Request.HasEntityBody)
            {
                requestMessage.Content = new StreamContent(context.Request.InputStream);
                string contentType = context.Request.Headers[HttpRequestHeader.ContentType];
                if (!string.IsNullOrEmpty(contentType))
                {
                    requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
            }

            string relativePath = context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            relativePath = relativePath.Replace(connectionSubPath, string.Empty);
            requestMessage.RequestUri = new Uri(relativePath, UriKind.RelativeOrAbsolute);
            requestMessage.Method = new HttpMethod(context.Request.HttpMethod);

            foreach (var headerName in context.Request.Headers.AllKeys)
            {
                if (string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerName, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't flow these headers here
                    continue;
                }

                requestMessage.Headers.Add(headerName, context.Request.Headers[headerName]);
            }

            return requestMessage;
        }

        private void LogRequest(DateTime startTimeUtc, RelayedHttpListenerContext context)
        {
            DateTime stopTimeUtc = DateTime.UtcNow;
            Logger.LogInformation($"{startTimeUtc.ToString("s", CultureInfo.InvariantCulture)}");
            Logger.LogInformation($"\"{context.Request.HttpMethod} {context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)}\", ");
            Logger.LogInformation($"{(int)context.Response.StatusCode}, ");
            Logger.LogInformation($"{(int)stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds}");
        }
        #endregion
    }
}
