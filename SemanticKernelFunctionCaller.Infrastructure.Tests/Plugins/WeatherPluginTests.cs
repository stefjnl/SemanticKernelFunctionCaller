using SemanticKernelFunctionCaller.Infrastructure.Plugins;
using System.Threading.Tasks;
using Xunit;

namespace SemanticKernelFunctionCaller.Infrastructure.Tests.Plugins;

public class WeatherPluginTests
{
    private readonly WeatherPlugin _weatherPlugin;

    public WeatherPluginTests()
    {
        _weatherPlugin = new WeatherPlugin();
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithValidLocation_ReturnsWeatherString()
    {
        // Arrange
        var location = "London, UK";

        // Act
        var result = await _weatherPlugin.GetCurrentWeatherAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(location, result);
        Assert.Contains("weather", result.ToLower());
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithValidLocation_ReturnsForecastString()
    {
        // Arrange
        var location = "New York, USA";
        var days = 3;

        // Act
        var result = await _weatherPlugin.GetWeatherForecastAsync(location, days);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(location, result);
        Assert.Contains("forecast", result.ToLower());
        Assert.Contains("day", result.ToLower());
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithDefaultDays_ReturnsThreeDayForecast()
    {
        // Arrange
        var location = "Paris, France";

        // Act
        var result = await _weatherPlugin.GetWeatherForecastAsync(location);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(location, result);
        // The mock implementation always returns a 3-day forecast by default
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public async Task GetWeatherForecastAsync_WithDifferentDays_ReturnsForecast(int days)
    {
        // Arrange
        var location = "Tokyo, Japan";

        // Act
        var result = await _weatherPlugin.GetWeatherForecastAsync(location, days);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(location, result);
        Assert.Contains("forecast", result.ToLower());
    }
}