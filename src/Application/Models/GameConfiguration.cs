using System.ComponentModel.DataAnnotations;

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
            PlayerNameMaxLength = PlayerNameMaxLength
        };
    }

    /// <summary>
    /// Returns a string representation of the configuration.
    /// </summary>
    /// <returns>A formatted string describing the configuration.</returns>
    public override string ToString()
    {
        return $"GameConfiguration: {NumberOfDecks} decks, {MinPlayers}-{MaxPlayers} players, " +
               $"DoubleDown: {AllowDoubleDown}, Split: {AllowSplit}, " +
               $"Penetration: {PenetrationThreshold:P1}, Payout: {BlackjackPayout:F1}:1";
    }
}