using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins;

/// <summary>
/// A sample date/time plugin that provides date and time information
/// </summary>
public class DateTimePlugin
{
    /// <summary>
    /// Gets the current date and time
    /// </summary>
    /// <returns>A string with the current date and time</returns>
    [KernelFunction]
    [Description("Gets the current date and time")]
    public string GetCurrentDateTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Gets the current date
    /// </summary>
    /// <returns>A string with the current date</returns>
    [KernelFunction]
    [Description("Gets the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Gets the current time
    /// </summary>
    /// <returns>A string with the current time</returns>
    [KernelFunction]
    [Description("Gets the current time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }
}