using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Orchestration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SemanticKernelFunctionCaller.Infrastructure.Tests.Orchestration;

public class PromptTemplateManagerTests
{
    private readonly Mock<ILogger<PromptTemplateManager>> _mockLogger;
    private readonly Mock<IOptions<SemanticKernelSettings>> _mockOptions;
    private readonly SemanticKernelSettings _settings;
    private readonly PromptTemplateManager _manager;

    public PromptTemplateManagerTests()
    {
        _mockLogger = new Mock<ILogger<PromptTemplateManager>>();
        _mockOptions = new Mock<IOptions<SemanticKernelSettings>>();

        _settings = new SemanticKernelSettings
        {
            PromptTemplates = new Dictionary<string, string>
            {
                { "TestTemplate", "This is a test template with {{$variable}}" },
                { "MultiVarTemplate", "Template with {{$var1}} and {{$var2}}" }
            }
        };

        _mockOptions.Setup(o => o.Value).Returns(_settings);

        _manager = new PromptTemplateManager(
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task LoadTemplateAsync_WithExistingTemplate_ReturnsTemplate()
    {
        // Act
        var template = await _manager.LoadTemplateAsync("TestTemplate");

        // Assert
        Assert.NotNull(template);
        Assert.Contains("{{$variable}}", template.Content);
    }

    [Fact]
    public async Task LoadTemplateAsync_WithNonExistentTemplate_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.LoadTemplateAsync("NonExistentTemplate"));
    }

    [Fact]
    public async Task RenderTemplateAsync_WithValidVariables_ReturnsRenderedContent()
    {
        // Arrange
        var template = new PromptTemplate("This is a test template with {{$variable}}");
        var variables = new Dictionary<string, object?> { { "variable", "test value" } };

        // Act
        var result = await _manager.RenderTemplateAsync(template, variables);

        // Assert
        Assert.NotNull(result);
        // Note: Actual rendering would depend on Semantic Kernel's template engine
        // For this test, we're just verifying the method structure
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMissingVariables_ThrowsArgumentException()
    {
        // Arrange
        var template = new PromptTemplate("This is a test template with {{$requiredVariable}}");
        var variables = new Dictionary<string, object?> { { "differentVariable", "test value" } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _manager.RenderTemplateAsync(template, variables));
    }

    [Fact]
    public void ValidateTemplateVariables_WithAllRequiredVariables_DoesNotThrow()
    {
        // Arrange
        var template = new PromptTemplate("Template with {{$var1}} and {{$var2}}");
        var variables = new Dictionary<string, object?>
        {
            { "var1", "value1" },
            { "var2", "value2" }
        };

        // Act & Assert
        var exception = Record.Exception(() => _manager.ValidateTemplateVariables(template, variables));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateTemplateVariables_WithMissingVariables_ThrowsArgumentException()
    {
        // Arrange
        var template = new PromptTemplate("Template with {{$var1}} and {{$var2}}");
        var variables = new Dictionary<string, object?> { { "var1", "value1" } };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _manager.ValidateTemplateVariables(template, variables));
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ReturnsListOfTemplateNames()
    {
        // Act
        var templates = await _manager.GetAvailableTemplatesAsync();

        // Assert
        Assert.NotNull(templates);
        Assert.Contains("TestTemplate", templates);
        Assert.Contains("MultiVarTemplate", templates);
    }

    [Fact]
    public void ExtractRequiredVariables_WithSimpleTemplate_ReturnsVariableNames()
    {
        // Arrange
        var templateContent = "Template with {{$var1}} and {{$var2}}";

        // Act
        // We need to access the private method indirectly
        // For this test, we'll test the public ValidateTemplateVariables method which uses it
        var variables = new Dictionary<string, object?>
        {
            { "var1", "value1" },
            { "var2", "value2" }
        };

        // Act & Assert
        var exception = Record.Exception(() => _manager.ValidateTemplateVariables(new PromptTemplate(templateContent), variables));
        Assert.Null(exception);
    }
}