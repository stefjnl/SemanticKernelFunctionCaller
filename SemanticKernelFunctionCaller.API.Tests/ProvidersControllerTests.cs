using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SemanticKernelFunctionCaller.API.Controllers;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using Xunit;

namespace SemanticKernelFunctionCaller.API.Tests;

public class ProvidersControllerTests
{
    private readonly Mock<IGetAvailableProvidersUseCase> _mockGetProvidersUseCase;
    private readonly Mock<IGetProviderModelsUseCase> _mockGetModelsUseCase;
    private readonly ProvidersController _controller;

    public ProvidersControllerTests()
    {
        _mockGetProvidersUseCase = new Mock<IGetAvailableProvidersUseCase>();
        _mockGetModelsUseCase = new Mock<IGetProviderModelsUseCase>();

        _controller = new ProvidersController(
            _mockGetProvidersUseCase.Object,
            _mockGetModelsUseCase.Object,
            NullLogger<ProvidersController>.Instance);
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

    [Fact]
    public void GetProviders_WhenUseCaseThrowsException_Returns500StatusCode_WithErrorMessage()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockGetProvidersUseCase.Setup(x => x.Execute())
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = _controller.GetProviders();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
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

    [Fact]
    public void GetModels_WhenUseCaseThrowsException_Returns500StatusCode_WithErrorMessage()
    {
        // Arrange
        var providerId = "InvalidProvider";
        var exceptionMessage = "Provider not found";
        _mockGetModelsUseCase.Setup(x => x.Execute(providerId))
            .Throws(new Exception(exceptionMessage));

        // Act
        var result = _controller.GetModels(providerId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Contains(exceptionMessage, statusCodeResult.Value?.ToString());
    }

    #endregion
}