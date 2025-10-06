using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using System.Text.Json;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IGetAvailableProvidersUseCase _getProvidersUseCase;
    private readonly IGetProviderModelsUseCase _getModelsUseCase;
    private readonly ISendChatMessageUseCase _sendMessageUseCase;
    private readonly IStreamChatMessageUseCase _streamMessageUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IGetAvailableProvidersUseCase getProvidersUseCase,
        IGetProviderModelsUseCase getModelsUseCase,
        ISendChatMessageUseCase sendMessageUseCase,
        IStreamChatMessageUseCase streamMessageUseCase,
        ILogger<ChatController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ChatController constructor - DI injection starting");
        
        _getProvidersUseCase = getProvidersUseCase ?? throw new ArgumentNullException(nameof(getProvidersUseCase));
        _getModelsUseCase = getModelsUseCase ?? throw new ArgumentNullException(nameof(getModelsUseCase));
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _streamMessageUseCase = streamMessageUseCase ?? throw new ArgumentNullException(nameof(streamMessageUseCase));
        
        _logger.LogInformation("ChatController constructor - All dependencies injected successfully");
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        try
        {
            _logger.LogInformation("GetProviders endpoint called");
            var providers = _getProvidersUseCase.Execute();
            _logger.LogInformation("Successfully retrieved {Count} providers", providers?.Count() ?? 0);
            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProviders endpoint");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
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