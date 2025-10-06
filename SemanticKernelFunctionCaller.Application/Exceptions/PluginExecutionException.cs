using System;

namespace SemanticKernelFunctionCaller.Application.Exceptions;

/// <summary>
/// Exception thrown when a plugin execution fails
/// </summary>
public class PluginExecutionException : Exception
{
    /// <summary>
    /// Gets the name of the plugin that failed
    /// </summary>
    public string PluginName { get; }

    /// <summary>
    /// Gets a value indicating whether the failure is transient and can be retried
    /// </summary>
    public bool IsTransient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginExecutionException"/> class
    /// </summary>
    /// <param name="pluginName">The name of the plugin that failed</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    /// <param name="isTransient">Whether the failure is transient</param>
    public PluginExecutionException(string pluginName, string message, Exception? innerException = null, bool isTransient = false)
        : base(message, innerException)
    {
        PluginName = pluginName;
        IsTransient = isTransient;
    }
}