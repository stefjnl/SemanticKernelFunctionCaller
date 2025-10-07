using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SemanticKernelFunctionCaller.API.Controllers;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Enums;
using Xunit;

namespace SemanticKernelFunctionCaller.API.Tests;

public class ChatControllerTests
{
    private readonly Mock<ISendChatMessageUseCase> _mockSendMessageUseCase;
    private readonly Mock<IStreamChatMessageUseCase> _mockStreamMessageUseCase;
    private readonly Mock<IStreamWithToolsUseCase> _mockStreamWithToolsUseCase;
    private readonly Mock<IGetAvailablePluginsUseCase> _mockGetAvailablePluginsUseCase;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockSendMessageUseCase = new Mock<ISendChatMessageUseCase>();
        _mockStreamMessageUseCase = new Mock<IStreamChatMessageUseCase>();
        _mockStreamWithToolsUseCase = new Mock<IStreamWithToolsUseCase>();
        _mockGetAvailablePluginsUseCase = new Mock<IGetAvailablePluginsUseCase>();

        _controller = new ChatController(
            _mockSendMessageUseCase.Object,
            _mockStreamMessageUseCase.Object,
            _mockStreamWithToolsUseCase.Object,
            _mockGetAvailablePluginsUseCase.Object,
            NullLogger<ChatController>.Instance);
    }


    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_WithValidRequest_ReturnsOkResult_WithResponse()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        var expectedResponse = new ChatResponseDto
        {
            Content = "Hello! How can I help you?",
            ModelId = "gpt-3.5-turbo",
            ProviderId = "OpenRouter"
        };

        _mockSendMessageUseCase.Setup(x => x.ExecuteAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<ChatResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.Content, actualResponse.Content);
        Assert.Equal(expectedResponse.ModelId, actualResponse.ModelId);
        Assert.Equal(expectedResponse.ProviderId, actualResponse.ProviderId);
    }

    [Fact]
    public async Task SendMessage_CallsUseCaseExecuteAsync_WithCorrectRequest()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "NanoGPT",
            ModelId = "nano-gpt",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Test message" }
            }
        };

        var expectedResponse = new ChatResponseDto
        {
            Content = "Response",
            ModelId = "nano-gpt",
            ProviderId = "NanoGPT"
        };

        _mockSendMessageUseCase.Setup(x => x.ExecuteAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.SendMessage(request);

        // Assert
        _mockSendMessageUseCase.Verify(x => x.ExecuteAsync(request), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WhenUseCaseThrowsException_Returns500StatusCode_WithErrorMessage()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        var exceptionMessage = "Provider unavailable";
        _mockSendMessageUseCase.Setup(x => x.ExecuteAsync(request))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
    }

    [Fact]
    public async Task SendMessage_WithNullMessages_ReturnsOkResult()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = null!
        };

        var expectedResponse = new ChatResponseDto
        {
            Content = "Response",
            ModelId = "gpt-3.5-turbo",
            ProviderId = "OpenRouter"
        };

        _mockSendMessageUseCase.Setup(x => x.ExecuteAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockSendMessageUseCase.Verify(x => x.ExecuteAsync(request), Times.Once);
    }

    #endregion

    #region StreamMessage Tests

    [Fact]
    public async Task StreamMessage_SetsCorrectContentTypeAndHeaders()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var mockResponseBody = new Mock<Stream>();

        mockResponse.Setup(r => r.ContentType).Returns(() => "text/plain");
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(mockResponseBody.Object);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        _mockStreamMessageUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Returns(CreateEmptyAsyncEnumerable<StreamingChatUpdate>());

        // Act
        await _controller.StreamMessage(request);

        // Assert
        mockResponse.VerifySet(r => r.ContentType = "text/event-stream");
        Assert.Equal("no-cache", mockResponse.Object.Headers.CacheControl);
        Assert.Equal("keep-alive", mockResponse.Object.Headers.Connection);
    }

    /*[Fact]
    public async Task StreamMessage_WithValidRequest_WritesStreamingData()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        var streamingUpdates = new List<StreamingChatUpdate>
        {
            new() { Content = "Hello", IsFinal = false },
            new() { Content = " there!", IsFinal = false },
            new() { Content = "", IsFinal = true }
        };

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var responseStream = new MemoryStream();

        mockResponse.SetupProperty(r => r.ContentType);
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(responseStream);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        _mockStreamMessageUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(streamingUpdates));

        // Act
        await _controller.StreamMessage(request);

        // Assert
        var writtenData = System.Text.Encoding.UTF8.GetString(responseStream.ToArray());

        // Verify that streaming data was written in SSE format
        Assert.Contains("data: ", writtenData);
        Assert.Contains("\"Content\":\"Hello\"", writtenData);
        Assert.Contains("\"IsFinal\":false", writtenData);
        Assert.Contains("\"Content\":\" there!\"", writtenData);
        Assert.Contains("\"Content\":\"\"", writtenData);
        Assert.Contains("\"IsFinal\":true", writtenData);
    }*/
    /*[Fact]
    public async Task StreamMessage_WhenUseCaseThrowsException_WritesErrorToStream()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        var exceptionMessage = "Streaming failed";

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var responseStream = new MemoryStream();

        mockResponse.SetupProperty(r => r.ContentType);
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(responseStream);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        var exception = new Exception(exceptionMessage);
        _mockStreamMessageUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Throws(exception);

        // Act
        await _controller.StreamMessage(request);

        // Assert
        var writtenData = System.Text.Encoding.UTF8.GetString(responseStream.ToArray());

        // Verify error was written in SSE format
        Assert.Contains("data: ", writtenData);
        Assert.Contains($"\"error\":\"An error occurred: {exceptionMessage}\"", writtenData);
    }*/

    /*[Fact]
    public async Task StreamMessage_WhenExceptionOccurs_StillFlushesResponse()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "Hello" }
            }
        };

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var responseStream = new MemoryStream();

        mockResponse.SetupProperty(r => r.ContentType);
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(responseStream);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        _mockStreamMessageUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Throws(new Exception("Test error"));

        // Act
        await _controller.StreamMessage(request);

        // Assert
        mockResponse.Verify(r => r.Body.FlushAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }*/

    #endregion

    #region StreamWithTools Tests

    [Fact]
    public async Task StreamWithTools_SetsCorrectContentTypeAndHeaders()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "What time is it?" }
            }
        };

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var mockResponseBody = new Mock<Stream>();

        mockResponse.Setup(r => r.ContentType).Returns(() => "text/plain");
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(mockResponseBody.Object);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        _mockStreamWithToolsUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Returns(CreateEmptyAsyncEnumerable<ToolStreamingUpdate>());

        // Act
        await _controller.StreamWithTools(request);

        // Assert
        mockResponse.VerifySet(r => r.ContentType = "text/event-stream");
        Assert.Equal("no-cache", mockResponse.Object.Headers.CacheControl);
        Assert.Equal("keep-alive", mockResponse.Object.Headers.Connection);
    }

    [Fact]
    public async Task StreamWithTools_WithValidRequest_CallsUseCaseExecuteAsync()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "What time is it?" }
            }
        };

        // Mock HttpContext and Response
        var mockHttpContext = new Mock<HttpContext>();
        var mockResponse = new Mock<HttpResponse>();
        var mockResponseBody = new Mock<Stream>();

        mockResponse.Setup(r => r.ContentType).Returns(() => "text/plain");
        mockResponse.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockResponse.Setup(r => r.Body).Returns(mockResponseBody.Object);
        mockResponse.Setup(r => r.Body.FlushAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
        mockHttpContext.Setup(c => c.RequestAborted).Returns(CancellationToken.None);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext.Object
        };

        _mockStreamWithToolsUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Returns(CreateEmptyAsyncEnumerable<ToolStreamingUpdate>());

        // Act
        await _controller.StreamWithTools(request);

        // Assert
        _mockStreamWithToolsUseCase.Verify(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamWithTools_WhenUseCaseThrowsException_HandlesExceptionGracefully()
    {
        // Arrange
        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "gpt-3.5-turbo",
            Messages = new List<MessageDto>
            {
                new() { Role = ChatRole.User, Content = "What time is it?" }
            }
        };

        var exceptionMessage = "Tools streaming failed";

        // Create a real HttpContext with a mock response stream
        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var exception = new Exception(exceptionMessage);
        _mockStreamWithToolsUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .Throws(exception);

        // Act
        await _controller.StreamWithTools(request);

        // Assert
        // Verify that the response was properly configured
        Assert.Equal("text/event-stream", httpContext.Response.ContentType);
        Assert.Equal("no-cache", httpContext.Response.Headers["Cache-Control"]);
        Assert.Equal("keep-alive", httpContext.Response.Headers["Connection"]);
        
        // Verify that the response stream has content (error was written)
        responseStream.Position = 0;
        using var reader = new StreamReader(responseStream);
        var streamContent = reader.ReadToEnd();
        Assert.NotEmpty(streamContent);
        Assert.Contains("error", streamContent.ToLowerInvariant());
    }

    #endregion

    #region GetAvailablePlugins Tests

    [Fact]
    public void GetAvailablePlugins_WithValidRequest_ReturnsOkResult_WithPlugins()
    {
        // Arrange
        var expectedPlugins = new List<PluginInfoDto>
        {
            new()
            {
                PluginName = "TimePlugin",
                FunctionName = "GetCurrentTime",
                Description = "Gets the current time",
                Parameters = new List<ParameterInfoDto>
                {
                    new() { Name = "timezone", Description = "Timezone", Type = "string", IsRequired = false }
                }
            }
        };

        _mockGetAvailablePluginsUseCase.Setup(x => x.Execute())
            .Returns(expectedPlugins);

        // Act
        var result = _controller.GetAvailablePlugins();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualPlugins = Assert.IsAssignableFrom<IEnumerable<PluginInfoDto>>(okResult.Value);
        Assert.Single(actualPlugins);
        
        var plugin = actualPlugins.First();
        Assert.Equal("TimePlugin", plugin.PluginName);
        Assert.Equal("GetCurrentTime", plugin.FunctionName);
        Assert.Equal("Gets the current time", plugin.Description);
        Assert.Single(plugin.Parameters);
    }

    [Fact]
    public void GetAvailablePlugins_CallsUseCaseExecute()
    {
        // Arrange
        var expectedPlugins = new List<PluginInfoDto>();
        _mockGetAvailablePluginsUseCase.Setup(x => x.Execute())
            .Returns(expectedPlugins);

        // Act
        _controller.GetAvailablePlugins();

        // Assert
        _mockGetAvailablePluginsUseCase.Verify(x => x.Execute(), Times.Once);
    }

    [Fact]
    public void GetAvailablePlugins_WhenUseCaseThrowsException_Returns500StatusCode_WithErrorMessage()
    {
        // Arrange
        var exceptionMessage = "Plugin retrieval failed";
        _mockGetAvailablePluginsUseCase.Setup(x => x.Execute())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = _controller.GetAvailablePlugins();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
    }

    [Fact]
    public void GetAvailablePlugins_WithEmptyPlugins_ReturnsOkResult()
    {
        // Arrange
        var expectedPlugins = new List<PluginInfoDto>();
        _mockGetAvailablePluginsUseCase.Setup(x => x.Execute())
            .Returns(expectedPlugins);

        // Act
        var result = _controller.GetAvailablePlugins();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualPlugins = Assert.IsAssignableFrom<IEnumerable<PluginInfoDto>>(okResult.Value);
        Assert.Empty(actualPlugins);
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        await Task.CompletedTask;
        foreach (var item in items)
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> CreateEmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    #endregion
}