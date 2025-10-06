using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Add logging services for the distributed application
builder.Services.AddLogging();

var apiService = builder.AddProject<Projects.SemanticKernelFunctionCaller_API>("apiservice")
    .WithExternalHttpEndpoints();

builder.Build().Run();
