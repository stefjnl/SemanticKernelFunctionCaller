namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Interface for the mediator pattern implementation
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request to the appropriate handler
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The request to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response from the handler</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}