using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Enums;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly SendOrchestratedChatMessageUseCase _sendOrchestratedMessageUseCase;
    private readonly ExecutePromptTemplateUseCase _executePromptTemplateUseCase;
    private readonly ExecuteWorkflowUseCase _executeWorkflowUseCase;

    public TestController(
        SendOrchestratedChatMessageUseCase sendOrchestratedMessageUseCase,
        ExecutePromptTemplateUseCase executePromptTemplateUseCase,
        ExecuteWorkflowUseCase executeWorkflowUseCase)
    {
        _sendOrchestratedMessageUseCase = sendOrchestratedMessageUseCase;
        _executePromptTemplateUseCase = executePromptTemplateUseCase;
        _executeWorkflowUseCase = executeWorkflowUseCase;
    }

    [HttpGet("orchestration")]
    public async Task<IActionResult> TestOrchestration()
    {
        try
        {
            // Test basic chat message
            var chatRequest = new ChatRequestDto
            {
                ProviderId = "OpenRouter",
                ModelId = "google/gemini-2.5-flash-lite-preview-09-2025",
                Messages = new List<MessageDto>
                {
                    new MessageDto { Role = ChatRole.User, Content = "Hello, what's the weather like in London?" }
                }
            };

            var response = await _sendOrchestratedMessageUseCase.ExecuteAsync(chatRequest);
            return Ok(new { Status = "Success", Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }

    [HttpGet("template")]
    public async Task<IActionResult> TestTemplate()
    {
        try
        {
            // Test prompt template
            var templateRequest = new PromptTemplateDto
            {
                TemplateName = "Summarize",
                Variables = new Dictionary<string, string>
                {
                    { "history", "User: Hello\nAssistant: Hi there!\nUser: How are you?\nAssistant: I'm doing well, thank you for asking!" }
                }
            };

            var response = await _executePromptTemplateUseCase.ExecuteAsync(templateRequest);
            return Ok(new { Status = "Success", Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }

    [HttpGet("workflow")]
    public async Task<IActionResult> TestWorkflow()
    {
        try
        {
            // Test workflow
            var workflowRequest = new WorkflowRequestDto
            {
                Goal = "Find the current weather in London and summarize it",
                AvailableFunctions = new List<string> { "Weather" }
            };

            var response = await _executeWorkflowUseCase.ExecuteAsync(workflowRequest);
            return Ok(new { Status = "Success", Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }
}