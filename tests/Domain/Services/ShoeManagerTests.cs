using GroupProject.Domain.Entities;
using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using Moq;
using Xunit;

namespace GroupProject.Tests.Domain.Services;

public class ShoeManagerTests
{
    private readonly Mock<IRandomProvider> _mockRandomProvider;
    private readonly Mock<IShoe> _mockShoe;

    public ShoeManagerTests()
    {
        _mockRandomProvider = new Mock<IRandomProvider>();
        _mockShoe = new Mock<IShoe>();
    }

    [Fact]
    public void Constructor_WithoutShoe_InitializesCorrectly()
    {
        // Act
        var shoeManager = new ShoeManager();

        // Assert
        Assert.True(shoeManager.AutoReshuffleEnabled);
        Assert.Equal(0.25, shoeManager.PenetrationThreshold);
    }

    [Fact]
    public void Constructor_WithShoe_InitializesAndConfiguresShoe()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act
        var shoeManager = new ShoeManager(shoe);

        // Assert
        Assert.Equal(shoe, shoeManager.CurrentShoe);
        Assert.True(shoeManager.AutoReshuffleEnabled);
        Assert.Equal(0.25, shoeManager.PenetrationThreshold);
    }

    [Fact]
    public void CurrentShoe_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shoeManager.CurrentShoe);
    }

    [Fact]
    public void Initialize_WithValidShoe_SetsCurrentShoe()
    {
        // Arrange
        var shoeManager = new ShoeManager();
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act
        shoeManager.Initialize(shoe);

        // Assert
        Assert.Equal(shoe, shoeManager.CurrentShoe);
    }

    [Fact]
    public void Initialize_WithNullShoe_ThrowsArgumentNullException()
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => shoeManager.Initialize(null!));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutoReshuffleEnabled_SetValue_UpdatesCorrectly(bool enabled)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);

        // Act
        shoeManager.AutoReshuffleEnabled = enabled;

        // Assert
        Assert.Equal(enabled, shoeManager.AutoReshuffleEnabled);
        Assert.Equal(enabled, shoe.AutoReshuffleEnabled);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void PenetrationThreshold_SetValidValue_UpdatesCorrectly(double threshold)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);

        // Act
        shoeManager.PenetrationThreshold = threshold;

        // Assert
        Assert.Equal(threshold, shoeManager.PenetrationThreshold);
        Assert.Equal(threshold, shoe.PenetrationThreshold);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void PenetrationThreshold_SetInvalidValue_ThrowsArgumentOutOfRangeException(double invalidThreshold)
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => shoeManager.PenetrationThreshold = invalidThreshold);
    }

    [Fact]
    public void IsReshuffleNeeded_WhenShoeNeedsReshuffle_ReturnsTrue()
    {
        // Arrange
        _mockShoe.Setup(s => s.NeedsReshuffle(It.IsAny<double>())).Returns(true);
        var shoeManager = new ShoeManager();
        shoeManager.Initialize(_mockShoe.Object);

        // Act
        var result = shoeManager.IsReshuffleNeeded();

        // Assert
        Assert.True(result);
        _mockShoe.Verify(s => s.NeedsReshuffle(0.25), Times.Once);
    }

    [Fact]
    public void IsReshuffleNeeded_WhenShoeDoesNotNeedReshuffle_ReturnsFalse()
    {
        // Arrange
        _mockShoe.Setup(s => s.NeedsReshuffle(It.IsAny<double>())).Returns(false);
        var shoeManager = new ShoeManager();
        shoeManager.Initialize(_mockShoe.Object);

        // Act
        var result = shoeManager.IsReshuffleNeeded();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsReshuffleNeeded_WhenNotInitialized_ReturnsFalse()
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act
        var result = shoeManager.IsReshuffleNeeded();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HandleAutomaticReshuffle_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shoeManager.HandleAutomaticReshuffle());
    }

    [Fact]
    public void HandleAutomaticReshuffle_WhenAutoReshuffleDisabled_ReturnsFalse()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        shoeManager.AutoReshuffleEnabled = false;

        // Act
        var result = shoeManager.HandleAutomaticReshuffle();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HandleAutomaticReshuffle_WhenReshuffleNeeded_PerformsReshuffleAndReturnsTrue()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        
        // Disable auto-reshuffle to prevent automatic reshuffling during card drawing
        shoeManager.AutoReshuffleEnabled = false;
        
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoeManager.ReshuffleOccurred += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Draw cards to trigger reshuffle need
        for (int i = 0; i < 40; i++)
        {
            shoe.Draw();
        }

        // Verify that reshuffle is actually needed
        Assert.True(shoeManager.IsReshuffleNeeded(), "Reshuffle should be needed after drawing 40 cards");

        // Re-enable auto-reshuffle for the test
        shoeManager.AutoReshuffleEnabled = true;

        // Act
        var result = shoeManager.HandleAutomaticReshuffle();

        // Assert
        Assert.True(result);
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Contains("Automatic reshuffle performed by ShoeManager", eventArgs.Reason);
        Assert.Equal(52, shoe.RemainingCards); // Should be reset
    }

    [Fact]
    public void HandleAutomaticReshuffle_WhenReshuffleNotNeeded_ReturnsFalse()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        var eventRaised = false;

        shoeManager.ReshuffleOccurred += (sender, e) => eventRaised = true;

        // Act (shoe is full, no reshuffle needed)
        var result = shoeManager.HandleAutomaticReshuffle();

        // Assert
        Assert.False(result);
        Assert.False(eventRaised);
    }

    [Fact]
    public void TriggerManualReshuffle_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var shoeManager = new ShoeManager();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shoeManager.TriggerManualReshuffle());
    }

    [Fact]
    public void TriggerManualReshuffle_WithDefaultReason_TriggersReshuffleWithDefaultReason()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoeManager.ReshuffleOccurred += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        shoeManager.TriggerManualReshuffle();

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Manual reshuffle", eventArgs.Reason);
    }

    [Fact]
    public void TriggerManualReshuffle_WithCustomReason_TriggersReshuffleWithCustomReason()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        var customReason = "End of session reshuffle";
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoeManager.ReshuffleOccurred += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        shoeManager.TriggerManualReshuffle(customReason);

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(customReason, eventArgs.Reason);
    }

    [Fact]
    public void ShoeReshuffleNeeded_Event_ForwardsToReshuffleRequired_WhenAutoReshuffleDisabled()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        
        var reshuffleRequiredEventRaised = false;
        var reshuffleOccurredEventRaised = false;

        shoeManager.ReshuffleRequired += (sender, e) => reshuffleRequiredEventRaised = true;
        shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurredEventRaised = true;

        // Disable auto-reshuffle on ShoeManager only (keep Shoe's auto-reshuffle enabled)
        shoeManager.AutoReshuffleEnabled = false;
        // Re-enable on the shoe to ensure it still raises events
        shoe.AutoReshuffleEnabled = true;

        // Act - Draw cards to trigger reshuffle need
        for (int i = 0; i < 40; i++)
        {
            shoe.Draw();
        }

        // Assert
        Assert.True(reshuffleRequiredEventRaised);
        Assert.False(reshuffleOccurredEventRaised);
    }

    [Fact]
    public void ShoeReshuffleNeeded_Event_TriggersAutomaticReshuffle_WhenAutoReshuffleEnabled()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(shoe);
        
        var reshuffleOccurredEventRaised = false;
        shoeManager.ReshuffleOccurred += (sender, e) => reshuffleOccurredEventRaised = true;

        // Act - Draw cards to trigger reshuffle need
        for (int i = 0; i < 40; i++)
        {
            shoe.Draw();
        }

        // Assert
        Assert.True(reshuffleOccurredEventRaised);
        Assert.Equal(52, shoe.RemainingCards); // Should be reset
    }

    [Fact]
    public void Initialize_WithNewShoe_UnsubscribesFromPreviousShoeEvents()
    {
        // Arrange
        var firstShoe = new Shoe(1, _mockRandomProvider.Object);
        var secondShoe = new Shoe(1, _mockRandomProvider.Object);
        var shoeManager = new ShoeManager(firstShoe);
        
        var eventCount = 0;
        shoeManager.ReshuffleOccurred += (sender, e) => eventCount++;

        // Act
        shoeManager.Initialize(secondShoe);
        firstShoe.TriggerReshuffle("Test first shoe");
        secondShoe.TriggerReshuffle("Test second shoe");

        // Assert
        Assert.Equal(1, eventCount); // Only second shoe event should be received
    }
}