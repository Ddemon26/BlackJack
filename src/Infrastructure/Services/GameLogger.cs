using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Services;

/// <summary>
/// Implementation of IGameLogger that provides comprehensive logging capabilities.
/// Supports structured logging with JSON formatting and file-based persistence.
/// </summary>
public class GameLogger : IGameLogger
{
    private readonly string _logDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private LogLevel _currentLogLevel;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the GameLogger class.
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored. If null, uses temp directory.</param>
    /// <param name="logLevel">Initial log level.</param>
    public GameLogger(string? logDirectory = null, LogLevel logLevel = LogLevel.Info)
    {
        _logDirectory = logDirectory ?? Path.Combine(Path.GetTempPath(), "BlackjackLogs");
        _currentLogLevel = logLevel;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure log directory exists
        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch (Exception ex)
        {
            // Fallback to console logging if directory creation fails
            Console.WriteLine($"[GameLogger] Failed to create log directory: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public LogLevel CurrentLogLevel => _currentLogLevel;

    /// <inheritdoc />
    public void SetLogLevel(LogLevel logLevel)
    {
        _currentLogLevel = logLevel;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _currentLogLevel && _currentLogLevel != LogLevel.None;
    }

    /// <inheritdoc />
    public async Task LogInfoAsync(string message, string? context = null, IDictionary<string, object>? properties = null)
    {
        if (IsEnabled(LogLevel.Info))
        {
            await WriteLogEntryAsync(LogLevel.Info, message, null, context, properties);
        }
    }

    /// <inheritdoc />
    public async Task LogWarningAsync(string message, string? context = null, IDictionary<string, object>? properties = null)
    {
        if (IsEnabled(LogLevel.Warning))
        {
            await WriteLogEntryAsync(LogLevel.Warning, message, null, context, properties);
        }
    }

    /// <inheritdoc />
    public async Task LogErrorAsync(string message, Exception? exception = null, string? context = null, IDictionary<string, object>? properties = null)
    {
        if (IsEnabled(LogLevel.Error))
        {
            await WriteLogEntryAsync(LogLevel.Error, message, exception, context, properties);
        }
    }

    /// <inheritdoc />
    public async Task LogDebugAsync(string message, string? context = null, IDictionary<string, object>? properties = null)
    {
        if (IsEnabled(LogLevel.Debug))
        {
            await WriteLogEntryAsync(LogLevel.Debug, message, null, context, properties);
        }
    }

    /// <inheritdoc />
    public async Task LogGameEventAsync(string eventName, string? playerName = null, IDictionary<string, object>? eventData = null)
    {
        if (IsEnabled(LogLevel.Info))
        {
            var properties = new Dictionary<string, object>
            {
                ["EventType"] = "GameEvent",
                ["EventName"] = eventName
            };

            if (!string.IsNullOrEmpty(playerName))
            {
                properties["PlayerName"] = playerName;
            }

            if (eventData != null)
            {
                foreach (var kvp in eventData)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            await WriteLogEntryAsync(LogLevel.Info, $"Game Event: {eventName}", null, "GameEvent", properties);
        }
    }

    /// <inheritdoc />
    public async Task LogPerformanceMetricAsync(string metricName, double value, string unit, IDictionary<string, object>? properties = null)
    {
        if (IsEnabled(LogLevel.Info))
        {
            var metricProperties = new Dictionary<string, object>
            {
                ["EventType"] = "PerformanceMetric",
                ["MetricName"] = metricName,
                ["Value"] = value,
                ["Unit"] = unit
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    metricProperties[kvp.Key] = kvp.Value;
                }
            }

            await WriteLogEntryAsync(LogLevel.Info, $"Performance Metric: {metricName} = {value} {unit}", null, "Performance", metricProperties);
        }
    }

    /// <inheritdoc />
    public IDisposable BeginTimedOperation(string operationName, string? context = null, IDictionary<string, object>? properties = null)
    {
        return new TimedOperation(this, operationName, context, properties);
    }

    /// <summary>
    /// Writes a log entry to the appropriate log file.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception information.</param>
    /// <param name="context">Optional context information.</param>
    /// <param name="properties">Optional structured properties.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WriteLogEntryAsync(LogLevel level, string message, Exception? exception, string? context, IDictionary<string, object>? properties)
    {
        try
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level.ToString(),
                Message = message,
                Context = context,
                Exception = exception?.ToString(),
                Properties = properties ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            var logLine = json + Environment.NewLine;

            // Write to daily log file
            var logFileName = $"blackjack_{DateTime.UtcNow:yyyyMMdd}.log";
            var logFilePath = Path.Combine(_logDirectory, logFileName);

            lock (_lockObject)
            {
                File.AppendAllText(logFilePath, logLine);
            }

            // Also write to console for immediate feedback (in debug mode)
            if (level >= LogLevel.Warning || _currentLogLevel == LogLevel.Debug)
            {
                var consoleMessage = $"[{DateTime.UtcNow:HH:mm:ss}] [{level}] {message}";
                if (!string.IsNullOrEmpty(context))
                {
                    consoleMessage += $" (Context: {context})";
                }
                Console.WriteLine(consoleMessage);
            }
        }
        catch (Exception ex)
        {
            // Fallback to console if file logging fails
            Console.WriteLine($"[GameLogger] Failed to write log entry: {ex.Message}");
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [{level}] {message}");
        }
    }

    /// <summary>
    /// Represents a log entry with structured data.
    /// </summary>
    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Context { get; set; }
        public string? Exception { get; set; }
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a timed operation that logs its duration when disposed.
    /// </summary>
    private class TimedOperation : IDisposable
    {
        private readonly GameLogger _logger;
        private readonly string _operationName;
        private readonly string? _context;
        private readonly IDictionary<string, object>? _properties;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public TimedOperation(GameLogger logger, string operationName, string? context, IDictionary<string, object>? properties)
        {
            _logger = logger;
            _operationName = operationName;
            _context = context;
            _properties = properties;
            _stopwatch = Stopwatch.StartNew();

            // Log operation start
            _ = Task.Run(async () => await _logger.LogDebugAsync($"Started operation: {operationName}", context, properties));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                var duration = _stopwatch.ElapsedMilliseconds;

                var completionProperties = new Dictionary<string, object>
                {
                    ["DurationMs"] = duration,
                    ["OperationName"] = _operationName
                };

                if (_properties != null)
                {
                    foreach (var kvp in _properties)
                    {
                        completionProperties[kvp.Key] = kvp.Value;
                    }
                }

                // Log operation completion
                _ = Task.Run(async () => 
                {
                    await _logger.LogDebugAsync($"Completed operation: {_operationName} in {duration}ms", _context, completionProperties);
                    await _logger.LogPerformanceMetricAsync($"Operation.{_operationName}.Duration", duration, "ms", completionProperties);
                });

                _disposed = true;
            }
        }
    }
}