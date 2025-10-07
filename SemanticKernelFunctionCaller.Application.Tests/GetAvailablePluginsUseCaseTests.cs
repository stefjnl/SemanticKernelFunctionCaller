using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.UseCases;
using Moq;
using Xunit;

namespace SemanticKernelFunctionCaller.Application.Tests;

public class GetAvailablePluginsUseCaseTests
{
    private readonly Mock<ILogger<GetAvailablePluginsUseCase>> _mockLogger;
    private readonly Kernel _kernel;
    private readonly GetAvailablePluginsUseCase _useCase;

    public GetAvailablePluginsUseCaseTests()
    {
        _mockLogger = new Mock<ILogger<GetAvailablePluginsUseCase>>();
        
        // Create a real kernel with no plugins
        _kernel = Kernel.CreateBuilder().Build();
        
        _useCase = new GetAvailablePluginsUseCase(_kernel, _mockLogger.Object);
    }
    [Fact]
    public void Execute_WithNoPlugins_ShouldReturnEmptyList()
    {
        // Act
        var result = _useCase.Execute();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving available plugins")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Execute_ShouldLogRetrievingPlugins()
    {
        // Act
        _useCase.Execute();

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving available plugins")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}