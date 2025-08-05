using GroupProject.Application.Models;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Application.Models;

/// <summary>
/// Unit tests for the BettingState model.
/// </summary>
public class BettingStateTests
{
    private readonly List<string> _defaultPlayerNames;
    private readonly Dictionary<string, Money> _defaultBankrolls;

    public BettingStateTests()
    {
        _defaultPlayerNames = new List<string> { "Alice", "Bob", "Charlie" };
        _defaultBankrolls = new Dictionary<string, Money>
        {
            { "Alice", new Money(1000m) },
            { "Bob", new Money(800m) },
            { "Charlie", new Money(1200m) }
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesBettingState()
    {
        // Act
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Assert
        Assert.Equal(BettingPhase.WaitingForBets, bettingState.CurrentPhase);
        Assert.Equal(0, bettingState.CurrentBettingPlayerIndex);
        Assert.Equal("Alice", bettingState.CurrentBettingPlayer);
        Assert.False(bettingState.IsComplete);
        Assert.Equal(3, bettingState.PlayerOrder.Count);
        Assert.Empty(bettingState.PlayerBets);
        Assert.Equal(Money.Zero, bettingState.TotalWagered);
        Assert.Equal(0, bettingState.PlayersWithBets);
        Assert.Equal(3, bettingState.PlayersWaitingToBet);
    }

    [Fact]
    public void Constructor_WithNullPlayerNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BettingState(null!, _defaultBankrolls));
    }

    [Fact]
    public void Constructor_WithNullBankrolls_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new BettingState(_defaultPlayerNames, null!));
    }

    [Fact]
    public void Constructor_WithEmptyPlayerNames_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new BettingState(new List<string>(), _defaultBankrolls));
    }

    [Fact]
    public void Constructor_WithMissingBankroll_ThrowsArgumentException()
    {
        // Arrange
        var incompleteBankrolls = new Dictionary<string, Money>
        {
            { "Alice", new Money(1000m) },
            { "Bob", new Money(800m) }
            // Missing Charlie
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new BettingState(_defaultPlayerNames, incompleteBankrolls));
    }

    [Fact]
    public void Constructor_WithDuplicatePlayerNames_RemovesDuplicates()
    {
        // Arrange
        var duplicateNames = new List<string> { "Alice", "Bob", "alice", "Charlie" };
        var bankrolls = new Dictionary<string, Money>(StringComparer.OrdinalIgnoreCase)
        {
            { "Alice", new Money(1000m) },
            { "Bob", new Money(800m) },
            { "Charlie", new Money(1200m) }
        };

        // Act
        var bettingState = new BettingState(duplicateNames, bankrolls);

        // Assert
        Assert.Equal(3, bettingState.PlayerOrder.Count);
        Assert.Contains("Alice", bettingState.PlayerOrder);
        Assert.Contains("Bob", bettingState.PlayerOrder);
        Assert.Contains("Charlie", bettingState.PlayerOrder);
    }

    [Fact]
    public void PlaceBet_WithValidBet_PlacesBetSuccessfully()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        var betAmount = new Money(100m);

        // Act
        var result = bettingState.PlaceBet("Alice", betAmount);

        // Assert
        Assert.True(result);
        Assert.True(bettingState.HasPlayerBet("Alice"));
        Assert.Equal(betAmount, bettingState.GetPlayerBet("Alice")!.Amount);
        Assert.Equal(new Money(900m), bettingState.GetPlayerBankroll("Alice"));
        Assert.Equal(betAmount, bettingState.TotalWagered);
        Assert.Equal(1, bettingState.PlayersWithBets);
        Assert.Equal(2, bettingState.PlayersWaitingToBet);
        Assert.Equal("Bob", bettingState.CurrentBettingPlayer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaceBet_WithInvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            bettingState.PlaceBet(playerName, new Money(100m)));
    }

    [Fact]
    public void PlaceBet_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            bettingState.PlaceBet("Alice", Money.Zero));
    }

    [Fact]
    public void PlaceBet_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        var excessiveAmount = new Money(2000m); // More than Alice's bankroll

        // Act
        var result = bettingState.PlaceBet("Alice", excessiveAmount);

        // Assert
        Assert.False(result);
        Assert.False(bettingState.HasPlayerBet("Alice"));
        Assert.Equal(Money.Zero, bettingState.TotalWagered);
    }

    [Fact]
    public void PlaceBet_WithNonExistentPlayer_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        var result = bettingState.PlaceBet("NonExistent", new Money(100m));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PlaceBet_WhenPlayerAlreadyBet_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act
        var result = bettingState.PlaceBet("Alice", new Money(50m));

        // Assert
        Assert.False(result);
        Assert.Equal(new Money(100m), bettingState.GetPlayerBet("Alice")!.Amount);
    }

    [Fact]
    public void PlaceBet_WhenAllPlayersHaveBet_CompletesRound()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        bettingState.PlaceBet("Alice", new Money(100m));
        bettingState.PlaceBet("Bob", new Money(50m));
        bettingState.PlaceBet("Charlie", new Money(75m));

        // Assert
        Assert.Equal(BettingPhase.Complete, bettingState.CurrentPhase);
        Assert.True(bettingState.IsComplete);
        Assert.Null(bettingState.CurrentBettingPlayer);
        Assert.Equal(new Money(225m), bettingState.TotalWagered);
        Assert.Equal(3, bettingState.PlayersWithBets);
        Assert.Equal(0, bettingState.PlayersWaitingToBet);
    }

    [Fact]
    public void CanPlayerBet_WithValidConditions_ReturnsTrue()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.True(bettingState.CanPlayerBet("Alice", new Money(100m)));
        Assert.True(bettingState.CanPlayerBet("Bob", new Money(800m))); // Exact bankroll amount
    }

    [Fact]
    public void CanPlayerBet_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.False(bettingState.CanPlayerBet("Alice", new Money(1500m)));
    }

    [Fact]
    public void CanPlayerBet_WithInvalidPlayerName_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.False(bettingState.CanPlayerBet("NonExistent", new Money(100m)));
        Assert.False(bettingState.CanPlayerBet(null, new Money(100m)));
        Assert.False(bettingState.CanPlayerBet("", new Money(100m)));
    }

    [Fact]
    public void CanPlayerBet_WhenPlayerAlreadyBet_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act & Assert
        Assert.False(bettingState.CanPlayerBet("Alice", new Money(50m)));
    }

    [Fact]
    public void CanPlayerBet_WhenBettingComplete_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.ForceComplete();

        // Act & Assert
        Assert.False(bettingState.CanPlayerBet("Alice", new Money(100m)));
    }

    [Fact]
    public void GetPlayerBet_WithExistingBet_ReturnsBet()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        var betAmount = new Money(100m);
        bettingState.PlaceBet("Alice", betAmount);

        // Act
        var bet = bettingState.GetPlayerBet("Alice");

        // Assert
        Assert.NotNull(bet);
        Assert.Equal(betAmount, bet.Amount);
        Assert.Equal("Alice", bet.PlayerName);
    }

    [Fact]
    public void GetPlayerBet_WithNonExistentBet_ReturnsNull()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        var bet = bettingState.GetPlayerBet("Alice");

        // Assert
        Assert.Null(bet);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPlayerBet_WithInvalidPlayerName_ReturnsNull(string playerName)
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        var bet = bettingState.GetPlayerBet(playerName);

        // Assert
        Assert.Null(bet);
    }

    [Fact]
    public void GetPlayerBankroll_WithValidPlayer_ReturnsBankroll()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        var bankroll = bettingState.GetPlayerBankroll("Alice");

        // Assert
        Assert.Equal(new Money(1000m), bankroll);
    }

    [Fact]
    public void GetPlayerBankroll_WithNonExistentPlayer_ReturnsNull()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act
        var bankroll = bettingState.GetPlayerBankroll("NonExistent");

        // Assert
        Assert.Null(bankroll);
    }

    [Fact]
    public void GetPlayersWaitingToBet_ReturnsCorrectPlayers()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act
        var waitingPlayers = bettingState.GetPlayersWaitingToBet().ToList();

        // Assert
        Assert.Equal(2, waitingPlayers.Count);
        Assert.Contains("Bob", waitingPlayers);
        Assert.Contains("Charlie", waitingPlayers);
        Assert.DoesNotContain("Alice", waitingPlayers);
    }

    [Fact]
    public void GetPlayersWithBets_ReturnsCorrectPlayers()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));
        bettingState.PlaceBet("Bob", new Money(50m));

        // Act
        var playersWithBets = bettingState.GetPlayersWithBets().ToList();

        // Assert
        Assert.Equal(2, playersWithBets.Count);
        Assert.Contains("Alice", playersWithBets);
        Assert.Contains("Bob", playersWithBets);
        Assert.DoesNotContain("Charlie", playersWithBets);
    }

    [Fact]
    public void SkipCurrentPlayer_WithWaitingPlayer_SkipsToNextPlayer()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        Assert.Equal("Alice", bettingState.CurrentBettingPlayer);

        // Act
        var result = bettingState.SkipCurrentPlayer();

        // Assert
        Assert.True(result);
        Assert.Equal("Bob", bettingState.CurrentBettingPlayer);
        Assert.Equal(BettingPhase.WaitingForBets, bettingState.CurrentPhase);
    }

    [Fact]
    public void SkipCurrentPlayer_WhenBettingComplete_ThrowsInvalidOperationException()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.ForceComplete();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bettingState.SkipCurrentPlayer());
    }

    [Fact]
    public void ForceComplete_WhenWaitingForBets_CompletesRound()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act
        bettingState.ForceComplete();

        // Assert
        Assert.Equal(BettingPhase.Complete, bettingState.CurrentPhase);
        Assert.True(bettingState.IsComplete);
        Assert.Null(bettingState.CurrentBettingPlayer);
    }

    [Fact]
    public void ForceComplete_WhenAlreadyComplete_ThrowsInvalidOperationException()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.ForceComplete();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => bettingState.ForceComplete());
    }

    [Fact]
    public void Reset_WithUpdatedBankrolls_ResetsState()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));
        bettingState.PlaceBet("Bob", new Money(50m));
        bettingState.ForceComplete();

        var updatedBankrolls = new Dictionary<string, Money>
        {
            { "Alice", new Money(950m) },
            { "Bob", new Money(850m) },
            { "Charlie", new Money(1150m) }
        };

        // Act
        bettingState.Reset(updatedBankrolls);

        // Assert
        Assert.Equal(BettingPhase.WaitingForBets, bettingState.CurrentPhase);
        Assert.Equal(0, bettingState.CurrentBettingPlayerIndex);
        Assert.Equal("Alice", bettingState.CurrentBettingPlayer);
        Assert.False(bettingState.IsComplete);
        Assert.Empty(bettingState.PlayerBets);
        Assert.Equal(Money.Zero, bettingState.TotalWagered);
        Assert.Equal(new Money(950m), bettingState.GetPlayerBankroll("Alice"));
        Assert.Equal(new Money(850m), bettingState.GetPlayerBankroll("Bob"));
        Assert.Equal(new Money(1150m), bettingState.GetPlayerBankroll("Charlie"));
    }

    [Fact]
    public void Reset_WithNullBankrolls_ThrowsArgumentNullException()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => bettingState.Reset(null!));
    }

    [Fact]
    public void ElapsedTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        
        // Wait a small amount to ensure elapsed time is positive
        Thread.Sleep(10);

        // Act
        var elapsedTime = bettingState.ElapsedTime;

        // Assert
        Assert.True(elapsedTime.TotalMilliseconds > 0);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act
        var result = bettingState.ToString();

        // Assert
        Assert.Contains("WaitingForBets", result);
        Assert.Contains("1/3 players bet", result);
        Assert.Contains("Current: Bob", result);
        Assert.Contains("100.00 USD", result);
    }

    [Fact]
    public void ToDetailedString_ReturnsComprehensiveString()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState.PlaceBet("Alice", new Money(100m));

        // Act
        var result = bettingState.ToDetailedString();

        // Assert
        Assert.Contains("Betting State: WaitingForBets", result);
        Assert.Contains("Current Player: Bob", result);
        Assert.Contains("Total Wagered: 100.00 USD", result);
        Assert.Contains("Alice: Bet", result);
        Assert.Contains("Bob: Waiting", result);
        Assert.Contains("Charlie: Waiting", result);
    }

    [Fact]
    public void Equals_WithSameState_ReturnsTrue()
    {
        // Arrange
        var bettingState1 = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        var bettingState2 = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.True(bettingState1.Equals(bettingState2));
        Assert.Equal(bettingState1.GetHashCode(), bettingState2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentState_ReturnsFalse()
    {
        // Arrange
        var bettingState1 = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        var bettingState2 = new BettingState(_defaultPlayerNames, _defaultBankrolls);
        bettingState2.PlaceBet("Alice", new Money(100m));

        // Act & Assert
        Assert.False(bettingState1.Equals(bettingState2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.False(bettingState.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var bettingState = new BettingState(_defaultPlayerNames, _defaultBankrolls);

        // Act & Assert
        Assert.False(bettingState.Equals("not a betting state"));
    }
}