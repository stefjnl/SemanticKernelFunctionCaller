using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.DTOs;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Domain.Enums;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly SendOrchestratedChatMessageUseCaseV2 _sendOrchestratedMessageUseCase;
    private readonly ExecutePromptTemplateUseCaseV2 _executePromptTemplateUseCase;

    public TestController(
        SendOrchestratedChatMessageUseCaseV2 sendOrchestratedMessageUseCase,
        ExecutePromptTemplateUseCaseV2 executePromptTemplateUseCase)
    {
        _sendOrchestratedMessageUseCase = sendOrchestratedMessageUseCase;
        _executePromptTemplateUseCase = executePromptTemplateUseCase;
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

            var response = await _sendOrchestratedMessageUseCase.ExecuteAsync(chatRequest, CancellationToken.None);
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

            var response = await _executePromptTemplateUseCase.ExecuteAsync(templateRequest, CancellationToken.None);
            return Ok(new { Status = "Success", Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }

}