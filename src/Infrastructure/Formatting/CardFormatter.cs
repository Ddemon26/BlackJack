using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;

namespace GroupProject.Infrastructure.Formatting;

/// <summary>
/// High-performance card formatting utilities with caching and pooling optimizations.
/// Provides efficient methods for formatting cards and collections of cards for display.
/// </summary>
/// <remarks>
/// This class uses pre-computed caches for all possible card representations to eliminate
/// runtime string formatting overhead. It also leverages StringBuilder pooling for
/// efficient multi-card formatting operations. All formatting methods are thread-safe
/// due to the immutable nature of the cached data.
/// </remarks>
public static class CardFormatter
{
    // Pre-computed card display strings for maximum performance
    private static readonly Dictionary<Card, string> _cardDisplayCache = new();
    private static readonly Dictionary<Card, string> _cardSymbolCache = new();
    
    // Unicode suit symbols for better display
    private static readonly Dictionary<Suit, string> _suitSymbols = new()
    {
        { Suit.Spades, "♠" },
        { Suit.Hearts, "♥" },
        { Suit.Diamonds, "♦" },
        { Suit.Clubs, "♣" }
    };

    // Rank display strings
    private static readonly Dictionary<Rank, string> _rankDisplays = new()
    {
        { Rank.Ace, "A" },
        { Rank.Jack, "J" },
        { Rank.Queen, "Q" },
        { Rank.King, "K" }
    };

    static CardFormatter()
    {
        // Pre-populate caches for all possible cards
        InitializeCaches();
    }

    /// <summary>
    /// Formats a card for display with suit symbol (e.g., "A♠").
    /// Uses cached values for optimal performance.
    /// </summary>
    /// <param name="card">The card to format.</param>
    /// <returns>A formatted string representation of the card.</returns>
    public static string FormatCardSymbol(Card card)
    {
        return _cardSymbolCache.TryGetValue(card, out var cached) ? cached : CreateCardSymbol(card);
    }

    /// <summary>
    /// Formats a card for detailed display (e.g., "Ace of Spades").
    /// Uses cached values for optimal performance.
    /// </summary>
    /// <param name="card">The card to format.</param>
    /// <returns>A detailed formatted string representation of the card.</returns>
    public static string FormatCardDetailed(Card card)
    {
        return _cardDisplayCache.TryGetValue(card, out var cached) ? cached : CreateCardDetailed(card);
    }

    /// <summary>
    /// Formats a collection of cards with symbols, using StringBuilder pooling for efficiency.
    /// </summary>
    /// <param name="cards">The cards to format.</param>
    /// <param name="separator">The separator to use between cards.</param>
    /// <returns>A formatted string representation of all cards.</returns>
    public static string FormatCardsSymbols(IEnumerable<Card> cards, string separator = ", ")
    {
        if (cards == null)
            return string.Empty;

        var cardList = cards as IList<Card> ?? cards.ToList();
        if (!cardList.Any())
            return string.Empty;

        if (cardList.Count == 1)
            return FormatCardSymbol(cardList[0]);

        var sb = StringBuilderPool.Get();
        try
        {
            var first = true;
            foreach (var card in cardList)
            {
                if (!first)
                    sb.Append(separator);
                
                sb.Append(FormatCardSymbol(card));
                first = false;
            }

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Formats a collection of cards with detailed names, using StringBuilder pooling for efficiency.
    /// </summary>
    /// <param name="cards">The cards to format.</param>
    /// <param name="separator">The separator to use between cards.</param>
    /// <returns>A detailed formatted string representation of all cards.</returns>
    public static string FormatCardsDetailed(IEnumerable<Card> cards, string separator = ", ")
    {
        if (cards == null)
            return string.Empty;

        var cardList = cards as IList<Card> ?? cards.ToList();
        if (!cardList.Any())
            return string.Empty;

        if (cardList.Count == 1)
            return FormatCardDetailed(cardList[0]);

        var sb = StringBuilderPool.Get();
        try
        {
            var first = true;
            foreach (var card in cardList)
            {
                if (!first)
                    sb.Append(separator);
                
                sb.Append(FormatCardDetailed(card));
                first = false;
            }

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Formats a hand value with optional soft indicator for display.
    /// </summary>
    /// <param name="value">The hand value.</param>
    /// <param name="isSoft">Whether the hand is soft.</param>
    /// <returns>A formatted hand value string.</returns>
    public static string FormatHandValue(int value, bool isSoft = false)
    {
        return isSoft ? $"{value} (Soft)" : value.ToString();
    }

    /// <summary>
    /// Creates a formatted player status line for display.
    /// </summary>
    /// <param name="playerName">The player's name.</param>
    /// <param name="handValue">The hand value.</param>
    /// <param name="isSoft">Whether the hand is soft.</param>
    /// <param name="isBlackjack">Whether the hand is blackjack.</param>
    /// <param name="isBusted">Whether the hand is busted.</param>
    /// <returns>A formatted status string.</returns>
    public static string FormatPlayerStatus(string playerName, int handValue, bool isSoft = false, 
        bool isBlackjack = false, bool isBusted = false)
    {
        var sb = StringBuilderPool.Get();
        try
        {
            sb.Append(playerName);
            sb.Append(": ");
            sb.Append(FormatHandValue(handValue, isSoft));

            if (isBlackjack)
                sb.Append(" ★ BLACKJACK!");
            else if (isBusted)
                sb.Append(" ✗ BUSTED!");

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    private static void InitializeCaches()
    {
        // Pre-compute all possible card representations
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                var card = new Card(suit, rank);
                _cardSymbolCache[card] = CreateCardSymbol(card);
                _cardDisplayCache[card] = CreateCardDetailed(card);
            }
        }
    }

    private static string CreateCardSymbol(Card card)
    {
        var rank = _rankDisplays.TryGetValue(card.Rank, out var rankDisplay) 
            ? rankDisplay 
            : ((int)card.Rank).ToString();
        
        var suit = _suitSymbols[card.Suit];
        
        return rank + suit;
    }

    private static string CreateCardDetailed(Card card)
    {
        var rankName = card.Rank switch
        {
            Rank.Ace => "Ace",
            Rank.Jack => "Jack",
            Rank.Queen => "Queen",
            Rank.King => "King",
            _ => ((int)card.Rank).ToString()
        };

        var suitName = card.Suit switch
        {
            Suit.Spades => "Spades",
            Suit.Hearts => "Hearts",
            Suit.Diamonds => "Diamonds",
            Suit.Clubs => "Clubs",
            _ => card.Suit.ToString()
        };

        return $"{rankName} of {suitName}";
    }
}