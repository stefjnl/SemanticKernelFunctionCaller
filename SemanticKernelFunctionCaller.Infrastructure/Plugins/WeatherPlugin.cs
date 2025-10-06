using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins;

/// <summary>
/// A sample weather plugin that provides weather information
/// </summary>
public class WeatherPlugin
{
    /// <summary>
    /// Gets the current weather for a specified location
    /// </summary>
    /// <param name="location">The location to get weather for</param>
    /// <returns>A string describing the current weather</returns>
    [KernelFunction]
    [Description("Gets the current weather for a specified location")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("The location to get weather for (e.g. London, UK)")] string location)
    {
        // In a real implementation, this would call a weather API
        // For this sample, we'll return mock data
        
        await Task.Delay(100); // Simulate network delay
        
        // Mock weather data based on location
        var weatherConditions = new[]
        {
            "sunny", "cloudy", "rainy", "snowy", "windy", "foggy"
        };
        
        var random = new Random();
        var condition = weatherConditions[random.Next(weatherConditions.Length)];
        var temperature = random.Next(-10, 35);
        
        return $"The current weather in {location} is {condition} with a temperature of {temperature}°C.";
    }

    /// <summary>
    /// Gets a weather forecast for a specified location
    /// </summary>
    /// <param name="location">The location to get forecast for</param>
    /// <param name="days">Number of days to forecast (1-7)</param>
    /// <returns>A string describing the weather forecast</returns>
    [KernelFunction]
    [Description("Gets a weather forecast for a specified location")]
    public async Task<string> GetWeatherForecastAsync(
        [Description("The location to get forecast for (e.g. London, UK)")] string location,
        [Description("Number of days to forecast (1-7)")] int days = 3)
    {
        // In a real implementation, this would call a weather API
        // For this sample, we'll return mock data
        
        await Task.Delay(100); // Simulate network delay
        
        // Ensure days is within reasonable bounds
        days = Math.Max(1, Math.Min(7, days));
        
        var weatherConditions = new[]
        {
            "sunny", "cloudy", "rainy", "snowy", "windy", "foggy"
        };
        
        var random = new Random();
        var forecast = new List<string>();
        
        for (int i = 1; i <= days; i++)
        {
            var condition = weatherConditions[random.Next(weatherConditions.Length)];
            var minTemp = random.Next(-10, 25);
            var maxTemp = minTemp + random.Next(5, 15);
            
            forecast.Add($"Day {i}: {condition}, {minTemp}°C to {maxTemp}°C");
        }
        
        return $"Weather forecast for {location}:\n" + string.Join("\n", forecast);
    }
}