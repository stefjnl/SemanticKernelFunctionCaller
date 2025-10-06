using Microsoft.SemanticKernel;
using SemanticKernelFunctionCaller.Infrastructure.Interfaces;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins;

/// <summary>
/// Plugin provider for the date/time plugin
/// </summary>
public class DateTimePluginProvider : IKernelPluginProvider
{
    /// <summary>
    /// Gets the name of the plugin
    /// </summary>
    public string Name => "DateTime";

    /// <summary>
    /// Gets the description of the plugin
    /// </summary>
    public string Description => "Provides date and time information";

    /// <summary>
    /// Creates and returns a kernel plugin for date/time functions
    /// </summary>
    /// <param name="kernel">The kernel instance</param>
    /// <returns>A kernel plugin</returns>
    public KernelPlugin CreatePlugin(Kernel kernel)
    {
        // Create the plugin from the DateTimePlugin class
        return KernelPluginFactory.CreateFromObject(new DateTimePlugin(), Name);
    }
}