using Microsoft.AspNetCore.Mvc;
using SemanticKernelFunctionCaller.Application.Interfaces;

namespace SemanticKernelFunctionCaller.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IGetAvailableProvidersUseCase _getProvidersUseCase;
    private readonly IGetProviderModelsUseCase _getModelsUseCase;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(
        IGetAvailableProvidersUseCase getProvidersUseCase,
        IGetProviderModelsUseCase getModelsUseCase,
        ILogger<ProvidersController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ProvidersController constructor - DI injection starting");
        
        _getProvidersUseCase = getProvidersUseCase ?? throw new ArgumentNullException(nameof(getProvidersUseCase));
        _getModelsUseCase = getModelsUseCase ?? throw new ArgumentNullException(nameof(getModelsUseCase));
        
        _logger.LogInformation("ProvidersController constructor - All dependencies injected successfully");
    }

    [HttpGet]
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

    [HttpGet("{providerId}/models")]
    public IActionResult GetModels(string providerId)
    {
        try
        {
            _logger.LogInformation("GetModels endpoint called for provider: {ProviderId}", providerId);
            var models = _getModelsUseCase.Execute(providerId);
            _logger.LogInformation("Successfully retrieved {Count} models for provider: {ProviderId}", models?.Count() ?? 0, providerId);
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetModels endpoint for provider: {ProviderId}", providerId);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}