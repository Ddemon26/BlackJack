using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using GroupProject.Infrastructure.Services;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Tests.Infrastructure.Services;

public class GameLoggerTests : IDisposable
{
    private readonly string _testLogDirectory;
    private readonly GameLogger _logger;

    public GameLoggerTests()
    {
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"GameLoggerTests_{Guid.NewGuid():N}");
        _logger = new GameLogger(_testLogDirectory, LogLevel.Debug);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            Directory.Delete(_testLogDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var logger = new GameLogger(_testLogDirectory, LogLevel.Warning);

        // Assert
        Assert.Equal(LogLevel.Warning, logger.CurrentLogLevel);
        Assert.True(Directory.Exists(_testLogDirectory));
    }

    [Fact]
    public void Constructor_WithNullDirectory_UsesDefaultDirectory()
    {
        // Arrange & Act
        var logger = new GameLogger(null, LogLevel.Info);

        // Assert
        Assert.Equal(LogLevel.Info, logger.CurrentLogLevel);
    }

    [Theory]
    [InlineData(LogLevel.Debug, LogLevel.Debug, true)]
    [InlineData(LogLevel.Info, LogLevel.Debug, true)]
    [InlineData(LogLevel.Warning, LogLevel.Info, true)]
    [InlineData(LogLevel.Error, LogLevel.Warning, true)]
    [InlineData(LogLevel.Debug, LogLevel.Info, false)]
    [InlineData(LogLevel.Info, LogLevel.Warning, false)]
    [InlineData(LogLevel.Warning, LogLevel.Error, false)]
    [InlineData(LogLevel.Debug, LogLevel.None, false)]
    public void IsEnabled_WithDifferentLevels_ReturnsCorrectValue(LogLevel messageLevel, LogLevel loggerLevel, bool expectedEnabled)
    {
        // Arrange
        var logger = new GameLogger(_testLogDirectory, loggerLevel);

        // Act
        var isEnabled = logger.IsEnabled(messageLevel);

        // Assert
        Assert.Equal(expectedEnabled, isEnabled);
    }

    [Fact]
    public void SetLogLevel_WithNewLevel_UpdatesCurrentLogLevel()
    {
        // Arrange
        var logger = new GameLogger(_testLogDirectory, LogLevel.Info);
        Assert.Equal(LogLevel.Info, logger.CurrentLogLevel);

        // Act
        logger.SetLogLevel(LogLevel.Error);

        // Assert
        Assert.Equal(LogLevel.Error, logger.CurrentLogLevel);
    }

    [Fact]
    public async Task LogInfoAsync_WithValidMessage_CreatesLogFile()
    {
        // Arrange
        const string message = "Test info message";
        const string context = "Test context";

        // Act
        await _logger.LogInfoAsync(message, context);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(message, logContent);
        Assert.Contains(context, logContent);
        Assert.Contains("Info", logContent);
    }

    [Fact]
    public async Task LogWarningAsync_WithValidMessage_CreatesLogEntry()
    {
        // Arrange
        const string message = "Test warning message";
        var properties = new Dictionary<string, object> { ["TestProperty"] = "TestValue" };

        // Act
        await _logger.LogWarningAsync(message, "TestContext", properties);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(message, logContent);
        Assert.Contains("Warning", logContent);
        Assert.Contains("TestProperty", logContent);
        Assert.Contains("TestValue", logContent);
    }

    [Fact]
    public async Task LogErrorAsync_WithException_IncludesExceptionDetails()
    {
        // Arrange
        const string message = "Test error message";
        var exception = new InvalidOperationException("Test exception message");

        // Act
        await _logger.LogErrorAsync(message, exception, "ErrorContext");

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(message, logContent);
        Assert.Contains("Error", logContent);
        Assert.Contains("Test exception message", logContent);
        Assert.Contains("InvalidOperationException", logContent);
    }

    [Fact]
    public async Task LogDebugAsync_WithDebugLevel_CreatesLogEntry()
    {
        // Arrange
        const string message = "Test debug message";

        // Act
        await _logger.LogDebugAsync(message);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(message, logContent);
        Assert.Contains("Debug", logContent);
    }

    [Fact]
    public async Task LogDebugAsync_WithInfoLevel_DoesNotCreateLogEntry()
    {
        // Arrange
        var logger = new GameLogger(_testLogDirectory, LogLevel.Info);
        const string message = "Test debug message";

        // Act
        await logger.LogDebugAsync(message);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Empty(logFiles);
    }

    [Fact]
    public async Task LogGameEventAsync_WithEventData_CreatesStructuredLogEntry()
    {
        // Arrange
        const string eventName = "PlayerAction";
        const string playerName = "TestPlayer";
        var eventData = new Dictionary<string, object>
        {
            ["Action"] = "Hit",
            ["HandValue"] = 15
        };

        // Act
        await _logger.LogGameEventAsync(eventName, playerName, eventData);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(eventName, logContent);
        Assert.Contains(playerName, logContent);
        Assert.Contains("GameEvent", logContent);
        Assert.Contains("Hit", logContent);
        Assert.Contains("15", logContent);
    }

    [Fact]
    public async Task LogPerformanceMetricAsync_WithMetricData_CreatesMetricLogEntry()
    {
        // Arrange
        const string metricName = "ResponseTime";
        const double value = 123.45;
        const string unit = "ms";
        var properties = new Dictionary<string, object> { ["Operation"] = "DealCards" };

        // Act
        await _logger.LogPerformanceMetricAsync(metricName, value, unit, properties);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(metricName, logContent);
        Assert.Contains("123.45", logContent);
        Assert.Contains(unit, logContent);
        Assert.Contains("PerformanceMetric", logContent);
        Assert.Contains("DealCards", logContent);
    }

    [Fact]
    public void BeginTimedOperation_WithValidOperation_ReturnsDisposable()
    {
        // Arrange
        const string operationName = "TestOperation";

        // Act
        var timedOperation = _logger.BeginTimedOperation(operationName);

        // Assert
        Assert.NotNull(timedOperation);
        Assert.IsAssignableFrom<IDisposable>(timedOperation);
    }

    [Fact]
    public async Task BeginTimedOperation_WhenDisposed_LogsCompletionTime()
    {
        // Arrange
        const string operationName = "TestOperation";

        // Act
        using (var timedOperation = _logger.BeginTimedOperation(operationName))
        {
            await Task.Delay(10); // Small delay to ensure measurable time
        }

        // Wait a bit for async logging to complete
        await Task.Delay(100);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains("Started operation", logContent);
        Assert.Contains("Completed operation", logContent);
        Assert.Contains(operationName, logContent);
        Assert.Contains("DurationMs", logContent);
    }

    [Fact]
    public async Task MultipleLogCalls_CreatesSingleLogFile_WithMultipleEntries()
    {
        // Arrange & Act
        await _logger.LogInfoAsync("First message");
        await _logger.LogWarningAsync("Second message");
        await _logger.LogErrorAsync("Third message");

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        
        Assert.Contains("First message", logContent);
        Assert.Contains("Second message", logContent);
        Assert.Contains("Third message", logContent);
    }

    [Fact]
    public async Task LogInfoAsync_WithNoneLogLevel_DoesNotCreateLogEntry()
    {
        // Arrange
        var logger = new GameLogger(_testLogDirectory, LogLevel.None);
        const string message = "Test message";

        // Act
        await logger.LogInfoAsync(message);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Empty(logFiles);
    }

    [Fact]
    public async Task LogGameEventAsync_WithNullPlayerName_HandlesGracefully()
    {
        // Arrange
        const string eventName = "GameStart";

        // Act
        await _logger.LogGameEventAsync(eventName, null);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(eventName, logContent);
        Assert.Contains("GameEvent", logContent);
    }

    [Fact]
    public async Task LogPerformanceMetricAsync_WithNullProperties_HandlesGracefully()
    {
        // Arrange
        const string metricName = "TestMetric";
        const double value = 42.0;
        const string unit = "count";

        // Act
        await _logger.LogPerformanceMetricAsync(metricName, value, unit, null);

        // Assert
        var logFiles = Directory.GetFiles(_testLogDirectory, "*.log");
        Assert.Single(logFiles);
        
        var logContent = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains(metricName, logContent);
        Assert.Contains("42", logContent);
        Assert.Contains(unit, logContent);
    }
}