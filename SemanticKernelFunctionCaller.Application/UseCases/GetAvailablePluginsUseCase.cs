using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.Application.UseCases;

/// <summary>
/// Implements the use case for retrieving available plugins and their functions.
/// </summary>
public class GetAvailablePluginsUseCase : IGetAvailablePluginsUseCase
{
    private readonly Kernel _kernel;
    private readonly ILogger<GetAvailablePluginsUseCase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAvailablePluginsUseCase"/> class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance.</param>
    /// <param name="logger">The logger instance.</param>
    public GetAvailablePluginsUseCase(Kernel kernel, ILogger<GetAvailablePluginsUseCase> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all available plugins and their functions from the kernel.
    /// </summary>
    /// <returns>A collection of plugin information including functions and parameters.</returns>
    public IEnumerable<PluginInfoDto> Execute()
    {
        _logger.LogInformation("Retrieving available plugins");
        
        var plugins = _kernel.Plugins
            .SelectMany(p => p.Select(f => new PluginInfoDto
            {
                PluginName = p.Name,
                FunctionName = f.Name,
                Description = f.Description,
                Parameters = f.Metadata.Parameters.Select(param => new ParameterInfoDto
                {
                    Name = param.Name,
                    Description = param.Description,
                    Type = param.ParameterType?.Name ?? "Unknown",
                    IsRequired = param.IsRequired
                }).ToList()
            }))
            .ToList();

        _logger.LogInformation("Found {Count} plugin functions", plugins.Count);
        return plugins;
    }
}