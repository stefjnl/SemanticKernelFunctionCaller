using Microsoft.AspNetCore.Mvc;
using ChatCompletionService.Application.DTOs;
using ChatCompletionService.Application.UseCases;
using System.Text.Json;

namespace ChatCompletionService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GetAvailableProvidersUseCase _getProvidersUseCase;
    private readonly GetProviderModelsUseCase _getModelsUseCase;
    private readonly SendChatMessageUseCase _sendMessageUseCase;
    private readonly StreamChatMessageUseCase _streamMessageUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        GetAvailableProvidersUseCase getProvidersUseCase,
        GetProviderModelsUseCase getModelsUseCase,
        SendChatMessageUseCase sendMessageUseCase,
        StreamChatMessageUseCase streamMessageUseCase,
        ILogger<ChatController> logger)
    {
        _getProvidersUseCase = getProvidersUseCase;
        _getModelsUseCase = getModelsUseCase;
        _sendMessageUseCase = sendMessageUseCase;
        _streamMessageUseCase = streamMessageUseCase;
        _logger = logger;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _getProvidersUseCase.Execute();
        return Ok(providers);
    }

    [HttpGet("providers/{providerId}/models")]
    public IActionResult GetModels(string providerId)
    {
        var models = _getModelsUseCase.Execute(providerId);
        return Ok(models);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequestDto request)
    {
        try
        {
            var response = await _sendMessageUseCase.ExecuteAsync(request);
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
            var stream = _streamMessageUseCase.ExecuteAsync(request, HttpContext.RequestAborted);

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