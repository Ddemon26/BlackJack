using GroupProject.Application.Services;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using GroupProject.Domain.Interfaces;
using Xunit;

namespace GroupProject.Tests.Application;

/// <summary>
/// Integration tests for GameService that test complete game flows with real dependencies.
/// </summary>
public class GameServiceIntegrationTests
{
    private readonly GameService _gameService;
    private readonly Shoe _shoe;
    private readonly GameRules _gameRules;
    private readonly TestRandomProvider _randomProvider;

    public GameServiceIntegrationTests()
    {
        _randomProvider = new TestRandomProvider();
        _shoe = new Shoe(1, _randomProvider); // Single deck for predictable testing
        _gameRules = new GameRules();
        _gameService = new GameService(_shoe, _gameRules);
    }

    [Fact]
    public void CompleteGameFlow_SinglePlayer_PlayerWins()
    {
        // Arrange - Set up a scenario where player gets 20 and dealer gets 19
        // Cards are dealt in order: Player1, Dealer1, Player2, Dealer2
        var cardsInDealOrder = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.Ten),    // Player second card (total: 20)
            new Card(Suit.Clubs, Rank.Ten)      // Dealer second card (total: 19)
        };

        _randomProvider.SetPredeterminedCards(cardsInDealOrder);

        // Act - Complete game flow
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();

        // Player stands with 20
        var standResult = _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        Assert.True(standResult.IsSuccess);

        // Dealer plays (should stand on 19)
        _gameService.PlayDealerTurn();

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Single(gameResults.PlayerResults);
        Assert.Equal(GameResult.Win, gameResults.PlayerResults["Alice"]);
        Assert.Equal(19, gameResults.DealerHand.GetValue());
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_SinglePlayer_PlayerBusts()
    {
        // Arrange - Set up a scenario where player busts
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.Nine),   // Player second card (19)
            new Card(Suit.Clubs, Rank.Ten),     // Dealer second card
            new Card(Suit.Hearts, Rank.Five)    // Player hits and busts (24)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();

        // Player hits and busts
        var hitResult = _gameService.ProcessPlayerAction("Alice", PlayerAction.Hit);
        Assert.True(hitResult.IsSuccess);
        Assert.True(hitResult.IsBusted);

        // Dealer turn should be skipped since player busted
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.Results, gameState!.CurrentPhase);

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Equal(GameResult.Lose, gameResults.PlayerResults["Alice"]);
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_SinglePlayer_PlayerBlackjack()
    {
        // Arrange - Set up blackjack scenario
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.King),   // Player second card (Blackjack!)
            new Card(Suit.Clubs, Rank.Ten)      // Dealer second card (19)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();

        var player = _gameService.GetPlayer("Alice");
        Assert.True(player!.HasBlackjack());

        // Player with blackjack should automatically advance to dealer turn
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.DealerTurn, gameState!.CurrentPhase);

        // Dealer plays
        _gameService.PlayDealerTurn();

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Equal(GameResult.Blackjack, gameResults.PlayerResults["Alice"]);
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_MultiplePlayersWithDifferentOutcomes()
    {
        // Arrange - Complex scenario with multiple players
        var cards = new[]
        {
            // Initial deal (2 cards each)
            new Card(Suit.Hearts, Rank.Ten),    // Alice first card
            new Card(Suit.Spades, Rank.Nine),   // Bob first card
            new Card(Suit.Diamonds, Rank.Seven), // Dealer first card
            new Card(Suit.Clubs, Rank.Five),    // Alice second card (15)
            new Card(Suit.Hearts, Rank.Two),    // Bob second card (11)
            new Card(Suit.Spades, Rank.Ten),    // Dealer second card (17)
            
            // Additional cards for hits
            new Card(Suit.Diamonds, Rank.Six),  // Alice hits (21)
            new Card(Suit.Clubs, Rank.Nine),    // Bob hits (20)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice", "Bob" });
        _gameService.DealInitialCards();

        // Alice's turn - hits and gets 21
        Assert.True(_gameService.IsPlayerTurn("Alice"));
        var aliceHitResult = _gameService.ProcessPlayerAction("Alice", PlayerAction.Hit);
        Assert.True(aliceHitResult.IsSuccess);
        Assert.Equal(21, aliceHitResult.UpdatedHand!.GetValue());

        // Bob's turn - hits and gets 20, then stands
        Assert.True(_gameService.IsPlayerTurn("Bob"));
        var bobHitResult = _gameService.ProcessPlayerAction("Bob", PlayerAction.Hit);
        Assert.True(bobHitResult.IsSuccess);
        Assert.Equal(20, bobHitResult.UpdatedHand!.GetValue());

        var bobStandResult = _gameService.ProcessPlayerAction("Bob", PlayerAction.Stand);
        Assert.True(bobStandResult.IsSuccess);

        // Dealer plays (should stand on 17)
        _gameService.PlayDealerTurn();

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Equal(2, gameResults.PlayerResults.Count);
        Assert.Equal(GameResult.Win, gameResults.PlayerResults["Alice"]); // 21 vs 17
        Assert.Equal(GameResult.Win, gameResults.PlayerResults["Bob"]);   // 20 vs 17
        Assert.Equal(17, gameResults.DealerHand.GetValue());
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_DealerBusts_AllRemainingPlayersWin()
    {
        // Arrange - Scenario where dealer busts
        var cards = new[]
        {
            // Initial deal
            new Card(Suit.Hearts, Rank.Ten),    // Alice first card
            new Card(Suit.Spades, Rank.Nine),   // Bob first card
            new Card(Suit.Diamonds, Rank.Ten),  // Dealer first card
            new Card(Suit.Clubs, Rank.Eight),   // Alice second card (18)
            new Card(Suit.Hearts, Rank.Seven),  // Bob second card (16)
            new Card(Suit.Spades, Rank.Six),    // Dealer second card (16)
            
            // Additional cards
            new Card(Suit.Diamonds, Rank.Four), // Bob hits (20)
            new Card(Suit.Clubs, Rank.Ten)      // Dealer hits and busts (26)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice", "Bob" });
        _gameService.DealInitialCards();

        // Alice stands with 18
        var aliceStandResult = _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        Assert.True(aliceStandResult.IsSuccess);

        // Bob hits to get 20, then stands
        var bobHitResult = _gameService.ProcessPlayerAction("Bob", PlayerAction.Hit);
        Assert.True(bobHitResult.IsSuccess);
        
        var bobStandResult = _gameService.ProcessPlayerAction("Bob", PlayerAction.Stand);
        Assert.True(bobStandResult.IsSuccess);

        // Dealer plays and busts
        _gameService.PlayDealerTurn();
        var dealer = _gameService.GetDealer();
        Assert.True(dealer!.IsBusted());

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert - Both players win because dealer busted
        Assert.Equal(GameResult.Win, gameResults.PlayerResults["Alice"]);
        Assert.Equal(GameResult.Win, gameResults.PlayerResults["Bob"]);
        Assert.True(gameResults.DealerHand.IsBusted());
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_PushScenario()
    {
        // Arrange - Player and dealer both get 20
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player first card
            new Card(Suit.Diamonds, Rank.Ten),  // Dealer first card
            new Card(Suit.Spades, Rank.Ten),    // Player second card (20)
            new Card(Suit.Clubs, Rank.Ten)      // Dealer second card (20)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();

        // Player stands
        var standResult = _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        Assert.True(standResult.IsSuccess);

        // Dealer plays (should stand on 20)
        _gameService.PlayDealerTurn();

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Equal(GameResult.Push, gameResults.PlayerResults["Alice"]);
        Assert.Equal(20, gameResults.DealerHand.GetValue());
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void CompleteGameFlow_BothHaveBlackjack_Push()
    {
        // Arrange - Both player and dealer get blackjack
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),    // Player first card
            new Card(Suit.Diamonds, Rank.Ace),  // Dealer first card
            new Card(Suit.Spades, Rank.King),   // Player second card (Blackjack)
            new Card(Suit.Clubs, Rank.Queen)    // Dealer second card (Blackjack)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();

        var player = _gameService.GetPlayer("Alice");
        var dealer = _gameService.GetDealer();
        
        Assert.True(player!.HasBlackjack());
        Assert.True(dealer!.HasBlackjack());

        // Should skip to results since both have blackjack
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.DealerTurn, gameState!.CurrentPhase);

        _gameService.PlayDealerTurn(); // This should just move to results

        // Get results
        var gameResults = _gameService.GetGameResults();

        // Assert
        Assert.Equal(GameResult.Push, gameResults.PlayerResults["Alice"]);
        Assert.True(gameResults.DealerHand.IsBlackjack());
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public void GameStateTransitions_FollowCorrectSequence()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player cards
            new Card(Suit.Diamonds, Rank.Nine), // Dealer cards
            new Card(Suit.Spades, Rank.Eight),
            new Card(Suit.Clubs, Rank.Seven)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Act & Assert - Test state transitions
        
        // Initial state
        Assert.False(_gameService.IsGameInProgress);
        Assert.Null(_gameService.GetCurrentGameState());

        // Start game
        _gameService.StartNewGame(new[] { "Alice" });
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.InitialDeal, gameState!.CurrentPhase);
        Assert.True(_gameService.IsGameInProgress);

        // Deal initial cards
        _gameService.DealInitialCards();
        gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.PlayerTurns, gameState!.CurrentPhase);
        Assert.Equal("Alice", gameState.CurrentPlayerName);

        // Player action
        _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.DealerTurn, gameState!.CurrentPhase);
        Assert.Null(gameState.CurrentPlayerName);

        // Dealer turn
        _gameService.PlayDealerTurn();
        gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.Results, gameState!.CurrentPhase);

        // Get results
        _gameService.GetGameResults();
        gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.GameOver, gameState!.CurrentPhase);
        Assert.True(_gameService.IsGameComplete);
        Assert.False(_gameService.IsGameInProgress);
    }

    [Fact]
    public void ErrorHandling_InsufficientCards_ThrowsException()
    {
        // Arrange - Create a shoe with very few cards
        var smallShoe = new Shoe(1, _randomProvider);
        var gameService = new GameService(smallShoe, _gameRules);

        // Draw most cards from the shoe
        while (smallShoe.RemainingCards > 3)
        {
            smallShoe.Draw();
        }

        // Act & Assert
        gameService.StartNewGame(new[] { "Alice", "Bob" }); // Need 6 cards for initial deal

        var exception = Assert.Throws<InvalidOperationException>(() => gameService.DealInitialCards());
        Assert.Contains("Not enough cards in the shoe", exception.Message);
    }

    [Fact]
    public void MultipleGames_CanBePlayedSequentially()
    {
        // Arrange
        var firstGameCards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),
            new Card(Suit.Diamonds, Rank.Nine),
            new Card(Suit.Spades, Rank.Eight),
            new Card(Suit.Clubs, Rank.Seven)
        };

        var secondGameCards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Diamonds, Rank.Ten),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Clubs, Rank.Nine)
        };

        // Act - Play first game
        _randomProvider.SetPredeterminedCards(firstGameCards);
        _gameService.StartNewGame(new[] { "Alice" });
        _gameService.DealInitialCards();
        _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        _gameService.PlayDealerTurn();
        var firstResults = _gameService.GetGameResults();

        Assert.True(_gameService.IsGameComplete);
        Assert.Equal(GameResult.Win, firstResults.PlayerResults["Alice"]); // 18 vs 16

        // Act - Play second game
        _randomProvider.SetPredeterminedCards(secondGameCards);
        _gameService.StartNewGame(new[] { "Bob" });
        _gameService.DealInitialCards();

        var player = _gameService.GetPlayer("Bob");
        Assert.True(player!.HasBlackjack());

        _gameService.PlayDealerTurn();
        var secondResults = _gameService.GetGameResults();

        // Assert
        Assert.True(_gameService.IsGameComplete);
        Assert.Equal(GameResult.Blackjack, secondResults.PlayerResults["Bob"]);
    }

    /// <summary>
    /// Test helper class that provides deterministic card sequences for testing.
    /// </summary>
    private class TestRandomProvider : IRandomProvider
    {
        private Card[]? _predeterminedCards;

        public void SetPredeterminedCards(Card[] cards)
        {
            _predeterminedCards = cards;
        }

        public int Next(int minValue, int maxValue)
        {
            return minValue; // Always return minimum for predictable behavior
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (_predeterminedCards != null && typeof(T) == typeof(Card))
            {
                // Clear the list and add our predetermined cards multiple times
                list.Clear();
                
                // Add predetermined cards enough times to ensure we have plenty for any test
                // We need at least 4 cards for initial deal, but dealer might need more
                int repetitions = Math.Max(20, 52 / _predeterminedCards.Length + 1);
                for (int i = 0; i < repetitions; i++)
                {
                    foreach (var card in _predeterminedCards)
                    {
                        list.Add((T)(object)card);
                    }
                }
            }
            // Otherwise do nothing (no shuffling for predictable tests)
        }
    }
}