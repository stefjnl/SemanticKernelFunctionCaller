using Microsoft.SemanticKernel;

namespace SemanticKernelFunctionCaller.Infrastructure.Interfaces;

/// <summary>
/// Interface for kernel plugin providers
/// </summary>
public interface IKernelPluginProvider
{
    /// <summary>
    /// Gets the name of the plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the plugin
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Creates and returns a kernel plugin
    /// </summary>
    /// <param name="kernel">The kernel instance</param>
    /// <returns>A kernel plugin</returns>
    KernelPlugin CreatePlugin(Kernel kernel);
}