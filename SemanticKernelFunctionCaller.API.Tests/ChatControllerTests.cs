using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using Xunit;

namespace SemanticKernelFunctionCaller.API.Tests;

public class ChatControllerTests
{
    private readonly Mock<IGetAvailableProvidersUseCase> _mockGetProvidersUseCase;
    private readonly Mock<IGetProviderModelsUseCase> _mockGetModelsUseCase;
    private readonly Mock<ISendChatMessageUseCase> _mockSendMessageUseCase;
    private readonly Mock<IStreamChatMessageUseCase> _mockStreamMessageUseCase;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockGetProvidersUseCase = new Mock<IGetAvailableProvidersUseCase>();
        _mockGetModelsUseCase = new Mock<IGetProviderModelsUseCase>();
        _mockSendMessageUseCase = new Mock<ISendChatMessageUseCase>();
        _mockStreamMessageUseCase = new Mock<IStreamChatMessageUseCase>();

        _controller = new ChatController(
            _mockGetProvidersUseCase.Object,
            _mockGetModelsUseCase.Object,
            _mockSendMessageUseCase.Object,
            _mockStreamMessageUseCase.Object,
            NullLogger<ChatController>.Instance);
    }

    #region GetProviders Tests

    [Fact]
    public void GetProviders_ReturnsOkResult_WithProviderList()
    {
        // Arrange
        var expectedProviders = new List<ProviderInfoDto>
        {
            new() { Id = "OpenRouter", DisplayName = "OpenRouter" },
            new() { Id = "NanoGPT", DisplayName = "NanoGPT" }
        };

        _mockGetProvidersUseCase.Setup(x => x.Execute())
            .Returns(expectedProviders);

        // Act
        var result = _controller.GetProviders();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualProviders = Assert.IsAssignableFrom<IEnumerable<ProviderInfoDto>>(okResult.Value);
        Assert.Equal(expectedProviders, actualProviders);
    }

    [Fact]
    public void GetProviders_CallsUseCaseExecute_ExactlyOnce()
    {
        // Arrange
        _mockGetProvidersUseCase.Setup(x => x.Execute())
            .Returns(new List<ProviderInfoDto>());

        // Act
        _controller.GetProviders();

        // Assert
        _mockGetProvidersUseCase.Verify(x => x.Execute(), Times.Once);
    }

    #endregion

    #region GetModels Tests

    [Fact]
    public void GetModels_WithValidProviderId_ReturnsOkResult_WithModelList()
    {
        // Arrange
        var providerId = "OpenRouter";
        var expectedModels = new List<ModelInfoDto>
        {
            new() { Id = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo" },
            new() { Id = "gpt-4", DisplayName = "GPT-4" }
        };

        _mockGetModelsUseCase.Setup(x => x.Execute(providerId))
            .Returns(expectedModels);

        // Act
        var result = _controller.GetModels(providerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualModels = Assert.IsAssignableFrom<IEnumerable<ModelInfoDto>>(okResult.Value);
        Assert.Equal(expectedModels, actualModels);
    }

    [Fact]
    public void GetModels_CallsUseCaseExecute_WithCorrectProviderId()
    {
        // Arrange
        var providerId = "NanoGPT";
        _mockGetModelsUseCase.Setup(x => x.Execute(providerId))
            .Returns(new List<ModelInfoDto>());

        // Act
        _controller.GetModels(providerId);

        // Assert
        _mockGetModelsUseCase.Verify(x => x.Execute(providerId), Times.Once);
    }

    [Theory]
    [InlineData("OpenRouter")]
    [InlineData("NanoGPT")]
    [InlineData("SomeOtherProvider")]
    public void GetModels_AcceptsAnyProviderId(string providerId)
    {
        // Arrange
        _mockGetModelsUseCase.Setup(x => x.Execute(providerId))
            .Returns(new List<ModelInfoDto>());

        // Act
        var result = _controller.GetModels(providerId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockGetModelsUseCase.Verify(x => x.Execute(providerId), Times.Once);
    }

    #endregion

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
            Messages = null
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

    #region Helper Methods

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
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