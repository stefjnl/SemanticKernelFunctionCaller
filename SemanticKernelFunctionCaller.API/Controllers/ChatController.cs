using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using System.Text.Json;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISendChatMessageUseCase _sendMessageUseCase;
    private readonly IStreamChatMessageUseCase _streamMessageUseCase;
    private readonly IStreamWithToolsUseCase _streamWithToolsUseCase;
    private readonly IGetAvailablePluginsUseCase _getAvailablePluginsUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ISendChatMessageUseCase sendMessageUseCase,
        IStreamChatMessageUseCase streamMessageUseCase,
        IStreamWithToolsUseCase streamWithToolsUseCase,
        IGetAvailablePluginsUseCase getAvailablePluginsUseCase,
        ILogger<ChatController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ChatController constructor - DI injection starting");
        
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _streamMessageUseCase = streamMessageUseCase ?? throw new ArgumentNullException(nameof(streamMessageUseCase));
        _streamWithToolsUseCase = streamWithToolsUseCase ?? throw new ArgumentNullException(nameof(streamWithToolsUseCase));
        _getAvailablePluginsUseCase = getAvailablePluginsUseCase ?? throw new ArgumentNullException(nameof(getAvailablePluginsUseCase));
        
        _logger.LogInformation("ChatController constructor - All dependencies injected successfully");
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

    [HttpPost("stream-with-tools")]
    public async Task StreamWithTools([FromBody] ChatRequestDto request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var stream = _streamWithToolsUseCase.ExecuteAsync(request, HttpContext.RequestAborted);

            await foreach (var update in stream)
            {
                var jsonUpdate = JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {jsonUpdate}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during stream-with-tools.");
            var jsonError = JsonSerializer.Serialize(new { error = $"An error occurred: {ex.Message}" });
            await Response.WriteAsync($"data: {jsonError}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    [HttpGet("plugins")]
    public IActionResult GetAvailablePlugins()
    {
        try
        {
            var plugins = _getAvailablePluginsUseCase.Execute();
            return Ok(plugins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving plugins.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}