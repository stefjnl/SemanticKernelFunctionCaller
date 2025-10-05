using Microsoft.AspNetCore.Mvc;
using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Domain.Enums;
using System.Text.Json;

namespace ChatCompletionService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ChatController> _logger;
    private readonly IProviderConfigurationReader _providerReader;
    private readonly IModelCatalog _modelCatalog;

    public ChatController(
        IProviderFactory providerFactory,
        ILogger<ChatController> logger,
        IProviderConfigurationReader providerReader,
        IModelCatalog modelCatalog)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        _providerReader = providerReader;
        _modelCatalog = modelCatalog;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _providerReader.GetProviders();
        return Ok(providers);
    }

    [HttpGet("providers/{providerId}/models")]
    public IActionResult GetModels(string providerId)
    {
        var models = _modelCatalog.GetModels(providerId);
        return Ok(models);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequestDto request)
    {
        try
        {
            var provider = _providerFactory.CreateProvider(request.ProviderId, request.ModelId);
            var response = await provider.SendMessageAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending a message.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("stream")]
    public async Task StreamMessage(ChatRequestDto request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var provider = _providerFactory.CreateProvider(request.ProviderId, request.ModelId);
            var stream = provider.StreamMessageAsync(request, HttpContext.RequestAborted);

            await foreach (var update in stream)
            {
                var jsonUpdate = JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {jsonUpdate}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during streaming.");
            var jsonError = JsonSerializer.Serialize(new { error = $"An error occurred: {ex.Message}" });
            await Response.WriteAsync($"data: {jsonError}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}