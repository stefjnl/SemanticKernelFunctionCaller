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
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using Xunit;

namespace SemanticKernelFunctionCaller.API.Tests;

public class ChatControllerTests
{
    private readonly Mock<SendOrchestratedChatMessageUseCaseV2> _mockOrchestratedUseCase;
    private readonly Mock<ExecutePromptTemplateUseCaseV2> _mockPromptTemplateUseCase;
    private readonly Mock<StreamOrchestratedChatMessageUseCaseV2> _mockStreamOrchestratedUseCase;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockOrchestratedUseCase = new Mock<SendOrchestratedChatMessageUseCaseV2>();
        _mockPromptTemplateUseCase = new Mock<ExecutePromptTemplateUseCaseV2>();
        _mockStreamOrchestratedUseCase = new Mock<StreamOrchestratedChatMessageUseCaseV2>();
        _controller = new ChatController(
            _mockOrchestratedUseCase.Object,
            _mockPromptTemplateUseCase.Object,
            _mockStreamOrchestratedUseCase.Object,
            NullLogger<ChatController>.Instance);
    }

    // Provider and model endpoints removed during rollback simplification

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

        _mockOrchestratedUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
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
    public async Task SendMessage_CallsOrchestratedUseCase_WithCorrectRequest()
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

        _mockOrchestratedUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _controller.SendMessage(request);

        // Assert
        _mockOrchestratedUseCase.Verify(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()), Times.Once);
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
        _mockOrchestratedUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
    }

    #endregion

    // StreamMessage endpoint removed during rollback simplification

    #region SendOrchestratedMessage Tests

    [Fact]
    public async Task SendOrchestratedMessage_WithValidRequest_ReturnsOkResult_WithResponse()
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

        _mockOrchestratedUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SendOrchestratedMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<ChatResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.Content, actualResponse.Content);
        Assert.Equal(expectedResponse.ModelId, actualResponse.ModelId);
        Assert.Equal(expectedResponse.ProviderId, actualResponse.ProviderId);
    }

    [Fact]
    public async Task SendOrchestratedMessage_WhenUseCaseThrowsException_Returns500StatusCode_WithErrorMessage()
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

        var exceptionMessage = "Orchestration failed";
        _mockOrchestratedUseCase.Setup(x => x.ExecuteAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.SendOrchestratedMessage(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
    }

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