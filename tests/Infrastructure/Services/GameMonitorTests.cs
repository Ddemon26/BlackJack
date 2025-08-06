using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using GroupProject.Infrastructure.Services;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Tests.Infrastructure.Services;

public class GameMonitorTests : IDisposable
{
    private readonly string _testDataDirectory;
    private readonly Mock<IGameLogger> _mockLogger;
    private readonly GameMonitor _monitor;

    public GameMonitorTests()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"GameMonitorTests_{Guid.NewGuid():N}");
        _mockLogger = new Mock<IGameLogger>();
        _monitor = new GameMonitor(_mockLogger.Object, _testDataDirectory, true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var monitor = new GameMonitor(_mockLogger.Object, _testDataDirectory, true);

        // Assert
        Assert.True(monitor.IsMonitoringEnabled);
        Assert.True(Directory.Exists(_testDataDirectory));
    }

    [Fact]
    public void Constructor_WithDisabledMonitoring_SetsEnabledToFalse()
    {
        // Arrange & Act
        var monitor = new GameMonitor(_mockLogger.Object, _testDataDirectory, false);

        // Assert
        Assert.False(monitor.IsMonitoringEnabled);
    }

    [Fact]
    public void SetMonitoringEnabled_WithTrue_EnablesMonitoring()
    {
        // Arrange
        var monitor = new GameMonitor(_mockLogger.Object, _testDataDirectory, false);
        Assert.False(monitor.IsMonitoringEnabled);

        // Act
        monitor.SetMonitoringEnabled(true);

        // Assert
        Assert.True(monitor.IsMonitoringEnabled);
        _mockLogger.Verify(x => x.LogInfoAsync("Monitoring enabled", null, null), Times.Once);
    }

    [Fact]
    public void SetMonitoringEnabled_WithFalse_DisablesMonitoring()
    {
        // Arrange
        Assert.True(_monitor.IsMonitoringEnabled);

        // Act
        _monitor.SetMonitoringEnabled(false);

        // Assert
        Assert.False(_monitor.IsMonitoringEnabled);
        _mockLogger.Verify(x => x.LogInfoAsync("Monitoring disabled", null, null), Times.Once);
    }

    [Fact]
    public async Task RecordSessionStartAsync_WithValidData_LogsAndPersistsEvent()
    {
        // Arrange
        const string sessionId = "test_session_123";
        const int playerCount = 3;
        var configuration = new Dictionary<string, object> { ["DeckCount"] = 6 };

        // Act
        await _monitor.RecordSessionStartAsync(sessionId, playerCount, configuration);

        // Assert
        _mockLogger.Verify(x => x.LogGameEventAsync("SessionStart", null, 
            It.Is<IDictionary<string, object>>(d => 
                d.ContainsKey("PlayerCount") && 
                (int)d["PlayerCount"] == playerCount)), Times.Once);

        // Check if monitoring file was created
        var monitoringFiles = Directory.GetFiles(_testDataDirectory, "monitoring_*.json");
        Assert.Single(monitoringFiles);
    }

    [Fact]
    public async Task RecordSessionStartAsync_WithDisabledMonitoring_DoesNotLog()
    {
        // Arrange
        _monitor.SetMonitoringEnabled(false);
        const string sessionId = "test_session_123";
        const int playerCount = 3;

        // Act
        await _monitor.RecordSessionStartAsync(sessionId, playerCount);

        // Assert
        _mockLogger.Verify(x => x.LogGameEventAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<IDictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task RecordSessionEndAsync_WithValidData_LogsAndUpdatesMetrics()
    {
        // Arrange
        const string sessionId = "test_session_123";
        var duration = TimeSpan.FromMinutes(30);
        const int roundsPlayed = 15;
        var sessionData = new Dictionary<string, object> { ["Winner"] = "Player1" };

        // Start session first
        await _monitor.RecordSessionStartAsync(sessionId, 2);

        // Act
        await _monitor.RecordSessionEndAsync(sessionId, duration, roundsPlayed, sessionData);

        // Assert
        _mockLogger.Verify(x => x.LogGameEventAsync("SessionEnd", null, 
            It.Is<IDictionary<string, object>>(d => 
                d.ContainsKey("Duration") && 
                d.ContainsKey("RoundsPlayed") &&
                (int)d["RoundsPlayed"] == roundsPlayed)), Times.Once);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync("Sessions.Completed", 1, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordPlayerActionAsync_WithValidData_LogsAndUpdatesMetrics()
    {
        // Arrange
        const string sessionId = "test_session_123";
        const string playerName = "TestPlayer";
        const string action = "Hit";
        var actionData = new Dictionary<string, object> { ["HandValue"] = 15 };

        // Start session first
        await _monitor.RecordSessionStartAsync(sessionId, 1);

        // Act
        await _monitor.RecordPlayerActionAsync(sessionId, playerName, action, actionData);

        // Assert
        _mockLogger.Verify(x => x.LogGameEventAsync("PlayerAction", playerName, 
            It.Is<IDictionary<string, object>>(d => 
                d.ContainsKey("PlayerName") && 
                d.ContainsKey("Action") &&
                (string)d["Action"] == action)), Times.Once);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync($"PlayerActions.{action}", 1, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordErrorAsync_WithValidData_LogsAndUpdatesMetrics()
    {
        // Arrange
        const string sessionId = "test_session_123";
        const string errorType = "InvalidBet";
        var exception = new InvalidOperationException("Test exception");
        var context = new Dictionary<string, object> { ["BetAmount"] = 100 };

        // Act
        await _monitor.RecordErrorAsync(sessionId, errorType, exception, context);

        // Assert
        _mockLogger.Verify(x => x.LogErrorAsync($"Monitored Error: {errorType}", exception, sessionId, 
            It.Is<IDictionary<string, object>>(d => 
                d.ContainsKey("ErrorType") && 
                d.ContainsKey("ExceptionType") &&
                (string)d["ErrorType"] == errorType)), Times.Once);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync($"Errors.{errorType}", 1, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordMetricAsync_WithValidData_LogsAndStoresMetric()
    {
        // Arrange
        const string metricName = "TestMetric";
        const double value = 42.5;
        var tags = new Dictionary<string, string> { ["Category"] = "Performance" };

        // Act
        await _monitor.RecordMetricAsync(metricName, value, tags);

        // Assert
        _mockLogger.Verify(x => x.LogPerformanceMetricAsync(metricName, value, "count", 
            It.Is<IDictionary<string, object>>(d => 
                d.ContainsKey("MetricName") && 
                d.ContainsKey("Value") &&
                (string)d["MetricName"] == metricName)), Times.Once);
    }

    [Fact]
    public async Task RecordSystemResourcesAsync_WithValidData_LogsSystemMetrics()
    {
        // Arrange
        const double memoryUsageMB = 256.5;
        const double cpuUsagePercent = 15.2;
        var additionalMetrics = new Dictionary<string, double> { ["DiskUsage"] = 75.0 };

        // Act
        await _monitor.RecordSystemResourcesAsync(memoryUsageMB, cpuUsagePercent, additionalMetrics);

        // Assert
        _mockLogger.Verify(x => x.LogPerformanceMetricAsync("System.MemoryUsageMB", memoryUsageMB, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync("System.CpuUsagePercent", cpuUsagePercent, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync("System.DiskUsage", 75.0, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);

        _mockLogger.Verify(x => x.LogDebugAsync(
            It.Is<string>(s => s.Contains("System Resources") && s.Contains("256.5MB") && s.Contains("15.2%")), 
            "SystemMonitoring", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithNoIssues_ReturnsHealthyStatus()
    {
        // Act
        var health = await _monitor.GetSystemHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, health.Status);
        Assert.True(health.LastChecked <= DateTime.UtcNow);
        Assert.True(health.LastChecked > DateTime.UtcNow.AddMinutes(-1));
        Assert.NotNull(health.Metrics);
        Assert.NotNull(health.Issues);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithRecentErrors_ReturnsWarningStatus()
    {
        // Arrange - Record multiple errors
        for (int i = 0; i < 3; i++)
        {
            await _monitor.RecordErrorAsync("session1", "TestError", new Exception("Test"), null);
        }

        // Act
        var health = await _monitor.GetSystemHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Warning, health.Status);
        Assert.Contains(health.Issues, issue => issue.Contains("Recent errors"));
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_WithValidTimeRange_ReturnsStatistics()
    {
        // Arrange - Record some test data first
        await _monitor.RecordSessionStartAsync("session1", 2);
        await _monitor.RecordSessionEndAsync("session1", TimeSpan.FromMinutes(30), 10);
        await _monitor.RecordPlayerActionAsync("session1", "Player1", "Hit");
        await _monitor.RecordErrorAsync("session1", "TestError", new Exception("Test"));

        // Set time range to include the recorded events
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddMinutes(1); // Add buffer for future time

        // Act
        var stats = await _monitor.GetUsageStatisticsAsync(startTime, endTime);

        // Assert
        Assert.Equal(startTime, stats.StartTime);
        Assert.Equal(endTime, stats.EndTime);
        Assert.Equal(1, stats.SessionCount);
        Assert.Equal(10, stats.TotalRounds);
        Assert.Equal(1, stats.ErrorCount);
        Assert.True(stats.AverageSessionDuration.TotalMinutes > 0);
        Assert.NotNull(stats.AdditionalMetrics);
    }

    [Fact]
    public async Task GetUsageStatisticsAsync_WithNoData_ReturnsEmptyStatistics()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow.AddHours(-1);

        // Act
        var stats = await _monitor.GetUsageStatisticsAsync(startTime, endTime);

        // Assert
        Assert.Equal(0, stats.SessionCount);
        Assert.Equal(0, stats.TotalRounds);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(TimeSpan.Zero, stats.AverageSessionDuration);
    }

    [Fact]
    public async Task RecordSessionStartAsync_WithNullConfiguration_HandlesGracefully()
    {
        // Arrange
        const string sessionId = "test_session";
        const int playerCount = 2;

        // Act & Assert (should not throw)
        await _monitor.RecordSessionStartAsync(sessionId, playerCount, null);

        _mockLogger.Verify(x => x.LogGameEventAsync("SessionStart", null, 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordPlayerActionAsync_WithNullActionData_HandlesGracefully()
    {
        // Arrange
        const string sessionId = "test_session";
        const string playerName = "TestPlayer";
        const string action = "Stand";

        await _monitor.RecordSessionStartAsync(sessionId, 1);

        // Act & Assert (should not throw)
        await _monitor.RecordPlayerActionAsync(sessionId, playerName, action, null);

        _mockLogger.Verify(x => x.LogGameEventAsync("PlayerAction", playerName, 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordErrorAsync_WithNullSessionId_HandlesGracefully()
    {
        // Arrange
        const string errorType = "SystemError";
        var exception = new Exception("Test exception");

        // Act & Assert (should not throw)
        await _monitor.RecordErrorAsync(null, errorType, exception, null);

        _mockLogger.Verify(x => x.LogErrorAsync($"Monitored Error: {errorType}", exception, null, 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public async Task RecordMetricAsync_WithNullTags_HandlesGracefully()
    {
        // Arrange
        const string metricName = "TestMetric";
        const double value = 123.0;

        // Act & Assert (should not throw)
        await _monitor.RecordMetricAsync(metricName, value, null);

        _mockLogger.Verify(x => x.LogPerformanceMetricAsync(metricName, value, "count", 
            It.IsAny<IDictionary<string, object>>()), Times.Once);
    }
}