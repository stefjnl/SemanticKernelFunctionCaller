using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using Moq;
using Xunit;

namespace SemanticKernelFunctionCaller.Application.Tests;

public class SendChatMessageUseCaseTests
{
    private readonly Mock<IProviderFactory> _mockProviderFactory;
    private readonly Mock<ISemanticKernelFunctionCaller> _mockProvider;
    private readonly SendChatMessageUseCase _useCase;

    public SendChatMessageUseCaseTests()
    {
        _mockProviderFactory = new Mock<IProviderFactory>();
        _mockProvider = new Mock<ISemanticKernelFunctionCaller>();
        _useCase = new SendChatMessageUseCase(_mockProviderFactory.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldMapDtoToDomainAndReturnResponse()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "Hello" },
                new MessageDto { Role = ChatRole.Assistant, Content = "Hi there" }
            }
        };

        var expectedResponse = new ChatResponse
        {
            Message = new ChatMessage { Role = ChatRole.Assistant, Content = "Hello! How can I help you?" },
            ModelUsed = "test-model",
            ProviderUsed = "OpenRouter"
        };

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), default))
                    .ReturnsAsync(expectedResponse);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello! How can I help you?", result.Content);
        Assert.Equal("test-model", result.ModelId);
        Assert.Equal("OpenRouter", result.ProviderId);

        _mockProviderFactory.Verify(f => f.CreateProvider("OpenRouter", "test-model"), Times.Once);
        _mockProvider.Verify(p => p.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), default), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyMessages_ShouldStillCallProvider()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "NanoGPT",
            ModelId = "nano-model",
            Messages = new List<MessageDto>()
        };

        var expectedResponse = new ChatResponse
        {
            Message = new ChatMessage { Role = ChatRole.Assistant, Content = "I need context to respond." },
            ModelUsed = "nano-model",
            ProviderUsed = "NanoGPT"
        };

        _mockProviderFactory.Setup(f => f.CreateProvider("NanoGPT", "nano-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), default))
                    .ReturnsAsync(expectedResponse);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("I need context to respond.", result.Content);

        _mockProviderFactory.Verify(f => f.CreateProvider("NanoGPT", "nano-model"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProviderThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "Test" }
            }
        };

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), default))
                    .ThrowsAsync(new Exception("Provider error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _useCase.ExecuteAsync(request));

        _mockProviderFactory.Verify(f => f.CreateProvider("OpenRouter", "test-model"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapMessagesCorrectly()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.System, Content = "You are a helpful assistant." },
                new MessageDto { Role = ChatRole.User, Content = "Hello" },
                new MessageDto { Role = ChatRole.Assistant, Content = "Hi! How can I help?" },
                new MessageDto { Role = ChatRole.User, Content = "Tell me a joke." }
            }
        };

        var expectedResponse = new ChatResponse
        {
            Message = new ChatMessage { Role = ChatRole.Assistant, Content = "Why did the chicken cross the road? To get to the other side!" },
            ModelUsed = "test-model",
            ProviderUsed = "OpenRouter"
        };

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.SendMessageAsync(It.Is<IEnumerable<ChatMessage>>(messages =>
            messages.Count() == 4 &&
            messages.ElementAt(0).Role == ChatRole.System &&
            messages.ElementAt(0).Content == "You are a helpful assistant." &&
            messages.ElementAt(1).Role == ChatRole.User &&
            messages.ElementAt(1).Content == "Hello" &&
            messages.ElementAt(2).Role == ChatRole.Assistant &&
            messages.ElementAt(2).Content == "Hi! How can I help?" &&
            messages.ElementAt(3).Role == ChatRole.User &&
            messages.ElementAt(3).Content == "Tell me a joke."
        ), default)).ReturnsAsync(expectedResponse);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal("Why did the chicken cross the road? To get to the other side!", result.Content);
        _mockProvider.Verify(p => p.SendMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), default), Times.Once);
    }
}

