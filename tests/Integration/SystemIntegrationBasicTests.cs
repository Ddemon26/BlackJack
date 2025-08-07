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
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace GroupProject.Tests.Integration;

/// <summary>
/// Basic system integration tests that verify core functionality without file system dependencies.
/// These tests focus on in-memory operations and service integration.
/// </summary>
public class SystemIntegrationBasicTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly IHost _host;

    public SystemIntegrationBasicTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a host with mocked file system dependencies
        _host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddBlackjackServices();
                
                // Mock configuration manager to avoid file system issues
                var mockConfigManager = new Mock<GroupProject.Domain.Interfaces.IConfigurationManager>();
                mockConfigManager.Setup(cm => cm.LoadConfigurationAsync())
                    .ReturnsAsync(new GameConfiguration
                    {
                        NumberOfDecks = 6,
                        MaxPlayers = 4,
                        AllowDoubleDown = true,
                        AllowSplit = true,
                        DealerHitsOnSoft17 = false,
                        BlackjackPayout = 1.5,
                        PenetrationThreshold = 0.75,
                        CardDisplayFormat = CardDisplayFormat.Symbols
                    });
                
                mockConfigManager.Setup(cm => cm.SaveConfigurationAsync(It.IsAny<GameConfiguration>()))
                    .Returns(Task.CompletedTask);
                
                services.AddSingleton(mockConfigManager.Object);
                
                // Mock statistics repository to avoid file system issues
                var mockStatsRepo = new Mock<IStatisticsRepository>();
                var playerStats = new Dictionary<string, PlayerStatistics>();
                
                mockStatsRepo.Setup(sr => sr.GetPlayerStatisticsAsync(It.IsAny<string>()))
                    .Returns<string>(playerName =>
                    {
                        if (!playerStats.ContainsKey(playerName))
                            playerStats[playerName] = new PlayerStatistics(playerName);
                        return Task.FromResult(playerStats[playerName]);
                    });
                
                mockStatsRepo.Setup(sr => sr.SavePlayerStatisticsAsync(It.IsAny<string>(), It.IsAny<PlayerStatistics>()))
                    .Returns<string, PlayerStatistics>((playerName, stats) =>
                    {
                        playerStats[playerName] = stats;
                        return Task.CompletedTask;
                    });
                
                services.AddSingleton(mockStatsRepo.Object);
            })
            .Build();
    }

    [Fact]
    public async Task BasicGameFlow_SinglePlayer_ProcessesCorrectly()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var bettingService = _host.Services.GetRequiredService<IBettingService>();
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();

        const string playerName = "TestPlayer";

        // Act - Play a complete game
        gameService.StartNewGame(new[] { playerName });
        
        // Process betting
        var bettingResult = await gameService.ProcessBettingRoundAsync();
        Assert.True(bettingResult.IsSuccess);
        
        var betResult = await gameService.PlacePlayerBetAsync(playerName, Money.FromUsd(50m));
        Assert.True(betResult.IsSuccess);
        
        // Verify all players have bets before dealing
        Assert.True(gameService.AllPlayersHaveBets());
        
        // Deal cards
        gameService.DealInitialCards();
        
        // Player stands
        var actionResult = gameService.ProcessPlayerAction(playerName, PlayerAction.Stand);
        Assert.True(actionResult.IsSuccess);
        
        // Dealer plays
        gameService.PlayDealerTurn();
        
        // Get results
        var gameResults = await gameService.GetGameResultsWithPayoutsAsync();
        Assert.NotNull(gameResults);
        Assert.Contains(playerName, gameResults.PlayerResults.Keys);
        
        // Update statistics
        var playerResult = gameResults.PlayerResults[playerName];
        await statisticsService.UpdatePlayerStatisticsAsync(playerName, playerResult, Money.FromUsd(50m), Money.Zero);
        
        // Verify statistics were updated
        var stats = await statisticsService.GetPlayerStatisticsAsync(playerName);
        Assert.Equal(1, stats.GamesPlayed);
        Assert.Equal(Money.FromUsd(50m), stats.TotalWagered);

        _output.WriteLine("✓ Basic game flow test passed");
    }

    [Fact]
    public async Task MultiPlayerGame_AllFeatures_ProcessesCorrectly()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();

        var playerNames = new[] { "Alice", "Bob", "Charlie" };
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true
        };

        // Act - Start session and play game
        var session = await sessionManager.StartSessionAsync(playerNames, config, Money.FromUsd(1000m));
        Assert.NotNull(session);
        Assert.True(session.IsActive);

        // Start game
        gameService.StartNewGame(playerNames);
        
        // Process betting
        await gameService.ProcessBettingRoundAsync();
        
        // All players place bets
        await gameService.PlacePlayerBetAsync("Alice", Money.FromUsd(100m));
        await gameService.PlacePlayerBetAsync("Bob", Money.FromUsd(50m));
        await gameService.PlacePlayerBetAsync("Charlie", Money.FromUsd(75m));
        
        Assert.True(gameService.AllPlayersHaveBets());
        
        // Deal cards
        gameService.DealInitialCards();
        
        // All players stand
        foreach (var playerName in playerNames)
        {
            if (gameService.IsPlayerTurn(playerName))
            {
                gameService.ProcessPlayerAction(playerName, PlayerAction.Stand);
            }
        }
        
        // Dealer plays
        gameService.PlayDealerTurn();
        
        // Get results
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
                _ => Money.FromUsd(50m)
            };
            
            await statisticsService.UpdatePlayerStatisticsAsync(result.Key, result.Value, betAmount, Money.Zero);
        }
        
        // End session
        var sessionSummary = await sessionManager.EndSessionAsync();
        
        // Assert
        Assert.Equal(1, sessionSummary.RoundsPlayed);
        Assert.Equal(3, sessionSummary.PlayerStatistics.Count);
        Assert.Contains("Alice", sessionSummary.PlayerStatistics.Keys);
        Assert.Contains("Bob", sessionSummary.PlayerStatistics.Keys);
        Assert.Contains("Charlie", sessionSummary.PlayerStatistics.Keys);

        _output.WriteLine("✓ Multi-player game test passed");
    }

    [Fact]
    public async Task ServiceIntegration_AllServicesWorking_ProcessesCorrectly()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var bettingService = _host.Services.GetRequiredService<IBettingService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var statisticsService = _host.Services.GetRequiredService<IStatisticsService>();
        var configManager = _host.Services.GetRequiredService<GroupProject.Domain.Interfaces.IConfigurationManager>();

        // Act & Assert - Test all services are properly integrated
        
        // Test configuration service
        var config = await configManager.LoadConfigurationAsync();
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks);
        
        // Test betting service
        Assert.True(bettingService.MinimumBet.IsPositive);
        Assert.True(bettingService.MaximumBet > bettingService.MinimumBet);
        
        // Test statistics service
        var playerStats = await statisticsService.GetPlayerStatisticsAsync("TestPlayer");
        Assert.NotNull(playerStats);
        Assert.Equal("TestPlayer", playerStats.PlayerName);
        
        // Test session manager
        var session = await sessionManager.StartSessionAsync(
            new[] { "TestPlayer" }, 
            config, 
            Money.FromUsd(1000m));
        Assert.NotNull(session);
        
        // Test game service
        gameService.StartNewGame(new[] { "TestPlayer" });
        Assert.True(gameService.IsGameInProgress);
        Assert.False(gameService.IsGameComplete);
        
        var players = gameService.GetPlayers();
        Assert.Single(players);
        Assert.Equal("TestPlayer", players[0].Name);

        _output.WriteLine("✓ Service integration test passed");
    }

    [Fact]
    public async Task ErrorHandling_ServiceLevel_HandlesGracefully()
    {
        // Arrange
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var bettingService = _host.Services.GetRequiredService<IBettingService>();
        var errorHandler = _host.Services.GetRequiredService<IErrorHandler>();

        // Act & Assert - Test error handling
        
        // Test invalid game operations
        Assert.Throws<InvalidOperationException>(() => gameService.DealInitialCards());
        Assert.Throws<ArgumentException>(() => gameService.StartNewGame(Array.Empty<string>()));
        
        // Test invalid betting operations
        var invalidBetResult = await bettingService.ValidateBetAsync("NonExistentPlayer", Money.FromUsd(-50m));
        Assert.False(invalidBetResult.IsSuccess);
        Assert.Contains("positive", invalidBetResult.Message.ToLower());
        
        // Test error handler
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            var userMessage = await errorHandler.HandleExceptionAsync(ex, "Test Context");
            Assert.NotNull(userMessage);
            Assert.NotEmpty(userMessage);
            Assert.True(errorHandler.IsRecoverableError(ex));
        }

        _output.WriteLine("✓ Error handling test passed");
    }

    [Fact]
    public async Task PerformanceBasic_MultipleOperations_CompletesInReasonableTime()
    {
        // Arrange
        const int iterations = 50;
        var gameService = _host.Services.GetRequiredService<IGameService>();
        var sessionManager = _host.Services.GetRequiredService<ISessionManager>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 2
        };

        // Act - Perform multiple game operations
        for (int i = 0; i < iterations; i++)
        {
            var session = await sessionManager.StartSessionAsync(
                new[] { $"Player{i}" }, 
                config, 
                Money.FromUsd(1000m));

            gameService.StartNewGame(new[] { $"Player{i}" });
            await gameService.ProcessBettingRoundAsync();
            await gameService.PlacePlayerBetAsync($"Player{i}", Money.FromUsd(50m));
            
            if (gameService.AllPlayersHaveBets())
            {
                gameService.DealInitialCards();
                gameService.ProcessPlayerAction($"Player{i}", PlayerAction.Stand);
                gameService.PlayDealerTurn();
                var results = await gameService.GetGameResultsWithPayoutsAsync();
            }

            await sessionManager.EndSessionAsync();
        }

        stopwatch.Stop();

        // Assert - Performance should be reasonable
        var averageTime = (double)stopwatch.ElapsedMilliseconds / iterations;
        _output.WriteLine($"Average time per operation: {averageTime:F2}ms");
        
        Assert.True(averageTime < 50, $"Average operation time too high: {averageTime:F2}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Total time too high: {stopwatch.ElapsedMilliseconds}ms");

        _output.WriteLine("✓ Basic performance test passed");
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}