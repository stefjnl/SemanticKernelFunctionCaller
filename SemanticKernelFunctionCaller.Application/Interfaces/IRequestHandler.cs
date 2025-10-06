namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Interface for request handlers
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}