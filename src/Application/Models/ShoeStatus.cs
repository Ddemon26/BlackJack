namespace GroupProject.Application.Models;

/// <summary>
/// Represents the current status of the shoe.
/// </summary>
public class ShoeStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShoeStatus"/> class.
    /// </summary>
    /// <param name="deckCount">The number of decks in the shoe.</param>
    /// <param name="remainingCards">The number of cards remaining in the shoe.</param>
    /// <param name="remainingPercentage">The percentage of cards remaining.</param>
    /// <param name="penetrationThreshold">The current penetration threshold.</param>
    /// <param name="needsReshuffle">Whether the shoe needs reshuffling.</param>
    /// <param name="autoReshuffleEnabled">Whether automatic reshuffling is enabled.</param>
    public ShoeStatus(
        int deckCount,
        int remainingCards,
        double remainingPercentage,
        double penetrationThreshold,
        bool needsReshuffle,
        bool autoReshuffleEnabled)
    {
        DeckCount = deckCount;
        RemainingCards = remainingCards;
        RemainingPercentage = remainingPercentage;
        PenetrationThreshold = penetrationThreshold;
        NeedsReshuffle = needsReshuffle;
        AutoReshuffleEnabled = autoReshuffleEnabled;
        TotalCards = deckCount * 52;
        CardsDealt = TotalCards - remainingCards;
    }

    /// <summary>
    /// Gets the number of decks in the shoe.
    /// </summary>
    public int DeckCount { get; }

    /// <summary>
    /// Gets the total number of cards in the shoe when full.
    /// </summary>
    public int TotalCards { get; }

    /// <summary>
    /// Gets the number of cards remaining in the shoe.
    /// </summary>
    public int RemainingCards { get; }

    /// <summary>
    /// Gets the number of cards that have been dealt.
    /// </summary>
    public int CardsDealt { get; }

    /// <summary>
    /// Gets the percentage of cards remaining in the shoe.
    /// </summary>
    public double RemainingPercentage { get; }

    /// <summary>
    /// Gets the current penetration threshold.
    /// </summary>
    public double PenetrationThreshold { get; }

    /// <summary>
    /// Gets a value indicating whether the shoe needs reshuffling.
    /// </summary>
    public bool NeedsReshuffle { get; }

    /// <summary>
    /// Gets a value indicating whether automatic reshuffling is enabled.
    /// </summary>
    public bool AutoReshuffleEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the shoe is empty.
    /// </summary>
    public bool IsEmpty => RemainingCards == 0;

    /// <summary>
    /// Gets a value indicating whether the shoe is nearly empty (less than 5% remaining).
    /// </summary>
    public bool IsNearlyEmpty => RemainingPercentage < 0.05;

    /// <summary>
    /// Returns a string representation of the shoe status.
    /// </summary>
    /// <returns>A string describing the shoe status.</returns>
    public override string ToString()
    {
        var status = $"Shoe: {RemainingCards}/{TotalCards} cards ({RemainingPercentage:P1})";
        if (NeedsReshuffle)
        {
            status += " - RESHUFFLE NEEDED";
        }
        return status;
    }
}