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

    public ChatController(IProviderFactory providerFactory, ILogger<ChatController> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _providerFactory.GetAvailableProviders();
        return Ok(providers);
    }

    [HttpGet("providers/{providerId}/models")]
    public IActionResult GetModels(string providerId)
    {
        if (Enum.TryParse<ProviderType>(providerId, true, out var providerType))
        {
            var models = _providerFactory.GetModelsForProvider(providerType);
            return Ok(models);
        }
        return BadRequest("Invalid provider ID");
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequestDto request)
    {
        if (!Enum.TryParse<ProviderType>(request.ProviderId, true, out var providerType))
        {
            return BadRequest("Invalid provider ID");
        }

        try
        {
            var provider = _providerFactory.CreateProvider(providerType, request.ModelId);
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

        if (!Enum.TryParse<ProviderType>(request.ProviderId, true, out var providerType))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { error = "Invalid provider ID" })}\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        try
        {
            Console.WriteLine($"[DEBUG] ChatController.StreamMessage - Provider: {providerType}, Model: {request.ModelId}");
            Console.WriteLine($"[DEBUG] ChatController.StreamMessage - Request messages count: {request.Messages?.Count}");

            var provider = _providerFactory.CreateProvider(providerType, request.ModelId);
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