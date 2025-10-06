namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Marker interface for requests
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Marker interface for requests with a response
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IRequest<out TResponse> : IRequest
{
}