using GroupProject.Domain.Events;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for managing shoe operations and automatic reshuffling.
/// </summary>
public interface IShoeManager
{
    /// <summary>
    /// Gets the current shoe instance.
    /// </summary>
    IShoe CurrentShoe { get; }

    /// <summary>
    /// Gets a value indicating whether automatic reshuffling is enabled.
    /// </summary>
    bool AutoReshuffleEnabled { get; set; }

    /// <summary>
    /// Gets or sets the penetration threshold for automatic reshuffling.
    /// </summary>
    double PenetrationThreshold { get; set; }

    /// <summary>
    /// Initializes the shoe manager with a shoe instance.
    /// </summary>
    /// <param name="shoe">The shoe to manage.</param>
    void Initialize(IShoe shoe);

    /// <summary>
    /// Handles automatic reshuffling when needed.
    /// </summary>
    /// <returns>True if a reshuffle occurred, false otherwise.</returns>
    bool HandleAutomaticReshuffle();

    /// <summary>
    /// Manually triggers a reshuffle.
    /// </summary>
    /// <param name="reason">The reason for the manual reshuffle.</param>
    void TriggerManualReshuffle(string reason = "Manual reshuffle");

    /// <summary>
    /// Checks if the shoe needs reshuffling.
    /// </summary>
    /// <returns>True if reshuffling is needed, false otherwise.</returns>
    bool IsReshuffleNeeded();

    /// <summary>
    /// Event raised when a reshuffle occurs.
    /// </summary>
    event EventHandler<ShoeReshuffleEventArgs>? ReshuffleOccurred;

    /// <summary>
    /// Event raised when a reshuffle is needed but not automatically handled.
    /// </summary>
    event EventHandler<ShoeReshuffleEventArgs>? ReshuffleRequired;
}