using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Events;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application.Services;

/// <summary>
/// Tests for GameOrchestrator shoe management functionality.
/// </summary>
public class GameOrchestratorShoeManagementTests
{
    private readonly Mock<IGameService> _mockGameService;
    private readonly Mock<IUserInterface> _mockUserInterface;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly GameOrchestrator _gameOrchestrator;

    public GameOrchestratorShoeManagementTests()
    {
        _mockGameService = new Mock<IGameService>();
        _mockUserInterface = new Mock<IUserInterface>();
        _mockErrorHandler = new Mock<IErrorHandler>();

        _gameOrchestrator = new GameOrchestrator(
            _mockGameService.Object,
            _mockUserInterface.Object,
            _mockErrorHandler.Object);
    }

    [Fact]
    public void Constructor_SubscribesToShoeReshuffledEvent()
    {
        // Arrange & Act - Constructor already called in setup
        var testEventArgs = new ShoeReshuffleEventArgs(0.2, 0.25, "Test reshuffle");

        // Act - Raise the event
        _mockGameService.Raise(gs => gs.ShoeReshuffled += null, _mockGameService.Object, testEventArgs);

        // Assert - Verify the UI method was called
        _mockUserInterface.Verify(ui => ui.ShowShoeReshuffleNotificationAsync(testEventArgs), Times.Once);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_ChecksShoeStatusBeforeEachRound()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 100, 0.32, 0.25, false, true);
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);
        
        // Setup to play only one round
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
                         .ReturnsAsync(false);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockGameService.Verify(gs => gs.GetShoeStatus(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_TriggersReshuffleWhenShoeNeedsReshuffle()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 50, 0.16, 0.25, true, true);
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);
        
        // Setup to play only one round
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
                         .ReturnsAsync(false);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockGameService.Verify(gs => gs.TriggerShoeReshuffle(It.Is<string>(reason => 
            reason.Contains("Pre-round reshuffle"))), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Preparing shoe for next round..."), Times.Once);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_TriggersReshuffleWhenShoeIsNearlyEmpty()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 15, 0.048, 0.25, false, true); // Nearly empty (< 5%)
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);
        
        // Setup to play only one round
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
                         .ReturnsAsync(false);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockGameService.Verify(gs => gs.TriggerShoeReshuffle(It.Is<string>(reason => 
            reason.Contains("Pre-round reshuffle"))), Times.Once);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Preparing shoe for next round..."), Times.Once);
    }

    [Fact]
    public async Task RunMultipleRoundsAsync_DoesNotTriggerReshuffleWhenShoeIsAdequate()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 200, 0.64, 0.25, false, true);
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);
        
        // Setup to play only one round
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
                         .ReturnsAsync(false);

        // Act
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Assert
        _mockGameService.Verify(gs => gs.TriggerShoeReshuffle(It.IsAny<string>()), Times.Never);
        _mockUserInterface.Verify(ui => ui.ShowMessageAsync("Preparing shoe for next round..."), Times.Never);
    }

    [Fact]
    public async Task ShowCurrentGameState_ShowsShoeStatusWhenNeedsReshuffle()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 50, 0.16, 0.25, true, true);
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowShoeStatusAsync(shoeStatus), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ShowCurrentGameState_ShowsShoeStatusWhenNearlyEmpty()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 15, 0.048, 0.25, false, true); // Nearly empty
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowShoeStatusAsync(shoeStatus), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ShowCurrentGameState_DoesNotShowShoeStatusWhenAdequate()
    {
        // Arrange
        SetupBasicMocks();
        
        var shoeStatus = new ShoeStatus(6, 200, 0.64, 0.25, false, true);
        _mockGameService.Setup(gs => gs.GetShoeStatus()).Returns(shoeStatus);

        // Act
        await _gameOrchestrator.RunGameAsync();

        // Assert
        _mockUserInterface.Verify(ui => ui.ShowShoeStatusAsync(It.IsAny<ShoeStatus>()), Times.Never);
    }

    [Fact]
    public void OnShoeReshuffled_HandlesExceptionGracefully()
    {
        // Arrange
        var testEventArgs = new ShoeReshuffleEventArgs(0.2, 0.25, "Test reshuffle");
        _mockUserInterface.Setup(ui => ui.ShowShoeReshuffleNotificationAsync(It.IsAny<ShoeReshuffleEventArgs>()))
                         .ThrowsAsync(new InvalidOperationException("UI error"));
        _mockErrorHandler.Setup(eh => eh.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                        .ReturnsAsync("Error handled");

        // Act & Assert - Should not throw
        _mockGameService.Raise(gs => gs.ShoeReshuffled += null, _mockGameService.Object, testEventArgs);

        // Verify error handler was called
        _mockErrorHandler.Verify(eh => eh.HandleExceptionAsync(
            It.IsAny<InvalidOperationException>(), 
            "OnShoeReshuffled"), Times.Once);
    }

    [Fact]
    public async Task CheckAndHandleShoeReshuffleAsync_HandlesExceptionGracefully()
    {
        // Arrange
        SetupBasicMocks();
        
        _mockGameService.Setup(gs => gs.GetShoeStatus())
                       .Throws(new InvalidOperationException("Shoe error"));
        _mockErrorHandler.Setup(eh => eh.HandleExceptionAsync(It.IsAny<Exception>(), It.IsAny<string>()))
                        .ReturnsAsync("Error handled");
        
        // Setup to play only one round
        _mockUserInterface.SetupSequence(ui => ui.ShouldPlayAnotherRoundAsync())
                         .ReturnsAsync(false);

        // Act & Assert - Should not throw
        await _gameOrchestrator.RunMultipleRoundsAsync();

        // Verify error handler was called
        _mockErrorHandler.Verify(eh => eh.HandleExceptionAsync(
            It.IsAny<InvalidOperationException>(), 
            "CheckAndHandleShoeReshuffleAsync"), Times.Once);
    }

    private void SetupBasicMocks()
    {
        // Setup basic game service mocks
        _mockGameService.Setup(gs => gs.StartNewGame(It.IsAny<IEnumerable<string>>()));
        _mockGameService.Setup(gs => gs.DealInitialCards());
        _mockGameService.Setup(gs => gs.GetCurrentGameState())
                       .Returns(new GameState(
                           new List<GroupProject.Domain.Entities.Player>().AsReadOnly(),
                           new GroupProject.Domain.Entities.Player("Dealer", GroupProject.Domain.ValueObjects.PlayerType.Dealer),
                           GamePhase.GameOver,
                           null));
        _mockGameService.Setup(gs => gs.GetGameResults())
                       .Returns(new GameSummary(
                           new Dictionary<string, GameResult>(),
                           new GroupProject.Domain.Entities.Hand(),
                           DateTime.UtcNow));

        // Setup user interface mocks
        _mockUserInterface.Setup(ui => ui.ShowWelcomeMessageAsync()).Returns(Task.CompletedTask);
        _mockUserInterface.Setup(ui => ui.GetPlayerCountAsync()).ReturnsAsync(1);
        _mockUserInterface.Setup(ui => ui.GetPlayerNamesAsync(It.IsAny<int>()))
                         .ReturnsAsync(new[] { "TestPlayer" });
        _mockUserInterface.Setup(ui => ui.ShowMessageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockUserInterface.Setup(ui => ui.ShowGameStateAsync(It.IsAny<GameState>())).Returns(Task.CompletedTask);
        _mockUserInterface.Setup(ui => ui.ShowGameResultsAsync(It.IsAny<GameSummary>())).Returns(Task.CompletedTask);
        _mockUserInterface.Setup(ui => ui.ShowShoeStatusAsync(It.IsAny<ShoeStatus>())).Returns(Task.CompletedTask);
        _mockUserInterface.Setup(ui => ui.ShowShoeReshuffleNotificationAsync(It.IsAny<ShoeReshuffleEventArgs>()))
                         .Returns(Task.CompletedTask);

        // Setup error handler
        _mockErrorHandler.Setup(eh => eh.IsRecoverableError(It.IsAny<Exception>())).Returns(false);
    }
}