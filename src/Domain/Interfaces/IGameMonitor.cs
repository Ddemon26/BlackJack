using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for monitoring game system health and usage patterns.
/// Provides hooks for tracking performance, errors, and user behavior.
/// </summary>
public interface IGameMonitor
{
    /// <summary>
    /// Records a game session start event.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="playerCount">The number of players in the session.</param>
    /// <param name="configuration">Game configuration details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordSessionStartAsync(string sessionId, int playerCount, IDictionary<string, object>? configuration = null);

    /// <summary>
    /// Records a game session end event.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="duration">The session duration.</param>
    /// <param name="roundsPlayed">The number of rounds played.</param>
    /// <param name="sessionData">Additional session data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordSessionEndAsync(string sessionId, TimeSpan duration, int roundsPlayed, IDictionary<string, object>? sessionData = null);

    /// <summary>
    /// Records a player action event.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="action">The action taken by the player.</param>
    /// <param name="actionData">Additional data about the action.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordPlayerActionAsync(string sessionId, string playerName, string action, IDictionary<string, object>? actionData = null);

    /// <summary>
    /// Records a game error event.
    /// </summary>
    /// <param name="sessionId">The session identifier (if applicable).</param>
    /// <param name="errorType">The type of error that occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="context">Additional context about the error.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordErrorAsync(string? sessionId, string errorType, Exception exception, IDictionary<string, object>? context = null);

    /// <summary>
    /// Records a performance metric.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="tags">Optional tags for categorizing the metric.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordMetricAsync(string metricName, double value, IDictionary<string, string>? tags = null);

    /// <summary>
    /// Records system resource usage.
    /// </summary>
    /// <param name="memoryUsageMB">Current memory usage in megabytes.</param>
    /// <param name="cpuUsagePercent">Current CPU usage percentage.</param>
    /// <param name="additionalMetrics">Additional system metrics.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordSystemResourcesAsync(double memoryUsageMB, double cpuUsagePercent, IDictionary<string, double>? additionalMetrics = null);

    /// <summary>
    /// Gets current system health status.
    /// </summary>
    /// <returns>A task that returns the current system health status.</returns>
    Task<SystemHealthStatus> GetSystemHealthAsync();

    /// <summary>
    /// Gets usage statistics for a specified time period.
    /// </summary>
    /// <param name="startTime">The start time for the statistics period.</param>
    /// <param name="endTime">The end time for the statistics period.</param>
    /// <returns>A task that returns usage statistics for the period.</returns>
    Task<UsageStatistics> GetUsageStatisticsAsync(DateTime startTime, DateTime endTime);

    /// <summary>
    /// Enables or disables monitoring.
    /// </summary>
    /// <param name="enabled">True to enable monitoring, false to disable.</param>
    void SetMonitoringEnabled(bool enabled);

    /// <summary>
    /// Gets a value indicating whether monitoring is currently enabled.
    /// </summary>
    bool IsMonitoringEnabled { get; }
}

/// <summary>
/// Represents the current system health status.
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Initializes a new instance of the SystemHealthStatus class.
    /// </summary>
    /// <param name="status">The overall health status.</param>
    /// <param name="lastChecked">When the health was last checked.</param>
    /// <param name="metrics">Current system metrics.</param>
    /// <param name="issues">Any current health issues.</param>
    public SystemHealthStatus(HealthStatus status, DateTime lastChecked, IDictionary<string, double> metrics, IList<string> issues)
    {
        Status = status;
        LastChecked = lastChecked;
        Metrics = metrics ?? new Dictionary<string, double>();
        Issues = issues ?? new List<string>();
    }

    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets when the health was last checked.
    /// </summary>
    public DateTime LastChecked { get; }

    /// <summary>
    /// Gets the current system metrics.
    /// </summary>
    public IDictionary<string, double> Metrics { get; }

    /// <summary>
    /// Gets any current health issues.
    /// </summary>
    public IList<string> Issues { get; }
}

/// <summary>
/// Represents usage statistics for a time period.
/// </summary>
public class UsageStatistics
{
    /// <summary>
    /// Initializes a new instance of the UsageStatistics class.
    /// </summary>
    /// <param name="startTime">The start time of the statistics period.</param>
    /// <param name="endTime">The end time of the statistics period.</param>
    /// <param name="sessionCount">The number of sessions during the period.</param>
    /// <param name="totalRounds">The total number of rounds played.</param>
    /// <param name="averageSessionDuration">The average session duration.</param>
    /// <param name="errorCount">The number of errors that occurred.</param>
    /// <param name="additionalMetrics">Additional usage metrics.</param>
    public UsageStatistics(DateTime startTime, DateTime endTime, int sessionCount, int totalRounds, 
        TimeSpan averageSessionDuration, int errorCount, IDictionary<string, double> additionalMetrics)
    {
        StartTime = startTime;
        EndTime = endTime;
        SessionCount = sessionCount;
        TotalRounds = totalRounds;
        AverageSessionDuration = averageSessionDuration;
        ErrorCount = errorCount;
        AdditionalMetrics = additionalMetrics ?? new Dictionary<string, double>();
    }

    /// <summary>
    /// Gets the start time of the statistics period.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the end time of the statistics period.
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    /// Gets the number of sessions during the period.
    /// </summary>
    public int SessionCount { get; }

    /// <summary>
    /// Gets the total number of rounds played.
    /// </summary>
    public int TotalRounds { get; }

    /// <summary>
    /// Gets the average session duration.
    /// </summary>
    public TimeSpan AverageSessionDuration { get; }

    /// <summary>
    /// Gets the number of errors that occurred.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Gets additional usage metrics.
    /// </summary>
    public IDictionary<string, double> AdditionalMetrics { get; }
}

/// <summary>
/// Represents the overall health status of the system.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy and operating normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// System is experiencing minor issues but is still functional.
    /// </summary>
    Warning,

    /// <summary>
    /// System is experiencing significant issues that may affect functionality.
    /// </summary>
    Critical,

    /// <summary>
    /// System health status is unknown or cannot be determined.
    /// </summary>
    Unknown
}