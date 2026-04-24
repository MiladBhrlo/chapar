namespace Chapar.Core.Abstractions;


/// <summary>
/// Abstraction for request/response messaging pattern.
/// The client is specialised for one request‑response pair and is typically
/// registered in the DI container for a specific <typeparamref name="TRequest"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the outgoing request (must implement <see cref="IMessage"/>).</typeparam>
/// <typeparam name="TResponse">The type of the expected response (any serializable type).</typeparam>
public interface IChaparRequestClient<TRequest, TResponse>
    where TRequest : class, IMessage
{
    /// <summary>
    /// Sends a request and waits for the corresponding response.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The response message.</returns>
    Task<TResponse> GetResponseAsync(TRequest request, CancellationToken cancellationToken = default);
}