using GroupProject.Domain.Entities;

namespace GroupProject.Application.Models;

/// <summary>
/// Represents the current state of a blackjack game.
/// </summary>
public class GameState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameState"/> class.
    /// </summary>
    /// <param name="players">The players in the game.</param>
    /// <param name="dealer">The dealer.</param>
    /// <param name="currentPhase">The current phase of the game.</param>
    /// <param name="currentPlayerName">The name of the current player (if applicable).</param>
    public GameState(
        IReadOnlyList<Player> players,
        Player dealer,
        GamePhase currentPhase,
        string? currentPlayerName = null)
    {
        Players = players ?? throw new ArgumentNullException(nameof(players));
        Dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
        CurrentPhase = currentPhase;
        CurrentPlayerName = currentPlayerName;
    }

    /// <summary>
    /// Gets the players in the game.
    /// </summary>
    public IReadOnlyList<Player> Players { get; }

    /// <summary>
    /// Gets the dealer.
    /// </summary>
    public Player Dealer { get; }

    /// <summary>
    /// Gets the current phase of the game.
    /// </summary>
    public GamePhase CurrentPhase { get; }

    /// <summary>
    /// Gets the name of the current player (if applicable).
    /// </summary>
    public string? CurrentPlayerName { get; }
}

/// <summary>
/// Represents the different phases of a blackjack game.
/// </summary>
public enum GamePhase
{
    /// <summary>
    /// Game is being set up with players
    /// </summary>
    Setup,

    /// <summary>
    /// Players are placing their bets
    /// </summary>
    Betting,

    /// <summary>
    /// Initial cards are being dealt
    /// </summary>
    InitialDeal,

    /// <summary>
    /// Players are taking their turns
    /// </summary>
    PlayerTurns,

    /// <summary>
    /// Dealer is taking their turn
    /// </summary>
    DealerTurn,

    /// <summary>
    /// Game results are being calculated
    /// </summary>
    Results,

    /// <summary>
    /// Game is over
    /// </summary>
    GameOver
}