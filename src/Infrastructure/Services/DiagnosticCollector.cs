using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Infrastructure.Services;

/// <summary>
/// Collects diagnostic information for troubleshooting system issues.
/// Provides comprehensive system information, performance metrics, and configuration details.
/// </summary>
public class DiagnosticCollector
{
    private readonly IGameLogger? _logger;
    private readonly IGameMonitor? _monitor;

    /// <summary>
    /// Initializes a new instance of the DiagnosticCollector class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic operations.</param>
    /// <param name="monitor">Optional monitor for system health information.</param>
    public DiagnosticCollector(IGameLogger? logger = null, IGameMonitor? monitor = null)
    {
        _logger = logger;
        _monitor = monitor;
    }

    /// <summary>
    /// Collects comprehensive diagnostic information about the system.
    /// </summary>
    /// <returns>A task that returns diagnostic information as a formatted string.</returns>
    public async Task<string> CollectDiagnosticInfoAsync()
    {
        var diagnosticInfo = new StringBuilder();
        
        try
        {
            diagnosticInfo.AppendLine("=== BLACKJACK GAME DIAGNOSTIC REPORT ===");
            diagnosticInfo.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            diagnosticInfo.AppendLine();

            // System Information
            await AppendSystemInfoAsync(diagnosticInfo);
            
            // Application Information
            await AppendApplicationInfoAsync(diagnosticInfo);
            
            // Performance Metrics
            await AppendPerformanceMetricsAsync(diagnosticInfo);
            
            // Memory Information
            await AppendMemoryInfoAsync(diagnosticInfo);
            
            // System Health (if monitor is available)
            if (_monitor != null)
            {
                await AppendSystemHealthAsync(diagnosticInfo);
            }
            
            // Recent Usage Statistics (if monitor is available)
            if (_monitor != null)
            {
                await AppendRecentUsageAsync(diagnosticInfo);
            }

            // Environment Variables
            await AppendEnvironmentInfoAsync(diagnosticInfo);

            diagnosticInfo.AppendLine("=== END DIAGNOSTIC REPORT ===");
            
            if (_logger != null)
            {
                await _logger.LogInfoAsync("Diagnostic information collected successfully");
            }
        }
        catch (Exception ex)
        {
            diagnosticInfo.AppendLine($"ERROR: Failed to collect complete diagnostic information: {ex.Message}");
            if (_logger != null)
            {
                await _logger.LogErrorAsync("Failed to collect diagnostic information", ex);
            }
        }

        return diagnosticInfo.ToString();
    }

    /// <summary>
    /// Collects diagnostic information and saves it to a file.
    /// </summary>
    /// <param name="filePath">The path where the diagnostic file should be saved.</param>
    /// <returns>A task that returns the path to the saved diagnostic file.</returns>
    public async Task<string> SaveDiagnosticReportAsync(string? filePath = null)
    {
        var diagnosticInfo = await CollectDiagnosticInfoAsync();
        
        if (string.IsNullOrEmpty(filePath))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            filePath = Path.Combine(Path.GetTempPath(), $"blackjack_diagnostic_{timestamp}.txt");
        }

        try
        {
            await File.WriteAllTextAsync(filePath, diagnosticInfo);
            if (_logger != null)
            {
                await _logger.LogInfoAsync($"Diagnostic report saved to: {filePath}");
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                await _logger.LogErrorAsync($"Failed to save diagnostic report to {filePath}", ex);
            }
            throw;
        }

        return filePath;
    }

    /// <summary>
    /// Appends system information to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendSystemInfoAsync(StringBuilder sb)
    {
        sb.AppendLine("=== SYSTEM INFORMATION ===");
        
        try
        {
            sb.AppendLine($"Operating System: {Environment.OSVersion}");
            sb.AppendLine($"Machine Name: {Environment.MachineName}");
            sb.AppendLine($"User Name: {Environment.UserName}");
            sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
            sb.AppendLine($"System Directory: {Environment.SystemDirectory}");
            sb.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
            sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($".NET Version: {Environment.Version}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting system info: {ex.Message}");
        }
        
        sb.AppendLine();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Appends application information to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendApplicationInfoAsync(StringBuilder sb)
    {
        sb.AppendLine("=== APPLICATION INFORMATION ===");
        
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            
            sb.AppendLine($"Application: {assemblyName.Name}");
            sb.AppendLine($"Version: {assemblyName.Version}");
            sb.AppendLine($"Location: {assembly.Location}");
            sb.AppendLine($"Framework: {assembly.ImageRuntimeVersion}");
            
            var process = Process.GetCurrentProcess();
            sb.AppendLine($"Process ID: {process.Id}");
            sb.AppendLine($"Process Name: {process.ProcessName}");
            sb.AppendLine($"Start Time: {process.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Uptime: {DateTime.Now - process.StartTime:dd\\.hh\\:mm\\:ss}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting application info: {ex.Message}");
        }
        
        sb.AppendLine();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Appends performance metrics to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendPerformanceMetricsAsync(StringBuilder sb)
    {
        sb.AppendLine("=== PERFORMANCE METRICS ===");
        
        try
        {
            var process = Process.GetCurrentProcess();
            
            sb.AppendLine($"CPU Time (Total): {process.TotalProcessorTime:hh\\:mm\\:ss\\.fff}");
            sb.AppendLine($"CPU Time (User): {process.UserProcessorTime:hh\\:mm\\:ss\\.fff}");
            sb.AppendLine($"Thread Count: {process.Threads.Count}");
            sb.AppendLine($"Handle Count: {process.HandleCount}");
            
            // GC Information
            sb.AppendLine($"GC Gen 0 Collections: {GC.CollectionCount(0)}");
            sb.AppendLine($"GC Gen 1 Collections: {GC.CollectionCount(1)}");
            sb.AppendLine($"GC Gen 2 Collections: {GC.CollectionCount(2)}");
            sb.AppendLine($"GC Total Memory: {GC.GetTotalMemory(false):N0} bytes");
            
            // Server GC information
            sb.AppendLine($"Server GC: {GCSettings.IsServerGC}");
            sb.AppendLine($"GC Latency Mode: {GCSettings.LatencyMode}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting performance metrics: {ex.Message}");
        }
        
        sb.AppendLine();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Appends memory information to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendMemoryInfoAsync(StringBuilder sb)
    {
        sb.AppendLine("=== MEMORY INFORMATION ===");
        
        try
        {
            var process = Process.GetCurrentProcess();
            
            sb.AppendLine($"Working Set: {process.WorkingSet64:N0} bytes ({process.WorkingSet64 / (1024.0 * 1024.0):F1} MB)");
            sb.AppendLine($"Private Memory: {process.PrivateMemorySize64:N0} bytes ({process.PrivateMemorySize64 / (1024.0 * 1024.0):F1} MB)");
            sb.AppendLine($"Virtual Memory: {process.VirtualMemorySize64:N0} bytes ({process.VirtualMemorySize64 / (1024.0 * 1024.0):F1} MB)");
            sb.AppendLine($"Peak Working Set: {process.PeakWorkingSet64:N0} bytes ({process.PeakWorkingSet64 / (1024.0 * 1024.0):F1} MB)");
            sb.AppendLine($"Peak Virtual Memory: {process.PeakVirtualMemorySize64:N0} bytes ({process.PeakVirtualMemorySize64 / (1024.0 * 1024.0):F1} MB)");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting memory info: {ex.Message}");
        }
        
        sb.AppendLine();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Appends system health information to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendSystemHealthAsync(StringBuilder sb)
    {
        sb.AppendLine("=== SYSTEM HEALTH ===");
        
        try
        {
            if (_monitor != null)
            {
                var health = await _monitor.GetSystemHealthAsync();
                
                sb.AppendLine($"Health Status: {health.Status}");
                sb.AppendLine($"Last Checked: {health.LastChecked:yyyy-MM-dd HH:mm:ss} UTC");
                sb.AppendLine($"Issues Count: {health.Issues.Count}");
                
                if (health.Issues.Count > 0)
                {
                    sb.AppendLine("Issues:");
                    foreach (var issue in health.Issues)
                    {
                        sb.AppendLine($"  - {issue}");
                    }
                }
                
                sb.AppendLine("Current Metrics:");
                foreach (var metric in health.Metrics)
                {
                    sb.AppendLine($"  {metric.Key}: {metric.Value:F2}");
                }
            }
            else
            {
                sb.AppendLine("System health monitoring not available (no monitor configured)");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting system health: {ex.Message}");
        }
        
        sb.AppendLine();
    }

    /// <summary>
    /// Appends recent usage statistics to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendRecentUsageAsync(StringBuilder sb)
    {
        sb.AppendLine("=== RECENT USAGE STATISTICS (Last 24 Hours) ===");
        
        try
        {
            if (_monitor != null)
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-24);
                var stats = await _monitor.GetUsageStatisticsAsync(startTime, endTime);
                
                sb.AppendLine($"Time Period: {stats.StartTime:yyyy-MM-dd HH:mm} to {stats.EndTime:yyyy-MM-dd HH:mm} UTC");
                sb.AppendLine($"Sessions: {stats.SessionCount}");
                sb.AppendLine($"Total Rounds: {stats.TotalRounds}");
                sb.AppendLine($"Average Session Duration: {stats.AverageSessionDuration:hh\\:mm\\:ss}");
                sb.AppendLine($"Errors: {stats.ErrorCount}");
                
                sb.AppendLine("Additional Metrics:");
                foreach (var metric in stats.AdditionalMetrics)
                {
                    sb.AppendLine($"  {metric.Key}: {metric.Value:F2}");
                }
            }
            else
            {
                sb.AppendLine("Usage statistics not available (no monitor configured)");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting usage statistics: {ex.Message}");
        }
        
        sb.AppendLine();
    }

    /// <summary>
    /// Appends environment information to the diagnostic report.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendEnvironmentInfoAsync(StringBuilder sb)
    {
        sb.AppendLine("=== ENVIRONMENT VARIABLES ===");
        
        try
        {
            var relevantVars = new[]
            {
                "PATH", "TEMP", "TMP", "USERPROFILE", "COMPUTERNAME",
                "DOTNET_ROOT", "DOTNET_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT"
            };
            
            foreach (var varName in relevantVars)
            {
                var value = Environment.GetEnvironmentVariable(varName);
                if (!string.IsNullOrEmpty(value))
                {
                    // Truncate very long values (like PATH)
                    var displayValue = value.Length > 200 ? value.Substring(0, 200) + "..." : value;
                    sb.AppendLine($"{varName}: {displayValue}");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR collecting environment info: {ex.Message}");
        }
        
        sb.AppendLine();
        await Task.CompletedTask;
    }
}