using System.ComponentModel.DataAnnotations;
using GroupProject.Application.Models;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Application.Models;

/// <summary>
/// Tests for the extended GameConfiguration class features.
/// </summary>
public class ExtendedGameConfigurationTests
{
    [Fact]
    public void GameConfiguration_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new GameConfiguration();

        // Assert
        Assert.True(config.IsValid);
        Assert.False(config.AllowLateSurrender);
        Assert.False(config.AllowEarlySurrender);
        Assert.Equal(4, config.MaxSplitHands);
        Assert.False(config.AllowDoubleAfterSplit);
        Assert.False(config.AllowResplitAces);
        Assert.False(config.AllowHitSplitAces);
        Assert.Equal(2.0, config.InsurancePayout);
        Assert.Equal(0.5, config.SurrenderPayout);
        Assert.False(config.ShowCardCountHints);
        Assert.False(config.ShowBasicStrategyHints);
        Assert.Equal(500, config.DealDelayMs);
        Assert.False(config.EnableSoundEffects);
        Assert.False(config.DebugMode);
        Assert.Equal(60, config.SessionTimeoutMinutes);
        Assert.True(config.AutoSaveSession);
        Assert.Equal(5, config.AutoSaveFrequency);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void MaxSplitHands_ValidValues_ShouldBeValid(int maxSplitHands)
    {
        // Arrange
        var config = new GameConfiguration { MaxSplitHands = maxSplitHands };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxSplitHands_InvalidValues_ShouldBeInvalid(int maxSplitHands)
    {
        // Arrange
        var config = new GameConfiguration { MaxSplitHands = maxSplitHands };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Maximum split hands"));
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(2.5)]
    [InlineData(3.0)]
    public void InsurancePayout_ValidValues_ShouldBeValid(double insurancePayout)
    {
        // Arrange
        var config = new GameConfiguration { InsurancePayout = insurancePayout };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(3.5)]
    [InlineData(5.0)]
    public void InsurancePayout_InvalidValues_ShouldBeInvalid(double insurancePayout)
    {
        // Arrange
        var config = new GameConfiguration { InsurancePayout = insurancePayout };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Insurance payout"));
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void SurrenderPayout_ValidValues_ShouldBeValid(double surrenderPayout)
    {
        // Arrange
        var config = new GameConfiguration { SurrenderPayout = surrenderPayout };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.8)]
    [InlineData(1.0)]
    public void SurrenderPayout_InvalidValues_ShouldBeInvalid(double surrenderPayout)
    {
        // Arrange
        var config = new GameConfiguration { SurrenderPayout = surrenderPayout };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Surrender payout"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void DealDelayMs_ValidValues_ShouldBeValid(int dealDelayMs)
    {
        // Arrange
        var config = new GameConfiguration { DealDelayMs = dealDelayMs };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5001)]
    [InlineData(10000)]
    public void DealDelayMs_InvalidValues_ShouldBeInvalid(int dealDelayMs)
    {
        // Arrange
        var config = new GameConfiguration { DealDelayMs = dealDelayMs };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Deal delay"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(480)]
    public void SessionTimeoutMinutes_ValidValues_ShouldBeValid(int sessionTimeoutMinutes)
    {
        // Arrange
        var config = new GameConfiguration { SessionTimeoutMinutes = sessionTimeoutMinutes };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(481)]
    [InlineData(1000)]
    public void SessionTimeoutMinutes_InvalidValues_ShouldBeInvalid(int sessionTimeoutMinutes)
    {
        // Arrange
        var config = new GameConfiguration { SessionTimeoutMinutes = sessionTimeoutMinutes };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Session timeout"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(50)]
    public void AutoSaveFrequency_ValidValues_ShouldBeValid(int autoSaveFrequency)
    {
        // Arrange
        var config = new GameConfiguration { AutoSaveFrequency = autoSaveFrequency };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(100)]
    public void AutoSaveFrequency_InvalidValues_ShouldBeInvalid(int autoSaveFrequency)
    {
        // Arrange
        var config = new GameConfiguration { AutoSaveFrequency = autoSaveFrequency };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Auto-save frequency"));
    }

    [Fact]
    public void Validate_BothEarlyAndLateSurrenderEnabled_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowEarlySurrender = true,
            AllowLateSurrender = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Cannot enable both early and late surrender"));
    }

    [Fact]
    public void Validate_ResplitAcesWithoutSplit_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowSplit = false,
            AllowResplitAces = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Cannot allow resplit Aces when splitting is disabled"));
    }

    [Fact]
    public void Validate_HitSplitAcesWithoutSplit_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowSplit = false,
            AllowHitSplitAces = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Cannot allow hitting split Aces when splitting is disabled"));
    }

    [Fact]
    public void Validate_DoubleAfterSplitWithoutSplit_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowSplit = false,
            AllowDoubleAfterSplit = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Cannot allow double after split when splitting is disabled"));
    }

    [Fact]
    public void Validate_SurrenderVariationsWithoutSurrender_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowSurrender = false,
            AllowEarlySurrender = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Cannot enable surrender variations when surrender is disabled"));
    }

    [Fact]
    public void Validate_CardCountHintsWithSingleDeck_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 1,
            ShowCardCountHints = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Card counting hints are not meaningful with single deck games"));
    }

    [Fact]
    public void Clone_WithExtendedProperties_ShouldCopyAllValues()
    {
        // Arrange
        var original = new GameConfiguration
        {
            NumberOfDecks = 8,
            AllowLateSurrender = true,
            MaxSplitHands = 3,
            AllowDoubleAfterSplit = false,
            AllowResplitAces = true,
            AllowHitSplitAces = true,
            InsurancePayout = 2.5,
            SurrenderPayout = 0.6,
            ShowCardCountHints = true,
            ShowBasicStrategyHints = true,
            DealDelayMs = 1000,
            EnableSoundEffects = true,
            DebugMode = true,
            SessionTimeoutMinutes = 120,
            AutoSaveSession = false,
            AutoSaveFrequency = 10
        };

        // Act
        var cloned = original.Clone();

        // Assert
        Assert.Equal(original.NumberOfDecks, cloned.NumberOfDecks);
        Assert.Equal(original.AllowLateSurrender, cloned.AllowLateSurrender);
        Assert.Equal(original.MaxSplitHands, cloned.MaxSplitHands);
        Assert.Equal(original.AllowDoubleAfterSplit, cloned.AllowDoubleAfterSplit);
        Assert.Equal(original.AllowResplitAces, cloned.AllowResplitAces);
        Assert.Equal(original.AllowHitSplitAces, cloned.AllowHitSplitAces);
        Assert.Equal(original.InsurancePayout, cloned.InsurancePayout);
        Assert.Equal(original.SurrenderPayout, cloned.SurrenderPayout);
        Assert.Equal(original.ShowCardCountHints, cloned.ShowCardCountHints);
        Assert.Equal(original.ShowBasicStrategyHints, cloned.ShowBasicStrategyHints);
        Assert.Equal(original.DealDelayMs, cloned.DealDelayMs);
        Assert.Equal(original.EnableSoundEffects, cloned.EnableSoundEffects);
        Assert.Equal(original.DebugMode, cloned.DebugMode);
        Assert.Equal(original.SessionTimeoutMinutes, cloned.SessionTimeoutMinutes);
        Assert.Equal(original.AutoSaveSession, cloned.AutoSaveSession);
        Assert.Equal(original.AutoSaveFrequency, cloned.AutoSaveFrequency);
        
        // Ensure it's a different instance
        Assert.NotSame(original, cloned);
    }

    [Fact]
    public void ToString_WithExtendedProperties_ShouldIncludeKeySettings()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MinPlayers = 1,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true,
            AllowSurrender = true,
            AllowInsurance = true,
            DealerHitsOnSoft17 = true,
            PenetrationThreshold = 0.25,
            BlackjackPayout = 1.5
        };

        // Act
        var result = config.ToString();

        // Assert
        Assert.Contains("6 decks", result);
        Assert.Contains("1-4 players", result);
        Assert.Contains("DoubleDown: True", result);
        Assert.Contains("Split: True", result);
        Assert.Contains("Surrender: True", result);
        Assert.Contains("Insurance: True", result);
        Assert.Contains("DealerSoft17: True", result);
        Assert.Contains("Penetration: 25.0%", result);
        Assert.Contains("Payout: 1.5:1", result);
    }

    [Fact]
    public void ExtendedConfiguration_ComplexValidScenario_ShouldBeValid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 7,
            MinPlayers = 1,
            AllowDoubleDown = true,
            AllowSplit = true,
            AllowSurrender = true,
            AllowInsurance = true,
            AllowLateSurrender = true,
            MaxSplitHands = 4,
            AllowDoubleAfterSplit = true,
            AllowResplitAces = true,
            AllowHitSplitAces = false,
            InsurancePayout = 2.0,
            SurrenderPayout = 0.5,
            ShowCardCountHints = true,
            ShowBasicStrategyHints = true,
            DealDelayMs = 750,
            EnableSoundEffects = true,
            DebugMode = false,
            SessionTimeoutMinutes = 90,
            AutoSaveSession = true,
            AutoSaveFrequency = 3,
            MinimumBet = new Money(10m),
            MaximumBet = new Money(1000m),
            DefaultBankroll = new Money(2000m),
            MinimumBankroll = new Money(100m),
            MaximumBankroll = new Money(50000m)
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
        Assert.True(config.IsValid);
    }

    [Fact]
    public void ExtendedConfiguration_ComplexInvalidScenario_ShouldBeInvalid()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 1,
            AllowSplit = false,
            AllowSurrender = false,
            AllowEarlySurrender = true,
            AllowLateSurrender = true,
            AllowResplitAces = true,
            AllowHitSplitAces = true,
            AllowDoubleAfterSplit = true,
            ShowCardCountHints = true,
            MaxSplitHands = 10,
            InsurancePayout = 5.0,
            SurrenderPayout = 1.0,
            DealDelayMs = 10000,
            SessionTimeoutMinutes = 1000,
            AutoSaveFrequency = 100
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.False(config.IsValid);
        
        // Should have multiple validation errors
        var errorMessages = validationResults.Select(vr => vr.ErrorMessage).ToList();
        Assert.Contains(errorMessages, msg => msg!.Contains("both early and late surrender"));
        Assert.Contains(errorMessages, msg => msg!.Contains("resplit Aces when splitting is disabled"));
        Assert.Contains(errorMessages, msg => msg!.Contains("hitting split Aces when splitting is disabled"));
        Assert.Contains(errorMessages, msg => msg!.Contains("double after split when splitting is disabled"));
        Assert.Contains(errorMessages, msg => msg!.Contains("surrender variations when surrender is disabled"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Card counting hints are not meaningful"));
    }
}