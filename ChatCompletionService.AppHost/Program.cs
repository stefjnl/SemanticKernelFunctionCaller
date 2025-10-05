var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ChatCompletionService_API>("apiservice")
    .WithExternalHttpEndpoints();

builder.Build().Run();