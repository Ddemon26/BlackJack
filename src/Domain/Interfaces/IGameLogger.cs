using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for logging game events, errors, and performance metrics.
/// Provides structured logging capabilities for blackjack game operations.
/// </summary>
public interface IGameLogger
{
    /// <summary>
    /// Logs an informational message about game events.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogInfoAsync(string message, string? context = null, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a warning message about potential issues.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogWarningAsync(string message, string? context = null, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogErrorAsync(string message, Exception? exception = null, string? context = null, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a debug message for detailed troubleshooting.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogDebugAsync(string message, string? context = null, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a game event with structured data.
    /// </summary>
    /// <param name="eventName">The name of the game event.</param>
    /// <param name="playerName">The name of the player involved (if applicable).</param>
    /// <param name="eventData">Structured data about the event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogGameEventAsync(string eventName, string? playerName = null, IDictionary<string, object>? eventData = null);

    /// <summary>
    /// Logs performance metrics for monitoring system health.
    /// </summary>
    /// <param name="metricName">The name of the performance metric.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="properties">Optional additional properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogPerformanceMetricAsync(string metricName, double value, string unit, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Logs the start of a timed operation and returns a disposable that logs the completion.
    /// </summary>
    /// <param name="operationName">The name of the operation being timed.</param>
    /// <param name="context">Additional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A disposable that will log the operation completion when disposed.</returns>
    IDisposable BeginTimedOperation(string operationName, string? context = null, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Gets the current log level for filtering messages.
    /// </summary>
    LogLevel CurrentLogLevel { get; }

    /// <summary>
    /// Sets the minimum log level for messages to be logged.
    /// </summary>
    /// <param name="logLevel">The minimum log level.</param>
    void SetLogLevel(LogLevel logLevel);

    /// <summary>
    /// Determines if logging is enabled for the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if logging is enabled for the level, false otherwise.</returns>
    bool IsEnabled(LogLevel logLevel);
}

/// <summary>
/// Represents the different levels of logging severity.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug level - detailed information for troubleshooting.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Information level - general operational messages.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning level - potentially harmful situations.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error level - error events that might still allow the application to continue.
    /// </summary>
    Error = 3,

    /// <summary>
    /// None - disables all logging.
    /// </summary>
    None = 4
}