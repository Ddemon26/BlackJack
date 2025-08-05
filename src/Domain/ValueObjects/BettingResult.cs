namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the result of a betting operation.
/// Contains information about the success or failure of bet placement or validation.
/// </summary>
public class BettingResult
{
    /// <summary>
    /// Initializes a new instance of the BettingResult class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the betting operation was successful.</param>
    /// <param name="message">A message describing the result.</param>
    /// <param name="bet">The bet associated with this result, if applicable.</param>
    public BettingResult(bool isSuccess, string message, Bet? bet = null)
    {
        IsSuccess = isSuccess;
        Message = message ?? string.Empty;
        Bet = bet;
    }

    /// <summary>
    /// Gets a value indicating whether the betting operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the betting operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the message describing the result.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the bet associated with this result, if applicable.
    /// </summary>
    public Bet? Bet { get; }

    /// <summary>
    /// Creates a successful betting result.
    /// </summary>
    /// <param name="message">A success message.</param>
    /// <param name="bet">The bet associated with the success.</param>
    /// <returns>A successful BettingResult.</returns>
    public static BettingResult Success(string message, Bet? bet = null)
    {
        return new BettingResult(true, message, bet);
    }

    /// <summary>
    /// Creates a failed betting result.
    /// </summary>
    /// <param name="message">An error message.</param>
    /// <returns>A failed BettingResult.</returns>
    public static BettingResult Failure(string message)
    {
        return new BettingResult(false, message);
    }

    /// <summary>
    /// Returns a string representation of the betting result.
    /// </summary>
    /// <returns>A formatted string showing the result status and message.</returns>
    public override string ToString()
    {
        var status = IsSuccess ? "Success" : "Failure";
        return $"{status}: {Message}";
    }
}