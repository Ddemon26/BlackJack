using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Services;

/// <summary>
/// Implementation of IGameMonitor that tracks system health and usage patterns.
/// Provides comprehensive monitoring capabilities with in-memory storage and optional persistence.
/// </summary>
public class GameMonitor : IGameMonitor
{
    private readonly IGameLogger? _logger;
    private readonly string? _dataDirectory;
    private readonly ConcurrentDictionary<string, SessionData> _activeSessions;
    private readonly ConcurrentQueue<MonitoringEvent> _eventHistory;
    private readonly ConcurrentDictionary<string, double> _metrics;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _monitoringEnabled;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the GameMonitor class.
    /// </summary>
    /// <param name="logger">Optional logger for monitoring events.</param>
    /// <param name="dataDirectory">Optional directory for persisting monitoring data.</param>
    /// <param name="enabled">Whether monitoring is initially enabled.</param>
    public GameMonitor(IGameLogger? logger = null, string? dataDirectory = null, bool enabled = true)
    {
        _logger = logger;
        _dataDirectory = dataDirectory;
        _monitoringEnabled = enabled;
        _activeSessions = new ConcurrentDictionary<string, SessionData>();
        _eventHistory = new ConcurrentQueue<MonitoringEvent>();
        _metrics = new ConcurrentDictionary<string, double>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure data directory exists if specified
        if (!string.IsNullOrEmpty(_dataDirectory))
        {
            try
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            catch (Exception ex)
            {
                _logger?.LogWarningAsync($"Failed to create monitoring data directory: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public bool IsMonitoringEnabled => _monitoringEnabled;

    /// <inheritdoc />
    public void SetMonitoringEnabled(bool enabled)
    {
        _monitoringEnabled = enabled;
        _logger?.LogInfoAsync($"Monitoring {(enabled ? "enabled" : "disabled")}");
    }

    /// <inheritdoc />
    public async Task RecordSessionStartAsync(string sessionId, int playerCount, IDictionary<string, object>? configuration = null)
    {
        if (!_monitoringEnabled) return;

        var sessionData = new SessionData
        {
            SessionId = sessionId,
            StartTime = DateTime.UtcNow,
            PlayerCount = playerCount,
            Configuration = configuration ?? new Dictionary<string, object>(),
            RoundsPlayed = 0,
            PlayerActions = new List<PlayerActionData>(),
            Errors = new List<ErrorData>()
        };

        _activeSessions.TryAdd(sessionId, sessionData);

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "SessionStart",
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Data = new Dictionary<string, object>
            {
                ["PlayerCount"] = playerCount,
                ["Configuration"] = configuration ?? new Dictionary<string, object>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogGameEventAsync("SessionStart", null, monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);
    }

    /// <inheritdoc />
    public async Task RecordSessionEndAsync(string sessionId, TimeSpan duration, int roundsPlayed, IDictionary<string, object>? sessionData = null)
    {
        if (!_monitoringEnabled) return;

        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            session.EndTime = DateTime.UtcNow;
            session.Duration = duration;
            session.RoundsPlayed = roundsPlayed;
        }

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "SessionEnd",
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Data = new Dictionary<string, object>
            {
                ["Duration"] = duration.TotalMinutes,
                ["RoundsPlayed"] = roundsPlayed,
                ["SessionData"] = sessionData ?? new Dictionary<string, object>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogGameEventAsync("SessionEnd", null, monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);

        // Update metrics
        await RecordMetricAsync("Sessions.Completed", 1);
        await RecordMetricAsync("Sessions.AverageDuration", duration.TotalMinutes);
        await RecordMetricAsync("Sessions.AverageRounds", roundsPlayed);
    }

    /// <inheritdoc />
    public async Task RecordPlayerActionAsync(string sessionId, string playerName, string action, IDictionary<string, object>? actionData = null)
    {
        if (!_monitoringEnabled) return;

        var playerActionData = new PlayerActionData
        {
            PlayerName = playerName,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Data = actionData ?? new Dictionary<string, object>()
        };

        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            session.PlayerActions.Add(playerActionData);
        }

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "PlayerAction",
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Data = new Dictionary<string, object>
            {
                ["PlayerName"] = playerName,
                ["Action"] = action,
                ["ActionData"] = actionData ?? new Dictionary<string, object>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogGameEventAsync("PlayerAction", playerName, monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);

        // Update action metrics
        await RecordMetricAsync($"PlayerActions.{action}", 1);
        await RecordMetricAsync("PlayerActions.Total", 1);
    }

    /// <inheritdoc />
    public async Task RecordErrorAsync(string? sessionId, string errorType, Exception exception, IDictionary<string, object>? context = null)
    {
        if (!_monitoringEnabled) return;

        var errorData = new ErrorData
        {
            ErrorType = errorType,
            Exception = exception.ToString(),
            Timestamp = DateTime.UtcNow,
            Context = context ?? new Dictionary<string, object>()
        };

        if (!string.IsNullOrEmpty(sessionId) && _activeSessions.TryGetValue(sessionId, out var session))
        {
            session.Errors.Add(errorData);
        }

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "Error",
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Data = new Dictionary<string, object>
            {
                ["ErrorType"] = errorType,
                ["ExceptionType"] = exception.GetType().Name,
                ["ExceptionMessage"] = exception.Message,
                ["Context"] = context ?? new Dictionary<string, object>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogErrorAsync($"Monitored Error: {errorType}", exception, sessionId, monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);

        // Update error metrics
        await RecordMetricAsync($"Errors.{errorType}", 1);
        await RecordMetricAsync("Errors.Total", 1);
    }

    /// <inheritdoc />
    public async Task RecordMetricAsync(string metricName, double value, IDictionary<string, string>? tags = null)
    {
        if (!_monitoringEnabled) return;

        _metrics.AddOrUpdate(metricName, value, (key, oldValue) => value);

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "Metric",
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                ["MetricName"] = metricName,
                ["Value"] = value,
                ["Tags"] = tags ?? new Dictionary<string, string>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogPerformanceMetricAsync(metricName, value, "count", monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);
    }

    /// <inheritdoc />
    public async Task RecordSystemResourcesAsync(double memoryUsageMB, double cpuUsagePercent, IDictionary<string, double>? additionalMetrics = null)
    {
        if (!_monitoringEnabled) return;

        await RecordMetricAsync("System.MemoryUsageMB", memoryUsageMB);
        await RecordMetricAsync("System.CpuUsagePercent", cpuUsagePercent);

        if (additionalMetrics != null)
        {
            foreach (var metric in additionalMetrics)
            {
                await RecordMetricAsync($"System.{metric.Key}", metric.Value);
            }
        }

        var monitoringEvent = new MonitoringEvent
        {
            EventType = "SystemResources",
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                ["MemoryUsageMB"] = memoryUsageMB,
                ["CpuUsagePercent"] = cpuUsagePercent,
                ["AdditionalMetrics"] = additionalMetrics ?? new Dictionary<string, double>()
            }
        };

        _eventHistory.Enqueue(monitoringEvent);
        await _logger?.LogDebugAsync($"System Resources: Memory={memoryUsageMB:F1}MB, CPU={cpuUsagePercent:F1}%", "SystemMonitoring", monitoringEvent.Data)!;
        await PersistEventAsync(monitoringEvent);
    }

    /// <inheritdoc />
    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        var currentTime = DateTime.UtcNow;
        var metrics = new Dictionary<string, double>();
        var issues = new List<string>();

        // Collect current metrics
        foreach (var metric in _metrics)
        {
            metrics[metric.Key] = metric.Value;
        }

        // Add system resource metrics
        var process = Process.GetCurrentProcess();
        var memoryUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
        metrics["System.MemoryUsageMB"] = memoryUsageMB;

        // Determine health status based on metrics and recent errors
        var healthStatus = HealthStatus.Healthy;

        // Check for high memory usage
        if (memoryUsageMB > 500) // 500MB threshold
        {
            issues.Add($"High memory usage: {memoryUsageMB:F1}MB");
            healthStatus = HealthStatus.Warning;
        }

        // Check for recent errors
        var recentErrors = _eventHistory
            .Where(e => e.EventType == "Error" && e.Timestamp > currentTime.AddMinutes(-5))
            .Count();

        if (recentErrors > 5)
        {
            issues.Add($"High error rate: {recentErrors} errors in last 5 minutes");
            healthStatus = HealthStatus.Critical;
        }
        else if (recentErrors > 0)
        {
            issues.Add($"Recent errors: {recentErrors} errors in last 5 minutes");
            if (healthStatus == HealthStatus.Healthy)
                healthStatus = HealthStatus.Warning;
        }

        // Check for active sessions that might be stuck
        var stuckSessions = _activeSessions.Values
            .Where(s => s.StartTime < currentTime.AddHours(-2))
            .Count();

        if (stuckSessions > 0)
        {
            issues.Add($"Long-running sessions: {stuckSessions} sessions active for over 2 hours");
            if (healthStatus == HealthStatus.Healthy)
                healthStatus = HealthStatus.Warning;
        }

        await _logger?.LogDebugAsync($"System health check: {healthStatus}, {issues.Count} issues", "HealthCheck")!;

        return new SystemHealthStatus(healthStatus, currentTime, metrics, issues);
    }

    /// <inheritdoc />
    public async Task<UsageStatistics> GetUsageStatisticsAsync(DateTime startTime, DateTime endTime)
    {
        var relevantEvents = _eventHistory
            .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
            .ToList();

        var sessionStarts = relevantEvents.Where(e => e.EventType == "SessionStart").Count();
        var sessionEnds = relevantEvents.Where(e => e.EventType == "SessionEnd").ToList();
        
        var totalRounds = sessionEnds
            .Where(e => e.Data.ContainsKey("RoundsPlayed"))
            .Sum(e => Convert.ToInt32(e.Data["RoundsPlayed"]));

        var averageDuration = sessionEnds
            .Where(e => e.Data.ContainsKey("Duration"))
            .Select(e => Convert.ToDouble(e.Data["Duration"]))
            .DefaultIfEmpty(0)
            .Average();

        var errorCount = relevantEvents.Where(e => e.EventType == "Error").Count();

        var additionalMetrics = new Dictionary<string, double>
        {
            ["PlayerActionsTotal"] = relevantEvents.Where(e => e.EventType == "PlayerAction").Count(),
            ["AveragePlayersPerSession"] = sessionStarts > 0 ? 
                relevantEvents.Where(e => e.EventType == "SessionStart" && e.Data.ContainsKey("PlayerCount"))
                    .Select(e => Convert.ToDouble(e.Data["PlayerCount"]))
                    .DefaultIfEmpty(0)
                    .Average() : 0
        };

        await _logger?.LogInfoAsync($"Usage statistics calculated for period {startTime:yyyy-MM-dd} to {endTime:yyyy-MM-dd}: {sessionStarts} sessions, {totalRounds} rounds, {errorCount} errors")!;

        return new UsageStatistics(
            startTime, 
            endTime, 
            sessionStarts, 
            totalRounds, 
            TimeSpan.FromMinutes(averageDuration), 
            errorCount, 
            additionalMetrics);
    }

    /// <summary>
    /// Persists a monitoring event to storage if configured.
    /// </summary>
    /// <param name="monitoringEvent">The event to persist.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PersistEventAsync(MonitoringEvent monitoringEvent)
    {
        if (string.IsNullOrEmpty(_dataDirectory)) return;

        try
        {
            var json = JsonSerializer.Serialize(monitoringEvent, _jsonOptions);
            var fileName = $"monitoring_{DateTime.UtcNow:yyyyMMdd}.json";
            var filePath = Path.Combine(_dataDirectory, fileName);

            lock (_lockObject)
            {
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            await _logger?.LogWarningAsync($"Failed to persist monitoring event: {ex.Message}")!;
        }
    }

    /// <summary>
    /// Represents session data for monitoring.
    /// </summary>
    private class SessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int PlayerCount { get; set; }
        public int RoundsPlayed { get; set; }
        public IDictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public List<PlayerActionData> PlayerActions { get; set; } = new();
        public List<ErrorData> Errors { get; set; } = new();
    }

    /// <summary>
    /// Represents player action data for monitoring.
    /// </summary>
    private class PlayerActionData
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents error data for monitoring.
    /// </summary>
    private class ErrorData
    {
        public string ErrorType { get; set; } = string.Empty;
        public string Exception { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public IDictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a monitoring event.
    /// </summary>
    private class MonitoringEvent
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? SessionId { get; set; }
        public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}