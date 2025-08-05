namespace GroupProject.Application.Interfaces;

/// <summary>
/// Defines the contract for managing complete game sessions and flow control.
/// </summary>
public interface IGameOrchestrator
{
    /// <summary>
    /// Runs a complete game session from start to finish.
    /// </summary>
    /// <returns>A task representing the asynchronous game operation.</returns>
    Task RunGameAsync();

    /// <summary>
    /// Prompts the user to determine if they want to play another round.
    /// </summary>
    /// <returns>A task that returns true if the user wants to play another round, false otherwise.</returns>
    Task<bool> ShouldPlayAnotherRoundAsync();

    /// <summary>
    /// Runs multiple game rounds until the user chooses to stop.
    /// </summary>
    /// <returns>A task representing the asynchronous multi-round game operation.</returns>
    Task RunMultipleRoundsAsync();

    /// <summary>
    /// Gets the number of players for a new game session.
    /// </summary>
    /// <returns>A task that returns the number of players.</returns>
    Task<int> GetPlayerCountAsync();

    /// <summary>
    /// Gets the names of players for a new game session.
    /// </summary>
    /// <param name="playerCount">The number of players to get names for.</param>
    /// <returns>A task that returns the collection of player names.</returns>
    Task<IEnumerable<string>> GetPlayerNamesAsync(int playerCount);
}