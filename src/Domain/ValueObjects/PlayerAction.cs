namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the possible actions a player can take during their turn.
/// </summary>
public enum PlayerAction
{
    /// <summary>
    /// Take another card
    /// </summary>
    Hit,
    
    /// <summary>
    /// Keep current hand and end turn
    /// </summary>
    Stand,
    
    /// <summary>
    /// Double the bet and take exactly one more card (future feature)
    /// </summary>
    DoubleDown,
    
    /// <summary>
    /// Split a pair into two separate hands (future feature)
    /// </summary>
    Split
}