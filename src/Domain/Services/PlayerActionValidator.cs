using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Services;

/// <summary>
/// Provides validation and processing logic for player actions in blackjack.
/// </summary>
public class PlayerActionValidator
{
    private readonly IGameRules _gameRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerActionValidator"/> class.
    /// </summary>
    /// <param name="gameRules">The game rules to use for validation.</param>
    public PlayerActionValidator(IGameRules gameRules)
    {
        _gameRules = gameRules ?? throw new ArgumentNullException(nameof(gameRules));
    }

    /// <summary>
    /// Validates if a player action is allowed for the given hand.
    /// </summary>
    /// <param name="action">The action to validate.</param>
    /// <param name="hand">The player's current hand.</param>
    /// <returns>A validation result indicating whether the action is valid and why.</returns>
    public ActionValidationResult ValidateAction(PlayerAction action, Hand hand)
    {
        if (hand == null)
        {
            return ActionValidationResult.Invalid("Hand cannot be null.");
        }

        // Cannot take any action if hand is already busted
        if (hand.IsBusted())
        {
            return ActionValidationResult.Invalid("Cannot take action on a busted hand.");
        }

        return action switch
        {
            PlayerAction.Hit => ValidateHit(hand),
            PlayerAction.Stand => ValidateStand(hand),
            PlayerAction.DoubleDown => ValidateDoubleDown(hand),
            PlayerAction.Split => ValidateSplit(hand),
            _ => ActionValidationResult.Invalid($"Unknown action: {action}")
        };
    }

    /// <summary>
    /// Gets all valid actions for the given hand.
    /// </summary>
    /// <param name="hand">The player's current hand.</param>
    /// <returns>A collection of valid actions for the hand.</returns>
    public IEnumerable<PlayerAction> GetValidActions(Hand hand)
    {
        if (hand == null || hand.IsBusted())
        {
            return Enumerable.Empty<PlayerAction>();
        }

        var validActions = new List<PlayerAction>();

        // Hit is always valid if not busted
        if (ValidateHit(hand).IsValid)
        {
            validActions.Add(PlayerAction.Hit);
        }

        // Stand is always valid if not busted
        if (ValidateStand(hand).IsValid)
        {
            validActions.Add(PlayerAction.Stand);
        }

        // Double down only on initial 2 cards
        if (ValidateDoubleDown(hand).IsValid)
        {
            validActions.Add(PlayerAction.DoubleDown);
        }

        // Split only on pairs
        if (ValidateSplit(hand).IsValid)
        {
            validActions.Add(PlayerAction.Split);
        }

        return validActions;
    }

    /// <summary>
    /// Processes a player action and returns the result.
    /// </summary>
    /// <param name="action">The action to process.</param>
    /// <param name="hand">The player's current hand.</param>
    /// <param name="card">The card to add (for Hit action).</param>
    /// <returns>The result of processing the action.</returns>
    public PlayerActionResult ProcessAction(PlayerAction action, Hand hand, Card? card = null)
    {
        var validation = ValidateAction(action, hand);
        if (!validation.IsValid)
        {
            return PlayerActionResult.Failure(validation.ErrorMessage);
        }

        return action switch
        {
            PlayerAction.Hit => ProcessHit(hand, card),
            PlayerAction.Stand => ProcessStand(hand),
            PlayerAction.DoubleDown => ProcessDoubleDown(hand, card),
            PlayerAction.Split => ProcessSplit(hand),
            _ => PlayerActionResult.Failure($"Cannot process unknown action: {action}")
        };
    }

    private ActionValidationResult ValidateHit(Hand hand)
    {
        // Can always hit if not busted
        return ActionValidationResult.Valid();
    }

    private ActionValidationResult ValidateStand(Hand hand)
    {
        // Can always stand if not busted
        return ActionValidationResult.Valid();
    }

    private ActionValidationResult ValidateDoubleDown(Hand hand)
    {
        if (hand.CardCount != 2)
        {
            return ActionValidationResult.Invalid("Can only double down on initial two cards.");
        }

        return ActionValidationResult.Valid();
    }

    private ActionValidationResult ValidateSplit(Hand hand)
    {
        if (hand.CardCount != 2)
        {
            return ActionValidationResult.Invalid("Can only split with exactly two cards.");
        }

        var cards = hand.Cards;
        if (cards[0].Rank != cards[1].Rank)
        {
            return ActionValidationResult.Invalid("Can only split pairs of the same rank.");
        }

        return ActionValidationResult.Valid();
    }

    private PlayerActionResult ProcessHit(Hand hand, Card? card)
    {
        if (card == null)
        {
            return PlayerActionResult.Failure("Card is required for hit action.");
        }

        // Create a copy of the hand to avoid modifying the original
        var newHand = CopyHand(hand);
        newHand.AddCard(card.Value);

        // If busted or blackjack, turn ends
        bool shouldContinue = !newHand.IsBusted() && !newHand.IsBlackjack();
        
        return shouldContinue 
            ? PlayerActionResult.Success(newHand, shouldContinueTurn: true)
            : PlayerActionResult.SuccessEndTurn(newHand);
    }

    private PlayerActionResult ProcessStand(Hand hand)
    {
        // Standing always ends the turn
        return PlayerActionResult.SuccessEndTurn(hand);
    }

    private PlayerActionResult ProcessDoubleDown(Hand hand, Card? card)
    {
        if (card == null)
        {
            return PlayerActionResult.Failure("Card is required for double down action.");
        }

        // Create a copy of the hand to avoid modifying the original
        var newHand = CopyHand(hand);
        newHand.AddCard(card.Value);

        // Double down always ends the turn after taking one card
        return PlayerActionResult.SuccessEndTurn(newHand);
    }

    private PlayerActionResult ProcessSplit(Hand hand)
    {
        // Split processing would require more complex logic to handle multiple hands
        // For now, return a placeholder result
        return PlayerActionResult.Failure("Split action is not yet implemented.");
    }

    private Hand CopyHand(Hand originalHand)
    {
        var newHand = new Hand();
        foreach (var card in originalHand.Cards)
        {
            newHand.AddCard(card);
        }
        return newHand;
    }
}

/// <summary>
/// Represents the result of validating a player action.
/// </summary>
public class ActionValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the action is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message if the action is not valid.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the action is valid.</param>
    /// <param name="errorMessage">The error message if the action is not valid.</param>
    private ActionValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a valid action result.
    /// </summary>
    /// <returns>A valid action result.</returns>
    public static ActionValidationResult Valid()
    {
        return new ActionValidationResult(true);
    }

    /// <summary>
    /// Creates an invalid action result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the action is invalid.</param>
    /// <returns>An invalid action result.</returns>
    public static ActionValidationResult Invalid(string errorMessage)
    {
        return new ActionValidationResult(false, errorMessage);
    }
}