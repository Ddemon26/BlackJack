using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Interfaces;
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
}