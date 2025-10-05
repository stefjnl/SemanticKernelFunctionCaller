using ChatCompletionService.Application.Interfaces;
using ChatCompletionService.Infrastructure.Extensions;
using ChatCompletionService.Infrastructure.Factories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure console logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add ILoggerFactory for Microsoft.Extensions.AI
builder.Services.AddLogging();

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register providers and configurations
builder.Services.AddProviderServices(builder.Configuration);

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

app.UseAuthorization();

app.MapControllers();

app.Run();