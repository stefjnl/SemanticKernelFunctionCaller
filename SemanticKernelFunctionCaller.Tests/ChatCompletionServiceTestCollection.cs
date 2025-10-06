using Microsoft.Extensions.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using SemanticKernelFunctionCaller.Infrastructure.Providers;
using SemanticKernelFunctionCaller.Domain.Entities;
using SemanticKernelFunctionCaller.Domain.Enums;
using SemanticKernelFunctionCaller.Application.DTOs;
using Moq;
using Microsoft.Extensions.AI;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using DomainChatMessage = SemanticKernelFunctionCaller.Domain.Entities.ChatMessage;
using DomainChatRole = SemanticKernelFunctionCaller.Domain.Enums.ChatRole;

namespace SemanticKernelFunctionCaller.Tests;

[CollectionDefinition("SemanticKernelFunctionCaller Integration Tests")]
public class SemanticKernelFunctionCallerTestCollection : ICollectionFixture<TestFixture>
{
}

public class TestFixture : IDisposable
{
    public TestFixture()
    {
        // Setup test fixtures if needed
    }

    public void Dispose()
    {
        // Cleanup test fixtures if needed
    }
}


[Collection("SemanticKernelFunctionCaller Integration Tests")]
[Trait("Category", "Integration")]
public class ProviderConfigurationTests : IDisposable
{
    private IConfiguration CreateTestConfiguration()
    {
        var jsonContent = @"{
          ""Providers"": {
            ""OpenRouter"": {
              ""ApiKey"": ""test-openrouter-key"",
              ""Endpoint"": ""https://openrouter.ai/api/v1/"",
              ""Models"": [
                {
                  ""Id"": ""test-model"",
                  ""DisplayName"": ""Test Model""
                }
              ]
            },
            ""NanoGPT"": {
              ""ApiKey"": ""test-nanogpt-key"",
              ""Endpoint"": ""https://api.nanogpt.com/v1/chat/completions"",
              ""Models"": [
                {
                  ""Id"": ""test-nanogpt-model"",
                  ""DisplayName"": ""Test NanoGPT Model""
                }
              ]
            }
          }
        }";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, jsonContent);

        return new ConfigurationBuilder()
            .AddJsonFile(tempFile)
            .Build();
    }

    [Fact]
    [Trait("Component", "ConfigurationManager")]
    [Trait("TestType", "Unit")]
    public void ProviderConfigurationManager_LoadsConfigurationCorrectly()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var configManager = new ProviderConfigurationManager(configuration);

        // Assert
        var openRouterConfig = configManager.GetProviderConfig("OpenRouter");
        Assert.NotNull(openRouterConfig);
        Assert.Equal("test-openrouter-key", openRouterConfig.ApiKey);
        Assert.Equal("https://openrouter.ai/api/v1/", openRouterConfig.Endpoint);
        Assert.Single(openRouterConfig.Models);
        Assert.Equal("test-model", openRouterConfig.Models[0].Id);
    }

    [Fact]
    [Trait("Component", "ProviderFactory")]
    [Trait("TestType", "Unit")]
    public void ChatProviderFactory_CreatesProviderWithCorrectApiKey()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var factory = new ChatProviderFactory(configuration);

        // Act
        var provider = factory.CreateProvider(ProviderType.OpenRouter, "test-model");

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.Equal("OpenRouter", metadata.ProviderName);
    }

    [Fact]
    [Trait("Component", "ProviderFactory")]
    [Trait("TestType", "Unit")]
    public void ChatProviderFactory_ThrowsExceptionForInvalidProvider()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var factory = new ChatProviderFactory(configuration);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() =>
            factory.CreateProvider((ProviderType)999, "test-model"));
    }

    [Fact]
    [Trait("Component", "OpenRouterProvider")]
    [Trait("TestType", "Unit")]
    public void OpenRouterChatProvider_InitializesWithCorrectApiKey()
    {
        // Arrange & Act
        var provider = new OpenRouterChatProvider("test-api-key", "test-model");

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.Equal("OpenRouter", metadata.ProviderName);
    }

    [Fact]
    [Trait("Component", "NanoGptProvider")]
    [Trait("TestType", "Unit")]
    public void NanoGptChatProvider_InitializesWithCorrectApiKey()
    {
        // Arrange & Act
        var provider = new NanoGptChatProvider("test-api-key", "test-model");

        // Assert
        Assert.NotNull(provider);
        var metadata = provider.GetMetadata();
        Assert.Equal("NanoGPT", metadata.ProviderName);
    }

    [Fact]
    [Trait("Component", "OpenRouterProvider")]
    [Trait("TestType", "Integration")]
    [Trait("RequiresAPI", "true")]
    public async Task OpenRouterChatProvider_MakesSuccessfulApiCall()
    {
        // Arrange
        var apiKey = "sk-or-v1"; // Your actual API key
        var provider = new OpenRouterChatProvider(apiKey, "google/gemini-2.5-flash-lite-preview-09-2025");

        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "google/gemini-2.5-flash-lite-preview-09-2025",
            Messages = new List<DomainChatMessage>
            {
                new DomainChatMessage { Id = Guid.NewGuid(), Role = DomainChatRole.User, Content = "Hello, world!", Timestamp = DateTime.UtcNow }
            }
        };

        // Act & Assert
        var response = await provider.SendMessageAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);
        Assert.Equal("OpenRouter", response.ProviderUsed);
        Assert.NotEmpty(response.Message.Content);
    }

    [Fact]
    [Trait("Component", "OpenRouterProvider")]
    [Trait("TestType", "Integration")]
    [Trait("RequiresAPI", "true")]
    public async Task OpenRouterChatProvider_StreamingWorksCorrectly()
    {
        // Arrange
        var apiKey = ""; // Your actual API key
        var provider = new OpenRouterChatProvider(apiKey, "google/gemini-2.5-flash-lite-preview-09-2025");

        var request = new ChatRequestDto
        {
            ProviderId = "OpenRouter",
            ModelId = "google/gemini-2.5-flash-lite-preview-09-2025",
            Messages = new List<DomainChatMessage>
            {
                new DomainChatMessage { Id = Guid.NewGuid(), Role = DomainChatRole.User, Content = "Count to 5", Timestamp = DateTime.UtcNow }
            }
        };

        // Act
        var streamingUpdates = new List<StreamingChatUpdate>();
        await foreach (var update in provider.StreamMessageAsync(request))
        {
            streamingUpdates.Add(update);
        }

        // Assert
        Assert.NotEmpty(streamingUpdates);
        Assert.Contains(streamingUpdates, update => !string.IsNullOrEmpty(update.Content));
        Assert.Single(streamingUpdates.Where(update => update.IsFinal));
    }

    [Fact]
    [Trait("Component", "OpenRouterAPI")]
    [Trait("TestType", "Integration")]
    [Trait("RequiresAPI", "true")]
    public async Task TestOpenRouterApiKeyFormat()
    {
        // Arrange
        var apiKey = "sk-or-v1-";
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "google/gemini-2.5-flash-lite-preview-09-2025",
            messages = new[]
            {
                new { role = "user", content = "Hello" }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);

        var responseObject = System.Text.Json.JsonSerializer.Deserialize<OpenRouterResponse>(responseContent);
        Assert.NotNull(responseObject);
        Assert.NotNull(responseObject.choices);
        Assert.NotEmpty(responseObject.choices);
        Assert.NotEmpty(responseObject.choices[0].message.content);
    }

    private class OpenRouterResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
    }

    private class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }

    private class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    private class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public void Dispose()
    {
        // Cleanup after tests if needed
    }
}
