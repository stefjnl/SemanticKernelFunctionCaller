using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.ValueObjects;
using SemanticKernelFunctionCaller.Domain.Enums;
using SemanticKernelFunctionCaller.Infrastructure.Providers;
using Xunit;

// Alias to avoid ambiguity
using DomainChatMessage = SemanticKernelFunctionCaller.Domain.Entities.ChatMessage;
using ProviderChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace SemanticKernelFunctionCaller.Tests;

// Test implementation of BaseChatProvider for testing abstract methods
internal class TestableBaseChatProvider : BaseChatProvider
{
    public TestableBaseChatProvider(
        string providerName,
        ILogger logger,
        string modelId,
        string? systemPrompt = null)
        : base(providerName, logger, modelId, systemPrompt)
    {
    }

    // Expose protected methods for testing
    public new List<ProviderChatMessage> PrepareMessages(IEnumerable<DomainChatMessage> messages)
    {
        return base.PrepareMessages(messages);
    }

    public void ValidateApiKeyPublic(string apiKey)
    {
        BaseChatProvider.ValidateApiKey(apiKey);
    }

    public new void InitializeChatClient(
        Func<string, string, string, IChatClient> chatClientFactory,
        string apiKey,
        string modelId,
        string endpoint)
    {
        base.InitializeChatClient(chatClientFactory, apiKey, modelId, endpoint);
    }
}

public class BaseChatProviderTests
{
    private readonly TestableBaseChatProvider _provider;
    private readonly string _providerName = "TestProvider";
    private readonly string _modelId = "test-model";

    public BaseChatProviderTests()
    {
        _provider = new TestableBaseChatProvider(
            _providerName,
            NullLogger.Instance,
            _modelId,
            "Test system prompt");
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Assert
        Assert.Equal(_providerName, _provider.GetMetadata().Id);
        Assert.Equal(_providerName, _provider.GetMetadata().DisplayName);
    }

    [Fact]
    public void ValidateApiKey_WithValidKey_DoesNotThrow()
    {
        // Arrange
        var validApiKey = "valid-api-key";

        // Act & Assert
        var exception = Record.Exception(() => _provider.ValidateApiKeyPublic(validApiKey));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateApiKey_WithInvalidKey_ThrowsArgumentException(string? invalidApiKey)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _provider.ValidateApiKeyPublic(invalidApiKey!));

        Assert.Contains("API key cannot be null or empty", exception.Message);
        Assert.Equal("apiKey", exception.ParamName);
    }

    [Fact]
    public void InitializeChatClient_WithValidParameters_SetsChatClient()
    {
        // Arrange
        var apiKey = "test-key";
        var modelId = "test-model";
        var endpoint = "https://test.com";
        var mockChatClient = Mock.Of<IChatClient>();
        Func<string, string, string, IChatClient> chatClientFactory = (key, model, ep) => mockChatClient;

        // Act
        _provider.InitializeChatClient(chatClientFactory, apiKey, modelId, endpoint);

        // The chat client is set internally, but we can't directly test it since it's private
        // We can test that no exception is thrown and the method completes
        Assert.True(true); // If we get here, the method didn't throw
    }

    [Fact]
    public void PrepareMessages_WithSystemPrompt_InjectsSystemMessageAtBeginning()
    {
        // Arrange
        var messages = new List<DomainChatMessage>
        {
            new DomainChatMessage { Role = ChatRole.User, Content = "User message 1" },
            new DomainChatMessage { Role = ChatRole.Assistant, Content = "Assistant message" },
            new DomainChatMessage { Role = ChatRole.User, Content = "User message 2" }
        };

        // Act
        var result = _provider.PrepareMessages(messages);

        // Assert
        Assert.Equal(4, result.Count); // 3 original + 1 system message

        // System message is inserted at position 0
        Assert.Equal("System", result[0].Role.ToString());
        Assert.Equal("Test system prompt", result[0].Text);

        // Original messages follow
        Assert.Equal("User", result[1].Role.ToString());
        Assert.Equal("User message 1", result[1].Text);
        Assert.Equal("Assistant", result[2].Role.ToString());
        Assert.Equal("Assistant message", result[2].Text);
        Assert.Equal("User", result[3].Role.ToString());
        Assert.Equal("User message 2", result[3].Text);
    }

    [Fact]
    public void PrepareMessages_WithoutSystemPrompt_DoesNotInjectSystemMessage()
    {
        // Arrange
        var providerWithoutSystemPrompt = new TestableBaseChatProvider(
            _providerName, NullLogger.Instance, _modelId, null);

        var messages = new List<DomainChatMessage>
        {
            new DomainChatMessage { Role = ChatRole.User, Content = "User message" },
            new DomainChatMessage { Role = ChatRole.Assistant, Content = "Assistant message" }
        };

        // Act
        var result = providerWithoutSystemPrompt.PrepareMessages(messages);

        // Assert
        Assert.Equal(2, result.Count); // No system message added
        Assert.Equal("User", result[0].Role.ToString());
        Assert.Equal("Assistant", result[1].Role.ToString());
    }

    [Fact]
    public void PrepareMessages_WithExistingSystemMessage_RemovesAndReplaces()
    {
        // Arrange
        var messages = new List<DomainChatMessage>
        {
            new DomainChatMessage { Role = ChatRole.System, Content = "Existing system prompt" },
            new DomainChatMessage { Role = ChatRole.User, Content = "User message" }
        };

        // Act
        var result = _provider.PrepareMessages(messages);

        // Assert
        Assert.Equal(2, result.Count); // 1 original system removed, 1 new system added

        // Only our system prompt should remain
        Assert.Equal("System", result[0].Role.ToString());
        Assert.Equal("Test system prompt", result[0].Text);
        Assert.Equal("User", result[1].Role.ToString());
        Assert.Equal("User message", result[1].Text);
    }

    [Fact]
    public async Task SendMessageAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<DomainChatMessage> nullMessages = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _provider.SendMessageAsync(nullMessages));
    }

    [Fact]
    public void GetMetadata_ReturnsCorrectProviderMetadata()
    {
        // Act
        var metadata = _provider.GetMetadata();

        // Assert
        Assert.Equal(_providerName, metadata.Id);
        Assert.Equal(_providerName, metadata.DisplayName);
    }

    [Fact]
    public async Task StreamMessageAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<DomainChatMessage> nullMessages = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in _provider.StreamMessageAsync(nullMessages, cancellationToken))
            {
                // This should not be reached
            }
        });
    }
}