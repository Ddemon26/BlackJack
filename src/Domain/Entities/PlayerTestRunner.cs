using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Simple test runner to verify Player functionality without requiring a test framework.
/// </summary>
public static class PlayerTestRunner
{
    public static void RunTests()
    {
        Console.WriteLine("Running Player class tests...");
        
        TestPlayerCreation();
        TestPlayerProperties();
        TestPlayerHandOperations();
        TestPlayerGameStates();
        TestPlayerStringRepresentation();
        TestPlayerEquality();
        TestPlayerValidation();
        
        Console.WriteLine("All Player tests completed successfully!");
    }
    
    private static void TestPlayerCreation()
    {
        Console.WriteLine("Testing player creation...");
        
        // Test human player creation
        var humanPlayer = new Player("Alice");
        Assert(humanPlayer.Name == "Alice", "Player name should be set correctly");
        Assert(humanPlayer.Type == PlayerType.Human, "Default player type should be Human");
        Assert(humanPlayer.IsHuman, "IsHuman should be true for human player");
        Assert(!humanPlayer.IsDealer, "IsDealer should be false for human player");
        
        // Test dealer creation
        var dealer = new Player("Dealer", PlayerType.Dealer);
        Assert(dealer.Name == "Dealer", "Dealer name should be set correctly");
        Assert(dealer.Type == PlayerType.Dealer, "Dealer type should be Dealer");
        Assert(!dealer.IsHuman, "IsHuman should be false for dealer");
        Assert(dealer.IsDealer, "IsDealer should be true for dealer");
        
        // Test name trimming
        var playerWithSpaces = new Player("  Bob  ");
        Assert(playerWithSpaces.Name == "Bob", "Player name should be trimmed");
        
        Console.WriteLine("✓ Player creation test passed");
    }
    
    private static void TestPlayerProperties()
    {
        Console.WriteLine("Testing player properties...");
        
        var player = new Player("Charlie");
        
        // Initial state
        Assert(player.GetHandValue() == 0, "New player should have hand value 0");
        Assert(player.GetCardCount() == 0, "New player should have 0 cards");
        Assert(!player.IsBusted(), "New player should not be busted");
        Assert(!player.HasBlackjack(), "New player should not have blackjack");
        Assert(!player.HasSoftHand(), "New player should not have soft hand");
        
        Console.WriteLine("✓ Player properties test passed");
    }
    
    private static void TestPlayerHandOperations()
    {
        Console.WriteLine("Testing player hand operations...");
        
        var player = new Player("David");
        
        // Add a card
        player.AddCard(new Card(Suit.Hearts, Rank.Seven));
        Assert(player.GetHandValue() == 7, "Player should have hand value 7 after adding 7");
        Assert(player.GetCardCount() == 1, "Player should have 1 card");
        
        // Add another card
        player.AddCard(new Card(Suit.Spades, Rank.Five));
        Assert(player.GetHandValue() == 12, "Player should have hand value 12 after adding 7 and 5");
        Assert(player.GetCardCount() == 2, "Player should have 2 cards");
        
        // Clear hand
        player.ClearHand();
        Assert(player.GetHandValue() == 0, "Player should have hand value 0 after clearing");
        Assert(player.GetCardCount() == 0, "Player should have 0 cards after clearing");
        
        Console.WriteLine("✓ Player hand operations test passed");
    }
    
    private static void TestPlayerGameStates()
    {
        Console.WriteLine("Testing player game states...");
        
        var player = new Player("Eve");
        
        // Test blackjack
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));
        Assert(player.HasBlackjack(), "Player should have blackjack with Ace and King");
        Assert(player.GetHandValue() == 21, "Blackjack should have value 21");
        Assert(!player.IsBusted(), "Blackjack should not be busted");
        
        // Test bust
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.King));
        player.AddCard(new Card(Suit.Spades, Rank.Queen));
        player.AddCard(new Card(Suit.Diamonds, Rank.Five));
        Assert(player.IsBusted(), "Player should be busted with K, Q, 5");
        Assert(player.GetHandValue() == 25, "Busted hand should have value 25");
        Assert(!player.HasBlackjack(), "Busted hand should not be blackjack");
        
        // Test soft hand
        player.ClearHand();
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.Six));
        Assert(player.HasSoftHand(), "Player should have soft hand with Ace and 6");
        Assert(player.GetHandValue() == 17, "Soft 17 should have value 17");
        
        Console.WriteLine("✓ Player game states test passed");
    }
    
    private static void TestPlayerStringRepresentation()
    {
        Console.WriteLine("Testing player string representation...");
        
        var humanPlayer = new Player("Frank");
        var dealer = new Player("House", PlayerType.Dealer);
        
        // Test empty hand representation
        var humanString = humanPlayer.ToString();
        Assert(humanString.Contains("Player Frank"), "Human player string should contain 'Player Frank'");
        Assert(humanString.Contains("Value: 0"), "Empty hand should show value 0");
        
        var dealerString = dealer.ToString();
        Assert(dealerString.Contains("Dealer House"), "Dealer string should contain 'Dealer House'");
        
        // Test with cards
        humanPlayer.AddCard(new Card(Suit.Hearts, Rank.Ace));
        humanPlayer.AddCard(new Card(Suit.Spades, Rank.King));
        var humanWithCardsString = humanPlayer.ToString();
        Assert(humanWithCardsString.Contains("A of Hearts"), "Should show Ace of Hearts");
        Assert(humanWithCardsString.Contains("K of Spades"), "Should show King of Spades");
        Assert(humanWithCardsString.Contains("Value: 21"), "Should show value 21");
        
        // Test hidden card representation
        dealer.AddCard(new Card(Suit.Diamonds, Rank.Ten));
        dealer.AddCard(new Card(Suit.Clubs, Rank.Seven));
        var hiddenString = dealer.ToStringWithHiddenCard(true);
        Assert(hiddenString.Contains("[Hidden]"), "Should contain hidden card marker");
        Assert(hiddenString.Contains("7 of Clubs"), "Should show second card");
        Assert(!hiddenString.Contains("10 of Diamonds"), "Should not show first card");
        
        Console.WriteLine("✓ Player string representation test passed");
    }
    
    private static void TestPlayerEquality()
    {
        Console.WriteLine("Testing player equality...");
        
        var player1 = new Player("Grace");
        var player2 = new Player("Grace");
        var player3 = new Player("grace"); // Different case
        var player4 = new Player("Henry");
        var dealer1 = new Player("Grace", PlayerType.Dealer);
        
        // Same name and type should be equal
        Assert(player1.Equals(player2), "Players with same name and type should be equal");
        Assert(player1.GetHashCode() == player2.GetHashCode(), "Equal players should have same hash code");
        
        // Same name, different case should be equal
        Assert(player1.Equals(player3), "Players with same name (different case) should be equal");
        
        // Different names should not be equal
        Assert(!player1.Equals(player4), "Players with different names should not be equal");
        
        // Same name, different type should not be equal
        Assert(!player1.Equals(dealer1), "Players with same name but different type should not be equal");
        
        // Null comparison
        Assert(!player1.Equals(null), "Player should not equal null");
        
        Console.WriteLine("✓ Player equality test passed");
    }
    
    private static void TestPlayerValidation()
    {
        Console.WriteLine("Testing player validation...");
        
        // Test null name
        try
        {
            new Player(null!);
            Assert(false, "Creating player with null name should throw exception");
        }
        catch (ArgumentException)
        {
            // Expected
        }
        
        // Test empty name
        try
        {
            new Player("");
            Assert(false, "Creating player with empty name should throw exception");
        }
        catch (ArgumentException)
        {
            // Expected
        }
        
        // Test whitespace name
        try
        {
            new Player("   ");
            Assert(false, "Creating player with whitespace name should throw exception");
        }
        catch (ArgumentException)
        {
            // Expected
        }
        
        Console.WriteLine("✓ Player validation test passed");
    }
    
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Test failed: {message}");
        }
    }
}