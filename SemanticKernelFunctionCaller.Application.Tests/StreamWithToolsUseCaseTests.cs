using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Enums;
using Moq;
using Xunit;

namespace SemanticKernelFunctionCaller.Application.Tests;

public class StreamWithToolsUseCaseTests
{
    private readonly Mock<IChatCompletionService> _mockChatCompletionService;
    private readonly Mock<ILogger<StreamWithToolsUseCase>> _mockLogger;
    private readonly Kernel _kernel;
    private readonly StreamWithToolsUseCase _useCase;

    public StreamWithToolsUseCaseTests()
    {
        _mockChatCompletionService = new Mock<IChatCompletionService>();
        _mockLogger = new Mock<ILogger<StreamWithToolsUseCase>>();
        
        // Create a real kernel with the mocked chat completion service
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_mockChatCompletionService.Object);
        _kernel = builder.Build();
        
        _useCase = new StreamWithToolsUseCase(_kernel, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldStreamContentUpdates()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "What time is it?" }
            }
        };

        var streamingUpdates = new[]
        {
            CreateStreamingChatMessageContent(AuthorRole.Assistant, "The current"),
            CreateStreamingChatMessageContent(AuthorRole.Assistant, " time is"),
            CreateStreamingChatMessageContent(AuthorRole.Assistant, " 3:00 PM.")
        };

        var asyncEnumerable = CreateAsyncEnumerable(streamingUpdates);

        _mockChatCompletionService.Setup(s => s.GetStreamingChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            _kernel,
            It.IsAny<CancellationToken>()))
            .Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request));

        // Assert
        Assert.Equal(4, results.Count); // 3 content updates + 1 final

        // Check content updates
        Assert.Equal("content", results[0].Type);
        Assert.Equal("The current", results[0].Content);
        Assert.False(results[0].IsFinal);

        Assert.Equal("content", results[1].Type);
        Assert.Equal(" time is", results[1].Content);
        Assert.False(results[1].IsFinal);

        Assert.Equal("content", results[2].Type);
        Assert.Equal(" 3:00 PM.", results[2].Content);
        Assert.False(results[2].IsFinal);

        // Check final update
        Assert.Equal("content", results[3].Type);
        Assert.True(results[3].IsFinal);
        Assert.Null(results[3].Content);

        _mockChatCompletionService.Verify(s => s.GetStreamingChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            _kernel,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithToolCall_ShouldStreamToolUpdate()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "test-model",
            Messages = new List<MessageDto>
            {
                new MessageDto { Role = ChatRole.User, Content = "What's the weather?" }
            }
        };

        var streamingUpdates = new[]
        {
            CreateStreamingChatMessageContent(AuthorRole.Tool, "TimePlugin.GetCurrentTime"),
            CreateStreamingChatMessageContent(AuthorRole.Assistant, "The current time is 3:00 PM.")
        };

        var asyncEnumerable = CreateAsyncEnumerable(streamingUpdates);

        _mockChatCompletionService.Setup(s => s.GetStreamingChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            _kernel,
            It.IsAny<CancellationToken>()))
            .Returns(asyncEnumerable);

        // Act
        var results = await ToListAsync(_useCase.ExecuteAsync(request));

        // Assert
        Assert.Equal(3, results.Count); // tool call + content + final

        // Check tool call update
        Assert.Equal("tool_call", results[0].Type);
        Assert.Equal("TimePlugin.GetCurrentTime", results[0].FunctionName);
        Assert.Equal("🔧 Calling TimePlugin.GetCurrentTime...", results[0].Content);
        Assert.False(results[0].IsFinal);

        // Check content update
        Assert.Equal("content", results[1].Type);
        Assert.Equal("The current time is 3:00 PM.", results[1].Content);
        Assert.False(results[1].IsFinal);

        // Check final update
        Assert.Equal("content", results[2].Type);
        Assert.True(results[2].IsFinal);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tool invocation: TimePlugin.GetCurrentTime")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static StreamingChatMessageContent CreateStreamingChatMessageContent(AuthorRole role, string content)
    {
        return new StreamingChatMessageContent(role, content, null);
    }

    private static async IAsyncEnumerable<StreamingChatMessageContent> CreateAsyncEnumerable(
        IEnumerable<StreamingChatMessageContent> content)
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