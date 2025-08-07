using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;
using GroupProject.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace GroupProject.Tests.Integration;

/// <summary>
/// Performance and stress tests for the blackjack system focusing on object pooling,
/// caching effectiveness, memory usage, and extended gameplay sessions.
/// </summary>
public class PerformanceAndStressTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _tempDirectory;

    public PerformanceAndStressTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        var services = new ServiceCollection();
        
        // Register services for performance testing
        services.AddSingleton<IRandomProvider, SystemRandomProvider>();
        services.AddSingleton<IShoe>(provider => new Shoe(6, provider.GetRequiredService<IRandomProvider>()));
        services.AddSingleton<IGameRules, GameRules>();
        services.AddSingleton<SplitHandManager>();
        services.AddSingleton<IShoeManager>(provider => new ShoeManager(provider.GetRequiredService<IShoe>()));
        
        // Mock betting service for performance testing
        services.AddSingleton<IBettingService>(provider =>
        {
            var mockBettingService = new Mock<IBettingService>();
            var playerBankrolls = new ConcurrentDictionary<string, Money>();
            
            mockBettingService.Setup(bs => bs.MinimumBet).Returns(Money.FromUsd(5.00m));
            mockBettingService.Setup(bs => bs.MaximumBet).Returns(Money.FromUsd(500.00m));
            mockBettingService.Setup(bs => bs.BlackjackMultiplier).Returns(1.5m);
            
            mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync(It.IsAny<string>()))
                .Returns<string>(playerName => 
                {
                    var bankroll = playerBankrolls.GetOrAdd(playerName, _ => Money.FromUsd(10000m));
                    return Task.FromResult(bankroll);
                });
            
            mockBettingService.Setup(bs => bs.ValidateBetAsync(It.IsAny<string>(), It.IsAny<Money>()))
                .Returns<string, Money>((playerName, amount) =>
                {
                    var bankroll = playerBankrolls.GetOrAdd(playerName, _ => Money.FromUsd(10000m));
                    if (bankroll < amount)
                        return Task.FromResult(BettingResult.Failure("Insufficient funds"));
                    return Task.FromResult(BettingResult.Success("Bet validated"));
                });
            
            mockBettingService.Setup(bs => bs.PlaceBetAsync(It.IsAny<string>(), It.IsAny<Money>()))
                .Returns<string, Money>((playerName, amount) =>
                {
                    playerBankrolls.AddOrUpdate(playerName, 
                        Money.FromUsd(10000m) - amount,
                        (key, current) => current - amount);
                    
                    var bet = new Bet(amount, playerName, BetType.Standard);
                    return Task.FromResult(BettingResult.Success("Bet placed", bet));
                });
            
            mockBettingService.Setup(bs => bs.ProcessPayoutsAsync(It.IsAny<Dictionary<string, GameResult>>()))
                .Returns<Dictionary<string, GameResult>>(results =>
                {
                    var payouts = new Dictionary<string, Money>();
                    foreach (var result in results)
                    {
                        var playerName = result.Key;
                        var gameResult = result.Value;
                        var betAmount = Money.FromUsd(50m);
                        
                        Money payout = gameResult switch
                        {
                            GameResult.Win => betAmount * 2m,
                            GameResult.Blackjack => betAmount + (betAmount * 1.5m),
                            GameResult.Push => betAmount,
                            GameResult.Lose => Money.Zero,
                            _ => Money.Zero
                        };
                        
                        if (payout.IsPositive)
                        {
                            playerBankrolls.AddOrUpdate(playerName, payout, (key, current) => current + payout);
                        }
                        
                        payouts[playerName] = payout - betAmount;
                    }
                    
                    var payoutResults = payouts.Select(kvp => 
                    {
                        var bet = new Bet(Money.FromUsd(50m), kvp.Key, BetType.Standard);
                        return new PayoutResult(bet, results[kvp.Key], kvp.Value, kvp.Value + Money.FromUsd(50m));
                    }).ToList();
                    return Task.FromResult(new PayoutSummary(payoutResults));
                });
            
            return mockBettingService.Object;
        });
        
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<ISessionManager>(provider => new SessionManager(_tempDirectory));
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ObjectPooling_StringBuilderPool_PerformsEfficiently()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = new Stopwatch();
        
        // Test without pooling (baseline)
        stopwatch.Start();
        for (int i = 0; i < iterations; i++)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Player: ");
            sb.Append($"Player{i}");
            sb.Append(" Hand: ");
            sb.Append("♠A ♥K");
            sb.Append(" Value: 21");
            var result = sb.ToString();
        }
        stopwatch.Stop();
        var baselineTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Baseline (no pooling): {baselineTime}ms");

        // Test with pooling
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var sb = StringBuilderPool.Get();
            try
            {
                sb.Append("Player: ");
                sb.Append($"Player{i}");
                sb.Append(" Hand: ");
                sb.Append("♠A ♥K");
                sb.Append(" Value: 21");
                var result = sb.ToString();
            }
            finally
            {
                StringBuilderPool.Return(sb);
            }
        }
        stopwatch.Stop();
        var pooledTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Pooled: {pooledTime}ms");

        // Assert - Pooling should be at least as fast, typically faster
        var improvementRatio = (double)baselineTime / pooledTime;
        _output.WriteLine($"Improvement ratio: {improvementRatio:F2}x");
        
        // Allow for some variance in timing, but pooling should not be significantly slower
        Assert.True(pooledTime <= baselineTime * 1.2, 
            $"Pooled implementation should not be more than 20% slower. Baseline: {baselineTime}ms, Pooled: {pooledTime}ms");
    }

    [Fact]
    public async Task ObjectPooling_ListPool_PerformsEfficiently()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = new Stopwatch();
        
        // Test without pooling (baseline)
        stopwatch.Start();
        for (int i = 0; i < iterations; i++)
        {
            var list = new List<string>();
            list.Add("Player1");
            list.Add("Player2");
            list.Add("Player3");
            list.Add("Player4");
            var count = list.Count;
            list.Clear();
        }
        stopwatch.Stop();
        var baselineTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"List baseline (no pooling): {baselineTime}ms");

        // Test with pooling
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var list = ListPool<string>.Get();
            try
            {
                list.Add("Player1");
                list.Add("Player2");
                list.Add("Player3");
                list.Add("Player4");
                var count = list.Count;
            }
            finally
            {
                ListPool<string>.Return(list);
            }
        }
        stopwatch.Stop();
        var pooledTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"List pooled: {pooledTime}ms");

        // Assert
        var improvementRatio = (double)baselineTime / pooledTime;
        _output.WriteLine($"List improvement ratio: {improvementRatio:F2}x");
        
        Assert.True(pooledTime <= baselineTime * 1.2, 
            $"List pooled implementation should not be more than 20% slower. Baseline: {baselineTime}ms, Pooled: {pooledTime}ms");
    }

    [Fact]
    public async Task HandValueCaching_ImprovesPerfomance()
    {
        // Arrange
        const int iterations = 1000;
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));
        
        var stopwatch = new Stopwatch();

        // Test repeated value calculations (should use caching)
        stopwatch.Start();
        for (int i = 0; i < iterations; i++)
        {
            var value1 = hand.GetValue();
            var value2 = hand.GetValue();
            var value3 = hand.GetValue();
            var value4 = hand.GetValue();
            var value5 = hand.GetValue();
        }
        stopwatch.Stop();
        var cachedTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Cached hand value calculations: {cachedTime}ms for {iterations * 5} calls");

        // Test with hand modifications (should invalidate cache)
        var modificationHand = new Hand();
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            modificationHand.AddCard(new Card(Suit.Hearts, Rank.Two));
            var value = modificationHand.GetValue();
            modificationHand.Clear();
        }
        stopwatch.Stop();
        var uncachedTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"Uncached hand value calculations: {uncachedTime}ms for {iterations} calls");

        // Assert - Cached calls should be significantly faster per call
        var cachedTimePerCall = (double)cachedTime / (iterations * 5);
        var uncachedTimePerCall = (double)uncachedTime / iterations;
        
        _output.WriteLine($"Cached time per call: {cachedTimePerCall:F4}ms");
        _output.WriteLine($"Uncached time per call: {uncachedTimePerCall:F4}ms");
        
        // Cached calls should be at least 2x faster per call
        Assert.True(cachedTimePerCall < uncachedTimePerCall / 2, 
            "Cached hand value calculations should be significantly faster than uncached ones");
    }

    [Fact]
    public async Task ExtendedGameplaySession_MaintainsPerformance()
    {
        // Arrange
        const int numberOfRounds = 1000;
        const int playersPerRound = 4;
        
        var gameService = _serviceProvider.GetRequiredService<IGameService>();
        var sessionManager = _serviceProvider.GetRequiredService<ISessionManager>();
        
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true
        };

        var playerNames = Enumerable.Range(1, playersPerRound)
            .Select(i => $"Player{i}")
            .ToArray();

        // Start session
        var session = await sessionManager.StartSessionAsync(playerNames, config, Money.FromUsd(10000m));
        
        var roundTimes = new List<long>();
        var memoryUsages = new List<long>();
        var stopwatch = new Stopwatch();

        // Act - Play many rounds and measure performance
        for (int round = 0; round < numberOfRounds; round++)
        {
            stopwatch.Restart();
            
            // Measure memory before round
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);

            // Play a complete round
            gameService.StartNewGame(playerNames);
            await gameService.ProcessBettingRoundAsync();
            
            foreach (var playerName in playerNames)
            {
                await gameService.PlacePlayerBetAsync(playerName, Money.FromUsd(50m));
            }
            
            gameService.DealInitialCards();
            
            // All players stand for simplicity
            foreach (var playerName in playerNames)
            {
                if (gameService.IsPlayerTurn(playerName))
                {
                    gameService.ProcessPlayerAction(playerName, PlayerAction.Stand);
                }
            }
            
            gameService.PlayDealerTurn();
            var gameResults = await gameService.GetGameResultsWithPayoutsAsync();
            await sessionManager.RecordRoundAsync(gameResults);
            
            stopwatch.Stop();
            roundTimes.Add(stopwatch.ElapsedMilliseconds);
            
            // Measure memory after round
            var memoryAfter = GC.GetTotalMemory(false);
            memoryUsages.Add(memoryAfter - memoryBefore);
            
            // Log progress every 100 rounds
            if ((round + 1) % 100 == 0)
            {
                var avgTime = roundTimes.Skip(Math.Max(0, roundTimes.Count - 100)).Average();
                var avgMemory = memoryUsages.Skip(Math.Max(0, memoryUsages.Count - 100)).Average();
                _output.WriteLine($"Round {round + 1}: Avg time: {avgTime:F2}ms, Avg memory delta: {avgMemory:F0} bytes");
            }
        }

        // End session
        var sessionSummary = await sessionManager.EndSessionAsync();

        // Assert - Performance should remain consistent
        var firstQuarterAvg = roundTimes.Take(numberOfRounds / 4).Average();
        var lastQuarterAvg = roundTimes.Skip(3 * numberOfRounds / 4).Average();
        
        _output.WriteLine($"First quarter average: {firstQuarterAvg:F2}ms");
        _output.WriteLine($"Last quarter average: {lastQuarterAvg:F2}ms");
        _output.WriteLine($"Performance degradation: {((lastQuarterAvg / firstQuarterAvg - 1) * 100):F1}%");
        
        // Performance should not degrade by more than 50%
        Assert.True(lastQuarterAvg < firstQuarterAvg * 1.5, 
            $"Performance degraded too much. First quarter: {firstQuarterAvg:F2}ms, Last quarter: {lastQuarterAvg:F2}ms");
        
        // Memory usage should not grow excessively
        var firstQuarterMemory = memoryUsages.Take(numberOfRounds / 4).Average();
        var lastQuarterMemory = memoryUsages.Skip(3 * numberOfRounds / 4).Average();
        
        _output.WriteLine($"First quarter memory delta: {firstQuarterMemory:F0} bytes");
        _output.WriteLine($"Last quarter memory delta: {lastQuarterMemory:F0} bytes");
        
        // Memory usage should not increase by more than 100% (indicating potential memory leaks)
        Assert.True(lastQuarterMemory < firstQuarterMemory * 2, 
            $"Memory usage increased too much. First quarter: {firstQuarterMemory:F0} bytes, Last quarter: {lastQuarterMemory:F0} bytes");
        
        // Verify session completed successfully
        Assert.Equal(numberOfRounds, sessionSummary.RoundsPlayed);
        Assert.Equal(playersPerRound, sessionSummary.PlayerStatistics.Count);
    }

    [Fact]
    public async Task ConcurrentGameOperations_ThreadSafety()
    {
        // Arrange
        const int numberOfTasks = 10;
        const int operationsPerTask = 100;
        
        var gameService = _serviceProvider.GetRequiredService<IGameService>();
        var sessionManager = _serviceProvider.GetRequiredService<ISessionManager>();
        
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 8
        };

        // Create multiple sessions concurrently
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        var completedOperations = new ConcurrentBag<int>();

        for (int taskId = 0; taskId < numberOfTasks; taskId++)
        {
            var currentTaskId = taskId;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var playerName = $"Player{currentTaskId}";
                    
                    for (int op = 0; op < operationsPerTask; op++)
                    {
                        // Create a new session for each operation to test concurrent session management
                        var session = await sessionManager.StartSessionAsync(
                            new[] { playerName }, 
                            config, 
                            Money.FromUsd(1000m));
                        
                        // Perform some operations
                        var statistics = new PlayerStatistics(playerName);
                        statistics.RecordGame(GameResult.Win, Money.FromUsd(50m), Money.FromUsd(50m));
                        
                        // End session
                        await sessionManager.EndSessionAsync();
                        
                        completedOperations.Add(1);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Completed {numberOfTasks} concurrent tasks with {operationsPerTask} operations each in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Total operations completed: {completedOperations.Count}");
        _output.WriteLine($"Exceptions encountered: {exceptions.Count}");

        // Log any exceptions for debugging
        foreach (var ex in exceptions)
        {
            _output.WriteLine($"Exception: {ex.Message}");
        }

        // Should complete most operations successfully
        var expectedOperations = numberOfTasks * operationsPerTask;
        var completionRate = (double)completedOperations.Count / expectedOperations;
        
        _output.WriteLine($"Completion rate: {completionRate:P1}");
        
        // Allow for some failures due to concurrency, but should complete at least 90%
        Assert.True(completionRate >= 0.9, 
            $"Completion rate too low: {completionRate:P1}. Expected at least 90%");
        
        // Should not have excessive exceptions
        var exceptionRate = (double)exceptions.Count / expectedOperations;
        Assert.True(exceptionRate <= 0.1, 
            $"Too many exceptions: {exceptionRate:P1}. Should be less than 10%");
    }

    [Fact]
    public async Task MemoryUsage_UnderLoad_RemainsStable()
    {
        // Arrange
        const int iterations = 500;
        var gameService = _serviceProvider.GetRequiredService<IGameService>();
        
        var initialMemory = GC.GetTotalMemory(true); // Force GC and get baseline
        var memoryMeasurements = new List<long>();

        // Act - Perform many game operations
        for (int i = 0; i < iterations; i++)
        {
            // Create and play a game
            gameService.StartNewGame(new[] { "TestPlayer" });
            await gameService.ProcessBettingRoundAsync();
            await gameService.PlacePlayerBetAsync("TestPlayer", Money.FromUsd(50m));
            gameService.DealInitialCards();
            gameService.ProcessPlayerAction("TestPlayer", PlayerAction.Stand);
            gameService.PlayDealerTurn();
            var results = await gameService.GetGameResultsWithPayoutsAsync();
            
            // Measure memory every 50 iterations
            if (i % 50 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var currentMemory = GC.GetTotalMemory(false);
                memoryMeasurements.Add(currentMemory);
                
                _output.WriteLine($"Iteration {i}: Memory usage: {currentMemory:N0} bytes ({currentMemory - initialMemory:+N0;-N0;0} from baseline)");
            }
        }

        // Final memory measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePercentage = (double)memoryIncrease / initialMemory * 100;
        
        _output.WriteLine($"Initial memory: {initialMemory:N0} bytes");
        _output.WriteLine($"Final memory: {finalMemory:N0} bytes");
        _output.WriteLine($"Memory increase: {memoryIncrease:N0} bytes ({memoryIncreasePercentage:F1}%)");
        
        // Memory should not increase by more than 200% (indicating significant memory leaks)
        Assert.True(memoryIncreasePercentage < 200, 
            $"Memory usage increased too much: {memoryIncreasePercentage:F1}%. This may indicate memory leaks.");
        
        // Check for memory stability (no continuous growth)
        if (memoryMeasurements.Count >= 4)
        {
            var firstHalf = memoryMeasurements.Take(memoryMeasurements.Count / 2).Average();
            var secondHalf = memoryMeasurements.Skip(memoryMeasurements.Count / 2).Average();
            var growthRate = (secondHalf - firstHalf) / firstHalf * 100;
            
            _output.WriteLine($"Memory growth rate between first and second half: {growthRate:F1}%");
            
            // Memory should not grow by more than 50% between first and second half
            Assert.True(growthRate < 50, 
                $"Memory appears to be growing continuously: {growthRate:F1}% growth rate");
        }
    }

    [Fact]
    public async Task ShoeReshuffling_PerformanceImpact()
    {
        // Arrange
        const int rounds = 100;
        var gameService = _serviceProvider.GetRequiredService<IGameService>();
        var shoe = _serviceProvider.GetRequiredService<IShoe>();
        var shoeManager = _serviceProvider.GetRequiredService<IShoeManager>();
        
        // Configure for frequent reshuffling
        shoeManager.PenetrationThreshold = 0.8; // Reshuffle when 80% of cards are used
        shoeManager.AutoReshuffleEnabled = true;
        
        var roundTimes = new List<long>();
        var reshuffleCount = 0;
        var stopwatch = new Stopwatch();
        
        // Subscribe to reshuffle events
        shoeManager.ReshuffleOccurred += (sender, e) => reshuffleCount++;

        // Act - Play rounds that will trigger reshuffles
        for (int round = 0; round < rounds; round++)
        {
            stopwatch.Restart();
            
            gameService.StartNewGame(new[] { "Player1", "Player2", "Player3", "Player4" });
            await gameService.ProcessBettingRoundAsync();
            
            foreach (var playerName in new[] { "Player1", "Player2", "Player3", "Player4" })
            {
                await gameService.PlacePlayerBetAsync(playerName, Money.FromUsd(50m));
            }
            
            gameService.DealInitialCards();
            
            // Players take various actions to use more cards
            foreach (var playerName in new[] { "Player1", "Player2", "Player3", "Player4" })
            {
                if (gameService.IsPlayerTurn(playerName))
                {
                    // Randomly hit or stand to use more cards
                    var action = (round + playerName.GetHashCode()) % 3 == 0 ? PlayerAction.Hit : PlayerAction.Stand;
                    gameService.ProcessPlayerAction(playerName, action);
                }
            }
            
            gameService.PlayDealerTurn();
            var results = await gameService.GetGameResultsWithPayoutsAsync();
            
            stopwatch.Stop();
            roundTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        _output.WriteLine($"Completed {rounds} rounds with {reshuffleCount} reshuffles");
        _output.WriteLine($"Average round time: {roundTimes.Average():F2}ms");
        _output.WriteLine($"Min round time: {roundTimes.Min()}ms");
        _output.WriteLine($"Max round time: {roundTimes.Max()}ms");
        
        // Should have triggered some reshuffles
        Assert.True(reshuffleCount > 0, "Expected at least one reshuffle to occur");
        
        // Performance should remain reasonable even with reshuffling
        var averageTime = roundTimes.Average();
        Assert.True(averageTime < 100, $"Average round time too high: {averageTime:F2}ms");
        
        // No single round should take excessively long
        var maxTime = roundTimes.Max();
        Assert.True(maxTime < 500, $"Maximum round time too high: {maxTime}ms");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        
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