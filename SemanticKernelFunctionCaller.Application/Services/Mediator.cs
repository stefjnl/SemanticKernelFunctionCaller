using Microsoft.Extensions.DependencyInjection;
using SemanticKernelFunctionCaller.Application.Interfaces;
using System.Reflection;

namespace SemanticKernelFunctionCaller.Application.Services;

/// <summary>
/// Mediator implementation that dispatches requests to handlers
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Get the handler type for this request
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        
        // Resolve the handler from the service provider
        var handler = _serviceProvider.GetRequiredService(handlerType);
        
        // Invoke the Handle method
        var method = handlerType.GetMethod("Handle");
        if (method == null)
        {
            throw new InvalidOperationException($"Handler for {request.GetType()} does not contain a Handle method");
        }
        
        var result = await (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken });
        return result;
    }
}