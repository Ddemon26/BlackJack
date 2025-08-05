using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GroupProject.Application.Interfaces;
using GroupProject.Infrastructure.Extensions;

namespace GroupProject;

internal static class Program {
    const string HELLO_WORLD = "--helloworld";
    const string BLACKJACK = "--blackjack";
    const string TEST_HAND = "--testhand";
    const string TEST_DECK = "--testdeck";
    const string TEST_PLAYER = "--testplayer";

    static async Task<int> Main(string[] args) {
        try
        {
            string[] processedArgs = ProcessArgs(args);
            
            switch (processedArgs.Length) {
                case > 0 when processedArgs[0].Equals(HELLO_WORLD):
                {
                    await RunWithGlobalErrorHandling(() => RunHelloWorldAsync());
                    break;
                }
                case > 0 when processedArgs[0].Equals(BLACKJACK):
                {
                    await RunWithGlobalErrorHandling(() => RunBlackjackGameAsync());
                    break;
                }
                case > 0 when processedArgs[0].Equals(TEST_HAND):
                {
                    await RunWithGlobalErrorHandling(() => Task.Run(RunHandTests));
                    break;
                }
                case > 0 when processedArgs[0].Equals(TEST_DECK):
                {
                    await RunWithGlobalErrorHandling(() => Task.Run(RunDeckTests));
                    break;
                }
                case > 0 when processedArgs[0].Equals(TEST_PLAYER):
                {
                    await RunWithGlobalErrorHandling(() => Task.Run(RunPlayerTests));
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
        catch (Exception ex)
        {
            // Final fallback error handling
            Console.WriteLine($"[FATAL ERROR] An unrecoverable error occurred: {ex.Message}");
            Console.WriteLine("The application will now exit.");
            return 1;
        }
    }

    /// <summary>
    /// Runs the specified operation with global error handling.
    /// </summary>
    /// <param name="operation">The operation to run.</param>
    static async Task RunWithGlobalErrorHandling(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            // Try to use the error handler if available
            try
            {
                using var host = CreateBlackjackHost();
                var errorHandler = host.Services.GetService<IErrorHandler>();
                
                if (errorHandler != null)
                {
                    var userMessage = await errorHandler.HandleExceptionAsync(ex, "Application Main");
                    Console.WriteLine($"Error: {userMessage}");
                }
                else
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            catch
            {
                // Fallback if error handler fails
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Runs the hello world example.
    /// </summary>
    static async Task RunHelloWorldAsync()
    {
        using var host = Host<Something>();
        var something = host.Services.GetRequiredService<Something>();
        await Task.Run(() => something.DoSomething());
    }

    /// <summary>
    /// Runs the blackjack game with full error handling.
    /// </summary>
    static async Task RunBlackjackGameAsync()
    {
        using var host = CreateBlackjackHost();
        var gameOrchestrator = host.Services.GetRequiredService<IGameOrchestrator>();
        
        try
        {
            await gameOrchestrator.RunMultipleRoundsAsync();
        }
        catch (Exception ex)
        {
            var errorHandler = host.Services.GetRequiredService<IErrorHandler>();
            var userMessage = await errorHandler.HandleExceptionAsync(ex, "Blackjack Game");
            Console.WriteLine($"Game Error: {userMessage}");
            
            if (!errorHandler.IsRecoverableError(ex))
            {
                Console.WriteLine("The game cannot continue and will now exit.");
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a host configured for the blackjack game.
    /// </summary>
    /// <returns>A configured host for the blackjack application.</returns>
    static IHost CreateBlackjackHost()
    {
        return new HostBuilder()
            .ConfigureServices((_, services) => {
                services.AddBlackjackServices();
            })
            .Build();
    }
    
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