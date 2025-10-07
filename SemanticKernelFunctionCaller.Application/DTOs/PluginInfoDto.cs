namespace SemanticKernelFunctionCaller.Application.DTOs;

/// <summary>
/// Represents information about a plugin function.
/// </summary>
public class PluginInfoDto
{
    /// <summary>
    /// Gets or sets the name of the plugin.
    /// </summary>
    public required string PluginName { get; set; }

    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public required string FunctionName { get; set; }

    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the list of parameters for the function.
    /// </summary>
    public required List<ParameterInfoDto> Parameters { get; set; }
}

/// <summary>
/// Represents information about a function parameter.
/// </summary>
public class ParameterInfoDto
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the type of the parameter.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets whether the parameter is required.
    /// </summary>
    public required bool IsRequired { get; set; }
}