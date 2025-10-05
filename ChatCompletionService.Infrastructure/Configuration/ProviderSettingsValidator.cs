using Microsoft.Extensions.Options;

namespace ChatCompletionService.Infrastructure.Configuration;

public class ProviderSettingsValidator : IValidateOptions<ProviderSettings>
{
    public ValidateOptionsResult Validate(string? name, ProviderSettings options)
    {
        var errors = new List<string>();

        if (options.OpenRouter == null)
            errors.Add("OpenRouter configuration is missing");
        else
            ValidateProvider("OpenRouter", options.OpenRouter, errors);

        if (options.NanoGPT == null)
            errors.Add("NanoGPT configuration is missing");
        else
            ValidateProvider("NanoGPT", options.NanoGPT, errors);

        return errors.Any()
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private void ValidateProvider(string name, ProviderConfig config, List<string> errors)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
            errors.Add($"{name}: ApiKey is required");

        if (string.IsNullOrEmpty(config.Endpoint))
            errors.Add($"{name}: Endpoint is required");
        else if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out _))
            errors.Add($"{name}: Invalid endpoint URI");

        if (config.Models == null || !config.Models.Any())
            errors.Add($"{name}: At least one model must be configured");
    }
}