using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Infrastructure.Extensions;
using Xunit;

namespace GroupProject.Tests.Infrastructure;

/// <summary>
/// Tests for the ServiceCollectionExtensions dependency injection configuration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBlackjackServices_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all key services can be resolved
        Assert.NotNull(serviceProvider.GetService<IRandomProvider>());
        Assert.NotNull(serviceProvider.GetService<IInputProvider>());
        Assert.NotNull(serviceProvider.GetService<IOutputProvider>());
        Assert.NotNull(serviceProvider.GetService<IGameRules>());
        Assert.NotNull(serviceProvider.GetService<IDeck>());
        Assert.NotNull(serviceProvider.GetService<IShoe>());
        Assert.NotNull(serviceProvider.GetService<IGameService>());
        Assert.NotNull(serviceProvider.GetService<IGameOrchestrator>());
        Assert.NotNull(serviceProvider.GetService<IUserInterface>());
        
        // Assert - Verify new enhanced services can be resolved
        Assert.NotNull(serviceProvider.GetService<IBettingService>());
        Assert.NotNull(serviceProvider.GetService<ISessionManager>());
        Assert.NotNull(serviceProvider.GetService<IStatisticsService>());
        Assert.NotNull(serviceProvider.GetService<IStatisticsRepository>());
        Assert.NotNull(serviceProvider.GetService<IShoeManager>());
        
        // Assert - Verify utility services can be resolved
        Assert.NotNull(serviceProvider.GetService<SplitHandManager>());
        Assert.NotNull(serviceProvider.GetService<PlayerActionValidator>());
    }

    [Fact]
    public void AddBlackjackServices_CreatesWorkingGameService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var gameService = serviceProvider.GetService<IGameService>();

        // Assert
        Assert.NotNull(gameService);
        Assert.False(gameService.IsGameInProgress);
        Assert.False(gameService.IsGameComplete);
    }

    [Fact]
    public void AddBlackjackServices_CreatesWorkingShoe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var shoe = serviceProvider.GetService<IShoe>();

        // Assert
        Assert.NotNull(shoe);
        Assert.Equal(312, shoe.RemainingCards); // 6 decks * 52 cards
        Assert.False(shoe.IsEmpty);
    }

    [Fact]
    public void AddBlackjackServices_CreatesDifferentDeckInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var deck1 = serviceProvider.GetService<IDeck>();
        var deck2 = serviceProvider.GetService<IDeck>();

        // Assert
        Assert.NotNull(deck1);
        Assert.NotNull(deck2);
        Assert.NotSame(deck1, deck2); // Should be different instances (transient)
    }

    [Fact]
    public void AddBlackjackServices_CreatesSameSingletonInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var shoe1 = serviceProvider.GetService<IShoe>();
        var shoe2 = serviceProvider.GetService<IShoe>();
        var randomProvider1 = serviceProvider.GetService<IRandomProvider>();
        var randomProvider2 = serviceProvider.GetService<IRandomProvider>();

        // Assert
        Assert.Same(shoe1, shoe2); // Should be same instance (singleton)
        Assert.Same(randomProvider1, randomProvider2); // Should be same instance (singleton)
    }

    [Fact]
    public void AddInfrastructureServices_RegistersInfrastructureServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructureServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IRandomProvider>());
        Assert.NotNull(serviceProvider.GetService<IInputProvider>());
        Assert.NotNull(serviceProvider.GetService<IOutputProvider>());
    }

    [Fact]
    public void AddDomainServices_RegistersDomainServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructureServices(); // Need this for IRandomProvider
        services.AddDefaultGameConfiguration(); // Need this for GameConfiguration
        services.AddDomainServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IGameRules>());
        Assert.NotNull(serviceProvider.GetService<IDeck>());
        Assert.NotNull(serviceProvider.GetService<IShoe>());
    }

    [Fact]
    public void AddApplicationServices_RegistersApplicationServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices(); // Need all dependencies
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IGameService>());
        Assert.NotNull(serviceProvider.GetService<IGameOrchestrator>());
    }

    [Fact]
    public void AddPresentationServices_RegistersPresentationServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructureServices(); // Need input/output providers
        services.AddPresentationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IUserInterface>());
    }

    [Fact]
    public void AddDefaultGameConfiguration_RegistersDefaultConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDefaultGameConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetService<GameConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks);
        Assert.Equal(4, config.MaxPlayers);
        Assert.True(config.IsValid);
    }

    [Fact]
    public void AddDefaultGameConfiguration_WithCustomOptions_AppliesCustomization()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDefaultGameConfiguration(config =>
        {
            config.NumberOfDecks = 4;
            config.MaxPlayers = 6;
            config.AllowSplit = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetService<GameConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(4, config.NumberOfDecks);
        Assert.Equal(6, config.MaxPlayers);
        Assert.True(config.AllowSplit);
    }

    [Fact]
    public void AddDefaultGameConfiguration_WithInvalidOptions_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDefaultGameConfiguration(config =>
            {
                config.NumberOfDecks = 0; // Invalid
            }));

        Assert.Contains("Invalid game configuration", exception.Message);
    }

    [Fact]
    public void AddGameConfiguration_WithValidConfiguration_RegistersConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["GameConfiguration:NumberOfDecks"] = "4",
            ["GameConfiguration:MaxPlayers"] = "6",
            ["GameConfiguration:AllowSplit"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        services.AddGameConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetService<GameConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(4, config.NumberOfDecks);
        Assert.Equal(6, config.MaxPlayers);
        Assert.True(config.AllowSplit);
    }

    [Fact]
    public void AddGameConfiguration_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["GameConfiguration:NumberOfDecks"] = "0" // Invalid
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddGameConfiguration(configuration));

        Assert.Contains("Invalid game configuration", exception.Message);
    }

    [Fact]
    public void AddGameConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddGameConfiguration(null!));
    }

    [Fact]
    public void AddBlackjackServices_WithConfiguration_UsesConfiguredNumberOfDecks()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["GameConfiguration:NumberOfDecks"] = "4"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        services.AddBlackjackServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var shoe = serviceProvider.GetService<IShoe>();
        Assert.NotNull(shoe);
        Assert.Equal(208, shoe.RemainingCards); // 4 decks * 52 cards
    }

    [Fact]
    public void AddBlackjackServices_WithoutConfiguration_UsesDefaultNumberOfDecks()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var shoe = serviceProvider.GetService<IShoe>();
        Assert.NotNull(shoe);
        Assert.Equal(312, shoe.RemainingCards); // 6 decks * 52 cards (default)
    }

    [Fact]
    public void AddBlackjackServices_RegistersNewEnhancedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify new enhanced services are registered with correct lifetimes
        // Test scoped services by using different scopes
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var bettingService1 = scope1.ServiceProvider.GetService<IBettingService>();
        var bettingService2 = scope2.ServiceProvider.GetService<IBettingService>();
        Assert.NotNull(bettingService1);
        Assert.NotNull(bettingService2);
        Assert.NotSame(bettingService1, bettingService2); // Should be different in different scopes

        var sessionManager1 = scope1.ServiceProvider.GetService<ISessionManager>();
        var sessionManager2 = scope2.ServiceProvider.GetService<ISessionManager>();
        Assert.NotNull(sessionManager1);
        Assert.NotNull(sessionManager2);
        Assert.NotSame(sessionManager1, sessionManager2); // Should be different in different scopes

        var statisticsService1 = scope1.ServiceProvider.GetService<IStatisticsService>();
        var statisticsService2 = scope2.ServiceProvider.GetService<IStatisticsService>();
        Assert.NotNull(statisticsService1);
        Assert.NotNull(statisticsService2);
        Assert.NotSame(statisticsService1, statisticsService2); // Should be different in different scopes

        // Test singleton services - should be same across scopes
        var statisticsRepository1 = scope1.ServiceProvider.GetService<IStatisticsRepository>();
        var statisticsRepository2 = scope2.ServiceProvider.GetService<IStatisticsRepository>();
        Assert.NotNull(statisticsRepository1);
        Assert.NotNull(statisticsRepository2);
        Assert.Same(statisticsRepository1, statisticsRepository2); // Should be singleton

        var shoeManager1 = scope1.ServiceProvider.GetService<IShoeManager>();
        var shoeManager2 = scope2.ServiceProvider.GetService<IShoeManager>();
        Assert.NotNull(shoeManager1);
        Assert.NotNull(shoeManager2);
        Assert.NotSame(shoeManager1, shoeManager2); // Should be different in different scopes
    }

    [Fact]
    public void AddBlackjackServices_RegistersUtilityServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify utility services are registered
        var splitHandManager1 = serviceProvider.GetService<SplitHandManager>();
        var splitHandManager2 = serviceProvider.GetService<SplitHandManager>();
        Assert.NotNull(splitHandManager1);
        Assert.NotNull(splitHandManager2);
        Assert.NotSame(splitHandManager1, splitHandManager2); // Should be transient

        var playerActionValidator1 = serviceProvider.GetService<PlayerActionValidator>();
        var playerActionValidator2 = serviceProvider.GetService<PlayerActionValidator>();
        Assert.NotNull(playerActionValidator1);
        Assert.NotNull(playerActionValidator2);
        Assert.NotSame(playerActionValidator1, playerActionValidator2); // Should be transient

        // MoneyFormatter is a static class and doesn't need DI registration
    }

    [Fact]
    public void AddApplicationServices_RegistersNewEnhancedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructureServices();
        services.AddDefaultGameConfiguration();
        services.AddDomainServices();
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify new services are registered in application services
        Assert.NotNull(serviceProvider.GetService<IBettingService>());
        Assert.NotNull(serviceProvider.GetService<ISessionManager>());
        Assert.NotNull(serviceProvider.GetService<IStatisticsService>());
        Assert.NotNull(serviceProvider.GetService<IStatisticsRepository>());
        Assert.NotNull(serviceProvider.GetService<IShoeManager>());
        Assert.NotNull(serviceProvider.GetService<SplitHandManager>());
        Assert.NotNull(serviceProvider.GetService<PlayerActionValidator>());
    }

    [Fact]
    public void AddBlackjackServices_BettingServiceHasCorrectDependencies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify betting service can be created and has dependencies
        var bettingService = serviceProvider.GetService<IBettingService>();
        Assert.NotNull(bettingService);
        
        // Verify it has the expected properties
        Assert.True(bettingService.MinimumBet.Amount > 0);
        Assert.True(bettingService.MaximumBet.Amount > bettingService.MinimumBet.Amount);
        Assert.True(bettingService.BlackjackMultiplier > 1);
    }

    [Fact]
    public void AddBlackjackServices_SessionManagerCanBeCreated()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify session manager can be created
        var sessionManager = serviceProvider.GetService<ISessionManager>();
        Assert.NotNull(sessionManager);
        
        // Verify it can perform basic operations
        var currentSession = sessionManager.GetCurrentSessionAsync().Result;
        Assert.Null(currentSession); // Should be null initially
    }

    [Fact]
    public void AddBlackjackServices_StatisticsServiceCanBeCreated()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify statistics service can be created
        var statisticsService = serviceProvider.GetService<IStatisticsService>();
        Assert.NotNull(statisticsService);
        
        // Verify it can perform basic operations
        var playerCount = statisticsService.GetPlayerCountAsync().Result;
        Assert.True(playerCount >= 0); // Should be non-negative
    }

    [Fact]
    public void AddBlackjackServices_AllServicesCanBeResolvedInScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBlackjackServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Verify all services can be resolved within a scope
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Core services
        Assert.NotNull(scopedProvider.GetService<IGameService>());
        Assert.NotNull(scopedProvider.GetService<IGameOrchestrator>());
        Assert.NotNull(scopedProvider.GetService<IUserInterface>());

        // New enhanced services
        Assert.NotNull(scopedProvider.GetService<IBettingService>());
        Assert.NotNull(scopedProvider.GetService<ISessionManager>());
        Assert.NotNull(scopedProvider.GetService<IStatisticsService>());
        Assert.NotNull(scopedProvider.GetService<IShoeManager>());

        // Utility services
        Assert.NotNull(scopedProvider.GetService<SplitHandManager>());
        Assert.NotNull(scopedProvider.GetService<PlayerActionValidator>());

        // Singletons should be the same across scopes
        var statisticsRepo1 = scopedProvider.GetService<IStatisticsRepository>();
        var statisticsRepo2 = serviceProvider.GetService<IStatisticsRepository>();
        Assert.Same(statisticsRepo1, statisticsRepo2);
    }
}