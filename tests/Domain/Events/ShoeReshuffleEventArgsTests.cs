using GroupProject.Domain.Events;
using Xunit;

namespace GroupProject.Tests.Domain.Events;

public class ShoeReshuffleEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var remainingPercentage = 0.15;
        var penetrationThreshold = 0.25;
        var reason = "Automatic reshuffle triggered";

        // Act
        var eventArgs = new ShoeReshuffleEventArgs(remainingPercentage, penetrationThreshold, reason);

        // Assert
        Assert.Equal(remainingPercentage, eventArgs.RemainingPercentage);
        Assert.Equal(penetrationThreshold, eventArgs.PenetrationThreshold);
        Assert.Equal(reason, eventArgs.Reason);
        Assert.True(eventArgs.Timestamp <= DateTime.UtcNow);
        Assert.True(eventArgs.Timestamp > DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public void Constructor_WithNullReason_ThrowsArgumentNullException()
    {
        // Arrange
        var remainingPercentage = 0.15;
        var penetrationThreshold = 0.25;
        string? reason = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new ShoeReshuffleEventArgs(remainingPercentage, penetrationThreshold, reason!));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var remainingPercentage = 0.15;
        var penetrationThreshold = 0.25;
        var reason = "Test reshuffle";
        var eventArgs = new ShoeReshuffleEventArgs(remainingPercentage, penetrationThreshold, reason);

        // Act
        var result = eventArgs.ToString();

        // Assert
        Assert.Contains("Shoe reshuffle", result);
        Assert.Contains(reason, result);
        Assert.Contains("15.0%", result);
        Assert.Contains("25.0%", result);
    }

    [Theory]
    [InlineData(0.0, 0.5, "Empty shoe")]
    [InlineData(0.5, 0.25, "Mid-game reshuffle")]
    [InlineData(1.0, 0.75, "Full shoe")]
    public void Constructor_WithVariousPercentages_SetsPropertiesCorrectly(
        double remainingPercentage, 
        double penetrationThreshold, 
        string reason)
    {
        // Act
        var eventArgs = new ShoeReshuffleEventArgs(remainingPercentage, penetrationThreshold, reason);

        // Assert
        Assert.Equal(remainingPercentage, eventArgs.RemainingPercentage);
        Assert.Equal(penetrationThreshold, eventArgs.PenetrationThreshold);
        Assert.Equal(reason, eventArgs.Reason);
    }
}