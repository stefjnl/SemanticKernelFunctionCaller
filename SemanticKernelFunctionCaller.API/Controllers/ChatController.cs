using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.Requests;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStreamChatMessageUseCase _streamMessageUseCase;
    private readonly StreamOrchestratedChatMessageUseCase _streamOrchestratedMessageUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMediator mediator,
        IStreamChatMessageUseCase streamMessageUseCase,
        StreamOrchestratedChatMessageUseCase streamOrchestratedMessageUseCase,
        ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _streamMessageUseCase = streamMessageUseCase;
        _streamOrchestratedMessageUseCase = streamOrchestratedMessageUseCase;
        _logger = logger;
    }

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var request = new GetAvailableProvidersRequest();
        var providers = await _mediator.Send(request);
        return Ok(providers);
    }

    [HttpGet("providers/{providerId}/models")]
    public IActionResult GetModels(string providerId)
    {
        var request = new GetProviderModelsRequest { ProviderId = providerId };
        var models = await _mediator.Send(request);
        return Ok(models);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(ChatRequestDto request)
    {
        try
        {
            var sendRequest = new SendChatMessageRequest { Request = request };
            var response = await _mediator.Send(sendRequest);
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
    [RateLimitPolicy("OrchestratedEndpoints")]
    public async Task<IActionResult> SendOrchestratedMessage(ChatRequestDto request)
    {
        try
        {
            var sendRequest = new SendOrchestratedChatMessageRequest { Request = request };
            var response = await _mediator.Send(sendRequest);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending an orchestrated message.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("orchestrated/stream")]
    [RateLimitPolicy("OrchestratedEndpoints")]
    public async Task StreamOrchestratedMessage(ChatRequestDto request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var stream = _streamOrchestratedMessageUseCase.ExecuteAsync(request, HttpContext.RequestAborted);

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
            var request = new ExecutePromptTemplateRequest { Request = templateRequest };
            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing a prompt template.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("workflow")]
    public async Task<IActionResult> ExecuteWorkflow(WorkflowRequestDto workflowRequest)
    {
        try
        {
            var request = new ExecuteWorkflowRequest { Request = workflowRequest };
            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing a workflow.");
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
        
        [HttpGet("templates")]
        public async Task<IActionResult> GetAvailableTemplates()
        {
            try
            {
                var templates = await _templateService.GetAvailableTemplatesAsync();
                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving available templates.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}