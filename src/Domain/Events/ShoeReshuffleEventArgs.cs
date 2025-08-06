namespace GroupProject.Domain.Events;

/// <summary>
/// Event arguments for shoe reshuffle events.
/// </summary>
public class ShoeReshuffleEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShoeReshuffleEventArgs"/> class.
    /// </summary>
    /// <param name="remainingPercentage">The percentage of cards remaining when reshuffle was triggered.</param>
    /// <param name="penetrationThreshold">The penetration threshold that triggered the reshuffle.</param>
    /// <param name="reason">The reason for the reshuffle.</param>
    public ShoeReshuffleEventArgs(double remainingPercentage, double penetrationThreshold, string reason)
    {
        RemainingPercentage = remainingPercentage;
        PenetrationThreshold = penetrationThreshold;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the percentage of cards remaining when reshuffle was triggered.
    /// </summary>
    public double RemainingPercentage { get; }

    /// <summary>
    /// Gets the penetration threshold that triggered the reshuffle.
    /// </summary>
    public double PenetrationThreshold { get; }

    /// <summary>
    /// Gets the reason for the reshuffle.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets the timestamp when the reshuffle event occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Returns a string representation of the reshuffle event.
    /// </summary>
    /// <returns>A string describing the reshuffle event.</returns>
    public override string ToString()
    {
        return $"Shoe reshuffle at {Timestamp:HH:mm:ss} - {Reason} (Remaining: {RemainingPercentage:P1}, Threshold: {PenetrationThreshold:P1})";
    }
}