using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GroupProject.Application.Interfaces;
using GroupProject.Infrastructure.Extensions;

namespace GroupProject;

/// <summary>
/// Main program entry point for the GroupProject application.
/// Provides command-line interface for various application features including blackjack game and testing utilities.
/// </summary>
internal static class Program 
{
    private const string BLACKJACK = "--blackjack";

    /// <summary>
    /// Main application entry point with comprehensive error handling and dependency injection setup.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code: 0 for success, 1 for failure</returns>
    static async Task<int> Main(string[] args) 
    {
        try
        {
            string[] processedArgs = ProcessArgs(args);
            
            switch (processedArgs.Length) 
            {
                case > 0 when processedArgs[0].Equals(BLACKJACK):
                {
                    await RunWithGlobalErrorHandling(() => RunBlackjackGameAsync());
                    break;
                }
                default:
                {
                    ShowUsageInformation();
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
    /// Displays usage information for available commands.
    /// </summary>
    private static void ShowUsageInformation()
    {
        Console.WriteLine("GroupProject - Blackjack Game Application");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  --blackjack   : Start blackjack game");
        Console.WriteLine();
        Console.WriteLine("Example usage:");
        Console.WriteLine("  dotnet run -- --blackjack");
        Console.WriteLine();
        Console.WriteLine("For running tests, use:");
        Console.WriteLine("  dotnet test");
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
    /// Creates a host configured for the blackjack game with full dependency injection setup.
    /// </summary>
    /// <returns>A configured host for the blackjack application.</returns>
    private static IHost CreateBlackjackHost()
    {
        return new HostBuilder()
            .ConfigureServices((_, services) => 
            {
                services.AddBlackjackServices();
            })
            .Build();
    }
    

    
    /// <summary>
    /// Processes command-line arguments by converting them to lowercase for case-insensitive comparison.
    /// </summary>
    /// <param name="args">Raw command-line arguments</param>
    /// <returns>Processed arguments in lowercase</returns>
    private static string[] ProcessArgs(string[] args)
        => args.Select(arg => arg.ToLowerInvariant()).ToArray();
}