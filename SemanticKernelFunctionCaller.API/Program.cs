using SemanticKernelFunctionCaller.API.HealthChecks;
using SemanticKernelFunctionCaller.API.Middleware;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Extensions;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using SemanticKernelFunctionCaller.Infrastructure.Plugins;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Configure console logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add ILoggerFactory for Microsoft.Extensions.AI
builder.Services.AddLogging();

// Configure and validate settings at startup
builder.Services.AddOptions<ProviderSettings>()
    .Bind(builder.Configuration.GetSection("Providers"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fails fast at startup

builder.Services.AddSingleton<IValidateOptions<ProviderSettings>, ProviderSettingsValidator>();

// Register ChatSettings
builder.Services.Configure<ChatSettings>(
    builder.Configuration.GetSection("ChatSettings")
);

// Configure Semantic Kernel settings
builder.Services.Configure<SemanticKernelSettings>(
    builder.Configuration.GetSection("SemanticKernel"));

// Validate Semantic Kernel settings at startup
builder.Services.PostConfigure<SemanticKernelSettings>(settings =>
{
    if (string.IsNullOrEmpty(settings.DefaultProvider))
        throw new InvalidOperationException("SemanticKernel:DefaultProvider is not configured");
    
    var provider = settings.DefaultProvider.ToLowerInvariant() switch
    {
        "openrouter" => settings.OpenRouter,
        "nanogpt" => settings.NanoGPT,
        _ => throw new InvalidOperationException($"Unknown provider: {settings.DefaultProvider}")
    };
    
    if (string.IsNullOrEmpty(provider?.ApiKey))
        throw new InvalidOperationException($"SemanticKernel: {settings.DefaultProvider} ApiKey is not configured");
    
    if (string.IsNullOrEmpty(provider?.ModelId))
        throw new InvalidOperationException($"SemanticKernel: {settings.DefaultProvider} ModelId is not configured");
    
    if (string.IsNullOrEmpty(provider?.Endpoint))
        throw new InvalidOperationException($"SemanticKernel: {settings.DefaultProvider} Endpoint is not configured");
});

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register providers and configurations
builder.Services.AddProviderServices(builder.Configuration);

// Register use cases - FIX: Register interfaces instead of concrete classes
builder.Services.AddScoped<IGetAvailableProvidersUseCase, GetAvailableProvidersUseCase>();
builder.Services.AddScoped<IGetProviderModelsUseCase, GetProviderModelsUseCase>();
builder.Services.AddScoped<ISendChatMessageUseCase, SendChatMessageUseCase>();
builder.Services.AddScoped<IStreamChatMessageUseCase, StreamChatMessageUseCase>();

// Register configuration manager
builder.Services.AddSingleton<IProviderConfigurationManager, ProviderConfigurationManager>();

// Register Semantic Kernel
builder.Services.AddSingleton<Kernel>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<SemanticKernelSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<Kernel>>();
    
    var kernelBuilder = Kernel.CreateBuilder();
    
    // Configure based on default provider
    var provider = settings.DefaultProvider;
    var providerSettings = provider.ToLowerInvariant() switch
    {
        "openrouter" => settings.OpenRouter,
        "nanogpt" => settings.NanoGPT,
        _ => settings.OpenRouter // fallback to OpenRouter
    };
    
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: providerSettings.ModelId,
        apiKey: providerSettings.ApiKey,
        endpoint: new Uri(providerSettings.Endpoint));
    
    // Add TimePlugin
    kernelBuilder.Plugins.AddFromType<TimePlugin>();
    
    logger.LogInformation("Semantic Kernel configured with provider: {Provider}, Model: {Model}",
        provider, providerSettings.ModelId);
    
    return kernelBuilder.Build();
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<ProviderHealthCheck>("providers");

// Configure CORS for development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Add exception middleware before authorization
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
