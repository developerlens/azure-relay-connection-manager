using System.Threading;
using System.Threading.Tasks;

namespace RelayConnectionManager.App
{
    public interface IHybridConnectionReverseProxy
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionListener"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task CloseAsync(ConnectionListener connectionListener, CancellationToken cancelToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionListener"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task OpenAsync(ConnectionListener connectionListener, CancellationToken cancelToken);
    }
}