using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using Moq;
using Xunit;

namespace GroupProject.Tests.Integration;

/// <summary>
/// Integration tests for shoe management functionality across all layers.
/// </summary>
public class ShoeManagementIntegrationTests
{
    private readonly Mock<IRandomProvider> _mockRandomProvider;
    private readonly Mock<IGameRules> _mockGameRules;
    private readonly Mock<IBettingService> _mockBettingService;
    private readonly Shoe _shoe;
    private readonly ShoeManager _shoeManager;
    private readonly GameService _gameService;

    public ShoeManagementIntegrationTests()
    {
        _mockRandomProvider = new Mock<IRandomProvider>();
        _mockGameRules = new Mock<IGameRules>();
        _mockBettingService = new Mock<IBettingService>();

        // Setup random provider to not actually shuffle (for predictable tests)
        _mockRandomProvider.Setup(rp => rp.Shuffle(It.IsAny<IList<Card>>()));

        _shoe = new Shoe(6, _mockRandomProvider.Object);
        _shoeManager = new ShoeManager(_shoe);
        _gameService = new GameService(_shoe, _mockGameRules.Object, _mockBettingService.Object, _shoeManager);
    }

    [Fact]
    public void ShoeManager_AutomaticallyReshufflesWhenThresholdReached()
    {
        // Arrange
        var reshuffleOccurred = false;
        ShoeReshuffleEventArgs? eventArgs = null;
        
        _shoeManager.ReshuffleOccurred += (sender, e) =>
        {
            reshuffleOccurred = true;
            eventArgs = e;
        };

        // Set a high penetration threshold so we can trigger it easily
        _shoeManager.PenetrationThreshold = 0.9; // 90%
        _shoeManager.AutoReshuffleEnabled = true;

        var initialCardCount = _shoe.RemainingCards;

        // Act - Draw cards until we hit the threshold
        var cardsToDrawToTriggerReshuffle = (int)(initialCardCount * 0.11); // Draw 11% to get below 90%
        for (int i = 0; i < cardsToDrawToTriggerReshuffle; i++)
        {
            _shoe.Draw();
        }

        // Assert
        Assert.True(reshuffleOccurred);
        Assert.NotNull(eventArgs);
        Assert.Contains("Automatic reshuffle", eventArgs.Reason);
        Assert.Equal(0.9, eventArgs.PenetrationThreshold);
        Assert.True(eventArgs.RemainingPercentage < 0.9);
    }

    [Fact]
    public void ShoeManager_DoesNotAutoReshuffleWhenDisabled()
    {
        // Arrange
        var reshuffleOccurred = false;
        
        _shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurred = true;
        _shoeManager.PenetrationThreshold = 0.9;
        _shoeManager.AutoReshuffleEnabled = false; // Disable auto reshuffle

        var initialCardCount = _shoe.RemainingCards;

        // Act - Draw cards until we would hit the threshold
        var cardsToDrawToTriggerReshuffle = (int)(initialCardCount * 0.11);
        for (int i = 0; i < cardsToDrawToTriggerReshuffle; i++)
        {
            _shoe.Draw();
        }

        // Assert
        Assert.False(reshuffleOccurred);
    }

    [Fact]
    public void GameService_ForwardsShoeReshuffleEvents()
    {
        // Arrange
        var gameServiceEventRaised = false;
        ShoeReshuffleEventArgs? receivedEventArgs = null;
        
        _gameService.ShoeReshuffled += (sender, e) =>
        {
            gameServiceEventRaised = true;
            receivedEventArgs = e;
        };

        // Act - Trigger a manual reshuffle
        _shoeManager.TriggerManualReshuffle("Integration test reshuffle");

        // Assert
        Assert.True(gameServiceEventRaised);
        Assert.NotNull(receivedEventArgs);
        Assert.Equal("Integration test reshuffle", receivedEventArgs.Reason);
    }

    [Fact]
    public void GameService_ChecksForReshuffleBeforeDealingCards()
    {
        // Arrange
        SetupBasicGameMocks();
        
        // Set up shoe to need reshuffling
        _shoeManager.PenetrationThreshold = 0.9;
        var initialCardCount = _shoe.RemainingCards;
        var cardsToDrawToTriggerReshuffle = (int)(initialCardCount * 0.11);
        for (int i = 0; i < cardsToDrawToTriggerReshuffle; i++)
        {
            _shoe.Draw();
        }

        var reshuffleOccurred = false;
        _shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurred = true;

        _gameService.StartNewGame(new[] { "TestPlayer" });

        // Act
        _gameService.DealInitialCards();

        // Assert
        Assert.True(reshuffleOccurred);
    }

    [Fact]
    public void GameService_ChecksForReshuffleBeforePlayerActions()
    {
        // Arrange
        SetupBasicGameMocks();
        SetupPlayerTurnState();

        // Set up shoe to need reshuffling
        _shoeManager.PenetrationThreshold = 0.9;
        var initialCardCount = _shoe.RemainingCards;
        var cardsToDrawToTriggerReshuffle = (int)(initialCardCount * 0.11);
        for (int i = 0; i < cardsToDrawToTriggerReshuffle; i++)
        {
            _shoe.Draw();
        }

        var reshuffleOccurred = false;
        _shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurred = true;

        // Act - Process a hit action
        _gameService.ProcessPlayerAction("TestPlayer", PlayerAction.Hit);

        // Assert
        Assert.True(reshuffleOccurred);
    }

    [Fact]
    public void GameService_ChecksForReshuffleBeforeDealerTurn()
    {
        // Arrange
        SetupBasicGameMocks();
        SetupDealerTurnState();

        // Set up shoe to need reshuffling
        _shoeManager.PenetrationThreshold = 0.9;
        var initialCardCount = _shoe.RemainingCards;
        var cardsToDrawToTriggerReshuffle = (int)(initialCardCount * 0.11);
        for (int i = 0; i < cardsToDrawToTriggerReshuffle; i++)
        {
            _shoe.Draw();
        }

        var reshuffleOccurred = false;
        _shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurred = true;

        // Setup dealer to hit once
        _mockGameRules.Setup(gr => gr.ShouldDealerHit(It.IsAny<int>())).Returns(true);

        // Act
        _gameService.PlayDealerTurn();

        // Assert
        Assert.True(reshuffleOccurred);
    }

    [Fact]
    public void ShoeStatus_ReflectsCurrentShoeState()
    {
        // Arrange
        var initialCardCount = _shoe.RemainingCards;
        var cardsToDraw = 50;

        // Act - Draw some cards
        for (int i = 0; i < cardsToDraw; i++)
        {
            _shoe.Draw();
        }

        var shoeStatus = _gameService.GetShoeStatus();

        // Assert
        Assert.Equal(6, shoeStatus.DeckCount);
        Assert.Equal(initialCardCount - cardsToDraw, shoeStatus.RemainingCards);
        Assert.Equal(cardsToDraw, shoeStatus.CardsDealt);
        Assert.Equal(312, shoeStatus.TotalCards); // 6 decks * 52 cards
        Assert.Equal((double)(initialCardCount - cardsToDraw) / 312, shoeStatus.RemainingPercentage, 3);
        Assert.True(shoeStatus.AutoReshuffleEnabled);
    }

    [Fact]
    public void ShoeManager_ConfigurationChangesAffectShoe()
    {
        // Arrange & Act
        _shoeManager.PenetrationThreshold = 0.3;
        _shoeManager.AutoReshuffleEnabled = false;

        // Assert
        Assert.Equal(0.3, _shoe.PenetrationThreshold);
        Assert.False(_shoe.AutoReshuffleEnabled);
    }

    [Theory]
    [InlineData(0.1, true)]  // 10% threshold - should need reshuffle with default cards
    [InlineData(0.9, false)] // 90% threshold - should not need reshuffle with default cards
    public void ShoeManager_IsReshuffleNeeded_ReflectsThreshold(double threshold, bool expectedNeedsReshuffle)
    {
        // Arrange
        _shoeManager.PenetrationThreshold = threshold;

        // Draw some cards to get below 90% but above 10%
        var initialCardCount = _shoe.RemainingCards;
        var cardsToDraw = (int)(initialCardCount * 0.5); // Draw 50% of cards
        for (int i = 0; i < cardsToDraw; i++)
        {
            _shoe.Draw();
        }

        // Act
        var needsReshuffle = _shoeManager.IsReshuffleNeeded();

        // Assert
        Assert.Equal(expectedNeedsReshuffle, needsReshuffle);
    }

    private void SetupBasicGameMocks()
    {
        _mockBettingService.Setup(bs => bs.MinimumBet).Returns(new Money(10));
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Money(1000));
        _mockBettingService.Setup(bs => bs.ValidateBetAsync(It.IsAny<string>(), It.IsAny<Money>()))
                          .ReturnsAsync(BettingResult.Success("Bet validated"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync(It.IsAny<string>(), It.IsAny<Money>()))
                          .ReturnsAsync(BettingResult.Success("Bet placed"));
    }

    private void SetupPlayerTurnState()
    {
        SetupBasicGameMocks();
        
        _gameService.StartNewGame(new[] { "TestPlayer" });
        
        // Complete betting phase
        var bettingTask = _gameService.ProcessBettingRoundAsync();
        bettingTask.Wait();
        
        _gameService.DealInitialCards();
        
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.Hit, It.IsAny<Hand>())).Returns(true);
    }

    private void SetupDealerTurnState()
    {
        SetupPlayerTurnState();
        
        // Advance to dealer turn by making player stand
        _mockGameRules.Setup(gr => gr.IsValidPlayerAction(PlayerAction.Stand, It.IsAny<Hand>())).Returns(true);
        _gameService.ProcessPlayerAction("TestPlayer", PlayerAction.Stand);
    }
}