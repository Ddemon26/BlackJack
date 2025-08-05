using GroupProject.Domain.Entities;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the result of a player action in a blackjack game.
/// </summary>
public class PlayerActionResult
{
    /// <summary>
    /// Gets a value indicating whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the action was not successful.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the updated hand after the action (if applicable).
    /// </summary>
    public Hand? UpdatedHand { get; }

    /// <summary>
    /// Gets a value indicating whether the player's turn should continue.
    /// </summary>
    public bool ShouldContinueTurn { get; }

    /// <summary>
    /// Gets a value indicating whether the hand is busted after the action.
    /// </summary>
    public bool IsBusted { get; }

    /// <summary>
    /// Gets a value indicating whether the action resulted in a blackjack.
    /// </summary>
    public bool IsBlackjack { get; }

    /// <summary>
    /// Gets a value indicating whether the action was a double down.
    /// </summary>
    public bool IsDoubleDown { get; }

    /// <summary>
    /// Gets a value indicating whether the action was a split.
    /// </summary>
    public bool IsSplit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerActionResult"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the action was successful.</param>
    /// <param name="errorMessage">The error message if the action failed.</param>
    /// <param name="updatedHand">The updated hand after the action.</param>
    /// <param name="shouldContinueTurn">Whether the player's turn should continue.</param>
    /// <param name="isBusted">Whether the hand is busted after the action.</param>
    /// <param name="isBlackjack">Whether the action resulted in a blackjack.</param>
    /// <param name="isDoubleDown">Whether the action was a double down.</param>
    /// <param name="isSplit">Whether the action was a split.</param>
    private PlayerActionResult(
        bool isSuccess,
        string? errorMessage = null,
        Hand? updatedHand = null,
        bool shouldContinueTurn = false,
        bool isBusted = false,
        bool isBlackjack = false,
        bool isDoubleDown = false,
        bool isSplit = false)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        UpdatedHand = updatedHand;
        ShouldContinueTurn = shouldContinueTurn;
        IsBusted = isBusted;
        IsBlackjack = isBlackjack;
        IsDoubleDown = isDoubleDown;
        IsSplit = isSplit;
    }

    /// <summary>
    /// Creates a successful action result.
    /// </summary>
    /// <param name="updatedHand">The updated hand after the action.</param>
    /// <param name="shouldContinueTurn">Whether the player's turn should continue.</param>
    /// <param name="isDoubleDown">Whether the action was a double down.</param>
    /// <param name="isSplit">Whether the action was a split.</param>
    /// <returns>A successful action result.</returns>
    public static PlayerActionResult Success(Hand updatedHand, bool shouldContinueTurn = true, bool isDoubleDown = false, bool isSplit = false)
    {
        return new PlayerActionResult(
            isSuccess: true,
            updatedHand: updatedHand,
            shouldContinueTurn: shouldContinueTurn,
            isBusted: updatedHand.IsBusted(),
            isBlackjack: updatedHand.IsBlackjack(),
            isDoubleDown: isDoubleDown,
            isSplit: isSplit);
    }

    /// <summary>
    /// Creates a failed action result.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the action failed.</param>
    /// <returns>A failed action result.</returns>
    public static PlayerActionResult Failure(string errorMessage)
    {
        return new PlayerActionResult(isSuccess: false, errorMessage: errorMessage);
    }

    /// <summary>
    /// Creates a successful action result that ends the player's turn.
    /// </summary>
    /// <param name="updatedHand">The updated hand after the action.</param>
    /// <param name="isDoubleDown">Whether the action was a double down.</param>
    /// <param name="isSplit">Whether the action was a split.</param>
    /// <returns>A successful action result that ends the turn.</returns>
    public static PlayerActionResult SuccessEndTurn(Hand updatedHand, bool isDoubleDown = false, bool isSplit = false)
    {
        return new PlayerActionResult(
            isSuccess: true,
            updatedHand: updatedHand,
            shouldContinueTurn: false,
            isBusted: updatedHand.IsBusted(),
            isBlackjack: updatedHand.IsBlackjack(),
            isDoubleDown: isDoubleDown,
            isSplit: isSplit);
    }
}