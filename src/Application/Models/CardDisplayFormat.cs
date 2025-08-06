namespace GroupProject.Application.Models;

/// <summary>
/// Defines the available card display formats.
/// </summary>
public enum CardDisplayFormat
{
    /// <summary>
    /// Display cards using symbols (e.g., A♠, K♥).
    /// </summary>
    Symbols,

    /// <summary>
    /// Display cards using text (e.g., Ace of Spades, King of Hearts).
    /// </summary>
    Text,

    /// <summary>
    /// Display cards using abbreviated text (e.g., AS, KH).
    /// </summary>
    Abbreviated
}