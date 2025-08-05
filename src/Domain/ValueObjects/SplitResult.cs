using GroupProject.Domain.Entities;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the result of a split operation in a blackjack game.
/// </summary>
public class SplitResult
{
    /// <summary>
    /// Gets a value indicating whether the split operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the split operation was not successful.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the first hand after the split (if applicable).
    /// </summary>
    public Hand? FirstHand { get; }

    /// <summary>
    /// Gets the second hand after the split (if applicable).
    /// </summary>
    public Hand? SecondHand { get; }

    /// <summary>
    /// Gets a value indicating whether the split hands are Aces (which receive only one card each).
    /// </summary>
    public bool IsSplitAces { get; }

    /// <summary>
    /// Gets the number of hands created by the split operation.
    /// </summary>
    public int HandCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SplitResult"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the split operation was successful.</param>
    /// <param name="errorMessage">The error message if the operation failed.</param>
    /// <param name="firstHand">The first hand after the split.</param>
    /// <param name="secondHand">The second hand after the split.</param>
    /// <param name="isSplitAces">Whether the split hands are Aces.</param>
    /// <param name="handCount">The number of hands created.</param>
    private SplitResult(
        bool isSuccess,
        string? errorMessage = null,
        Hand? firstHand = null,
        Hand? secondHand = null,
        bool isSplitAces = false,
        int handCount = 0)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        FirstHand = firstHand;
        SecondHand = secondHand;
        IsSplitAces = isSplitAces;
        HandCount = handCount;
    }

    /// <summary>
    /// Creates a successful split result.
    /// </summary>
    /// <param name="firstHand">The first hand after the split.</param>
    /// <param name="secondHand">The second hand after the split.</param>
    /// <param name="isSplitAces">Whether the split hands are Aces.</param>
    /// <returns>A successful split result.</returns>
    public static SplitResult Success(Hand firstHand, Hand secondHand, bool isSplitAces = false)
    {
        return new SplitResult(
            isSuccess: true,
            firstHand: firstHand,
            secondHand: secondHand,
            isSplitAces: isSplitAces,
            handCount: 2);
    }

    /// <summary>
    /// Creates a failed split result.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the split failed.</param>
    /// <returns>A failed split result.</returns>
    public static SplitResult Failure(string errorMessage)
    {
        return new SplitResult(isSuccess: false, errorMessage: errorMessage);
    }

    /// <summary>
    /// Returns a string representation of the split result.
    /// </summary>
    /// <returns>A formatted string showing the split result details.</returns>
    public override string ToString()
    {
        if (!IsSuccess)
        {
            return $"Split Failed: {ErrorMessage}";
        }

        var acesText = IsSplitAces ? " (Aces)" : "";
        return $"Split Successful: {HandCount} hands created{acesText}";
    }
}