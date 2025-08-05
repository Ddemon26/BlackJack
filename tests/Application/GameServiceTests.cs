using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application;

public class GameServiceTests
{
    private readonly Mock<IShoe> _mockShoe;
    private readonly Mock<IGameRules> _mockGameRules;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _mockShoe = new Mock<IShoe>();
        _mockGameRules = new Mock<IGameRules>();
        _gameService = new GameService(_mockShoe.Object, _mockGameRules.Object);
    }

    [Fact]
    public void Constructor_WithNullShoe_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameService(null!, _mockGameRules.Object));
    }

    [Fact]
    public void Constructor_WithNullGameRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameService(_mockShoe.Object, null!));
    }

    [Fact]
    public void IsGameInProgress_InitialState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_gameService.IsGameInProgress);
    }

    [Fact]
    public void IsGameComplete_InitialState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_gameService.IsGameComplete);
    }

    [Fact]
    public void StartNewGame_WithValidPlayerNames_InitializesGame()
    {
        // Arrange
        var playerNames = new[] { "Alice", "Bob" };

        // Act
        _gameService.StartNewGame(playerNames);

        // Assert
        Assert.True(_gameService.IsGameInProgress);
        Assert.False(_gameService.IsGameComplete);
        
        var players = _gameService.GetPlayers();
        Assert.Equal(2, players.Count);
        Assert.Contains(players, p => p.Name == "Alice");
        Assert.Contains(players, p => p.Name == "Bob");
        
        var dealer = _gameService.GetDealer();
        Assert.NotNull(dealer);
        Assert.Equal("Dealer", dealer.Name);
        Assert.True(dealer.IsDealer);
        
        _mockShoe.Verify(s => s.Shuffle(), Times.Once);
    }

    [Fact]
    public void StartNewGame_WithNullPlayerNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _gameService.StartNewGame(null!));
    }

    [Fact]
    public void StartNewGame_WithEmptyPlayerNames_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _gameService.StartNewGame(new string[0]));
        Assert.Contains("At least one player name must be provided", exception.Message);
    }

    [Fact]
    public void StartNewGame_WithNullOrWhitespacePlayerName_ThrowsArgumentException()
    {
        // Arrange
        var playerNames = new[] { "Alice", "", "Bob" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _gameService.StartNewGame(playerNames));
        Assert.Contains("Player names cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void StartNewGame_WithDuplicatePlayerNames_ThrowsArgumentException()
    {
        // Arrange
        var playerNames = new[] { "Alice", "Bob", "alice" }; // Case-insensitive duplicate

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _gameService.StartNewGame(playerNames));
        Assert.Contains("Duplicate player names are not allowed", exception.Message);
    }

    [Fact]
    public void StartNewGame_WhenGameInProgress_ThrowsInvalidOperationException()
    {
        // Arrange
        _gameService.StartNewGame(new[] { "Alice" });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _gameService.StartNewGame(new[] { "Bob" }));
        Assert.Contains("A game is already in progress", exception.Message);
    }

    [Fact]
    public void DealInitialCards_WithValidGame_DealsCardsToAllPlayers()
    {
        // Arrange
        var playerNames = new[] { "Alice", "Bob" };
        _gameService.StartNewGame(playerNames);
        
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Two),
            new Card(Suit.Spades, Rank.Three),
            new Card(Suit.Diamonds, Rank.Four),
            new Card(Suit.Clubs, Rank.Five),
            new Card(Suit.Hearts, Rank.Six),
            new Card(Suit.Spades, Rank.Seven)
        };

        var cardIndex = 0;
        _mockShoe.Setup(s => s.Draw()).Returns(() => cards[cardIndex++]);
        _mockShoe.Setup(s => s.RemainingCards).Returns(52);

        // Act
        _gameService.DealInitialCards();

        // Assert
        var players = _gameService.GetPlayers();
        var dealer = _gameService.GetDealer();

        Assert.Equal(2, players[0].GetCardCount()); // Alice
        Assert.Equal(2, players[1].GetCardCount()); // Bob
        Assert.Equal(2, dealer!.GetCardCount()); // Dealer

        _mockShoe.Verify(s => s.Draw(), Times.Exactly(6)); // 2 cards each for 2 players + dealer
    }

    [Fact]
    public void DealInitialCards_WhenNotInInitialDealPhase_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _gameService.DealInitialCards());
        Assert.Contains("Cannot deal initial cards", exception.Message);
    }

    [Fact]
    public void DealInitialCards_WithInsufficientCards_ThrowsInvalidOperationException()
    {
        // Arrange
        _gameService.StartNewGame(new[] { "Alice", "Bob" });
        _mockShoe.Setup(s => s.RemainingCards).Returns(5); // Need 6 cards

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _gameService.DealInitialCards());
        Assert.Contains("Not enough cards in the shoe", exception.Message);
    }

    [Fact]
    public void ProcessPlayerAction_WithValidHitAction_ReturnsSuccessResult()
    {
        // Arrange
        SetupGameWithDealtCards();
        var newCard = new Card(Suit.Hearts, Rank.Five);
        
        // Reset the mock to clear previous Draw() calls from setup
        _mockShoe.Reset();
        _mockShoe.Setup(s => s.Draw()).Returns(newCard);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Hit, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = _gameService.ProcessPlayerAction("Alice", PlayerAction.Hit);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        _mockShoe.Verify(s => s.Draw(), Times.Once);
    }

    [Fact]
    public void ProcessPlayerAction_WithValidStandAction_ReturnsSuccessAndEndsTurn()
    {
        // Arrange
        SetupGameWithDealtCards();
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Stand, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.ShouldContinueTurn);
    }

    [Fact]
    public void ProcessPlayerAction_WhenNotPlayersTurn_ReturnsFailureResult()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act
        var result = _gameService.ProcessPlayerAction("Bob", PlayerAction.Hit); // Alice's turn

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("It is not Bob's turn", result.ErrorMessage!);
    }

    [Fact]
    public void ProcessPlayerAction_WithInvalidPlayer_ReturnsFailureResult()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act
        var result = _gameService.ProcessPlayerAction("Charlie", PlayerAction.Hit);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Player 'Charlie' not found", result.ErrorMessage!);
    }

    [Fact]
    public void ProcessPlayerAction_WithInvalidAction_ReturnsFailureResult()
    {
        // Arrange
        SetupGameWithDealtCards();
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Hit, It.IsAny<Hand>())).Returns(false);

        // Act
        var result = _gameService.ProcessPlayerAction("Alice", PlayerAction.Hit);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Action 'Hit' is not valid", result.ErrorMessage!);
    }

    [Fact]
    public void PlayDealerTurn_WithValidGame_PlaysAccordingToRules()
    {
        // Arrange
        SetupGameForDealerTurn();
        var dealerCards = new[]
        {
            new Card(Suit.Hearts, Rank.Six), // Dealer needs to hit on 16
            new Card(Suit.Spades, Rank.Five)
        };

        var cardIndex = 0;
        _mockShoe.Setup(s => s.Draw()).Returns(() => dealerCards[cardIndex++]);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        
        // Setup dealer to hit on 16, stand on 17
        _mockGameRules.SetupSequence(r => r.ShouldDealerHit(It.IsAny<int>()))
            .Returns(true)  // Hit on initial value
            .Returns(false); // Stand after hitting

        // Act
        _gameService.PlayDealerTurn();

        // Assert
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.Results, gameState!.CurrentPhase);
    }

    [Fact]
    public void PlayDealerTurn_WhenNotDealerTurnPhase_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _gameService.PlayDealerTurn());
        Assert.Contains("Cannot play dealer turn", exception.Message);
    }

    [Fact]
    public void GetGameResults_WithCompletedGame_ReturnsValidSummary()
    {
        // Arrange
        SetupCompletedGame();
        _mockGameRules.Setup(r => r.DetermineResult(It.IsAny<Hand>(), It.IsAny<Hand>()))
            .Returns(GameResult.Win);

        // Act
        var summary = _gameService.GetGameResults();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(2, summary.PlayerResults.Count);
        Assert.Contains("Alice", summary.PlayerResults.Keys);
        Assert.Contains("Bob", summary.PlayerResults.Keys);
        Assert.NotNull(summary.DealerHand);
        Assert.True(summary.GameEndTime > DateTime.MinValue);
    }

    [Fact]
    public void GetGameResults_WhenGameNotComplete_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _gameService.GetGameResults());
        Assert.Contains("Game results are not available", exception.Message);
    }

    [Fact]
    public void GetPlayer_WithValidName_ReturnsPlayer()
    {
        // Arrange
        _gameService.StartNewGame(new[] { "Alice", "Bob" });

        // Act
        var player = _gameService.GetPlayer("Alice");

        // Assert
        Assert.NotNull(player);
        Assert.Equal("Alice", player.Name);
    }

    [Fact]
    public void GetPlayer_WithInvalidName_ReturnsNull()
    {
        // Arrange
        _gameService.StartNewGame(new[] { "Alice", "Bob" });

        // Act
        var player = _gameService.GetPlayer("Charlie");

        // Assert
        Assert.Null(player);
    }

    [Fact]
    public void IsPlayerTurn_WithCurrentPlayer_ReturnsTrue()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act & Assert
        Assert.True(_gameService.IsPlayerTurn("Alice"));
        Assert.False(_gameService.IsPlayerTurn("Bob"));
    }

    private void SetupGameWithDealtCards()
    {
        _gameService.StartNewGame(new[] { "Alice", "Bob" });
        
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Two),   // Alice card 1
            new Card(Suit.Spades, Rank.Three), // Bob card 1
            new Card(Suit.Diamonds, Rank.Four), // Dealer card 1
            new Card(Suit.Clubs, Rank.Five),   // Alice card 2
            new Card(Suit.Hearts, Rank.Six),   // Bob card 2
            new Card(Suit.Spades, Rank.Seven)  // Dealer card 2
        };

        var cardIndex = 0;
        _mockShoe.Setup(s => s.Draw()).Returns(() => cards[cardIndex++]);
        _mockShoe.Setup(s => s.RemainingCards).Returns(52);

        _gameService.DealInitialCards();
    }

    private void SetupGameForDealerTurn()
    {
        SetupGameWithDealtCards();
        
        // Simulate all players standing
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Stand, It.IsAny<Hand>())).Returns(true);
        _gameService.ProcessPlayerAction("Alice", PlayerAction.Stand);
        _gameService.ProcessPlayerAction("Bob", PlayerAction.Stand);
    }

    private void SetupCompletedGame()
    {
        SetupGameForDealerTurn();
        
        _mockGameRules.Setup(r => r.ShouldDealerHit(It.IsAny<int>())).Returns(false);
        _gameService.PlayDealerTurn();
    }
}