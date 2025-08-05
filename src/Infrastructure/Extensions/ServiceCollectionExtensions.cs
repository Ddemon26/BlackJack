using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Infrastructure.Providers;
using GroupProject.Presentation.Console;

namespace GroupProject.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration instance to bind settings from.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddBlackjackServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Register configuration
        var gameConfig = new GameConfiguration();
        if (configuration != null)
        {
            configuration.GetSection("GameConfiguration").Bind(gameConfig);
            
            // Validate configuration
            var validationResults = gameConfig.Validate();
            if (validationResults.Any())
            {
                var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new InvalidOperationException($"Invalid game configuration: {errors}");
            }
        }
        services.AddSingleton(gameConfig);

        // Register domain services
        services.AddSingleton<IGameRules, GameRules>();
        
        // Register infrastructure services
        services.AddSingleton<IRandomProvider, SystemRandomProvider>();
        services.AddSingleton<IInputProvider, ConsoleInputProvider>();
        services.AddSingleton<IOutputProvider, ConsoleOutputProvider>();
        
        // Register domain entities with proper lifetimes
        services.AddTransient<IDeck>(provider => 
        {
            var randomProvider = provider.GetRequiredService<IRandomProvider>();
            return new Deck(randomProvider);
        });
        
        services.AddSingleton<IShoe>(provider =>
        {
            var randomProvider = provider.GetRequiredService<IRandomProvider>();
            var config = provider.GetRequiredService<GameConfiguration>();
            return new Shoe(config.NumberOfDecks, randomProvider);
        });
        
        // Register application services
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IGameOrchestrator, GameOrchestrator>();
        
        // Register presentation services
        services.AddScoped<IUserInterface, ConsoleUserInterface>();
        
        return services;
    }
    
    /// <summary>
    /// Registers core infrastructure services required by the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IRandomProvider, SystemRandomProvider>();
        services.AddSingleton<IInputProvider, ConsoleInputProvider>();
        services.AddSingleton<IOutputProvider, ConsoleOutputProvider>();
        
        return services;
    }
    
    /// <summary>
    /// Registers domain services and entities.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IGameRules, GameRules>();
        
        // Register deck and shoe with proper random provider integration
        services.AddTransient<IDeck>(provider => 
        {
            var randomProvider = provider.GetRequiredService<IRandomProvider>();
            return new Deck(randomProvider);
        });
        
        services.AddSingleton<IShoe>(provider =>
        {
            var randomProvider = provider.GetRequiredService<IRandomProvider>();
            var config = provider.GetRequiredService<GameConfiguration>();
            return new Shoe(config.NumberOfDecks, randomProvider);
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers application layer services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IGameOrchestrator, GameOrchestrator>();
        
        return services;
    }
    
    /// <summary>
    /// Registers presentation layer services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserInterface, ConsoleUserInterface>();
        
        return services;
    }

    /// <summary>
    /// Registers and configures game configuration from the provided configuration source.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration instance to bind settings from.</param>
    /// <param name="sectionName">The configuration section name (defaults to "GameConfiguration").</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddGameConfiguration(this IServiceCollection services, IConfiguration configuration, string sectionName = "GameConfiguration")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var gameConfig = new GameConfiguration();
        configuration.GetSection(sectionName).Bind(gameConfig);
        
        // Validate configuration
        var validationResults = gameConfig.Validate();
        if (validationResults.Any())
        {
            var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            throw new InvalidOperationException($"Invalid game configuration: {errors}");
        }

        services.AddSingleton(gameConfig);
        return services;
    }

    /// <summary>
    /// Registers a default game configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">Optional action to configure the default settings.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    public static IServiceCollection AddDefaultGameConfiguration(this IServiceCollection services, Action<GameConfiguration>? configureOptions = null)
    {
        var gameConfig = new GameConfiguration();
        configureOptions?.Invoke(gameConfig);
        
        // Validate configuration
        var validationResults = gameConfig.Validate();
        if (validationResults.Any())
        {
            var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            throw new InvalidOperationException($"Invalid game configuration: {errors}");
        }

        services.AddSingleton(gameConfig);
        return services;
    }
}