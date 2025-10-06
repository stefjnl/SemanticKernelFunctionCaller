using Microsoft.SemanticKernel;
using SemanticKernelFunctionCaller.Infrastructure.Interfaces;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins;

/// <summary>
/// Plugin provider for the weather plugin
/// </summary>
public class WeatherPluginProvider : IKernelPluginProvider
{
    /// <summary>
    /// Gets the name of the plugin
    /// </summary>
    public string Name => "Weather";

    /// <summary>
    /// Gets the description of the plugin
    /// </summary>
    public string Description => "Provides weather information for locations";

    /// <summary>
    /// Creates and returns a kernel plugin for weather functions
    /// </summary>
    /// <param name="kernel">The kernel instance</param>
    /// <returns>A kernel plugin</returns>
    public KernelPlugin CreatePlugin(Kernel kernel)
    {
        // Create the plugin from the WeatherPlugin class
        return KernelPluginFactory.CreateFromObject(new WeatherPlugin(), Name);
    }
}