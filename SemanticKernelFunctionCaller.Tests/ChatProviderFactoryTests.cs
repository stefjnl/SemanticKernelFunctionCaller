using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moq;
using SemanticKernelFunctionCaller.Domain.Enums;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using SemanticKernelFunctionCaller.Application.Interfaces;
using Xunit;

namespace SemanticKernelFunctionCaller.Tests;

public class ChatProviderFactoryTests
{
    private readonly Mock<IOptions<ProviderSettings>> _mockProviderSettings;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ChatProviderFactory>> _mockLogger;
    private readonly Mock<Func<string, string, string, IChatClient>> _mockChatClientFactory;
    private readonly Mock<IOptions<ChatSettings>> _mockChatSettings;
    private readonly ChatProviderFactory _factory;

    public ChatProviderFactoryTests()
    {
        _mockProviderSettings = new Mock<IOptions<ProviderSettings>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ChatProviderFactory>>();
        _mockChatClientFactory = new Mock<Func<string, string, string, IChatClient>>();
        _mockChatSettings = new Mock<IOptions<ChatSettings>>();

        // Setup provider settings
        var providerSettings = new ProviderSettings
        {
            OpenRouter = new ProviderConfig
            {
                ApiKey = "test-openrouter-key",
                Endpoint = "https://openrouter.ai/api/v1",
                SystemPrompt = "You are a helpful assistant.",
                Models = new List<ModelInfo> { new() { Id = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo" } }
            },
            NanoGPT = new ProviderConfig
            {
                ApiKey = "test-nanogpt-key",
                Endpoint = "https://nanogpt.ai/api/v1",
                Models = new List<ModelInfo> { new() { Id = "nano-gpt", DisplayName = "Nano GPT" } }
            }
        };

        var chatSettings = new ChatSettings
        {
            DefaultSystemPrompt = "Default system prompt",
            EnableSystemPrompt = true
        };

        _mockProviderSettings.Setup(x => x.Value).Returns(providerSettings);
        _mockChatSettings.Setup(x => x.Value).Returns(chatSettings);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        _factory = new ChatProviderFactory(
            _mockProviderSettings.Object,
            _mockLogger.Object,
            _mockLoggerFactory.Object,
            _mockChatClientFactory.Object,
            _mockChatSettings.Object);
    }

    [Theory]
    [InlineData("OpenRouter", "gpt-3.5-turbo")]
    [InlineData("openrouter", "gpt-3.5-turbo")] // Test case insensitive
    [InlineData("NanoGPT", "nano-gpt")]
    [InlineData("nanogpt", "nano-gpt")] // Test case insensitive
    public void CreateProvider_WithValidProviderNames_ReturnsProviderInstance(string providerName, string modelId)
    {
        // Arrange
        _mockChatClientFactory.Setup(f => f(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Mock.Of<IChatClient>());

        // Act
        var result = _factory.CreateProvider(providerName, modelId);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISemanticKernelFunctionCaller>(result);
    }

    [Fact]
    public void CreateProvider_WithInvalidProviderName_ThrowsNotSupportedException()
    {
        // Arrange
        var invalidProviderName = "InvalidProvider";

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            _factory.CreateProvider(invalidProviderName, "some-model"));

        Assert.Contains("Provider 'InvalidProvider' is not a valid provider type", exception.Message);
    }

    [Fact]
    public void CreateProvider_WithUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var unsupportedProvider = "Claude"; // Not in our enum

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            _factory.CreateProvider(unsupportedProvider, "claude-3"));

        Assert.Contains("Provider 'Claude' is not supported", exception.Message);
    }

    [Fact]
    public void CreateProvider_WhenProviderCreationFails_ThrowsAndLogsError()
    {
        // Arrange
        var providerName = "OpenRouter";
        var modelId = "gpt-3.5-turbo";

        _mockChatClientFactory.Setup(f => f(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Chat client creation failed"));

        // Act & Assert
        var exception = Assert.Throws<Exception>(() =>
            _factory.CreateProvider(providerName, modelId));

        Assert.Equal("Chat client creation failed", exception.Message);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create provider OpenRouter with model gpt-3.5-turbo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateProvider_OpenRouter_UsesCorrectConfiguration()
    {
        // Arrange
        var expectedApiKey = "test-openrouter-key";
        var expectedEndpoint = "https://openrouter.ai/api/v1";
        var modelId = "gpt-3.5-turbo";

        _mockChatClientFactory.Setup(f => f(expectedApiKey, modelId, expectedEndpoint))
            .Returns(Mock.Of<IChatClient>())
            .Verifiable();

        // Act
        var result = _factory.CreateProvider("OpenRouter", modelId);

        // Assert
        Assert.NotNull(result);
        _mockChatClientFactory.Verify(f => f(expectedApiKey, modelId, expectedEndpoint), Times.Once);
    }

    [Fact]
    public void CreateProvider_NanoGPT_UsesCorrectConfiguration()
    {
        // Arrange
        var expectedApiKey = "test-nanogpt-key";
        var expectedEndpoint = "https://nanogpt.ai/api/v1";
        var modelId = "nano-gpt";

        _mockChatClientFactory.Setup(f => f(expectedApiKey, modelId, expectedEndpoint))
            .Returns(Mock.Of<IChatClient>())
            .Verifiable();

        // Act
        var result = _factory.CreateProvider("NanoGPT", modelId);

        // Assert
        Assert.NotNull(result);
        _mockChatClientFactory.Verify(f => f(expectedApiKey, modelId, expectedEndpoint), Times.Once);
    }
}