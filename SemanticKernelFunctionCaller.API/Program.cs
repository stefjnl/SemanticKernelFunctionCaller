using SemanticKernelFunctionCaller.API.HealthChecks;
using SemanticKernelFunctionCaller.API.Middleware;
using SemanticKernelFunctionCaller.Application.Interfaces;
using SemanticKernelFunctionCaller.Application.UseCases;
using SemanticKernelFunctionCaller.Infrastructure.Configuration;
using SemanticKernelFunctionCaller.Infrastructure.Extensions;
using SemanticKernelFunctionCaller.Infrastructure.Factories;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

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

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register providers and configurations
builder.Services.AddProviderServices(builder.Configuration);

// Register use cases
builder.Services.AddScoped<GetAvailableProvidersUseCase>();
builder.Services.AddScoped<GetProviderModelsUseCase>();
builder.Services.AddScoped<SendChatMessageUseCase>();
builder.Services.AddScoped<StreamChatMessageUseCase>();

// Register configuration manager
builder.Services.AddSingleton<IProviderConfigurationManager, ProviderConfigurationManager>();

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
