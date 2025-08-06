using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application.Services;

public class GameServiceShoeManagementTests
{
    private readonly Mock<IShoe> _mockShoe;
    private readonly Mock<IGameRules> _mockGameRules;
    private readonly Mock<IBettingService> _mockBettingService;
    private readonly Mock<IShoeManager> _mockShoeManager;
    private readonly GameService _gameService;

    public GameServiceShoeManagementTests()
    {
        _mockShoe = new Mock<IShoe>();
        _mockGameRules = new Mock<IGameRules>();
        _mockBettingService = new Mock<IBettingService>();
        _mockShoeManager = new Mock<IShoeManager>();

        _gameService = new GameService(
            _mockShoe.Object,
            _mockGameRules.Object,
            _mockBettingService.Object,
            _mockShoeManager.Object);
    }

    [Fact]
    public void GetShoeStatus_ReturnsCorrectShoeStatus()
    {
        // Arrange
        _mockShoe.Setup(s => s.DeckCount).Returns(6);
        _mockShoe.Setup(s => s.RemainingCards).Returns(200);
        _mockShoe.Setup(s => s.GetRemainingPercentage()).Returns(0.64);
        _mockShoeManager.Setup(sm => sm.PenetrationThreshold).Returns(0.25);
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(false);
        _mockShoeManager.Setup(sm => sm.AutoReshuffleEnabled).Returns(true);

        // Act
        var status = _gameService.GetShoeStatus();

        // Assert
        Assert.Equal(6, status.DeckCount);
        Assert.Equal(200, status.RemainingCards);
        Assert.Equal(0.64, status.RemainingPercentage);
        Assert.Equal(0.25, status.PenetrationThreshold);
        Assert.False(status.NeedsReshuffle);
        Assert.True(status.AutoReshuffleEnabled);
    }

    [Fact]
    public void IsShoeReshuffleNeeded_ReturnsShoeManagerResult()
    {
        // Arrange
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);

        // Act
        var result = _gameService.IsShoeReshuffleNeeded();

        // Assert
        Assert.True(result);
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
    }

    [Fact]
    public void TriggerShoeReshuffle_CallsShoeManagerWithDefaultReason()
    {
        // Act
        _gameService.TriggerShoeReshuffle();

        // Assert
        _mockShoeManager.Verify(sm => sm.TriggerManualReshuffle("Manual reshuffle"), Times.Once);
    }

    [Fact]
    public void TriggerShoeReshuffle_CallsShoeManagerWithCustomReason()
    {
        // Arrange
        var customReason = "End of session reshuffle";

        // Act
        _gameService.TriggerShoeReshuffle(customReason);

        // Assert
        _mockShoeManager.Verify(sm => sm.TriggerManualReshuffle(customReason), Times.Once);
    }

    [Fact]
    public void ShoeReshuffled_Event_ForwardsFromShoeManager()
    {
        // Arrange
        var eventRaised = false;
        ShoeReshuffleEventArgs? receivedEventArgs = null;
        
        _gameService.ShoeReshuffled += (sender, e) =>
        {
            eventRaised = true;
            receivedEventArgs = e;
        };

        var testEventArgs = new ShoeReshuffleEventArgs(0.2, 0.25, "Test reshuffle");

        // Act
        _mockShoeManager.Raise(sm => sm.ReshuffleOccurred += null, _mockShoeManager.Object, testEventArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(testEventArgs, receivedEventArgs);
    }

    [Fact]
    public void DealInitialCards_ChecksForReshuffleBeforeDealing()
    {
        // Arrange
        SetupBasicGameState();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);
        _mockShoe.Setup(s => s.RemainingCards).Returns(100);

        // Act
        _gameService.DealInitialCards();

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Once);
    }

    [Fact]
    public void DealInitialCards_DoesNotReshuffleWhenNotNeeded()
    {
        // Arrange
        SetupBasicGameState();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(false);
        _mockShoe.Setup(s => s.RemainingCards).Returns(100);

        // Act
        _gameService.DealInitialCards();

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Never);
    }

    [Fact]
    public void ProcessPlayerAction_Hit_ChecksForReshuffleBeforeDrawing()
    {
        // Arrange
        SetupPlayerTurnState();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Hearts, Rank.Five));
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.Hit, It.IsAny<Hand>())).Returns(true);

        // Act
        _gameService.ProcessPlayerAction("Player1", PlayerAction.Hit);

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Once);
    }

    [Fact]
    public void ProcessPlayerAction_DoubleDown_ChecksForReshuffleBeforeDrawing()
    {
        // Arrange
        SetupPlayerTurnStateForDoubleDown();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Hearts, Rank.Five));
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.DoubleDown, It.IsAny<Hand>())).Returns(true);

        // Act
        _gameService.ProcessPlayerAction("Player1", PlayerAction.DoubleDown);

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Once);
    }

    [Fact]
    public void ProcessPlayerAction_Split_ChecksForReshuffleBeforeDrawing()
    {
        // Arrange
        SetupPlayerTurnStateForSplit();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);
        _mockShoe.Setup(s => s.RemainingCards).Returns(10);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Hearts, Rank.Five));
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.Split, It.IsAny<Hand>())).Returns(true);
        _mockGameRules.Setup(gr => gr.CanSplit(It.IsAny<Hand>())).Returns(true);

        // Act
        _gameService.ProcessPlayerAction("Player1", PlayerAction.Split);

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Once);
    }

    [Fact]
    public void PlayDealerTurn_ChecksForReshuffleBeforeEachDraw()
    {
        // Arrange
        SetupDealerTurnState();
        _mockShoeManager.Setup(sm => sm.IsReshuffleNeeded()).Returns(true);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);
        _mockShoe.Setup(s => s.Draw()).Returns(new Card(Suit.Hearts, Rank.Five));
        
        // Setup dealer to hit once then stand
        var hitCount = 0;
        _mockGameRules.Setup(gr => gr.ShouldDealerHit(It.IsAny<int>()))
                     .Returns(() => hitCount++ == 0); // Hit once, then stand

        // Act
        _gameService.PlayDealerTurn();

        // Assert
        _mockShoeManager.Verify(sm => sm.IsReshuffleNeeded(), Times.Once);
        _mockShoeManager.Verify(sm => sm.HandleAutomaticReshuffle(), Times.Once);
    }

    private void SetupBasicGameState()
    {
        _gameService.StartNewGame(new[] { "Player1" });
        
        // Setup betting service for betting phase
        _mockBettingService.Setup(bs => bs.MinimumBet).Returns(new Money(10));
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
                          .ReturnsAsync(new Money(1000));
        
        // Complete betting phase
        var bettingTask = _gameService.ProcessBettingRoundAsync();
        bettingTask.Wait();
    }

    private void SetupPlayerTurnState()
    {
        SetupBasicGameState();
        
        // Setup cards for dealing
        _mockShoe.Setup(s => s.Draw())
               .Returns(new Card(Suit.Hearts, Rank.Ten));
        
        _gameService.DealInitialCards();
    }

    private void SetupPlayerTurnStateForDoubleDown()
    {
        SetupPlayerTurnState();
        
        // Setup player with valid double down conditions
        var player = _gameService.GetPlayer("Player1");
        if (player != null)
        {
            player.PlaceBet(new Money(100), BetType.Standard);
            player.AddFunds(new Money(1000)); // Ensure sufficient funds
        }
    }

    private void SetupPlayerTurnStateForSplit()
    {
        SetupPlayerTurnState();
        
        // Setup player with valid split conditions
        var player = _gameService.GetPlayer("Player1");
        if (player != null)
        {
            player.PlaceBet(new Money(100), BetType.Standard);
            player.AddFunds(new Money(1000)); // Ensure sufficient funds
        }
    }

    private void SetupDealerTurnState()
    {
        SetupPlayerTurnState();
        
        // Advance to dealer turn by making player stand
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.Stand, It.IsAny<Hand>())).Returns(true);
        _gameService.ProcessPlayerAction("Player1", PlayerAction.Stand);
    }
}