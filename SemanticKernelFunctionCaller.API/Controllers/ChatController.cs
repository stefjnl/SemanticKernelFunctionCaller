using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.UseCases;
using System.Text.Json;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly SendOrchestratedChatMessageUseCaseV2 _orchestratedChatUseCase;
    private readonly ExecutePromptTemplateUseCaseV2 _promptTemplateUseCase;
    private readonly StreamOrchestratedChatMessageUseCaseV2 _streamOrchestratedUseCase;
    private readonly IStreamChatMessageUseCase _streamMessageUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        SendOrchestratedChatMessageUseCaseV2 orchestratedChatUseCase,
        ExecutePromptTemplateUseCaseV2 promptTemplateUseCase,
        StreamOrchestratedChatMessageUseCaseV2 streamOrchestratedUseCase,
        IStreamChatMessageUseCase streamMessageUseCase,
        ILogger<ChatController> logger)
    {
        _orchestratedChatUseCase = orchestratedChatUseCase;
        _promptTemplateUseCase = promptTemplateUseCase;
        _streamOrchestratedUseCase = streamOrchestratedUseCase;
        _streamMessageUseCase = streamMessageUseCase;
        _logger = logger;
    }

    // Provider and model endpoints removed - not in rollback requirements
    // These can be added back later if needed

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequestDto request)
    {
        try
        {
            // For now, delegate to orchestrated endpoint
            // This can be enhanced later with direct chat completion service
            var response = await _orchestratedChatUseCase.ExecuteAsync(request);
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

    [HttpPost("orchestrated")]
    public async Task<IActionResult> SendOrchestratedMessage(ChatRequestDto request)
    {
        try
        {
            var response = await _orchestratedChatUseCase.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending an orchestrated message.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("orchestrated/stream")]
    public async Task StreamOrchestratedMessage(ChatRequestDto request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var stream = _streamOrchestratedUseCase.ExecuteAsync(request, HttpContext.RequestAborted);

            await foreach (var update in stream)
            {
                var jsonUpdate = JsonSerializer.Serialize(update);
                await Response.WriteAsync($"data: {jsonUpdate}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during orchestrated streaming.");
            var jsonError = JsonSerializer.Serialize(new { error = $"An error occurred: {ex.Message}" });
            await Response.WriteAsync($"data: {jsonError}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    [HttpPost("prompt-template")]
    public async Task<IActionResult> ExecutePromptTemplate(PromptTemplateDto templateRequest)
    {
        try
        {
            var response = await _promptTemplateUseCase.ExecuteAsync(templateRequest);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing a prompt template.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    // Templates endpoint removed - not in original requirements
}