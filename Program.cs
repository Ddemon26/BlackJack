using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Interfaces;
using GroupProject.Infrastructure.Extensions;
using GroupProject.Infrastructure.Services;

namespace GroupProject;

/// <summary>
/// Main program entry point for the GroupProject application.
/// Provides command-line interface for various application features including blackjack game and testing utilities.
/// </summary>
public static class Program 
{
    // Command constants
    private const string BLACKJACK = "--blackjack";
    private const string CONFIG = "--config";
    private const string STATS = "--stats";
    private const string TEST = "--test";
    private const string HELP = "--help";
    private const string VERSION = "--version";
    private const string DIAGNOSTICS = "--diagnostics";
    
    // Sub-command constants
    private const string CONFIG_SHOW = "show";
    private const string CONFIG_SET = "set";
    private const string CONFIG_RESET = "reset";
    private const string STATS_SHOW = "show";
    private const string STATS_EXPORT = "export";
    private const string STATS_RESET = "reset";
    private const string TEST_CONNECTION = "connection";
    private const string TEST_PERFORMANCE = "performance";

    /// <summary>
    /// Main application entry point with comprehensive error handling and dependency injection setup.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code: 0 for success, 1 for failure</returns>
    static async Task<int> Main(string[] args) 
    {
        try
        {
            string[] processedArgs = ProcessArgs(args);
            
            if (processedArgs.Length == 0)
            {
                ShowUsageInformation();
                return 0;
            }

            return processedArgs[0] switch
            {
                BLACKJACK => await RunWithGlobalErrorHandling(() => RunBlackjackGameAsync()),
                CONFIG => await RunWithGlobalErrorHandling(() => HandleConfigurationCommandAsync(processedArgs)),
                STATS => await RunWithGlobalErrorHandling(() => HandleStatisticsCommandAsync(processedArgs)),
                TEST => await RunWithGlobalErrorHandling(() => HandleTestCommandAsync(processedArgs)),
                HELP => await RunWithGlobalErrorHandling(() => ShowDetailedHelpAsync(processedArgs)),
                VERSION => await RunWithGlobalErrorHandling(() => ShowVersionInformationAsync()),
                DIAGNOSTICS => await RunWithGlobalErrorHandling(() => ShowSystemDiagnosticsAsync()),
                _ => await RunWithGlobalErrorHandling(() => ShowUnknownCommandErrorAsync(processedArgs[0]))
            };
        }
        catch (Exception ex)
        {
            // Final fallback error handling
            Console.WriteLine($"[FATAL ERROR] An unrecoverable error occurred: {ex.Message}");
            Console.WriteLine("The application will now exit.");
            return 1;
        }
    }

    /// <summary>
    /// Runs the specified operation with global error handling.
    /// </summary>
    /// <param name="operation">The operation to run.</param>
    /// <returns>Exit code: 0 for success, 1 for failure</returns>
    static async Task<int> RunWithGlobalErrorHandling(Func<Task> operation)
    {
        try
        {
            await operation();
            return 0;
        }
        catch (Exception ex)
        {
            // Try to use the error handler if available
            try
            {
                using var host = CreateBlackjackHost();
                var errorHandler = host.Services.GetService<IErrorHandler>();
                
                if (errorHandler != null)
                {
                    var userMessage = await errorHandler.HandleExceptionAsync(ex, "Application Main");
                    Console.WriteLine($"Error: {userMessage}");
                }
                else
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            catch
            {
                // Fallback if error handler fails
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return 1;
        }
    }

    /// <summary>
    /// Displays basic usage information for available commands.
    /// </summary>
    private static void ShowUsageInformation()
    {
        Console.WriteLine("GroupProject - Blackjack Game Application");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  --blackjack     : Start blackjack game");
        Console.WriteLine("  --config        : Manage game configuration");
        Console.WriteLine("  --stats         : View and manage player statistics");
        Console.WriteLine("  --test          : Run system tests and diagnostics");
        Console.WriteLine("  --help          : Show detailed help information");
        Console.WriteLine("  --version       : Show version information");
        Console.WriteLine("  --diagnostics   : Show system diagnostics");
        Console.WriteLine();
        Console.WriteLine("Example usage:");
        Console.WriteLine("  dotnet run -- --blackjack");
        Console.WriteLine("  dotnet run -- --config show");
        Console.WriteLine("  dotnet run -- --stats show");
        Console.WriteLine("  dotnet run -- --help config");
        Console.WriteLine();
        Console.WriteLine("Use '--help [command]' for detailed information about a specific command.");
    }

    /// <summary>
    /// Runs the blackjack game with full error handling.
    /// </summary>
    static async Task RunBlackjackGameAsync()
    {
        using var host = CreateBlackjackHost();
        var gameOrchestrator = host.Services.GetRequiredService<IGameOrchestrator>();
        
        try
        {
            await gameOrchestrator.RunMultipleRoundsAsync();
        }
        catch (Exception ex)
        {
            var errorHandler = host.Services.GetRequiredService<IErrorHandler>();
            var userMessage = await errorHandler.HandleExceptionAsync(ex, "Blackjack Game");
            Console.WriteLine($"Game Error: {userMessage}");
            
            if (!errorHandler.IsRecoverableError(ex))
            {
                Console.WriteLine("The game cannot continue and will now exit.");
                throw;
            }
        }
    }

    /// <summary>
    /// Handles configuration-related commands.
    /// </summary>
    /// <param name="args">Command arguments</param>
    private static async Task HandleConfigurationCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Configuration command requires a sub-command.");
            Console.WriteLine("Available sub-commands: show, set, reset");
            Console.WriteLine("Use '--help config' for detailed information.");
            return;
        }

        using var host = CreateBlackjackHost();
        var configManager = host.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();

        switch (args[1])
        {
            case CONFIG_SHOW:
                await ShowConfigurationAsync(configManager);
                break;
            case CONFIG_SET:
                await SetConfigurationAsync(configManager, args);
                break;
            case CONFIG_RESET:
                await ResetConfigurationAsync(configManager);
                break;
            default:
                Console.WriteLine($"Unknown configuration sub-command: {args[1]}");
                Console.WriteLine("Available sub-commands: show, set, reset");
                break;
        }
    }

    /// <summary>
    /// Handles statistics-related commands.
    /// </summary>
    /// <param name="args">Command arguments</param>
    private static async Task HandleStatisticsCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Statistics command requires a sub-command.");
            Console.WriteLine("Available sub-commands: show, export, reset");
            Console.WriteLine("Use '--help stats' for detailed information.");
            return;
        }

        using var host = CreateBlackjackHost();
        var statsService = host.Services.GetRequiredService<IStatisticsService>();

        switch (args[1])
        {
            case STATS_SHOW:
                await ShowStatisticsAsync(statsService, args);
                break;
            case STATS_EXPORT:
                await ExportStatisticsAsync(statsService, args);
                break;
            case STATS_RESET:
                await ResetStatisticsAsync(statsService, args);
                break;
            default:
                Console.WriteLine($"Unknown statistics sub-command: {args[1]}");
                Console.WriteLine("Available sub-commands: show, export, reset");
                break;
        }
    }

    /// <summary>
    /// Handles test-related commands.
    /// </summary>
    /// <param name="args">Command arguments</param>
    private static async Task HandleTestCommandAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Test command requires a sub-command.");
            Console.WriteLine("Available sub-commands: connection, performance");
            Console.WriteLine("Use '--help test' for detailed information.");
            return;
        }

        using var host = CreateBlackjackHost();

        switch (args[1])
        {
            case TEST_CONNECTION:
                await TestConnectionAsync(host);
                break;
            case TEST_PERFORMANCE:
                await TestPerformanceAsync(host);
                break;
            default:
                Console.WriteLine($"Unknown test sub-command: {args[1]}");
                Console.WriteLine("Available sub-commands: connection, performance");
                break;
        }
    }

    /// <summary>
    /// Shows detailed help information.
    /// </summary>
    /// <param name="args">Command arguments</param>
    private static async Task ShowDetailedHelpAsync(string[] args)
    {
        if (args.Length < 2)
        {
            ShowUsageInformation();
            return;
        }

        switch (args[1])
        {
            case "blackjack":
                ShowBlackjackHelp();
                break;
            case "config":
                ShowConfigurationHelp();
                break;
            case "stats":
                ShowStatisticsHelp();
                break;
            case "test":
                ShowTestHelp();
                break;
            case "version":
                ShowVersionHelp();
                break;
            case "diagnostics":
                ShowDiagnosticsHelp();
                break;
            default:
                Console.WriteLine($"No detailed help available for command: {args[1]}");
                ShowUsageInformation();
                break;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Shows version information.
    /// </summary>
    private static async Task ShowVersionInformationAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var buildDate = GetBuildDate(assembly);
        
        Console.WriteLine("GroupProject - Blackjack Game Application");
        Console.WriteLine($"Version: {version}");
        Console.WriteLine($"Build Date: {buildDate:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Runtime: {Environment.Version}");
        Console.WriteLine($"Platform: {Environment.OSVersion}");
        Console.WriteLine($"Architecture: {Environment.Is64BitProcess switch { true => "x64", false => "x86" }}");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Shows system diagnostics information.
    /// </summary>
    private static async Task ShowSystemDiagnosticsAsync()
    {
        using var host = CreateBlackjackHost();
        var diagnosticCollector = host.Services.GetRequiredService<DiagnosticCollector>();
        
        Console.WriteLine("System Diagnostics");
        Console.WriteLine("==================");
        
        var diagnosticsInfo = await diagnosticCollector.CollectDiagnosticInfoAsync();
        Console.WriteLine(diagnosticsInfo);
        
        // Test service resolution
        Console.WriteLine();
        Console.WriteLine("Service Resolution Test");
        Console.WriteLine("=======================");
        
        try
        {
            var gameService = host.Services.GetRequiredService<IGameService>();
            var gameOrchestrator = host.Services.GetRequiredService<IGameOrchestrator>();
            var bettingService = host.Services.GetRequiredService<IBettingService>();
            var sessionManager = host.Services.GetRequiredService<ISessionManager>();
            var statisticsService = host.Services.GetRequiredService<IStatisticsService>();
            
            Console.WriteLine("✓ All core services resolved successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Service resolution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows error for unknown commands.
    /// </summary>
    /// <param name="command">The unknown command</param>
    private static async Task ShowUnknownCommandErrorAsync(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine();
        ShowUsageInformation();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a host configured for the blackjack game with full dependency injection setup.
    /// </summary>
    /// <returns>A configured host for the blackjack application.</returns>
    private static IHost CreateBlackjackHost()
    {
        return new HostBuilder()
            .ConfigureServices((_, services) => 
            {
                services.AddBlackjackServices();
            })
            .Build();
    }
    

    
    /// <summary>
    /// Processes command-line arguments by converting them to lowercase for case-insensitive comparison.
    /// </summary>
    /// <param name="args">Raw command-line arguments</param>
    /// <returns>Processed arguments in lowercase</returns>
    private static string[] ProcessArgs(string[] args)
        => args.Select(arg => arg.ToLowerInvariant()).ToArray();

    #region Configuration Operations

    /// <summary>
    /// Shows current configuration settings.
    /// </summary>
    /// <param name="configManager">Configuration manager instance</param>
    private static async Task ShowConfigurationAsync(GroupProject.Domain.Interfaces.IConfigurationManager configManager)
    {
        try
        {
            var config = await configManager.LoadConfigurationAsync();
            
            Console.WriteLine("Current Configuration");
            Console.WriteLine("====================");
            Console.WriteLine($"Number of Decks: {config.NumberOfDecks}");
            Console.WriteLine($"Max Players: {config.MaxPlayers}");
            Console.WriteLine($"Allow Split: {config.AllowSplit}");
            Console.WriteLine($"Allow Double Down: {config.AllowDoubleDown}");
            Console.WriteLine($"Dealer Hits Soft 17: {config.DealerHitsOnSoft17}");
            Console.WriteLine($"Blackjack Payout: {config.BlackjackPayout:F1}:1");
            Console.WriteLine($"Penetration Threshold: {config.PenetrationThreshold:P0}");
            Console.WriteLine($"Card Display Format: {config.CardDisplayFormat}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets configuration values.
    /// </summary>
    /// <param name="configManager">Configuration manager instance</param>
    /// <param name="args">Command arguments</param>
    private static async Task SetConfigurationAsync(GroupProject.Domain.Interfaces.IConfigurationManager configManager, string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: --config set <key> <value>");
            Console.WriteLine("Available keys: decks, players, split, doubledown, dealerhitssoft17, blackjackpayout, shufflepenetration, cardformat");
            return;
        }

        try
        {
            var config = await configManager.LoadConfigurationAsync();
            var key = args[2].ToLowerInvariant();
            var value = args[3];

            switch (key)
            {
                case "decks":
                    if (int.TryParse(value, out var decks) && decks > 0 && decks <= 8)
                    {
                        config.NumberOfDecks = decks;
                        Console.WriteLine($"Number of decks set to: {decks}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid deck count. Must be between 1 and 8.");
                        return;
                    }
                    break;
                case "players":
                    if (int.TryParse(value, out var players) && players > 0 && players <= 7)
                    {
                        config.MaxPlayers = players;
                        Console.WriteLine($"Max players set to: {players}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid player count. Must be between 1 and 7.");
                        return;
                    }
                    break;
                case "split":
                    if (bool.TryParse(value, out var allowSplit))
                    {
                        config.AllowSplit = allowSplit;
                        Console.WriteLine($"Allow split set to: {allowSplit}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid boolean value. Use 'true' or 'false'.");
                        return;
                    }
                    break;
                case "doubledown":
                    if (bool.TryParse(value, out var allowDoubleDown))
                    {
                        config.AllowDoubleDown = allowDoubleDown;
                        Console.WriteLine($"Allow double down set to: {allowDoubleDown}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid boolean value. Use 'true' or 'false'.");
                        return;
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown configuration key: {key}");
                    return;
            }

            await configManager.SaveConfigurationAsync(config);
            Console.WriteLine("Configuration saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    /// <param name="configManager">Configuration manager instance</param>
    private static async Task ResetConfigurationAsync(GroupProject.Domain.Interfaces.IConfigurationManager configManager)
    {
        try
        {
            await configManager.ResetToDefaultsAsync();
            Console.WriteLine("Configuration reset to defaults successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reset configuration: {ex.Message}");
        }
    }

    #endregion

    #region Statistics Operations

    /// <summary>
    /// Shows player statistics.
    /// </summary>
    /// <param name="statsService">Statistics service instance</param>
    /// <param name="args">Command arguments</param>
    private static async Task ShowStatisticsAsync(IStatisticsService statsService, string[] args)
    {
        try
        {
            if (args.Length > 2)
            {
                // Show specific player statistics
                var playerName = args[2];
                var playerStats = await statsService.GetPlayerStatisticsAsync(playerName);
                
                if (playerStats == null)
                {
                    Console.WriteLine($"No statistics found for player: {playerName}");
                    return;
                }

                Console.WriteLine($"Statistics for {playerName}");
                Console.WriteLine("========================");
                Console.WriteLine($"Games Played: {playerStats.GamesPlayed}");
                Console.WriteLine($"Games Won: {playerStats.GamesWon}");
                Console.WriteLine($"Games Lost: {playerStats.GamesLost}");
                Console.WriteLine($"Games Pushed: {playerStats.GamesPushed}");
                Console.WriteLine($"Blackjacks: {playerStats.BlackjacksAchieved}");
                Console.WriteLine($"Win Percentage: {playerStats.WinPercentage:P1}");
                Console.WriteLine($"Total Wagered: {playerStats.TotalWagered}");
                Console.WriteLine($"Net Winnings: {playerStats.NetWinnings}");
                Console.WriteLine($"First Played: {playerStats.FirstPlayed:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"Last Played: {playerStats.LastPlayed:yyyy-MM-dd HH:mm}");
            }
            else
            {
                // Show all player statistics
                var allStats = await statsService.GetAllPlayerStatisticsAsync();
                
                if (!allStats.Any())
                {
                    Console.WriteLine("No player statistics available.");
                    return;
                }

                Console.WriteLine("All Player Statistics");
                Console.WriteLine("====================");
                
                foreach (var stats in allStats.OrderByDescending(s => s.GamesPlayed))
                {
                    Console.WriteLine($"{stats.PlayerName}: {stats.GamesPlayed} games, {stats.WinPercentage:P1} win rate, {stats.NetWinnings} net");
                }
                
                Console.WriteLine();
                Console.WriteLine("Use '--stats show <playername>' for detailed statistics.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports statistics to a file.
    /// </summary>
    /// <param name="statsService">Statistics service instance</param>
    /// <param name="args">Command arguments</param>
    private static async Task ExportStatisticsAsync(IStatisticsService statsService, string[] args)
    {
        try
        {
            var exportPath = args.Length > 2 ? args[2] : "statistics_export.json";
            var filePath = await statsService.ExportStatisticsAsync(exportPath, Domain.ValueObjects.StatisticsExportFormat.Json);
            
            Console.WriteLine($"Statistics exported successfully to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to export statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets player statistics.
    /// </summary>
    /// <param name="statsService">Statistics service instance</param>
    /// <param name="args">Command arguments</param>
    private static async Task ResetStatisticsAsync(IStatisticsService statsService, string[] args)
    {
        try
        {
            if (args.Length > 2)
            {
                // Reset specific player statistics
                var playerName = args[2];
                var success = await statsService.ResetPlayerStatisticsAsync(playerName);
                
                if (success)
                {
                    Console.WriteLine($"Statistics reset successfully for player: {playerName}");
                }
                else
                {
                    Console.WriteLine($"No statistics found for player: {playerName}");
                }
            }
            else
            {
                // Reset all statistics
                Console.Write("Are you sure you want to reset ALL player statistics? (y/N): ");
                var confirmation = Console.ReadLine();
                
                if (confirmation?.ToLowerInvariant() == "y")
                {
                    var resetCount = await statsService.ResetAllPlayerStatisticsAsync();
                    Console.WriteLine($"Statistics reset for {resetCount} players.");
                }
                else
                {
                    Console.WriteLine("Operation cancelled.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reset statistics: {ex.Message}");
        }
    }

    #endregion

    #region Test Operations

    /// <summary>
    /// Tests system connections and dependencies.
    /// </summary>
    /// <param name="host">Application host</param>
    private static async Task TestConnectionAsync(IHost host)
    {
        Console.WriteLine("Testing System Connections");
        Console.WriteLine("==========================");
        
        try
        {
            // Test service resolution
            Console.Write("Testing service resolution... ");
            var gameService = host.Services.GetRequiredService<IGameService>();
            var bettingService = host.Services.GetRequiredService<IBettingService>();
            var sessionManager = host.Services.GetRequiredService<ISessionManager>();
            var statisticsService = host.Services.GetRequiredService<IStatisticsService>();
            Console.WriteLine("✓ PASSED");

            // Test configuration loading
            Console.Write("Testing configuration loading... ");
            var configManager = host.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();
            var config = await configManager.LoadConfigurationAsync();
            Console.WriteLine("✓ PASSED");

            // Test statistics service
            Console.Write("Testing statistics service... ");
            var playerCount = await statisticsService.GetPlayerCountAsync();
            Console.WriteLine("✓ PASSED");

            // Test session manager
            Console.Write("Testing session manager... ");
            var currentSession = await sessionManager.GetCurrentSessionAsync();
            Console.WriteLine("✓ PASSED");

            Console.WriteLine();
            Console.WriteLine("All connection tests passed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ FAILED: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests system performance.
    /// </summary>
    /// <param name="host">Application host</param>
    private static async Task TestPerformanceAsync(IHost host)
    {
        Console.WriteLine("Testing System Performance");
        Console.WriteLine("=========================");
        
        try
        {
            var diagnosticCollector = host.Services.GetRequiredService<DiagnosticCollector>();
            
            // Collect initial diagnostics
            var initialDiagnostics = await diagnosticCollector.CollectDiagnosticInfoAsync();
            Console.WriteLine("Initial System State:");
            Console.WriteLine(initialDiagnostics);
            
            // Perform some operations
            Console.Write("Testing service creation performance... ");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                using var scope = host.Services.CreateScope();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                var bettingService = scope.ServiceProvider.GetRequiredService<IBettingService>();
            }
            
            stopwatch.Stop();
            Console.WriteLine($"✓ COMPLETED in {stopwatch.ElapsedMilliseconds}ms");
            
            // Collect final diagnostics
            var finalDiagnostics = await diagnosticCollector.CollectDiagnosticInfoAsync();
            Console.WriteLine();
            Console.WriteLine("Final System State:");
            Console.WriteLine(finalDiagnostics);
            
            Console.WriteLine();
            Console.WriteLine("Performance test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ FAILED: {ex.Message}");
        }
    }

    #endregion

    #region Help Methods

    /// <summary>
    /// Shows help for blackjack command.
    /// </summary>
    private static void ShowBlackjackHelp()
    {
        Console.WriteLine("Blackjack Game Command");
        Console.WriteLine("=====================");
        Console.WriteLine();
        Console.WriteLine("Usage: --blackjack");
        Console.WriteLine();
        Console.WriteLine("Starts the interactive blackjack game with full betting system,");
        Console.WriteLine("statistics tracking, and session management.");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  • Multi-player support");
        Console.WriteLine("  • Betting system with bankroll management");
        Console.WriteLine("  • Advanced actions (split, double down)");
        Console.WriteLine("  • Statistics tracking");
        Console.WriteLine("  • Session persistence");
        Console.WriteLine("  • Configurable game rules");
    }

    /// <summary>
    /// Shows help for configuration command.
    /// </summary>
    private static void ShowConfigurationHelp()
    {
        Console.WriteLine("Configuration Management");
        Console.WriteLine("=======================");
        Console.WriteLine();
        Console.WriteLine("Usage: --config <sub-command> [options]");
        Console.WriteLine();
        Console.WriteLine("Sub-commands:");
        Console.WriteLine("  show                    : Display current configuration");
        Console.WriteLine("  set <key> <value>      : Set configuration value");
        Console.WriteLine("  reset                  : Reset to default configuration");
        Console.WriteLine();
        Console.WriteLine("Configuration Keys:");
        Console.WriteLine("  decks                  : Number of decks (1-8)");
        Console.WriteLine("  players                : Maximum players (1-7)");
        Console.WriteLine("  split                  : Allow splitting (true/false)");
        Console.WriteLine("  doubledown             : Allow double down (true/false)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  --config show");
        Console.WriteLine("  --config set decks 4");
        Console.WriteLine("  --config set split true");
        Console.WriteLine("  --config reset");
    }

    /// <summary>
    /// Shows help for statistics command.
    /// </summary>
    private static void ShowStatisticsHelp()
    {
        Console.WriteLine("Statistics Management");
        Console.WriteLine("====================");
        Console.WriteLine();
        Console.WriteLine("Usage: --stats <sub-command> [options]");
        Console.WriteLine();
        Console.WriteLine("Sub-commands:");
        Console.WriteLine("  show [player]          : Show statistics (all players or specific)");
        Console.WriteLine("  export [file]          : Export statistics to file");
        Console.WriteLine("  reset [player]         : Reset statistics (all or specific player)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  --stats show");
        Console.WriteLine("  --stats show Alice");
        Console.WriteLine("  --stats export stats.json");
        Console.WriteLine("  --stats reset Alice");
        Console.WriteLine("  --stats reset");
    }

    /// <summary>
    /// Shows help for test command.
    /// </summary>
    private static void ShowTestHelp()
    {
        Console.WriteLine("System Testing");
        Console.WriteLine("=============");
        Console.WriteLine();
        Console.WriteLine("Usage: --test <sub-command>");
        Console.WriteLine();
        Console.WriteLine("Sub-commands:");
        Console.WriteLine("  connection             : Test system connections and dependencies");
        Console.WriteLine("  performance            : Test system performance and memory usage");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  --test connection");
        Console.WriteLine("  --test performance");
    }

    /// <summary>
    /// Shows help for version command.
    /// </summary>
    private static void ShowVersionHelp()
    {
        Console.WriteLine("Version Information");
        Console.WriteLine("==================");
        Console.WriteLine();
        Console.WriteLine("Usage: --version");
        Console.WriteLine();
        Console.WriteLine("Displays version information including:");
        Console.WriteLine("  • Application version");
        Console.WriteLine("  • Build date");
        Console.WriteLine("  • Runtime version");
        Console.WriteLine("  • Platform information");
        Console.WriteLine("  • Architecture");
    }

    /// <summary>
    /// Shows help for diagnostics command.
    /// </summary>
    private static void ShowDiagnosticsHelp()
    {
        Console.WriteLine("System Diagnostics");
        Console.WriteLine("=================");
        Console.WriteLine();
        Console.WriteLine("Usage: --diagnostics");
        Console.WriteLine();
        Console.WriteLine("Displays comprehensive system diagnostics including:");
        Console.WriteLine("  • Memory usage");
        Console.WriteLine("  • Garbage collection statistics");
        Console.WriteLine("  • Thread count");
        Console.WriteLine("  • Working set");
        Console.WriteLine("  • Application uptime");
        Console.WriteLine("  • Service resolution test");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the build date from the assembly.
    /// </summary>
    /// <param name="assembly">Assembly to get build date from</param>
    /// <returns>Build date</returns>
    private static DateTime GetBuildDate(Assembly assembly)
    {
        try
        {
            var attribute = assembly.GetCustomAttribute<System.Reflection.AssemblyMetadataAttribute>();
            if (attribute?.Key == "BuildDate" && DateTime.TryParse(attribute.Value, out var buildDate))
            {
                return buildDate;
            }
        }
        catch
        {
            // Fallback to file creation time
        }
        
        return File.GetCreationTime(assembly.Location);
    }

    #endregion
}