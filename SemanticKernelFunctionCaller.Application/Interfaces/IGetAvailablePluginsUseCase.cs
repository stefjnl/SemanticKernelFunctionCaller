using SemanticKernelFunctionCaller.Application.DTOs;

namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Defines a contract for retrieving available plugins and their functions.
/// </summary>
public interface IGetAvailablePluginsUseCase
{
    /// <summary>
    /// Retrieves all available plugins and their functions from the kernel.
    /// </summary>
    /// <returns>A collection of plugin information including functions and parameters.</returns>
    IEnumerable<PluginInfoDto> Execute();
}