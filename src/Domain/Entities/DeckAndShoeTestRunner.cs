using GroupProject.Domain.ValueObjects;
using GroupProject.Domain.Interfaces;
using GroupProject.Infrastructure.Providers;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Simple test runner to verify Deck and Shoe functionality without requiring a test framework.
/// </summary>
public static class DeckAndShoeTestRunner
{
    public static void RunTests()
    {
        Console.WriteLine("Running Deck and Shoe class tests...");
        
        TestDeckBasicOperations();
        TestDeckShuffling();
        TestDeckReset();
        TestShoeBasicOperations();
        TestShoeMultipleDecks();
        TestShoeShuffling();
        TestShoeReset();
        TestShoePenetration();
        
        Console.WriteLine("All Deck and Shoe tests completed successfully!");
    }
    
    private static void TestDeckBasicOperations()
    {
        Console.WriteLine("Testing deck basic operations...");
        
        var randomProvider = new SystemRandomProvider();
        var deck = new Deck(randomProvider);
        
        // New deck should have 52 cards
        Assert(deck.RemainingCards == 52, "New deck should have 52 cards");
        Assert(!deck.IsEmpty, "New deck should not be empty");
        
        // Draw a card
        var card = deck.Draw();
        Assert(deck.RemainingCards == 51, "Deck should have 51 cards after drawing one");
        Assert(Enum.IsDefined(card.Suit), "Drawn card should have valid suit");
        Assert(Enum.IsDefined(card.Rank), "Drawn card should have valid rank");
        
        // Draw all remaining cards
        for (int i = 0; i < 51; i++)
        {
            deck.Draw();
        }
        
        Assert(deck.RemainingCards == 0, "Deck should be empty after drawing all cards");
        Assert(deck.IsEmpty, "Deck should report as empty");
        
        // Try to draw from empty deck should throw
        try
        {
            deck.Draw();
            Assert(false, "Drawing from empty deck should throw exception");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
        
        Console.WriteLine("✓ Deck basic operations test passed");
    }
    
    private static void TestDeckShuffling()
    {
        Console.WriteLine("Testing deck shuffling...");
        
        // Test that shuffling changes the order
        var randomProvider1 = new SystemRandomProvider(12345);
        var randomProvider2 = new SystemRandomProvider(12345);
        var deck1 = new Deck(randomProvider1);
        var deck2 = new Deck(randomProvider2);
        
        // Both decks should start with the same order (unshuffled)
        var card1 = deck1.Draw();
        var card2 = deck2.Draw();
        Assert(card1.Equals(card2), "Both decks should start with same first card");
        
        // Reset both decks
        deck1.Reset(); // This will shuffle with the random
        deck2.Reset(); // This will shuffle with the same random seed
        
        // Now they should have the same shuffled order
        var shuffledCard1 = deck1.Draw();
        var shuffledCard2 = deck2.Draw();
        Assert(shuffledCard1.Equals(shuffledCard2), "Both decks should have same first card after reset with same seed");
        
        // Test that shuffle actually changes the order
        var randomProvider3 = new SystemRandomProvider();
        var deck3 = new Deck(randomProvider3);
        var originalCards = new List<Card>();
        for (int i = 0; i < 10; i++)
        {
            originalCards.Add(deck3.Draw());
        }
        
        deck3.Reset(); // This will shuffle
        var shuffledCards = new List<Card>();
        for (int i = 0; i < 10; i++)
        {
            shuffledCards.Add(deck3.Draw());
        }
        
        // It's extremely unlikely that all 10 cards are in the same order after shuffling
        bool orderChanged = false;
        for (int i = 0; i < 10; i++)
        {
            if (!originalCards[i].Equals(shuffledCards[i]))
            {
                orderChanged = true;
                break;
            }
        }
        Assert(orderChanged, "Shuffling should change the order of cards");
        
        Console.WriteLine("✓ Deck shuffling test passed");
    }
    
    private static void TestDeckReset()
    {
        Console.WriteLine("Testing deck reset...");
        
        var randomProvider = new SystemRandomProvider();
        var deck = new Deck(randomProvider);
        
        // Draw some cards
        for (int i = 0; i < 10; i++)
        {
            deck.Draw();
        }
        
        Assert(deck.RemainingCards == 42, "Deck should have 42 cards after drawing 10");
        
        // Reset deck
        deck.Reset();
        
        Assert(deck.RemainingCards == 52, "Deck should have 52 cards after reset");
        Assert(!deck.IsEmpty, "Deck should not be empty after reset");
        
        Console.WriteLine("✓ Deck reset test passed");
    }
    
    private static void TestShoeBasicOperations()
    {
        Console.WriteLine("Testing shoe basic operations...");
        
        var randomProvider = new SystemRandomProvider();
        var shoe = new Shoe(2, randomProvider); // 2 decks
        
        // Shoe should have 104 cards (2 * 52)
        Assert(shoe.RemainingCards == 104, "2-deck shoe should have 104 cards");
        Assert(shoe.DeckCount == 2, "Shoe should report 2 decks");
        Assert(!shoe.IsEmpty, "New shoe should not be empty");
        
        // Draw a card
        var card = shoe.Draw();
        Assert(shoe.RemainingCards == 103, "Shoe should have 103 cards after drawing one");
        Assert(Enum.IsDefined(card.Suit), "Drawn card should have valid suit");
        Assert(Enum.IsDefined(card.Rank), "Drawn card should have valid rank");
        
        Console.WriteLine("✓ Shoe basic operations test passed");
    }
    
    private static void TestShoeMultipleDecks()
    {
        Console.WriteLine("Testing shoe with multiple decks...");
        
        var randomProvider = new SystemRandomProvider();
        var shoe = new Shoe(6, randomProvider); // Standard 6-deck shoe
        
        Assert(shoe.RemainingCards == 312, "6-deck shoe should have 312 cards (6 * 52)");
        Assert(shoe.DeckCount == 6, "Shoe should report 6 decks");
        
        // Count occurrences of Ace of Spades (should be 6)
        var cards = new List<Card>();
        while (!shoe.IsEmpty)
        {
            cards.Add(shoe.Draw());
        }
        
        var aceOfSpadesCount = cards.Count(c => c.Suit == Suit.Spades && c.Rank == Rank.Ace);
        Assert(aceOfSpadesCount == 6, "6-deck shoe should contain exactly 6 Ace of Spades");
        
        Console.WriteLine("✓ Shoe multiple decks test passed");
    }
    
    private static void TestShoeShuffling()
    {
        Console.WriteLine("Testing shoe shuffling...");
        
        // Test that two shoes with the same seed produce the same sequence
        var randomProvider1 = new SystemRandomProvider(54321);
        var randomProvider2 = new SystemRandomProvider(54321);
        var shoe1 = new Shoe(1, randomProvider1);
        var shoe2 = new Shoe(1, randomProvider2);
        
        // Draw first 5 cards from each
        var cards1 = new List<Card>();
        var cards2 = new List<Card>();
        for (int i = 0; i < 5; i++)
        {
            cards1.Add(shoe1.Draw());
            cards2.Add(shoe2.Draw());
        }
        
        // Cards should be the same due to same random seed
        for (int i = 0; i < 5; i++)
        {
            Assert(cards1[i].Equals(cards2[i]), $"Card {i} should be the same with same random seed");
        }
        
        // Test that shuffling changes the order
        var randomProvider3 = new SystemRandomProvider();
        var shoe3 = new Shoe(1, randomProvider3);
        var originalCards = new List<Card>();
        for (int i = 0; i < 10; i++)
        {
            originalCards.Add(shoe3.Draw());
        }
        
        shoe3.Reset(); // This will shuffle
        var shuffledCards = new List<Card>();
        for (int i = 0; i < 10; i++)
        {
            shuffledCards.Add(shoe3.Draw());
        }
        
        // It's extremely unlikely that all 10 cards are in the same order after shuffling
        bool orderChanged = false;
        for (int i = 0; i < 10; i++)
        {
            if (!originalCards[i].Equals(shuffledCards[i]))
            {
                orderChanged = true;
                break;
            }
        }
        Assert(orderChanged, "Shuffling should change the order of cards");
        
        Console.WriteLine("✓ Shoe shuffling test passed");
    }
    
    private static void TestShoeReset()
    {
        Console.WriteLine("Testing shoe reset...");
        
        var randomProvider = new SystemRandomProvider();
        var shoe = new Shoe(3, randomProvider);
        
        // Draw some cards
        for (int i = 0; i < 50; i++)
        {
            shoe.Draw();
        }
        
        Assert(shoe.RemainingCards == 106, "Shoe should have 106 cards after drawing 50");
        
        // Reset shoe
        shoe.Reset();
        
        Assert(shoe.RemainingCards == 156, "Shoe should have 156 cards after reset (3 * 52)");
        Assert(!shoe.IsEmpty, "Shoe should not be empty after reset");
        
        Console.WriteLine("✓ Shoe reset test passed");
    }
    
    private static void TestShoePenetration()
    {
        Console.WriteLine("Testing shoe penetration...");
        
        var randomProvider = new SystemRandomProvider();
        var shoe = new Shoe(6, randomProvider);
        
        // Initially should not need reshuffle
        Assert(!shoe.NeedsReshuffle(), "Full shoe should not need reshuffle");
        Assert(Math.Abs(shoe.GetRemainingPercentage() - 1.0) < 0.001, "Full shoe should be at 100%");
        
        // Draw more than 75% of cards (235 cards) to get below 25%
        for (int i = 0; i < 235; i++)
        {
            shoe.Draw();
        }
        
        // Should be below 25% remaining
        var percentage = shoe.GetRemainingPercentage();
        Assert(percentage < 0.25, $"Should be below 25% remaining, got {percentage:P1}");
        
        // Should need reshuffle at default threshold (25%)
        Assert(shoe.NeedsReshuffle(), "Shoe below 25% should need reshuffle");
        
        // Should not need reshuffle at 20% threshold
        Assert(!shoe.NeedsReshuffle(0.20), "Shoe at 25% should not need reshuffle with 20% threshold");
        
        Console.WriteLine("✓ Shoe penetration test passed");
    }
    
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Test failed: {message}");
        }
    }
}