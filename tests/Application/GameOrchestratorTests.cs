using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application;

public class GameOrchestratorTests
{
    private readonly Mock<IGameService> _mockGameService;
    private readonly Mock<IUserInterface> _mockUserInterface;
    private readonly GameOrchestrator _gameOrchestrator;

    public GameOrchestratorTests()
    {
        _mockGameService = new Mock<IGameService>();
        _mockUserInterface = new Mock<IUserInterface>();
        _gameOrchestrator = new GameOrchestrator(_mockGameService.Object, _mockUserInterface.Object);
    }

    [Fact]
    public void Constructor_WithNullGameService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameOrchestrator(null!, _mockUserInterface.Object));
    }

    [Fact]
    public void Constructor_WithNullUserInterface_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GameOrchestrator(_mockGameService.Object, null!));
    }

    [Fact]
    public async Task GetPlayerCountAsync_CallsUserInterface()
    {
        // Arrange
        const int expectedPlayerCount = 3;
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(expectedPlayerCount);

        // Act
        var result = await _gameOrchestrator.GetPlayerCountAsync();

        // Assert
        Assert.Equal(expectedPlayerCount, result);
        _mockUserInterface.Verify(ui => ui.GetPlayerCountAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPlayerNamesAsync_CallsUserInterface()
    {
        // Arrange
        const int playerCount = 2;
        var expectedNames = new[] { "Alice", "Bob" };
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(playerCount)).ReturnsAsync(expectedNames);

        // Act
        var result = await _gameOrchestrator.GetPlayerNamesAsync(playerCount);

        // Assert
        Assert.Equal(expectedNames, result);
        _mockUserInterface.Verify(ui => ui.GetPlayerNamesAsync(playerCount), Times.Once);
    }

    [Fact]
    public async Task ShouldPlayAnotherRoundAsync_CallsUserInterface()
    {
        // Arrange
        const bool expectedResult = true;
        _mockUserInterface.Setup(ui => ui.ShouldPlayAnotherRoundAsync()).ReturnsAsync(expectedResult);

        // Act
        var result = await _gameOrchestrator.ShouldPlayAnotherRoundAsync();

        // Assert
        Assert.Equal(expectedResult, result);
        _mockUserInterface.Verify(ui => ui.ShouldPlayAnotherRoundAsync(), Times.Once);
    }

    [Fact]
    public async Task RunGameAsync_WithValidFlow_CompletesSuccessfully()
    {
        // Arrange
        var playerNames = new[] { "Alice", "Bob" };
        var players = new List<Player>
        {
            new Player("Alice", PlayerType.Human),
            new Player("Bob", PlayerType.Human)
        };
        var dealer = new Player("Dealer", PlayerType.Dealer);
        var gameState = new GameState(players, dealer, GamePhase.PlayerTurns, "Alice");
        var gameSummary = new GameSummary(
            new Dictionary<string, GameResult> { { "Alice", GameResult.Win }, { "Bob", GameResult.Lose } },
            dealer.Hand,
            DateTime.UtcNow);

        // Setup user interface
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(2);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(2)).ReturnsAsync(playerNames);
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
            .ReturnsAsync(PlayerAction.Stand);
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Bob", It.IsAny<IEnumerable<PlayerAction>>()))
            .ReturnsAsync(PlayerAction.Stand);

        // Setup game service
        _mockGameService.Setup(gs => gs.GetCurrentGameState()).Returns(gameState);
        _mockGameService.Setup(gs => gs.GetPlayers()).Returns(players);
        _mockGameService.Setup(gs => gs.GetDealer()).Returns(dealer);
        _mockGameService.Setup(gs => gs.IsPlayerTurn("Alice")).Returns(true);
        _mockGameService.Setup(gs => gs.IsPlayerTurn("Bob")).Returns(true);
        _mockGameService.Setup(gs => gs.ProcessPlayerAction("Alice", PlayerAction.Stand))
            .Returns(PlayerActionResult.SuccessEndTurn(players[0].Hand));
        _mockGameService.Setup(gs => gs.ProcessPlayerAction("Bob", PlayerAction.Stand))
            .Returns(PlayerActionResult.SuccessEndTurn(players[1].Hand));
        _mockGameService.Setup(gs => gs.GetGameResults()).Returns(gameSummary);

        // Setup game state transitions
        _mockGameService.SetupSequence(gs => gs.GetCurrentGameState())
            .Returns(new GameState(players, dealer, GamePhase.PlayerTurns, "Alice"))
            .Returns(new GameState(players, dealer, GamePhase.PlayerTurns, "Bob"))
            .Returns(new GameState(players, dealer, GamePhase.DealerTurn))
            .Returns(new GameState(players, dealer, GamePhase.Results));

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockGameService.Verify(gs => gs.StartNewGame(playerNames), Times.Once);
        _mockGameService.Verify(gs => gs.DealInitialCards(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowGameResultsAsync(gameSummary), Times.Once);
    }

    [Fact]
    public async Task RunGameAsync_WithPlayerBlackjack_SkipsPlayerTurn()
    {
        // Arrange
        var playerNames = new[] { "Alice" };
        var alice = new Player("Alice", PlayerType.Human);
        alice.AddCard(new Card(Suit.Hearts, Rank.Ace));
        alice.AddCard(new Card(Suit.Spades, Rank.King));
        var players = new List<Player> { alice };
        var dealer = new Player("Dealer", PlayerType.Dealer);
        var gameState = new GameState(players, dealer, GamePhase.PlayerTurns);
        var gameSummary = new GameSummary(
            new Dictionary<string, GameResult> { { "Alice", GameResult.Blackjack } },
            dealer.Hand,
            DateTime.UtcNow);

        // Setup user interface
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(playerNames);

        // Setup game service
        _mockGameService.Setup(gs => gs.GetCurrentGameState()).Returns(gameState);
        _mockGameService.Setup(gs => gs.GetPlayers()).Returns(players);
        _mockGameService.Setup(gs => gs.GetDealer()).Returns(dealer);
        _mockGameService.Setup(gs => gs.GetGameResults()).Returns(gameSummary);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Alice has blackjack!"), Times.Once);
        _mockUserInterface.Verify(ui => ui.GetPlayerActionAsync(It.IsAny<string>(), It.IsAny<IEnumerable<PlayerAction>>()), Times.Never);
    }

    [Fact]
    public async Task RunGameAsync_WithDealerTurn_HandlesDealerPlay()
    {
        // Arrange
        var playerNames = new[] { "Alice" };
        var players = new List<Player> { new Player("Alice", PlayerType.Human) };
        var dealer = new Player("Dealer", PlayerType.Dealer);
        var gameSummary = new GameSummary(
            new Dictionary<string, GameResult> { { "Alice", GameResult.Win } },
            dealer.Hand,
            DateTime.UtcNow);

        // Setup user interface
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(playerNames);
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
            .ReturnsAsync(PlayerAction.Stand);

        // Setup game service
        _mockGameService.Setup(gs => gs.GetPlayers()).Returns(players);
        _mockGameService.Setup(gs => gs.GetDealer()).Returns(dealer);
        _mockGameService.SetupSequence(gs => gs.IsPlayerTurn("Alice"))
            .Returns(true)   // First call - player can take action
            .Returns(false); // Second call - player turn ended
        _mockGameService.Setup(gs => gs.ProcessPlayerAction("Alice", PlayerAction.Stand))
            .Returns(PlayerActionResult.SuccessEndTurn(players[0].Hand));
        _mockGameService.Setup(gs => gs.GetGameResults()).Returns(gameSummary);

        // Setup game state transitions
        _mockGameService.SetupSequence(gs => gs.GetCurrentGameState())
            .Returns(new GameState(players, dealer, GamePhase.PlayerTurns, "Alice"))  // Initial state for HandlePlayerTurnsAsync
            .Returns(new GameState(players, dealer, GamePhase.DealerTurn))            // After player turn ends
            .Returns(new GameState(players, dealer, GamePhase.DealerTurn))            // For HandleDealerTurnAsync check
            .Returns(new GameState(players, dealer, GamePhase.Results));              // After dealer turn

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockGameService.Verify(gs => gs.PlayDealerTurn(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("\nDealer's turn:"), Times.Once);
    }

    [Fact]
    public async Task RunGameAsync_WithGameServiceException_ShowsErrorAndRethrows()
    {
        // Arrange
        var playerNames = new[] { "Alice" };
        var expectedException = new InvalidOperationException("Test exception");

        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(playerNames);
        _mockGameService.Setup(gs => gs.StartNewGame(playerNames)).Throws(expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => _gameOrchestrator.RunGameAsync());
        
        Assert.Same(expectedException, actualException);
        _mockUserInterface.Verify(ui => ui.ShowErrorMessageAsync("An error occurred during the game: Test exception"), Times.Once);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_WithSingleRound_CompletesSuccessfully()
    {
        // Arrange
        var playerNames = new[] { "Alice" };
        var players = new List<Player> { new Player("Alice", PlayerType.Human) };
        var dealer = new Player("Dealer", PlayerType.Dealer);
        var gameState = new GameState(players, dealer, GamePhase.PlayerTurns, "Alice");
        var gameSummary = new GameSummary(
            new Dictionary<string, GameResult> { { "Alice", GameResult.Win } },
            dealer.Hand,
            DateTime.UtcNow);

        // Setup for single round
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(playerNames);
        _mockUserInterface.Setup(ui => ui.ShouldPlayAnotherRoundAsync()).ReturnsAsync(false);
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
            .ReturnsAsync(PlayerAction.Stand);

        _mockGameService.Setup(gs => gs.GetCurrentGameState()).Returns(gameState);
        _mockGameService.Setup(gs => gs.GetPlayers()).Returns(players);
        _mockGameService.Setup(gs => gs.GetDealer()).Returns(dealer);
        _mockGameService.Setup(gs => gs.IsPlayerTurn("Alice")).Returns(true);
        _mockGameService.Setup(gs => gs.ProcessPlayerAction("Alice", PlayerAction.Stand))
            .Returns(PlayerActionResult.SuccessEndTurn(players[0].Hand));
        _mockGameService.Setup(gs => gs.GetGameResults()).Returns(gameSummary);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowWelcomeMessageAsync(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShouldPlayAnotherRoundAsync(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Thanks for playing!"), Times.Once);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_WithMultipleRounds_RunsUntilUserStops()
    {
        // Arrange
        var playerNames = new[] { "Alice" };
        var players = new List<Player> { new Player("Alice", PlayerType.Human) };
        var dealer = new Player("Dealer", PlayerType.Dealer);
        var gameState = new GameState(players, dealer, GamePhase.PlayerTurns, "Alice");
        var gameSummary = new GameSummary(
            new Dictionary<string, GameResult> { { "Alice", GameResult.Win } },
            dealer.Hand,
            DateTime.UtcNow);

        // Setup for multiple rounds
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(1)).ReturnsAsync(playerNames);
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
            .ReturnsAsync(true)  // Play second round
            .ReturnsAsync(false); // Stop after second round
        _mockUserInterface.Setup(ui => ui.GetPlayerActionAsync("Alice", It.IsAny<IEnumerable<PlayerAction>>()))
            .ReturnsAsync(PlayerAction.Stand);

        _mockGameService.Setup(gs => gs.GetCurrentGameState()).Returns(gameState);
        _mockGameService.Setup(gs => gs.GetPlayers()).Returns(players);
        _mockGameService.Setup(gs => gs.GetDealer()).Returns(dealer);
        _mockGameService.Setup(gs => gs.IsPlayerTurn("Alice")).Returns(true);
        _mockGameService.Setup(gs => gs.ProcessPlayerAction("Alice", PlayerAction.Stand))
            .Returns(PlayerActionResult.SuccessEndTurn(players[0].Hand));
        _mockGameService.Setup(gs => gs.GetGameResults()).Returns(gameSummary);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowWelcomeMessageAsync(), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShouldPlayAnotherRoundAsync(), Times.Exactly(2));
        _mockGameService.Verify(gs => gs.StartNewGame(playerNames), Times.Exactly(2));
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Thanks for playing!"), Times.Once);
    }
}