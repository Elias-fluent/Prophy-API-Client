using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prophy.ApiClient.Modules
{
    /// <summary>
    /// Interface for resilience operations like retry, circuit breaker, and timeout.
    /// Simplified version for .NET Framework 4.8 compatibility.
    /// </summary>
    public interface IResilienceModule
    {
        /// <summary>
        /// Executes an asynchronous operation with resilience policies applied.
        /// </summary>
        /// <typeparam name="T">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation with the result.</returns>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an asynchronous operation with resilience policies applied.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    }
} 