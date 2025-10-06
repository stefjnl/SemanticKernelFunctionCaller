using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Domain.Enums;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISendChatMessageUseCase _sendMessageUseCase;
    private readonly IStreamChatMessageUseCase _streamMessageUseCase;
    private readonly Kernel _kernel;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ISendChatMessageUseCase sendMessageUseCase,
        IStreamChatMessageUseCase streamMessageUseCase,
        Kernel kernel,
        ILogger<ChatController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ChatController constructor - DI injection starting");
        
        _sendMessageUseCase = sendMessageUseCase ?? throw new ArgumentNullException(nameof(sendMessageUseCase));
        _streamMessageUseCase = streamMessageUseCase ?? throw new ArgumentNullException(nameof(streamMessageUseCase));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        
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
    public async Task StreamWithTools(
        [FromBody] ChatRequestDto request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            _logger.LogInformation("Starting stream-with-tools request");

            // Convert request messages to ChatHistory
            var chatHistory = new ChatHistory();
            foreach (var msg in request.Messages)
            {
                var role = msg.Role == ChatRole.User
                    ? AuthorRole.User
                    : AuthorRole.Assistant;
                chatHistory.Add(new ChatMessageContent(role, msg.Content));
            }

            // Configure execution settings for tool calling
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Stream the response with tool calling
            await foreach (var update in _kernel.GetRequiredService<IChatCompletionService>().GetStreamingChatMessageContentsAsync(
                chatHistory,
                settings,
                _kernel,
                cancellationToken: HttpContext.RequestAborted))
            {
                if (!string.IsNullOrEmpty(update.Content))
                {
                    var json = JsonSerializer.Serialize(new { content = update.Content });
                    await Response.WriteAsync($"data: {json}\n\n");
                    await Response.Body.FlushAsync();
                }
            }

            _logger.LogInformation("Completed stream-with-tools request successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during stream-with-tools.");
            var jsonError = JsonSerializer.Serialize(new { error = $"An error occurred: {ex.Message}" });
            await Response.WriteAsync($"data: {jsonError}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}