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
    private readonly Mock<IBettingService> _mockBettingService;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _mockShoe = new Mock<IShoe>();
        _mockGameRules = new Mock<IGameRules>();
        _mockBettingService = new Mock<IBettingService>();
        
        // Setup default betting service behavior
        _mockBettingService.Setup(bs => bs.MinimumBet).Returns(Money.FromUsd(5.00m));
        _mockBettingService.Setup(bs => bs.MaximumBet).Returns(Money.FromUsd(500.00m));
        _mockBettingService.Setup(bs => bs.BlackjackMultiplier).Returns(1.5m);
        
        _gameService = new GameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
    }

    [Fact]
    public void Constructor_WithNullShoe_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameService(null!, _mockGameRules.Object, _mockBettingService.Object));
    }

    [Fact]
    public void Constructor_WithNullGameRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameService(_mockShoe.Object, null!, _mockBettingService.Object));
    }

    [Fact]
    public void Constructor_WithNullBettingService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameService(_mockShoe.Object, _mockGameRules.Object, null!));
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

    #region Double Down Tests

    [Fact]
    public async Task ProcessDoubleDownAsync_WithValidConditions_ReturnsSuccessAndEndsPlayerTurn()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Hearts, Rank.Five));
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.ShouldContinueTurn);
        Assert.True(result.IsDoubleDown);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(3, result.UpdatedHand.CardCount); // Original 2 cards + 1 double down card
        
        // Verify bet was doubled
        Assert.Equal(new Money(20m), player.CurrentBet!.Amount);
        Assert.Equal(BetType.DoubleDown, player.CurrentBet.Type);
        
        // Verify bankroll was reduced by additional bet amount
        Assert.Equal(new Money(80m), player.Bankroll);
        
        _mockShoe.Verify(s => s.Draw(), Times.Once);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithInsufficientFunds_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with insufficient bankroll
        player.SetBankroll(new Money(15m));
        player.PlaceBet(new Money(10m));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot double down", result.ErrorMessage!);
        
        // Verify original bet remains unchanged
        Assert.Equal(new Money(10m), player.CurrentBet!.Amount);
        Assert.Equal(BetType.Standard, player.CurrentBet.Type);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithMoreThanTwoCards_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Add a third card to make double down invalid
        player.AddCard(new Card(Suit.Clubs, Rank.Three));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(false);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot double down", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithBlackjack_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add blackjack cards
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(false);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot double down", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithNoBet_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll but no bet
        player.SetBankroll(new Money(100m));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Player must have an active bet to double down", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithEmptyShoe_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        _mockShoe.Setup(s => s.IsEmpty).Returns(true);
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No more cards available in the shoe", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessDoubleDownAsync_WithInvalidPlayerName_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act
        var result = await _gameService.ProcessDoubleDownAsync("NonExistentPlayer");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Player 'NonExistentPlayer' not found", result.ErrorMessage!);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithValidConditions_ReturnsTrue()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));

        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync("Alice");

        // Assert
        Assert.True(canDoubleDown);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with insufficient bankroll
        player.SetBankroll(new Money(5m));
        player.PlaceBet(new Money(10m));

        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync("Alice");

        // Assert
        Assert.False(canDoubleDown);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithInvalidPlayerName_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync("NonExistentPlayer");

        // Assert
        Assert.False(canDoubleDown);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithNullPlayerName_ReturnsFalse()
    {
        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync(null!);

        // Assert
        Assert.False(canDoubleDown);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithMoreThanTwoCards_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Add a third card
        player.AddCard(new Card(Suit.Clubs, Rank.Three));

        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync("Alice");

        // Assert
        Assert.False(canDoubleDown);
    }

    [Fact]
    public async Task CanPlayerDoubleDownAsync_WithBlackjack_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add blackjack cards
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var canDoubleDown = await _gameService.CanPlayerDoubleDownAsync("Alice");

        // Assert
        Assert.False(canDoubleDown);
    }

    #endregion

    #region Split Tests

    [Fact]
    public async Task ProcessSplitAsync_WithValidPair_ReturnsSuccessAndSplitsHand()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Eight));
        
        _mockShoe.Setup(s => s.RemainingCards).Returns(10);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Clubs, Rank.Five));
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(true);
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessSplitAsync("Alice");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsSplit);
        Assert.True(result.ShouldContinueTurn); // Non-Ace splits can continue
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(2, result.UpdatedHand.CardCount); // Original card + new card
        Assert.True(result.UpdatedHand.IsSplitHand);
        
        // Verify bet was matched for split
        Assert.Equal(new Money(10m), player.CurrentBet!.Amount);
        Assert.Equal(BetType.Split, player.CurrentBet.Type);
        
        // Verify bankroll was reduced by additional bet amount
        Assert.Equal(new Money(80m), player.Bankroll);
        
        _mockShoe.Verify(s => s.Draw(), Times.Once);
    }

    [Fact]
    public async Task ProcessSplitAsync_WithAces_ReturnsSuccessAndEndsPlayerTurn()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair of Aces
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.Ace));
        
        _mockShoe.Setup(s => s.RemainingCards).Returns(10);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Clubs, Rank.Five));
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(true);
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessSplitAsync("Alice");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsSplit);
        Assert.False(result.ShouldContinueTurn); // Ace splits end the turn
        Assert.NotNull(result.UpdatedHand);
        Assert.True(result.UpdatedHand.IsSplitHand);
        Assert.True(result.UpdatedHand.IsComplete);
        
        _mockShoe.Verify(s => s.Draw(), Times.Once);
    }

    [Fact]
    public async Task ProcessSplitAsync_WithInsufficientFunds_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with insufficient bankroll
        player.SetBankroll(new Money(5m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Eight));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(true);
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessSplitAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot split", result.ErrorMessage!);
        
        // Verify original bet remains unchanged
        Assert.Equal(new Money(10m), player.CurrentBet!.Amount);
        Assert.Equal(BetType.Standard, player.CurrentBet.Type);
    }

    [Fact]
    public async Task ProcessSplitAsync_WithNonPair_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add non-matching cards
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Nine));
        
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(false);
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(false);

        // Act
        var result = await _gameService.ProcessSplitAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot split", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessSplitAsync_WithInsufficientCards_ReturnsFailure()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Eight));
        
        _mockShoe.Setup(s => s.RemainingCards).Returns(1); // Not enough cards
        _mockGameRules.Setup(r => r.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(true);
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var result = await _gameService.ProcessSplitAsync("Alice");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Not enough cards available", result.ErrorMessage!);
    }

    [Fact]
    public async Task CanPlayerSplitAsync_WithValidPair_ReturnsTrue()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with bankroll and bet
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Eight));
        
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var canSplit = await _gameService.CanPlayerSplitAsync("Alice");

        // Assert
        Assert.True(canSplit);
    }

    [Fact]
    public async Task CanPlayerSplitAsync_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();
        var player = _gameService.GetPlayer("Alice")!;
        
        // Set up player with insufficient bankroll
        player.SetBankroll(new Money(5m));
        player.PlaceBet(new Money(10m));
        
        // Clear hand and add a pair
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Eight));
        player.AddCard(new Card(Suit.Spades, Rank.Eight));
        
        _mockGameRules.Setup(r => r.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        var canSplit = await _gameService.CanPlayerSplitAsync("Alice");

        // Assert
        Assert.False(canSplit);
    }

    [Fact]
    public async Task CanPlayerSplitAsync_WithInvalidPlayerName_ReturnsFalse()
    {
        // Arrange
        SetupGameWithDealtCards();

        // Act
        var canSplit = await _gameService.CanPlayerSplitAsync("NonExistentPlayer");

        // Assert
        Assert.False(canSplit);
    }

    [Fact]
    public async Task CanPlayerSplitAsync_WithNullPlayerName_ReturnsFalse()
    {
        // Act
        var canSplit = await _gameService.CanPlayerSplitAsync(null!);

        // Assert
        Assert.False(canSplit);
    }

    #endregion
}