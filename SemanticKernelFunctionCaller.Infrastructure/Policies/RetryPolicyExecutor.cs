using Microsoft.Extensions.Logging;
using SemanticKernelFunctionCaller.Application.Interfaces;
using System.Diagnostics;

namespace SemanticKernelFunctionCaller.Infrastructure.Policies;

/// <summary>
/// Provides centralized retry execution with exponential backoff and comprehensive logging
/// </summary>
public class RetryPolicyExecutor : IRetryPolicyExecutor
{
    private readonly ILogger<RetryPolicyExecutor> _logger;

    public RetryPolicyExecutor(ILogger<RetryPolicyExecutor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, Task<T>>? fallbackOperation = null,
        string operationName = "Operation",
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = operationName
        });

        _logger.LogInformation("Starting {OperationName} with correlation ID: {CorrelationId}", operationName, correlationId);

        try
        {
            var result = await operation();
            _logger.LogInformation("Successfully completed {OperationName} with correlation ID: {CorrelationId}", operationName, correlationId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {OperationName}. Correlation ID: {CorrelationId}", operationName, correlationId);
            
            if (fallbackOperation != null)
            {
                _logger.LogInformation("Executing fallback for {OperationName}. Correlation ID: {CorrelationId}", operationName, correlationId);
                return await fallbackOperation(ex);
            }
            
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T> ExecuteWithTransientRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool> isTransient,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        string operationName = "Operation",
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = operationName,
            ["MaxRetries"] = maxRetries
        });

        _logger.LogInformation("Starting {OperationName} with transient retry policy. Correlation ID: {CorrelationId}", operationName, correlationId);

        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                if (retryCount > 0)
                {
                    _logger.LogInformation("Retry attempt {RetryCount}/{MaxRetries} for {OperationName}. Correlation ID: {CorrelationId}", 
                        retryCount, maxRetries, operationName, correlationId);
                    
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromTicks(delay.Ticks * 2); // Exponential backoff
                }

                var stopwatch = Stopwatch.StartNew();
                var result = await operation();
                stopwatch.Stop();

                _logger.LogInformation("Successfully completed {OperationName} in {ElapsedMs}ms. Correlation ID: {CorrelationId}", 
                    operationName, stopwatch.ElapsedMilliseconds, correlationId);
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                stopwatch?.Stop();

                if (isTransient(ex) && retryCount < maxRetries)
                {
                    _logger.LogWarning(ex, "Transient error during {OperationName} attempt {RetryCount}. Retrying... Correlation ID: {CorrelationId}", 
                        operationName, retryCount, correlationId);
                    retryCount++;
                }
                else
                {
                    _logger.LogError(ex, "Permanent error or max retries exhausted for {OperationName}. Correlation ID: {CorrelationId}", 
                        operationName, correlationId);
                    throw;
                }
            }
        }

        // This should never be reached, but just in case
        _logger.LogError("All retries exhausted for {OperationName}. Correlation ID: {CorrelationId}", operationName, correlationId);
        throw lastException ?? new InvalidOperationException($"All retries exhausted for {operationName}");
    }
}