namespace SemanticKernelFunctionCaller.Application.Interfaces;

/// <summary>
/// Provides centralized retry execution with exponential backoff
/// </summary>
public interface IRetryPolicyExecutor
{
    /// <summary>
    /// Executes an operation with retry policy and fallback handling
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="fallbackOperation">Optional fallback operation if all retries fail</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation or fallback</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, Task<T>>? fallbackOperation = null,
        string operationName = "Operation",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation with retry policy for transient exceptions
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="isTransient">Function to determine if an exception is transient</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="initialDelay">Initial delay before first retry</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<T> ExecuteWithTransientRetryAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool> isTransient,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        string operationName = "Operation",
        CancellationToken cancellationToken = default);
}