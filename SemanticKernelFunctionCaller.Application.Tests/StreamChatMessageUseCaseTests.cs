using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using Moq;
using Xunit;
using System.Runtime.CompilerServices;

namespace SemanticKernelFunctionCaller.Application.Tests;

public class StreamChatMessageUseCaseTests
{
    private readonly Mock<IProviderFactory> _mockProviderFactory;
    private readonly Mock<ISemanticKernelFunctionCaller> _mockProvider;
    private readonly StreamChatMessageUseCase _useCase;

    public StreamChatMessageUseCaseTests()
    {
        _mockProviderFactory = new Mock<IProviderFactory>();
        _mockProvider = new Mock<ISemanticKernelFunctionCaller>();
        _useCase = new StreamChatMessageUseCase(_mockProviderFactory.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldStreamResponsesAndFinalUpdate()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "Tell me a story" }
            }
        };

        var streamingContent = new[] { "Once", " upon", " a", " time", "..." };
        var asyncEnumerable = CreateAsyncEnumerable(streamingContent);

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.StreamMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
                    .Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request));

        // Assert
        Assert.Equal(6, results.Count); // 5 streaming updates + 1 final

        // Check streaming updates
        for (int i = 0; i < streamingContent.Length; i++)
        {
            Assert.Equal(streamingContent[i], results[i].Content);
            Assert.False(results[i].IsFinal);
        }

        // Check final update
        Assert.Equal(string.Empty, results.Last().Content);
        Assert.True(results.Last().IsFinal);

        _mockProviderFactory.Verify(f => f.CreateProvider("OpenRouter", "test-model"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToProvider()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "NanoGPT",
            ModelId = "nano-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "Hello" }
            }
        };

        var cts = new CancellationTokenSource();
        var streamingContent = new[] { "Hi", " there", "!" };
        var asyncEnumerable = CreateAsyncEnumerable(streamingContent);

        _mockProviderFactory.Setup(f => f.CreateProvider("NanoGPT", "nano-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.StreamMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), cts.Token))
                    .Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request, cts.Token));

        // Assert
        Assert.Equal(4, results.Count); // 3 streaming + 1 final
        _mockProvider.Verify(p => p.StreamMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyMessages_ShouldStillWork()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>()
        };

        var streamingContent = new[] { "I", " need", " context" };
        var asyncEnumerable = CreateAsyncEnumerable(streamingContent);

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.StreamMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
                    .Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request));

        // Assert
        Assert.Equal(4, results.Count); // 3 streaming + 1 final
        Assert.True(results.Last().IsFinal);
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
                new MessageDto { Role = ChatRole.User, Content = "Continue this story: Once upon a time" }
            }
        };

        var streamingContent = new[] { "Once", " upon", " a", " time", ",", " there", " was", "..." };
        var asyncEnumerable = CreateAsyncEnumerable(streamingContent);

        _mockProviderFactory.Setup(f => f.CreateProvider("OpenRouter", "test-model")).Returns(_mockProvider.Object);
        _mockProvider.Setup(p => p.StreamMessageAsync(It.Is<IEnumerable<ChatMessage>>(messages =>
            messages.Count() == 2 &&
            messages.ElementAt(0).Role == ChatRole.System &&
            messages.ElementAt(0).Content == "You are a helpful assistant." &&
            messages.ElementAt(1).Role == ChatRole.User &&
            messages.ElementAt(1).Content == "Continue this story: Once upon a time"
        ), It.IsAny<CancellationToken>())).Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request));

        // Assert
        Assert.Equal(9, results.Count); // 8 streaming + 1 final
        _mockProvider.Verify(p => p.StreamMessageAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(IEnumerable<string> content)
    {
        await Task.CompletedTask;
        foreach (var item in content)
        {
            yield return item;
        }
    }

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        var list = new List<T>();
        await foreach (var item in asyncEnumerable)
        {
            list.Add(item);
        }
        return list;
    }
}

