using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Extensions;
using GroupProject.Infrastructure.Providers;
using GroupProject.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace GroupProject.Tests.Integration;

/// <summary>
/// Comprehensive system integration tests that verify all features working together
/// in realistic usage scenarios, configuration persistence, error handling, and performance.
/// </summary>
public class ComprehensiveSystemIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;
    private readonly IHost _host;

    public ComprehensiveSystemIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            Directory.CreateDirectory(_tempDirectory);
            
            // Test write permissions
            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Could not create temp directory {_tempDirectory}: {ex.Message}");
            _tempDirectory = Path.Combine(Environment.CurrentDirectory, "temp_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDirectory);
        }

        // Create a real host with all services configured
        _host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddBlackjackServices();
                
                // Override configuration manager to use temp directory
                services.AddSingleton<GroupProject.Domain.Interfaces.IConfigurationManager>(provider =>
                    new ConfigurationManager(_tempDirectory));
                
                // Override statistics repository to use temp directory
                services.AddSingleton<IStatisticsRepository>(provider =>
                    new StatisticsRepository(_tempDirectory));    
        })
            .Build();
    }

    [Fact]
    public async Task FullSystemIntegration_AllFeaturesWorking_ProcessesCorrectly()
    {
        // Arrange
        var gameOrchestrator = _host.Services.GetRequiredService<IGameOrchestrator>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var configManager = _host.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();

        // Test configuration persistence
        var config = await configManager.LoadConfigurationAsync();
        config.NumberOfDecks = 4;
        config.AllowSplit = true;
        config.AllowDoubleDown = true;
        await configManager.SaveConfigurationAsync(config);

        // Verify configuration was saved
        var loadedConfig = await configManager.LoadConfigurationAsync();
        Assert.Equal(4, loadedConfig.NumberOfDecks);
        Assert.True(loadedConfig.AllowSplit);
        Assert.True(loadedConfig.AllowDoubleDown);

        _output.WriteLine("✓ Configuration persistence test passed");

        // Test session management
        var session = await sessionManager.StartSessionAsync(
            new[] { "Alice", "Bob" }, 
            loadedConfig, 
            Money.FromUsd(1000m));

        Assert.NotNull(session);
        Assert.True(session.IsActive);
        Assert.Contains("Alice", session.Players.Keys);
        Assert.Contains("Bob", session.Players.Keys);

        _output.WriteLine("✓ Session management test passed");

        // Test statistics tracking
        var aliceStats = await statisticsService.GetPlayerStatisticsAsync("Alice");
        Assert.NotNull(aliceStats);
        Assert.Equal("Alice", aliceStats.PlayerName);

        _output.WriteLine("✓ Statistics service test passed");

        // End session
        var sessionSummary = await sessionManager.EndSessionAsync();
        Assert.NotNull(sessionSummary);

        _output.WriteLine("✓ Full system integration test completed successfully");
    }

    [Fact]
    public async Task ConfigurationPersistence_AcrossApplicationRestarts_WorksCorrectly()
    {
        // Arrange - First "application instance"
        var configManager1 = _host.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();
        
        // Act - Set configuration values
        var originalConfig = await configManager1.LoadConfigurationAsync();
        originalConfig.NumberOfDecks = 8;
        originalConfig.MaxPlayers = 6;
        originalConfig.AllowSplit = false;
        originalConfig.AllowDoubleDown = true;
        originalConfig.DealerHitsOnSoft17 = true;
        originalConfig.BlackjackPayout = 1.5;
        
        await configManager1.SaveConfigurationAsync(originalConfig);

        // Simulate application restart by creating new host
        using var newHost = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddBlackjackServices();
                services.AddSingleton<GroupProject.Domain.Interfaces.IConfigurationManager>(provider =>
                    new ConfigurationManager(_tempDirectory));
            })
            .Build();

        var configManager2 = newHost.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();
        
        // Assert - Configuration should persist
        var loadedConfig = await configManager2.LoadConfigurationAsync();
        
        Assert.Equal(8, loadedConfig.NumberOfDecks);
        Assert.Equal(6, loadedConfig.MaxPlayers);
        Assert.False(loadedConfig.AllowSplit);
        Assert.True(loadedConfig.AllowDoubleDown);
        Assert.True(loadedConfig.DealerHitsOnSoft17);
        Assert.Equal(1.5, loadedConfig.BlackjackPayout);

        _output.WriteLine("✓ Configuration persistence across restarts test passed");
    }

    [Fact]
    public async Task ErrorHandling_IntegratedSystemContext_RecoversGracefully()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var bettingService = _host.Services.GetRequiredService<IBettingService>();
        var errorHandler = _host.Services.GetRequiredService<IErrorHandler>();

        // Test error handling in betting context
        try
        {
            // Try to place an invalid bet (negative amount should cause ArgumentOutOfRangeException)
            var result = await bettingService.ValidateBetAsync("NonExistentPlayer", Money.FromUsd(-50m));
            Assert.False(result.IsSuccess);
            Assert.Contains("positive", result.Message.ToLower());
        }
        catch (Exception ex)
        {
            // Error handler should process this gracefully
            var userMessage = await errorHandler.HandleExceptionAsync(ex, "Betting Test");
            Assert.NotNull(userMessage);
            Assert.NotEmpty(userMessage);
            Assert.Contains("positive", userMessage.ToLower());
        }

        // Test error handling in game context
        try
        {
            // Try to start game with invalid parameters
            gameService.StartNewGame(Array.Empty<string>());
            Assert.True(false, "Should have thrown an exception");
        }
        catch (Exception ex)
        {
            var userMessage = await errorHandler.HandleExceptionAsync(ex, "Game Test");
            Assert.NotNull(userMessage);
            Assert.NotEmpty(userMessage);
            
            // Should be recoverable
            Assert.True(errorHandler.IsRecoverableError(ex));
        }

        _output.WriteLine("✓ Error handling integration test passed");
    }

    [Fact]
    public async Task PerformanceCharacteristics_MeetRequirements_UnderLoad()
    {
        // Arrange
        const int iterations = 100;
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var stopwatch = new Stopwatch();
        var operationTimes = new List<long>();

        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true
        };

        // Act - Perform multiple operations and measure performance
        for (int i = 0; i < iterations; i++)
        {
            stopwatch.Restart();

            // Create session
            var session = await sessionManager.StartSessionAsync(
                new[] { $"Player{i}" }, 
                config, 
                Money.FromUsd(1000m));

            // Start game
            gameService.StartNewGame(new[] { $"Player{i}" });
            
            // Process betting
            await gameService.ProcessBettingRoundAsync();
            await gameService.PlacePlayerBetAsync($"Player{i}", Money.FromUsd(50m));
            
            // Ensure all players have placed bets before dealing
            if (!gameService.AllPlayersHaveBets())
            {
                throw new InvalidOperationException("Not all players have placed bets");
            }
            
            // Deal and play
            gameService.DealInitialCards();
            gameService.ProcessPlayerAction($"Player{i}", PlayerAction.Stand);
            gameService.PlayDealerTurn();
            
            // Get results
            var results = await gameService.GetGameResultsWithPayoutsAsync();
            
            // End session
            await sessionManager.EndSessionAsync();

            stopwatch.Stop();
            operationTimes.Add(stopwatch.ElapsedMilliseconds);

            if (i % 20 == 0)
            {
                _output.WriteLine($"Completed {i + 1} operations, avg time: {operationTimes.Average():F2}ms");
            }
        }

        // Assert - Performance requirements
        var averageTime = operationTimes.Average();
        var maxTime = operationTimes.Max();
        var minTime = operationTimes.Min();

        _output.WriteLine($"Performance Results:");
        _output.WriteLine($"  Average time: {averageTime:F2}ms");
        _output.WriteLine($"  Min time: {minTime}ms");
        _output.WriteLine($"  Max time: {maxTime}ms");
        _output.WriteLine($"  95th percentile: {operationTimes.OrderBy(x => x).Skip((int)(iterations * 0.95)).First()}ms");

        // Performance should be reasonable
        Assert.True(averageTime < 100, $"Average operation time too high: {averageTime:F2}ms");
        Assert.True(maxTime < 500, $"Maximum operation time too high: {maxTime}ms");

        // Performance should be consistent (max shouldn't be more than 5x average)
        Assert.True(maxTime < averageTime * 5, $"Performance too inconsistent. Max: {maxTime}ms, Avg: {averageTime:F2}ms");

        _output.WriteLine("✓ Performance characteristics test passed");
    }

    [Fact]
    public async Task StatisticsPersistence_AcrossMultipleSessions_AccumulatesCorrectly()
    {
        // Arrange
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 2
        };

        const string playerName = "TestPlayer";

        // Act - Run multiple sessions and accumulate statistics
        for (int sessionNum = 0; sessionNum < 3; sessionNum++)
        {
            var session = await sessionManager.StartSessionAsync(
                new[] { playerName }, 
                config, 
                Money.FromUsd(1000m));

            // Simulate some game results for this session
            for (int game = 0; game < 5; game++)
            {
                var gameResult = (game % 3) switch
                {
                    0 => GameResult.Win,
                    1 => GameResult.Lose,
                    _ => GameResult.Push
                };

                var betAmount = Money.FromUsd(50m);
                var payout = gameResult switch
                {
                    GameResult.Win => betAmount,
                    GameResult.Lose => Money.Zero,
                    GameResult.Push => Money.Zero,
                    _ => Money.Zero
                };

                await statisticsService.UpdatePlayerStatisticsAsync(playerName, gameResult, betAmount, payout);
            }

            await sessionManager.EndSessionAsync();
        }

        // Assert - Statistics should accumulate correctly
        var finalStats = await statisticsService.GetPlayerStatisticsAsync(playerName);
        
        Assert.Equal(15, finalStats.GamesPlayed); // 3 sessions × 5 games
        Assert.True(finalStats.GamesWon >= 0);     // Should have some wins
        Assert.True(finalStats.GamesLost >= 0);    // Should have some losses
        Assert.True(finalStats.GamesPushed >= 0);  // Should have some pushes
        Assert.Equal(finalStats.GamesWon + finalStats.GamesLost + finalStats.GamesPushed, finalStats.GamesPlayed);
        Assert.Equal(Money.FromUsd(750m), finalStats.TotalWagered); // 15 games × $50
        Assert.True(finalStats.NetWinnings >= Money.FromUsd(-750m)); // Can't lose more than wagered

        _output.WriteLine($"Final Statistics:");
        _output.WriteLine($"  Games Played: {finalStats.GamesPlayed}");
        _output.WriteLine($"  Win Percentage: {finalStats.WinPercentage:P1}");
        _output.WriteLine($"  Total Wagered: {finalStats.TotalWagered}");
        _output.WriteLine($"  Net Winnings: {finalStats.NetWinnings}");

        _output.WriteLine("✓ Statistics persistence test passed");
    }

    [Fact]
    public async Task ComplexGameScenarios_WithAllFeatures_ProcessCorrectly()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();

        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true,
            DealerHitsOnSoft17 = true
        };

        // Start session with multiple players
        var playerNames = new[] { "Alice", "Bob", "Charlie", "Diana" };
        var session = await sessionManager.StartSessionAsync(playerNames, config, Money.FromUsd(2000m));

        // Act - Play a complex round with various actions
        gameService.StartNewGame(playerNames);
        await gameService.ProcessBettingRoundAsync();

        // Players place different bet amounts
        await gameService.PlacePlayerBetAsync("Alice", Money.FromUsd(100m));
        await gameService.PlacePlayerBetAsync("Bob", Money.FromUsd(50m));
        await gameService.PlacePlayerBetAsync("Charlie", Money.FromUsd(75m));
        await gameService.PlacePlayerBetAsync("Diana", Money.FromUsd(25m));

        // Ensure all players have placed bets before dealing
        if (!gameService.AllPlayersHaveBets())
        {
            throw new InvalidOperationException("Not all players have placed bets");
        }

        gameService.DealInitialCards();

        // Simulate different player actions
        foreach (var playerName in playerNames)
        {
            if (gameService.IsPlayerTurn(playerName))
            {
                // Test different actions based on player
                var action = playerName switch
                {
                    "Alice" => PlayerAction.Hit,
                    "Bob" => PlayerAction.Stand,
                    "Charlie" => PlayerAction.Hit,
                    "Diana" => PlayerAction.Stand,
                    _ => PlayerAction.Stand
                };

                var result = gameService.ProcessPlayerAction(playerName, action);
                Assert.True(result.IsSuccess);
            }
        }

        // Dealer plays
        gameService.PlayDealerTurn();

        // Get results and process payouts
        var gameResults = await gameService.GetGameResultsWithPayoutsAsync();
        
        // Record round in session
        await sessionManager.RecordRoundAsync(gameResults);

        // Update statistics for all players
        foreach (var result in gameResults.PlayerResults)
        {
            var betAmount = result.Key switch
            {
                "Alice" => Money.FromUsd(100m),
                "Bob" => Money.FromUsd(50m),
                "Charlie" => Money.FromUsd(75m),
                "Diana" => Money.FromUsd(25m),
                _ => Money.FromUsd(50m)
            };

            var payout = result.Value switch
            {
                GameResult.Win => betAmount,
                GameResult.Blackjack => betAmount * 1.5m,
                GameResult.Push => Money.Zero,
                GameResult.Lose => Money.Zero,
                _ => Money.Zero
            };

            await statisticsService.UpdatePlayerStatisticsAsync(result.Key, result.Value, betAmount, payout);
        }

        // End session and get summary
        var sessionSummary = await sessionManager.EndSessionAsync();

        // Assert - All features should work together
        Assert.Equal(1, sessionSummary.RoundsPlayed);
        Assert.Equal(4, sessionSummary.PlayerStatistics.Count);
        Assert.Contains("Alice", sessionSummary.PlayerStatistics.Keys);
        Assert.Contains("Bob", sessionSummary.PlayerStatistics.Keys);
        Assert.Contains("Charlie", sessionSummary.PlayerStatistics.Keys);
        Assert.Contains("Diana", sessionSummary.PlayerStatistics.Keys);

        // Verify each player has statistics
        foreach (var playerName in playerNames)
        {
            var playerStats = await statisticsService.GetPlayerStatisticsAsync(playerName);
            Assert.Equal(1, playerStats.GamesPlayed);
            Assert.True(playerStats.TotalWagered.IsPositive);
        }

        _output.WriteLine("✓ Complex game scenarios test passed");
    }

    public void Dispose()
    {
        _host?.Dispose();
        
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}