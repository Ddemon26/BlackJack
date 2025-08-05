using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace GroupProject;

internal static class Program {
    const string HELLO_WORLD = "--helloworld";
    const string BLACKJACK = "--blackjack";
    const string TEST_HAND = "--testhand";
    const string TEST_DECK = "--testdeck";
    const string TEST_PLAYER = "--testplayer";

    static int Main(string[] args) {
        string[] processedArgs = ProcessArgs(args);
        
        switch (processedArgs.Length) {
            case > 0 when processedArgs[0].Equals(HELLO_WORLD):
            {
                using var host = Host<Something>();
                var something = host.Services.GetRequiredService<Something>();
                something.DoSomething();
                break;
            }
            case > 0 when processedArgs[0].Equals(BLACKJACK):
            {
                Console.WriteLine("Blackjack game is being refactored. Please check back later!");
                break;
            }
            case > 0 when processedArgs[0].Equals(TEST_HAND):
            {
                RunHandTests();
                break;
            }
            case > 0 when processedArgs[0].Equals(TEST_DECK):
            {
                RunDeckTests();
                break;
            }
            case > 0 when processedArgs[0].Equals(TEST_PLAYER):
            {
                RunPlayerTests();
                break;
            }
            default:
            {
                Console.WriteLine("Available commands:");
                Console.WriteLine("  --helloworld  : Run hello world example");
                Console.WriteLine("  --blackjack   : Start blackjack game");
                Console.WriteLine("  --testhand    : Run Hand class tests");
                Console.WriteLine("  --testdeck    : Run Deck and Shoe class tests");
                Console.WriteLine("  --testplayer  : Run Player class tests");
                break;
            }
        }

        return 0;
    }

    // Blackjack game functionality temporarily disabled during refactoring
    // Will be replaced with new architecture implementation
    
    static void RunHandTests()
    {
        try
        {
            GroupProject.Domain.Entities.HandTestRunner.RunTests();
            Console.WriteLine("\n🎉 All tests passed! Hand implementation is working correctly.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
        }
    }
    
    static void RunDeckTests()
    {
        try
        {
            GroupProject.Domain.Entities.DeckAndShoeTestRunner.RunTests();
            Console.WriteLine("\n🎉 All tests passed! Deck and Shoe implementations are working correctly.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
        }
    }
    
    static void RunPlayerTests()
    {
        try
        {
            GroupProject.Domain.Entities.PlayerTestRunner.RunTests();
            Console.WriteLine("\n🎉 All tests passed! Player implementation is working correctly.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
        }
    }
    
    static IHost Host<T>() where T : class {
        IHost? host = null;
        try {
            host = new HostBuilder()
                .ConfigureServices((_, services) => {
                    services.AddSingleton<T>();
                })
                .Build();
            return host;
        }
        catch {
            host?.Dispose();
            throw;
        }
    }

    static string[] ProcessArgs(string[] args)
        => args.Select(arg => arg.ToLowerInvariant()).ToArray();
}

internal class Something {
    string m_text;
    public Something() {
        m_text = "Hello, World!";
    }

    public void DoSomething() {
        Console.WriteLine($"{m_text}");
    }
}