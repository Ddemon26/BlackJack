using System.ComponentModel.DataAnnotations;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Models;

/// <summary>
/// Configuration settings for the blackjack game.
/// </summary>
public class GameConfiguration
{
    /// <summary>
    /// Gets or sets the number of decks in the shoe.
    /// </summary>
    [Range(1, 8, ErrorMessage = "Number of decks must be between 1 and 8.")]
    public int NumberOfDecks { get; set; } = 6;

    /// <summary>
    /// Gets or sets the maximum number of players allowed in a game.
    /// </summary>
    [Range(1, 7, ErrorMessage = "Maximum players must be between 1 and 7.")]
    public int MaxPlayers { get; set; } = 4;

    /// <summary>
    /// Gets or sets the minimum number of players required to start a game.
    /// </summary>
    [Range(1, 7, ErrorMessage = "Minimum players must be between 1 and 7.")]
    public int MinPlayers { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether double down is allowed.
    /// </summary>
    public bool AllowDoubleDown { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether splitting pairs is allowed.
    /// </summary>
    public bool AllowSplit { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether surrender is allowed.
    /// </summary>
    public bool AllowSurrender { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether insurance is offered when dealer shows an Ace.
    /// </summary>
    public bool AllowInsurance { get; set; } = false;

    /// <summary>
    /// Gets or sets the penetration threshold for reshuffling the shoe (0.0 to 1.0).
    /// When the remaining cards fall below this percentage, the shoe should be reshuffled.
    /// </summary>
    [Range(0.1, 0.9, ErrorMessage = "Penetration threshold must be between 0.1 and 0.9.")]
    public double PenetrationThreshold { get; set; } = 0.25;

    /// <summary>
    /// Gets or sets the blackjack payout ratio (e.g., 1.5 for 3:2 payout).
    /// </summary>
    [Range(1.0, 2.0, ErrorMessage = "Blackjack payout must be between 1.0 and 2.0.")]
    public double BlackjackPayout { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets a value indicating whether the dealer hits on soft 17.
    /// </summary>
    public bool DealerHitsOnSoft17 { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum length allowed for player names.
    /// </summary>
    [Range(3, 50, ErrorMessage = "Player name max length must be between 3 and 50.")]
    public int PlayerNameMaxLength { get; set; } = 20;

    /// <summary>
    /// Gets or sets the minimum bet amount allowed.
    /// </summary>
    public Money MinimumBet { get; set; } = new Money(5m);

    /// <summary>
    /// Gets or sets the maximum bet amount allowed.
    /// </summary>
    public Money MaximumBet { get; set; } = new Money(500m);

    /// <summary>
    /// Gets or sets the default starting bankroll for new players.
    /// </summary>
    public Money DefaultBankroll { get; set; } = new Money(1000m);

    /// <summary>
    /// Gets or sets the minimum bankroll amount allowed.
    /// </summary>
    public Money MinimumBankroll { get; set; } = new Money(50m);

    /// <summary>
    /// Gets or sets the maximum bankroll amount allowed.
    /// </summary>
    public Money MaximumBankroll { get; set; } = new Money(10000m);

    /// <summary>
    /// Gets or sets a value indicating whether automatic reshuffling is enabled.
    /// </summary>
    public bool AutoReshuffleEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the card display format preference.
    /// </summary>
    public CardDisplayFormat CardDisplayFormat { get; set; } = CardDisplayFormat.Symbols;

    /// <summary>
    /// Gets or sets a value indicating whether to show detailed statistics.
    /// </summary>
    public bool ShowDetailedStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to save game statistics.
    /// </summary>
    public bool SaveStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to allow late surrender.
    /// </summary>
    public bool AllowLateSurrender { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to allow early surrender.
    /// </summary>
    public bool AllowEarlySurrender { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of hands a player can have after splitting.
    /// </summary>
    [Range(2, 4, ErrorMessage = "Maximum split hands must be between 2 and 4.")]
    public int MaxSplitHands { get; set; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether doubling after split is allowed.
    /// </summary>
    public bool AllowDoubleAfterSplit { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether resplitting Aces is allowed.
    /// </summary>
    public bool AllowResplitAces { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether hitting split Aces is allowed.
    /// </summary>
    public bool AllowHitSplitAces { get; set; } = false;

    /// <summary>
    /// Gets or sets the insurance payout ratio (e.g., 2.0 for 2:1 payout).
    /// </summary>
    [Range(1.5, 3.0, ErrorMessage = "Insurance payout must be between 1.5 and 3.0.")]
    public double InsurancePayout { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the surrender payout ratio (e.g., 0.5 for half bet back).
    /// </summary>
    [Range(0.25, 0.75, ErrorMessage = "Surrender payout must be between 0.25 and 0.75.")]
    public double SurrenderPayout { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets a value indicating whether to show card count hints (for educational purposes).
    /// </summary>
    public bool ShowCardCountHints { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to show basic strategy hints.
    /// </summary>
    public bool ShowBasicStrategyHints { get; set; } = false;

    /// <summary>
    /// Gets or sets the delay in milliseconds between card deals for dramatic effect.
    /// </summary>
    [Range(0, 5000, ErrorMessage = "Deal delay must be between 0 and 5000 milliseconds.")]
    public int DealDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets a value indicating whether to use sound effects.
    /// </summary>
    public bool EnableSoundEffects { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable debug mode with additional logging.
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the session timeout in minutes for automatic session ending.
    /// </summary>
    [Range(5, 480, ErrorMessage = "Session timeout must be between 5 and 480 minutes.")]
    public int SessionTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically save session progress.
    /// </summary>
    public bool AutoSaveSession { get; set; } = true;

    /// <summary>
    /// Gets or sets the frequency of auto-save in rounds.
    /// </summary>
    [Range(1, 50, ErrorMessage = "Auto-save frequency must be between 1 and 50 rounds.")]
    public int AutoSaveFrequency { get; set; } = 5;

    /// <summary>
    /// Validates the configuration and returns any validation errors.
    /// </summary>
    /// <returns>A collection of validation results, empty if configuration is valid.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);

        // Perform data annotation validation
        Validator.TryValidateObject(this, validationContext, validationResults, true);

        // Custom validation rules
        if (MinPlayers > MaxPlayers)
        {
            validationResults.Add(new ValidationResult(
                "Minimum players cannot be greater than maximum players.",
                new[] { nameof(MinPlayers), nameof(MaxPlayers) }));
        }

        if (NumberOfDecks > 1 && PenetrationThreshold > 0.8)
        {
            validationResults.Add(new ValidationResult(
                "Penetration threshold should be lower for multi-deck games to maintain game balance.",
                new[] { nameof(PenetrationThreshold) }));
        }

        // Betting validation rules
        if (MinimumBet >= MaximumBet)
        {
            validationResults.Add(new ValidationResult(
                "Minimum bet must be less than maximum bet.",
                new[] { nameof(MinimumBet), nameof(MaximumBet) }));
        }

        if (MinimumBankroll >= MaximumBankroll)
        {
            validationResults.Add(new ValidationResult(
                "Minimum bankroll must be less than maximum bankroll.",
                new[] { nameof(MinimumBankroll), nameof(MaximumBankroll) }));
        }

        if (DefaultBankroll < MinimumBankroll || DefaultBankroll > MaximumBankroll)
        {
            validationResults.Add(new ValidationResult(
                "Default bankroll must be within the minimum and maximum bankroll range.",
                new[] { nameof(DefaultBankroll) }));
        }

        if (MinimumBet.Currency != MaximumBet.Currency || 
            MinimumBet.Currency != DefaultBankroll.Currency ||
            MinimumBet.Currency != MinimumBankroll.Currency ||
            MinimumBet.Currency != MaximumBankroll.Currency)
        {
            validationResults.Add(new ValidationResult(
                "All monetary amounts must use the same currency.",
                new[] { nameof(MinimumBet), nameof(MaximumBet), nameof(DefaultBankroll), nameof(MinimumBankroll), nameof(MaximumBankroll) }));
        }

        // Advanced rule validation
        if (AllowEarlySurrender && AllowLateSurrender)
        {
            validationResults.Add(new ValidationResult(
                "Cannot enable both early and late surrender simultaneously.",
                new[] { nameof(AllowEarlySurrender), nameof(AllowLateSurrender) }));
        }

        if (AllowResplitAces && !AllowSplit)
        {
            validationResults.Add(new ValidationResult(
                "Cannot allow resplit Aces when splitting is disabled.",
                new[] { nameof(AllowResplitAces), nameof(AllowSplit) }));
        }

        if (AllowHitSplitAces && !AllowSplit)
        {
            validationResults.Add(new ValidationResult(
                "Cannot allow hitting split Aces when splitting is disabled.",
                new[] { nameof(AllowHitSplitAces), nameof(AllowSplit) }));
        }

        if (AllowDoubleAfterSplit && !AllowSplit)
        {
            validationResults.Add(new ValidationResult(
                "Cannot allow double after split when splitting is disabled.",
                new[] { nameof(AllowDoubleAfterSplit), nameof(AllowSplit) }));
        }

        if ((AllowEarlySurrender || AllowLateSurrender) && !AllowSurrender)
        {
            validationResults.Add(new ValidationResult(
                "Cannot enable surrender variations when surrender is disabled.",
                new[] { nameof(AllowSurrender), nameof(AllowEarlySurrender), nameof(AllowLateSurrender) }));
        }

        if (ShowCardCountHints && NumberOfDecks == 1)
        {
            validationResults.Add(new ValidationResult(
                "Card counting hints are not meaningful with single deck games.",
                new[] { nameof(ShowCardCountHints), nameof(NumberOfDecks) }));
        }

        return validationResults;
    }

    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid => !Validate().Any();

    /// <summary>
    /// Creates a copy of the current configuration.
    /// </summary>
    /// <returns>A new GameConfiguration instance with the same values.</returns>
    public GameConfiguration Clone()
    {
        return new GameConfiguration
        {
            NumberOfDecks = NumberOfDecks,
            MaxPlayers = MaxPlayers,
            MinPlayers = MinPlayers,
            AllowDoubleDown = AllowDoubleDown,
            AllowSplit = AllowSplit,
            AllowSurrender = AllowSurrender,
            AllowInsurance = AllowInsurance,
            PenetrationThreshold = PenetrationThreshold,
            BlackjackPayout = BlackjackPayout,
            DealerHitsOnSoft17 = DealerHitsOnSoft17,
            PlayerNameMaxLength = PlayerNameMaxLength,
            MinimumBet = MinimumBet,
            MaximumBet = MaximumBet,
            DefaultBankroll = DefaultBankroll,
            MinimumBankroll = MinimumBankroll,
            MaximumBankroll = MaximumBankroll,
            AutoReshuffleEnabled = AutoReshuffleEnabled,
            CardDisplayFormat = CardDisplayFormat,
            ShowDetailedStatistics = ShowDetailedStatistics,
            SaveStatistics = SaveStatistics,
            AllowLateSurrender = AllowLateSurrender,
            AllowEarlySurrender = AllowEarlySurrender,
            MaxSplitHands = MaxSplitHands,
            AllowDoubleAfterSplit = AllowDoubleAfterSplit,
            AllowResplitAces = AllowResplitAces,
            AllowHitSplitAces = AllowHitSplitAces,
            InsurancePayout = InsurancePayout,
            SurrenderPayout = SurrenderPayout,
            ShowCardCountHints = ShowCardCountHints,
            ShowBasicStrategyHints = ShowBasicStrategyHints,
            DealDelayMs = DealDelayMs,
            EnableSoundEffects = EnableSoundEffects,
            DebugMode = DebugMode,
            SessionTimeoutMinutes = SessionTimeoutMinutes,
            AutoSaveSession = AutoSaveSession,
            AutoSaveFrequency = AutoSaveFrequency
        };
    }

    /// <summary>
    /// Returns a string representation of the configuration.
    /// </summary>
    /// <returns>A formatted string describing the configuration.</returns>
    public override string ToString()
    {
        return $"GameConfiguration: {NumberOfDecks} decks, {MinPlayers}-{MaxPlayers} players, " +
               $"DoubleDown: {AllowDoubleDown}, Split: {AllowSplit}, Surrender: {AllowSurrender}, " +
               $"Insurance: {AllowInsurance}, DealerSoft17: {DealerHitsOnSoft17}, " +
               $"Penetration: {PenetrationThreshold:P1}, Payout: {BlackjackPayout:F1}:1";
    }
}