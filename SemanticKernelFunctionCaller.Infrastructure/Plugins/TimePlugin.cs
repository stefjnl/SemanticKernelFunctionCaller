using Microsoft.SemanticKernel;

namespace SemanticKernelFunctionCaller.Infrastructure.Plugins;

/// <summary>
/// Plugin for time-related operations that can be used by Semantic Kernel
/// </summary>
public class TimePlugin
{
    /// <summary>
    /// Initializes a new instance of the TimePlugin class
    /// </summary>
    public TimePlugin()
    {
    }

    /// <summary>
    /// Gets the current time in a specified timezone
    /// </summary>
    /// <param name="timezone">Timezone identifier (e.g., 'America/New_York', 'UTC', 'Europe/London')</param>
    /// <returns>The current time in the specified timezone formatted as ISO 8601 string</returns>
    [KernelFunction]
    public string GetCurrentTime(string timezone = "UTC")
    {
        try
        {
            TimeZoneInfo tz;
            
            // Handle common timezone aliases
            var normalizedTimezone = timezone.Trim();
            if (normalizedTimezone.Equals("UTC", StringComparison.OrdinalIgnoreCase))
            {
                tz = TimeZoneInfo.Utc;
            }
            else if (normalizedTimezone.Equals("GMT", StringComparison.OrdinalIgnoreCase))
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            }
            else if (normalizedTimezone.Equals("EST", StringComparison.OrdinalIgnoreCase))
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            else if (normalizedTimezone.Equals("PST", StringComparison.OrdinalIgnoreCase))
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            else
            {
                // Try to find the timezone by system ID
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimezone);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback to common Windows timezone mappings
                    tz = normalizedTimezone.ToLowerInvariant() switch
                    {
                        "america/new_york" => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                        "america/los_angeles" => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"),
                        "america/chicago" => TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"),
                        "europe/london" => TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
                        "europe/paris" => TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"),
                        "europe/berlin" => TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"),
                        "asia/tokyo" => TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"),
                        "asia/shanghai" => TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"),
                        "australia/sydney" => TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"),
                        _ => TimeZoneInfo.Utc
                    };
                }
            }
            
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var result = currentTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            return result;
        }
        catch (Exception ex)
        {
            return $"Error: Unable to get time for timezone '{timezone}'. {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the current UTC time
    /// </summary>
    /// <returns>The current UTC time formatted as ISO 8601 string</returns>
    [KernelFunction]
    public string GetUtcTime()
    {
        return GetCurrentTime("UTC");
    }

    /// <summary>
    /// Gets a list of available timezone identifiers
    /// </summary>
    /// <returns>A list of common timezone identifiers that can be used with GetCurrentTime</returns>
    [KernelFunction]
    public string GetAvailableTimezones()
    {
        var commonTimezones = new[]
        {
            "UTC", "GMT", "EST", "PST",
            "America/New_York", "America/Los_Angeles", "America/Chicago",
            "Europe/London", "Europe/Paris", "Europe/Berlin",
            "Asia/Tokyo", "Asia/Shanghai", "Asia/Dubai",
            "Australia/Sydney", "Australia/Melbourne"
        };

        return string.Join(", ", commonTimezones);
    }
}