using GroupProject.Application.Interfaces;
using GroupProject.Application.Services;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using GroupProject.Domain.Interfaces;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application;

/// <summary>
/// Integration tests for GameOrchestrator that test complete game orchestration flows.
/// </summary>
public class GameOrchestratorIntegrationTests
{
    private readonly Mock<IUserInterface> _mockUserInterface;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly GameService _gameService;
    private readonly GameOrchestrator _gameOrchestrator;
    private readonly TestRandomProvider _randomProvider;

    public GameOrchestratorIntegrationTests()
    {
        _mockUserInterface = new Mock<IUserInterface>();
        _mockErrorHandler = new Mock<IErrorHandler>();
        
        _randomProvider = new TestRandomProvider();
        var shoe = new Shoe(1, _randomProvider);
        var gameRules = new GameRules();
        _gameService = new GameService(shoe, gameRules);
        
        _gameOrchestrator = new GameOrchestrator(_gameService, _mockUserInterface.Object, _mockErrorHandler.Object);
    }

    [Fact]
    public async Task RunGameAsync_CompleteGameFlow_ExecutesAllPhases()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.Eight),  // Player second card (18)
            new Card(Suit.Clubs, Rank.Seven)    // Dealer second card (16)
        };

        _randomProvider.SetPredeterminedCards(cards);

        // Setup UI interactions
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .ReturnsAsync(PlayerAction.Stand);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert - Verify all phases were executed
        _mockUserInterface.Verify(ui => ui.GetPlayerCountAsync(), Times.Once);
        _mockUserInterface.Verify(ui => ui.GetPlayerNamesAsync(1), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Starting new game..."), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowGameStateAsync(It.IsAny<GameState>()), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowPlayerHandAsync(It.IsAny<Player>(), It.IsAny<bool>()), Times.AtLeastOnce);
        _mockUserInterface.Verify(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>()), Times.Once);

        // Verify game completed successfully
        Assert.True(_gameService.IsGameComplete);
    }

    [Fact]
    public async Task RunGameAsync_PlayerHitsAndBusts_HandlesCorrectly()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.Nine),   // Player second card (19)
            new Card(Suit.Clubs, Rank.Seven),   // Dealer second card
            new Card(Suit.Hearts, Rank.Five)    // Player hits and busts (24)
        };

        _randomProvider.SetPredeterminedCards(cards);

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .ReturnsAsync(PlayerAction.Hit);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice hits."), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice is busted!"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>()), Times.Once);

        // Verify game completed and player lost
        Assert.True(_gameService.IsGameComplete);
        var results = _gameService.GetGameResults();
        Assert.Equal(GameResult.Lose, results.PlayerResults["Alice"]);
    }

    [Fact]
    public async Task RunGameAsync_PlayerGetsBlackjack_SkipsPlayerTurn()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),    // Player first card
            new Card(Suit.Diamonds, Rank.Nine), // Dealer first card
            new Card(Suit.Spades, Rank.King),   // Player second card (Blackjack!)
            new Card(Suit.Clubs, Rank.Seven)    // Dealer second card
        };

        _randomProvider.SetPredeterminedCards(cards);

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice has blackjack!"), Times.Once);
        _mockUserInterface.Verify(ui => ui.GetPlayerActionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<PlayerAction>>()), Times.Never);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>()), Times.Once);

        // Verify game completed and player won with blackjack
        Assert.True(_gameService.IsGameComplete);
        var results = _gameService.GetGameResults();
        Assert.Equal(GameResult.Blackjack, results.PlayerResults["Alice"]);
    }

    [Fact]
    public async Task RunGameAsync_MultiplePlayersWithDifferentActions_HandlesCorrectly()
    {
        // Arrange
        var cards = new[]
        {
            // Initial deal
            new Card(Suit.Hearts, Rank.Ten),    // Alice first card
            new Card(Suit.Spades, Rank.Nine),   // Bob first card
            new Card(Suit.Diamonds, Rank.Eight), // Dealer first card
            new Card(Suit.Clubs, Rank.Five),    // Alice second card (15)
            new Card(Suit.Hearts, Rank.Two),    // Bob second card (11)
            new Card(Suit.Spades, Rank.Seven),  // Dealer second card (15)
            
            // Additional cards
            new Card(Suit.Diamonds, Rank.Six),  // Alice hits (21)
            new Card(Suit.Clubs, Rank.Nine),    // Bob hits (20)
            new Card(Suit.Hearts, Rank.Eight)   // Dealer hits (23 - busts)
        };

        _randomProvider.SetPredeterminedCards(cards);

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(2);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(2)).ReturnsAsync(new[] { "Alice", "Bob" });
        
        // Alice hits once then automatically ends turn (gets 21)
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .ReturnsAsync(PlayerAction.Hit);
        
        // Bob hits once then stands
        var bobActionSequence = new Queue<PlayerAction>(new[] { PlayerAction.Hit, PlayerAction.Stand });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Bob", It.IsAny<IEnumerable<PlayerAction>>()))
                         .Returns(() => Task.FromResult(bobActionSequence.Dequeue()));

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice hits."), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice has 21!"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Bob hits."), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Bob stands."), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Dealer is busted!"), Times.Once);

        // Verify both players won (dealer busted)
        Assert.True(_gameService.IsGameComplete);
        var results = _gameService.GetGameResults();
        Assert.Equal(GameResult.Win, results.PlayerResults["Alice"]);
        Assert.Equal(GameResult.Win, results.PlayerResults["Bob"]);
    }

    [Fact]
    public async Task RunGameAsync_DealerPlaysCorrectly_ShowsAllDealerActions()
    {
        // Arrange - Dealer needs to hit
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),    // Player cards (20)
            new Card(Suit.Diamonds, Rank.Five), // Dealer first card
            new Card(Suit.Spades, Rank.Ten),
            new Card(Suit.Clubs, Rank.Ten),     // Dealer second card (15)
            new Card(Suit.Hearts, Rank.Three)   // Dealer hits (18)
        };

        _randomProvider.SetPredeterminedCards(cards);

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .ReturnsAsync(PlayerAction.Stand);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert - Verify dealer turn messages
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("\nDealer's turn:"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Dealer's final hand:"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowPlayerHandAsync(It.Is<Player>(p => p.IsDealer), It.IsAny<bool>()), Times.AtLeast(2));

        // Verify game result
        Assert.True(_gameService.IsGameComplete);
        var results = _gameService.GetGameResults();
        Assert.Equal(GameResult.Win, results.PlayerResults["Alice"]); // 20 vs 18
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_PlaysTwoRounds_HandlesCorrectly()
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

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .ReturnsAsync(PlayerAction.Stand);

        // Setup for multiple rounds
        var shouldPlayAnotherSequence = new Queue<bool>(new[] { true, false }); // Play 2 rounds
        _mockUserInterface.Setup(ui => ui.ShouldPlayAnotherRoundAsync())
                         .Returns(() => Task.FromResult(shouldPlayAnotherSequence.Dequeue()));

        // Act
        _randomProvider.SetPredeterminedCards(firstGameCards.Concat(secondGameCards).ToArray());
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowWelcomeMessageAsync(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShouldPlayAnotherRoundAsync(), Times.Exactly(2));
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Thanks for playing!"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunGameAsync_WithInvalidPlayerAction_HandlesGracefully()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ten),
            new Card(Suit.Diamonds, Rank.Nine),
            new Card(Suit.Spades, Rank.Eight),
            new Card(Suit.Clubs, Rank.Seven)
        };

        _randomProvider.SetPredeterminedCards(cards);

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(new[] { "Alice" });

        // First call returns an invalid action (for a busted hand), second call returns valid action
        var actionSequence = new Queue<PlayerAction>(new[] { PlayerAction.Hit, PlayerAction.Stand });
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
                         .Returns(() => Task.FromResult(actionSequence.Dequeue()));

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert - Should complete successfully despite the invalid action attempt
        Assert.True(_gameService.IsGameComplete);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>()), Times.Once);
    }

    [Fact]
    public async Task RunGameAsync_WithException_HandlesErrorGracefully()
    {
        // Arrange
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));
        _mockErrorHandler.Setup(eh => eh.IsRecoverableError(It.IsAny<Exception>())).Returns(true);
        _mockErrorHandler.Setup(eh => eh.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                         .ReturnsAsync("User-friendly error message");

        // Act & Assert - Should not throw
        await _gameOrchestrator.RunGameAsync();

        // Verify error handling was called
        _mockErrorHandler.Verify(eh => eh.HandleExceptionAsync(It.IsAny<InvalidOperationException>(), "RunGameAsync"), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowErrorMessageAsync("User-friendly error message"), Times.Once);
    }

    [Fact]
    public async Task GetPlayerCountAsync_DelegatesToUserInterface()
    {
        // Arrange
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(3);

        // Act
        var result = await _gameOrchestrator.GetPlayerCountAsync();

        // Assert
        Assert.Equal(3, result);
        _mockUserInterface.Verify(ui => ui.GetPlayerCountAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPlayerNamesAsync_DelegatesToUserInterface()
    {
        // Arrange
        var expectedNames = new[] { "Alice", "Bob", "Charlie" };
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(3)).ReturnsAsync(expectedNames);

        // Act
        var result = await _gameOrchestrator.GetPlayerNamesAsync(3);

        // Assert
        Assert.Equal(expectedNames, result);
        _mockUserInterface.Verify(ui => ui.GetPlayerNamesAsync(3), Times.Once);
    }

    [Fact]
    public async Task ShouldPlayAnotherRoundAsync_DelegatesToUserInterface()
    {
        // Arrange
        _mockUserInterface.Setup(ui => ui.ShouldPlayAnotherRoundAsync()).ReturnsAsync(true);

        // Act
        var result = await _gameOrchestrator.ShouldPlayAnotherRoundAsync();

        // Assert
        Assert.True(result);
        _mockUserInterface.Verify(ui => ui.ShouldPlayAnotherRoundAsync(), Times.Once);
    }

    /// <summary>
    /// Test helper class that provides deterministic card sequences for testing.
    /// </summary>
    private class TestRandomProvider : IRandomProvider
    {
        private Card[]? _predeterminedCards;
        private int _cardIndex = 0;

        public void SetPredeterminedCards(Card[] cards)
        {
            _predeterminedCards = cards;
            _cardIndex = 0;
        }

        public int Next(int minValue, int maxValue)
        {
            return minValue;
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (_predeterminedCards != null && typeof(T) == typeof(Card))
            {
                // Clear the list and add our predetermined cards
                list.Clear();
                foreach (var card in _predeterminedCards)
                {
                    list.Add((T)(object)card);
                }
            }
        }
    }
}