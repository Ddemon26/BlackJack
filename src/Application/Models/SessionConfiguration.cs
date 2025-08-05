using GroupProject.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace GroupProject.Application.Models;

/// <summary>
/// Configuration settings for a blackjack gaming session including player setup, bankrolls, and betting limits.
/// </summary>
/// <remarks>
/// This model defines all the parameters needed to configure a multi-round gaming session,
/// including player management, financial constraints, and game rule variations.
/// It provides comprehensive validation to ensure all settings are within acceptable ranges.
/// </remarks>
public class SessionConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionConfiguration"/> class with default values.
    /// </summary>
    public SessionConfiguration()
    {
        PlayerNames = new List<string>();
        DefaultBankroll = new Money(1000m);
        MinimumBet = new Money(10m);
        MaximumBet = new Money(500m);
        GameRules = new GameConfiguration();
        EnableStatistics = true;
        AllowRebuy = true;
        MaxRebuyAmount = new Money(1000m);
        MaxRebuysPerSession = 3;
        SessionTimeoutMinutes = 120;
        AutoSaveInterval = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionConfiguration"/> class with specified values.
    /// </summary>
    /// <param name="playerNames">The names of players to include in the session.</param>
    /// <param name="defaultBankroll">The default bankroll for each player.</param>
    /// <param name="minimumBet">The minimum bet amount allowed.</param>
    /// <param name="maximumBet">The maximum bet amount allowed.</param>
    /// <param name="gameRules">The game configuration rules.</param>
    /// <exception cref="ArgumentNullException">Thrown when playerNames or gameRules is null.</exception>
    /// <exception cref="ArgumentException">Thrown when playerNames is empty or contains invalid names.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when monetary amounts are invalid.</exception>
    public SessionConfiguration(
        IEnumerable<string> playerNames,
        Money defaultBankroll,
        Money minimumBet,
        Money maximumBet,
        GameConfiguration gameRules)
    {
        if (playerNames == null)
            throw new ArgumentNullException(nameof(playerNames));

        if (gameRules == null)
            throw new ArgumentNullException(nameof(gameRules));

        var playerNamesList = playerNames.ToList();
        if (!playerNamesList.Any())
            throw new ArgumentException("At least one player name must be provided.", nameof(playerNames));

        if (playerNamesList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Player names cannot be null or empty.", nameof(playerNames));

        if (defaultBankroll.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(defaultBankroll), "Default bankroll cannot be negative.");

        if (!minimumBet.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(minimumBet), "Minimum bet must be positive.");

        if (!maximumBet.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(maximumBet), "Maximum bet must be positive.");

        if (minimumBet >= maximumBet)
            throw new ArgumentOutOfRangeException(nameof(maximumBet), "Maximum bet must be greater than minimum bet.");

        PlayerNames = playerNamesList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        DefaultBankroll = defaultBankroll;
        MinimumBet = minimumBet;
        MaximumBet = maximumBet;
        GameRules = gameRules;
        EnableStatistics = true;
        AllowRebuy = true;
        MaxRebuyAmount = defaultBankroll;
        MaxRebuysPerSession = 3;
        SessionTimeoutMinutes = 120;
        AutoSaveInterval = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Gets or sets the names of players to include in the session.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one player name must be provided.")]
    public IReadOnlyList<string> PlayerNames { get; set; }

    /// <summary>
    /// Gets or sets the default bankroll for each player.
    /// </summary>
    [Required]
    public Money DefaultBankroll { get; set; }

    /// <summary>
    /// Gets or sets the minimum bet amount allowed in the session.
    /// </summary>
    [Required]
    public Money MinimumBet { get; set; }

    /// <summary>
    /// Gets or sets the maximum bet amount allowed in the session.
    /// </summary>
    [Required]
    public Money MaximumBet { get; set; }

    /// <summary>
    /// Gets or sets the game configuration rules for the session.
    /// </summary>
    [Required]
    public GameConfiguration GameRules { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether statistics tracking is enabled for the session.
    /// </summary>
    public bool EnableStatistics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether players can rebuy chips during the session.
    /// </summary>
    public bool AllowRebuy { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount a player can rebuy at one time.
    /// </summary>
    public Money MaxRebuyAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of rebuys allowed per player per session.
    /// </summary>
    [Range(0, 10, ErrorMessage = "Maximum rebuys per session must be between 0 and 10.")]
    public int MaxRebuysPerSession { get; set; }

    /// <summary>
    /// Gets or sets the session timeout in minutes after which inactive sessions are automatically ended.
    /// </summary>
    [Range(30, 480, ErrorMessage = "Session timeout must be between 30 and 480 minutes.")]
    public int SessionTimeoutMinutes { get; set; }

    /// <summary>
    /// Gets or sets the interval at which session state is automatically saved.
    /// </summary>
    public TimeSpan AutoSaveInterval { get; set; }

    /// <summary>
    /// Gets or sets custom betting limits for specific players.
    /// </summary>
    public IReadOnlyDictionary<string, (Money Min, Money Max)> PlayerBettingLimits { get; set; } = 
        new Dictionary<string, (Money Min, Money Max)>();

    /// <summary>
    /// Gets or sets the session name or description.
    /// </summary>
    [StringLength(100, ErrorMessage = "Session name cannot exceed 100 characters.")]
    public string? SessionName { get; set; }

    /// <summary>
    /// Gets or sets additional session metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = 
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the effective minimum bet for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The minimum bet amount for the player.</returns>
    public Money GetMinimumBetForPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return MinimumBet;

        if (PlayerBettingLimits.TryGetValue(playerName, out var limits))
            return limits.Min;

        return MinimumBet;
    }

    /// <summary>
    /// Gets the effective maximum bet for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The maximum bet amount for the player.</returns>
    public Money GetMaximumBetForPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return MaximumBet;

        if (PlayerBettingLimits.TryGetValue(playerName, out var limits))
            return limits.Max;

        return MaximumBet;
    }

    /// <summary>
    /// Sets custom betting limits for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="minimumBet">The minimum bet for the player.</param>
    /// <param name="maximumBet">The maximum bet for the player.</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when bet amounts are invalid.</exception>
    public void SetPlayerBettingLimits(string playerName, Money minimumBet, Money maximumBet)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (!minimumBet.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(minimumBet), "Minimum bet must be positive.");

        if (!maximumBet.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(maximumBet), "Maximum bet must be positive.");

        if (minimumBet >= maximumBet)
            throw new ArgumentOutOfRangeException(nameof(maximumBet), "Maximum bet must be greater than minimum bet.");

        var limits = new Dictionary<string, (Money Min, Money Max)>(PlayerBettingLimits, StringComparer.OrdinalIgnoreCase)
        {
            [playerName] = (minimumBet, maximumBet)
        };

        PlayerBettingLimits = limits;
    }

    /// <summary>
    /// Removes custom betting limits for a specific player, reverting to session defaults.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    public void RemovePlayerBettingLimits(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (PlayerBettingLimits.ContainsKey(playerName))
        {
            var limits = new Dictionary<string, (Money Min, Money Max)>(PlayerBettingLimits, StringComparer.OrdinalIgnoreCase);
            limits.Remove(playerName);
            PlayerBettingLimits = limits;
        }
    }

    /// <summary>
    /// Validates the session configuration and returns any validation errors.
    /// </summary>
    /// <returns>A collection of validation results, empty if configuration is valid.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);

        // Perform data annotation validation
        Validator.TryValidateObject(this, validationContext, validationResults, true);

        // Custom validation rules
        if (!PlayerNames.Any())
        {
            validationResults.Add(new ValidationResult(
                "At least one player name must be provided.",
                new[] { nameof(PlayerNames) }));
        }

        if (PlayerNames.Any(string.IsNullOrWhiteSpace))
        {
            validationResults.Add(new ValidationResult(
                "Player names cannot be null or empty.",
                new[] { nameof(PlayerNames) }));
        }

        if (PlayerNames.Count != PlayerNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            validationResults.Add(new ValidationResult(
                "Player names must be unique (case-insensitive).",
                new[] { nameof(PlayerNames) }));
        }

        if (DefaultBankroll.IsNegative)
        {
            validationResults.Add(new ValidationResult(
                "Default bankroll cannot be negative.",
                new[] { nameof(DefaultBankroll) }));
        }

        if (!MinimumBet.IsPositive)
        {
            validationResults.Add(new ValidationResult(
                "Minimum bet must be positive.",
                new[] { nameof(MinimumBet) }));
        }

        if (!MaximumBet.IsPositive)
        {
            validationResults.Add(new ValidationResult(
                "Maximum bet must be positive.",
                new[] { nameof(MaximumBet) }));
        }

        if (MinimumBet >= MaximumBet)
        {
            validationResults.Add(new ValidationResult(
                "Maximum bet must be greater than minimum bet.",
                new[] { nameof(MinimumBet), nameof(MaximumBet) }));
        }

        if (MaxRebuyAmount.IsNegative)
        {
            validationResults.Add(new ValidationResult(
                "Maximum rebuy amount cannot be negative.",
                new[] { nameof(MaxRebuyAmount) }));
        }

        if (AutoSaveInterval.TotalMinutes < 1 || AutoSaveInterval.TotalMinutes > 60)
        {
            validationResults.Add(new ValidationResult(
                "Auto-save interval must be between 1 and 60 minutes.",
                new[] { nameof(AutoSaveInterval) }));
        }

        // Validate game rules
        var gameRulesValidation = GameRules?.Validate();
        if (gameRulesValidation != null)
        {
            validationResults.AddRange(gameRulesValidation);
        }

        // Validate player betting limits
        foreach (var (playerName, limits) in PlayerBettingLimits)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                validationResults.Add(new ValidationResult(
                    "Player betting limit keys cannot be null or empty.",
                    new[] { nameof(PlayerBettingLimits) }));
                continue;
            }

            if (!limits.Min.IsPositive)
            {
                validationResults.Add(new ValidationResult(
                    $"Player {playerName} minimum bet must be positive.",
                    new[] { nameof(PlayerBettingLimits) }));
            }

            if (!limits.Max.IsPositive)
            {
                validationResults.Add(new ValidationResult(
                    $"Player {playerName} maximum bet must be positive.",
                    new[] { nameof(PlayerBettingLimits) }));
            }

            if (limits.Min >= limits.Max)
            {
                validationResults.Add(new ValidationResult(
                    $"Player {playerName} maximum bet must be greater than minimum bet.",
                    new[] { nameof(PlayerBettingLimits) }));
            }
        }

        return validationResults;
    }

    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid => !Validate().Any();

    /// <summary>
    /// Creates a copy of the current session configuration.
    /// </summary>
    /// <returns>A new SessionConfiguration instance with the same values.</returns>
    public SessionConfiguration Clone()
    {
        var clone = new SessionConfiguration(PlayerNames, DefaultBankroll, MinimumBet, MaximumBet, GameRules.Clone())
        {
            EnableStatistics = EnableStatistics,
            AllowRebuy = AllowRebuy,
            MaxRebuyAmount = MaxRebuyAmount,
            MaxRebuysPerSession = MaxRebuysPerSession,
            SessionTimeoutMinutes = SessionTimeoutMinutes,
            AutoSaveInterval = AutoSaveInterval,
            SessionName = SessionName,
            PlayerBettingLimits = new Dictionary<string, (Money Min, Money Max)>(PlayerBettingLimits),
            Metadata = new Dictionary<string, string>(Metadata)
        };

        return clone;
    }

    /// <summary>
    /// Returns a string representation of the session configuration.
    /// </summary>
    /// <returns>A formatted string describing the configuration.</returns>
    public override string ToString()
    {
        var playerCount = PlayerNames.Count;
        var sessionNamePart = !string.IsNullOrEmpty(SessionName) ? $" '{SessionName}'" : "";
        
        return $"SessionConfiguration{sessionNamePart}: {playerCount} players, " +
               $"Bankroll: {DefaultBankroll}, Bets: {MinimumBet}-{MaximumBet}, " +
               $"Statistics: {EnableStatistics}, Rebuy: {AllowRebuy}";
    }

    /// <summary>
    /// Determines equality based on all configuration values.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not SessionConfiguration other)
            return false;

        return PlayerNames.SequenceEqual(other.PlayerNames, StringComparer.OrdinalIgnoreCase) &&
               DefaultBankroll.Equals(other.DefaultBankroll) &&
               MinimumBet.Equals(other.MinimumBet) &&
               MaximumBet.Equals(other.MaximumBet) &&
               GameRules.Equals(other.GameRules) &&
               EnableStatistics == other.EnableStatistics &&
               AllowRebuy == other.AllowRebuy &&
               MaxRebuyAmount.Equals(other.MaxRebuyAmount) &&
               MaxRebuysPerSession == other.MaxRebuysPerSession &&
               SessionTimeoutMinutes == other.SessionTimeoutMinutes &&
               AutoSaveInterval.Equals(other.AutoSaveInterval) &&
               string.Equals(SessionName, other.SessionName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the hash code based on key configuration values.
    /// </summary>
    /// <returns>The hash code for this session configuration.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        
        foreach (var playerName in PlayerNames)
        {
            hash.Add(playerName.ToLowerInvariant());
        }
        
        hash.Add(DefaultBankroll);
        hash.Add(MinimumBet);
        hash.Add(MaximumBet);
        hash.Add(GameRules);
        hash.Add(EnableStatistics);
        hash.Add(AllowRebuy);
        hash.Add(MaxRebuyAmount);
        hash.Add(MaxRebuysPerSession);
        hash.Add(SessionTimeoutMinutes);
        hash.Add(AutoSaveInterval);
        hash.Add(SessionName?.ToLowerInvariant());
        
        return hash.ToHashCode();
    }
}