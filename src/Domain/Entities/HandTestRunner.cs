using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Simple test runner to verify Hand functionality without requiring a test framework.
/// This will be replaced by proper unit tests once the test project is set up correctly.
/// </summary>
public static class HandTestRunner
{
    public static void RunTests()
    {
        Console.WriteLine("Running Hand class tests...");
        
        TestBasicHandOperations();
        TestAceHandling();
        TestBlackjackDetection();
        TestBustDetection();
        TestSoftHandDetection();
        
        Console.WriteLine("All Hand tests completed successfully!");
    }
    
    private static void TestBasicHandOperations()
    {
        Console.WriteLine("Testing basic hand operations...");
        
        var hand = new Hand();
        Assert(hand.CardCount == 0, "Empty hand should have 0 cards");
        Assert(hand.GetValue() == 0, "Empty hand should have value 0");
        
        hand.AddCard(new Card(Suit.Hearts, Rank.Five));
        Assert(hand.CardCount == 1, "Hand should have 1 card after adding one");
        Assert(hand.GetValue() == 5, "Hand with 5 should have value 5");
        
        hand.AddCard(new Card(Suit.Spades, Rank.Seven));
        Assert(hand.CardCount == 2, "Hand should have 2 cards after adding two");
        Assert(hand.GetValue() == 12, "Hand with 5 and 7 should have value 12");
        
        hand.Clear();
        Assert(hand.CardCount == 0, "Hand should be empty after clear");
        Assert(hand.GetValue() == 0, "Cleared hand should have value 0");
        
        Console.WriteLine("✓ Basic hand operations test passed");
    }
    
    private static void TestAceHandling()
    {
        Console.WriteLine("Testing Ace handling...");
        
        var hand = new Hand();
        
        // Single Ace should be 11
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        Assert(hand.GetValue() == 11, "Single Ace should be valued at 11");
        
        // Ace + 10 should be 21 (blackjack)
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));
        Assert(hand.GetValue() == 21, "Ace + King should be 21");
        Assert(hand.IsBlackjack(), "Ace + King should be blackjack");
        
        // Two Aces should be 12 (one as 11, one as 1)
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ace));
        Assert(hand.GetValue() == 12, "Two Aces should be 12");
        
        // Ace + 6 + 5 should be 12 (Ace as 1)
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Six));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Five));
        Assert(hand.GetValue() == 12, "Ace + 6 + 5 should be 12 (Ace as 1)");
        
        Console.WriteLine("✓ Ace handling test passed");
    }
    
    private static void TestBlackjackDetection()
    {
        Console.WriteLine("Testing blackjack detection...");
        
        var hand = new Hand();
        
        // Ace + King = blackjack
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));
        Assert(hand.IsBlackjack(), "Ace + King should be blackjack");
        
        // 7 + 7 + 7 = 21 but not blackjack (3 cards)
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.Seven));
        hand.AddCard(new Card(Suit.Spades, Rank.Seven));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Seven));
        Assert(hand.GetValue() == 21, "Three 7s should equal 21");
        Assert(!hand.IsBlackjack(), "Three 7s should not be blackjack");
        
        // King + Queen = 20, not blackjack
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.King));
        hand.AddCard(new Card(Suit.Spades, Rank.Queen));
        Assert(!hand.IsBlackjack(), "King + Queen should not be blackjack");
        
        Console.WriteLine("✓ Blackjack detection test passed");
    }
    
    private static void TestBustDetection()
    {
        Console.WriteLine("Testing bust detection...");
        
        var hand = new Hand();
        
        // King + Queen = 20, not busted
        hand.AddCard(new Card(Suit.Hearts, Rank.King));
        hand.AddCard(new Card(Suit.Spades, Rank.Queen));
        Assert(!hand.IsBusted(), "King + Queen should not be busted");
        
        // King + Queen + 5 = 25, busted
        hand.AddCard(new Card(Suit.Diamonds, Rank.Five));
        Assert(hand.IsBusted(), "King + Queen + 5 should be busted");
        
        Console.WriteLine("✓ Bust detection test passed");
    }
    
    private static void TestSoftHandDetection()
    {
        Console.WriteLine("Testing soft hand detection...");
        
        var hand = new Hand();
        
        // Ace + 6 = soft 17
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Six));
        Assert(hand.IsSoft(), "Ace + 6 should be a soft hand");
        Assert(hand.GetValue() == 17, "Ace + 6 should be 17");
        
        // Ace + 6 + 5 = hard 12 (Ace becomes 1)
        hand.AddCard(new Card(Suit.Diamonds, Rank.Five));
        Assert(!hand.IsSoft(), "Ace + 6 + 5 should not be a soft hand");
        Assert(hand.GetValue() == 12, "Ace + 6 + 5 should be 12");
        
        // King + Queen = hard 20
        hand.Clear();
        hand.AddCard(new Card(Suit.Hearts, Rank.King));
        hand.AddCard(new Card(Suit.Spades, Rank.Queen));
        Assert(!hand.IsSoft(), "King + Queen should not be a soft hand");
        
        Console.WriteLine("✓ Soft hand detection test passed");
    }
    
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Test failed: {message}");
        }
    }
}