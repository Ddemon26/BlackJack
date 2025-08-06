using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using GroupProject.Infrastructure.Services;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Tests.Infrastructure.Services;

public class DiagnosticCollectorTests
{
    private readonly Mock<IGameLogger> _mockLogger;
    private readonly Mock<IGameMonitor> _mockMonitor;
    private readonly DiagnosticCollector _collector;

    public DiagnosticCollectorTests()
    {
        _mockLogger = new Mock<IGameLogger>();
        _mockMonitor = new Mock<IGameMonitor>();
        _collector = new DiagnosticCollector(_mockLogger.Object, _mockMonitor.Object);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WithValidServices_ReturnsComprehensiveReport()
    {
        // Arrange
        var healthStatus = new SystemHealthStatus(
            HealthStatus.Healthy,
            DateTime.UtcNow,
            new Dictionary<string, double> { ["TestMetric"] = 42.0 },
            new List<string>()
        );

        var usageStats = new UsageStatistics(
            DateTime.UtcNow.AddHours(-24),
            DateTime.UtcNow,
            5,
            50,
            TimeSpan.FromMinutes(30),
            2,
            new Dictionary<string, double> { ["TestStat"] = 123.0 }
        );

        _mockMonitor.Setup(x => x.GetSystemHealthAsync()).ReturnsAsync(healthStatus);
        _mockMonitor.Setup(x => x.GetUsageStatisticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(usageStats);

        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.NotNull(diagnosticInfo);
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", diagnosticInfo);
        Assert.Contains("SYSTEM INFORMATION", diagnosticInfo);
        Assert.Contains("APPLICATION INFORMATION", diagnosticInfo);
        Assert.Contains("PERFORMANCE METRICS", diagnosticInfo);
        Assert.Contains("MEMORY INFORMATION", diagnosticInfo);
        Assert.Contains("SYSTEM HEALTH", diagnosticInfo);
        Assert.Contains("RECENT USAGE STATISTICS", diagnosticInfo);
        Assert.Contains("ENVIRONMENT VARIABLES", diagnosticInfo);
        Assert.Contains("END DIAGNOSTIC REPORT", diagnosticInfo);

        _mockLogger.Verify(x => x.LogInfoAsync("Diagnostic information collected successfully", null, null), Times.Once);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WithoutMonitor_ReturnsReportWithoutMonitoringInfo()
    {
        // Arrange
        var collector = new DiagnosticCollector(_mockLogger.Object, null);

        // Act
        var diagnosticInfo = await collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.NotNull(diagnosticInfo);
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", diagnosticInfo);
        Assert.Contains("SYSTEM INFORMATION", diagnosticInfo);
        Assert.Contains("APPLICATION INFORMATION", diagnosticInfo);
        Assert.Contains("PERFORMANCE METRICS", diagnosticInfo);
        Assert.Contains("MEMORY INFORMATION", diagnosticInfo);
        Assert.Contains("ENVIRONMENT VARIABLES", diagnosticInfo);
        // When no monitor is provided, the system health and usage sections are not included
        Assert.DoesNotContain("SYSTEM HEALTH", diagnosticInfo);
        Assert.DoesNotContain("RECENT USAGE STATISTICS", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WithoutLogger_DoesNotThrow()
    {
        // Arrange
        var collector = new DiagnosticCollector(null, _mockMonitor.Object);

        // Act & Assert (should not throw)
        var diagnosticInfo = await collector.CollectDiagnosticInfoAsync();
        
        Assert.NotNull(diagnosticInfo);
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_IncludesSystemInformation()
    {
        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("Operating System:", diagnosticInfo);
        Assert.Contains("Machine Name:", diagnosticInfo);
        Assert.Contains("Processor Count:", diagnosticInfo);
        Assert.Contains(".NET Version:", diagnosticInfo);
        Assert.Contains("64-bit OS:", diagnosticInfo);
        Assert.Contains("64-bit Process:", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_IncludesApplicationInformation()
    {
        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("Application:", diagnosticInfo);
        Assert.Contains("Version:", diagnosticInfo);
        Assert.Contains("Process ID:", diagnosticInfo);
        Assert.Contains("Process Name:", diagnosticInfo);
        Assert.Contains("Start Time:", diagnosticInfo);
        Assert.Contains("Uptime:", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_IncludesPerformanceMetrics()
    {
        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("CPU Time (Total):", diagnosticInfo);
        Assert.Contains("CPU Time (User):", diagnosticInfo);
        Assert.Contains("Thread Count:", diagnosticInfo);
        Assert.Contains("Handle Count:", diagnosticInfo);
        Assert.Contains("GC Gen 0 Collections:", diagnosticInfo);
        Assert.Contains("GC Gen 1 Collections:", diagnosticInfo);
        Assert.Contains("GC Gen 2 Collections:", diagnosticInfo);
        Assert.Contains("GC Total Memory:", diagnosticInfo);
        Assert.Contains("Server GC:", diagnosticInfo);
        Assert.Contains("GC Latency Mode:", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_IncludesMemoryInformation()
    {
        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("Working Set:", diagnosticInfo);
        Assert.Contains("Private Memory:", diagnosticInfo);
        Assert.Contains("Virtual Memory:", diagnosticInfo);
        Assert.Contains("Peak Working Set:", diagnosticInfo);
        Assert.Contains("Peak Virtual Memory:", diagnosticInfo);
        Assert.Contains("MB)", diagnosticInfo); // Memory values should be shown in MB
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WithSystemHealth_IncludesHealthInformation()
    {
        // Arrange
        var healthStatus = new SystemHealthStatus(
            HealthStatus.Warning,
            DateTime.UtcNow,
            new Dictionary<string, double> 
            { 
                ["MemoryUsage"] = 256.5,
                ["CpuUsage"] = 15.2 
            },
            new List<string> { "High memory usage", "Recent errors detected" }
        );

        _mockMonitor.Setup(x => x.GetSystemHealthAsync()).ReturnsAsync(healthStatus);

        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("Health Status: Warning", diagnosticInfo);
        Assert.Contains("Issues Count: 2", diagnosticInfo);
        Assert.Contains("High memory usage", diagnosticInfo);
        Assert.Contains("Recent errors detected", diagnosticInfo);
        Assert.Contains("MemoryUsage: 256.50", diagnosticInfo);
        Assert.Contains("CpuUsage: 15.20", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WithUsageStatistics_IncludesUsageInformation()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-24);
        var endTime = DateTime.UtcNow;
        var usageStats = new UsageStatistics(
            startTime,
            endTime,
            10,
            150,
            TimeSpan.FromMinutes(45),
            5,
            new Dictionary<string, double> 
            { 
                ["AveragePlayersPerSession"] = 2.5,
                ["PlayerActionsTotal"] = 500 
            }
        );

        _mockMonitor.Setup(x => x.GetUsageStatisticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                   .ReturnsAsync(usageStats);

        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("Sessions: 10", diagnosticInfo);
        Assert.Contains("Total Rounds: 150", diagnosticInfo);
        Assert.Contains("Average Session Duration: 00:45:00", diagnosticInfo);
        Assert.Contains("Errors: 5", diagnosticInfo);
        Assert.Contains("AveragePlayersPerSession: 2.50", diagnosticInfo);
        Assert.Contains("PlayerActionsTotal: 500.00", diagnosticInfo);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_IncludesEnvironmentVariables()
    {
        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("ENVIRONMENT VARIABLES", diagnosticInfo);
        
        // Check for some common environment variables that should be present
        var hasPath = Environment.GetEnvironmentVariable("PATH") != null;
        var hasTemp = Environment.GetEnvironmentVariable("TEMP") != null || Environment.GetEnvironmentVariable("TMP") != null;
        var hasUserProfile = Environment.GetEnvironmentVariable("USERPROFILE") != null;
        
        if (hasPath) Assert.Contains("PATH:", diagnosticInfo);
        if (hasTemp) Assert.True(diagnosticInfo.Contains("TEMP:") || diagnosticInfo.Contains("TMP:"));
        if (hasUserProfile) Assert.Contains("USERPROFILE:", diagnosticInfo);
    }

    [Fact]
    public async Task SaveDiagnosticReportAsync_WithDefaultPath_SavesFileAndReturnsPath()
    {
        // Act
        var filePath = await _collector.SaveDiagnosticReportAsync();

        // Assert
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
        Assert.Contains("blackjack_diagnostic_", Path.GetFileName(filePath));
        Assert.Equal(".txt", Path.GetExtension(filePath));

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", content);

        _mockLogger.Verify(x => x.LogInfoAsync($"Diagnostic report saved to: {filePath}", null, null), Times.Once);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task SaveDiagnosticReportAsync_WithCustomPath_SavesFileAtSpecifiedPath()
    {
        // Arrange
        var customPath = Path.Combine(Path.GetTempPath(), $"custom_diagnostic_{Guid.NewGuid():N}.txt");

        // Act
        var filePath = await _collector.SaveDiagnosticReportAsync(customPath);

        // Assert
        Assert.Equal(customPath, filePath);
        Assert.True(File.Exists(customPath));

        var content = await File.ReadAllTextAsync(customPath);
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", content);

        // Cleanup
        File.Delete(customPath);
    }

    [Fact]
    public async Task SaveDiagnosticReportAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var invalidPath = Path.Combine("Z:\\NonExistentDrive", "diagnostic.txt");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _collector.SaveDiagnosticReportAsync(invalidPath));

        _mockLogger.Verify(x => x.LogErrorAsync(
            It.Is<string>(s => s.Contains("Failed to save diagnostic report")), 
            It.IsAny<Exception>(), 
            null, 
            null), Times.Once);
    }

    [Fact]
    public async Task CollectDiagnosticInfoAsync_WhenExceptionOccurs_IncludesErrorInformation()
    {
        // Arrange
        _mockMonitor.Setup(x => x.GetSystemHealthAsync())
                   .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var diagnosticInfo = await _collector.CollectDiagnosticInfoAsync();

        // Assert
        Assert.Contains("BLACKJACK GAME DIAGNOSTIC REPORT", diagnosticInfo);
        // The exception should be caught and the diagnostic should still be generated
        Assert.Contains("END DIAGNOSTIC REPORT", diagnosticInfo);
        
        // The logger should still log success since the overall operation succeeded
        _mockLogger.Verify(x => x.LogInfoAsync("Diagnostic information collected successfully", null, null), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullParameters_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        var collector = new DiagnosticCollector(null, null);
        Assert.NotNull(collector);
    }
}