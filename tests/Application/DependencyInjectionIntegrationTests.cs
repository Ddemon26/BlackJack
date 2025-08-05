using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Interfaces;
using GroupProject.Infrastructure.Extensions;
using GroupProject.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GroupProject.Tests.Application;

/// <summary>
/// Integration tests for dependency injection setup and configuration.
/// </summary>
public class DependencyInjectionIntegrationTests
{
    [Fact]
    public void ServiceCollection_WithAllServices_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        // Act
        services.AddBlackjackServices(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all services can be resolved
        Assert.NotNull(serviceProvider.GetService<IGameService>());
        Assert.NotNull(serviceProvider.GetService<IGameOrchestrator>());
        Assert.NotNull(serviceProvider.GetService<IUserInterface>());
        Assert.NotNull(serviceProvider.GetService<IErrorHandler>());
        Assert.NotNull(serviceProvider.GetService<IRandomProvider>());
        Assert.NotNull(serviceProvider.GetService<IShoe>());
        Assert.NotNull(serviceProvider.GetService<IGameRules>());
        Assert.NotNull(serviceProvider.GetService<GameConfiguration>());
    }

    [Fact]
    public void ServiceCollection_WithConfiguration_BindsConfigurationCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["GameConfiguration:NumberOfDecks"] = "8",
            ["GameConfiguration:MaxPlayers"] = "6",
            ["GameConfiguration:MinPlayers"] = "2",
            ["GameConfiguration:AllowDoubleDown"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        services.AddBlackjackServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var gameConfig = serviceProvider.GetRequiredService<GameConfiguration>();
        Assert.Equal(8, gameConfig.NumberOfDecks);
        Assert.Equal(6, gameConfig.MaxPlayers);
        Assert.Equal(2, gameConfig.MinPlayers);
        Assert.False(gameConfig.AllowDoubleDown);
    }

    [Fact]
    public void ServiceCollection_WithDefaultConfiguration_UsesDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(); // Empty configuration

        // Act
        services.AddBlackjackServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var gameConfig = serviceProvider.GetRequiredService<GameConfiguration>();
        Assert.Equal(6, gameConfig.NumberOfDecks); // Default value
        Assert.Equal(4, gameConfig.MaxPlayers);    // Default value
        Assert.Equal(1, gameConfig.MinPlayers);    // Default value
        Assert.True(gameConfig.AllowDoubleDown);   // Default value
    }

    [Fact]
    public void CompleteApplicationFlow_WithDependencyInjection_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddBlackjackServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act - Get services and perform basic operations
        var gameService = serviceProvider.GetRequiredService<IGameService>();
        var gameRules = serviceProvider.GetRequiredService<IGameRules>();
        var shoe = serviceProvider.GetRequiredService<IShoe>();

        // Test basic game flow
        gameService.StartNewGame(new[] { "TestPlayer" });
        gameService.DealInitialCards();

        var player = gameService.GetPlayer("TestPlayer");
        var dealer = gameService.GetDealer();

        // Assert
        Assert.NotNull(player);
        Assert.NotNull(dealer);
        Assert.Equal(2, player.GetCardCount());
        Assert.Equal(2, dealer.GetCardCount());
        Assert.True(gameService.IsGameInProgress);
    }

    [Fact]
    public void ErrorHandler_Integration_HandlesExceptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration();

        services.AddBlackjackServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var errorHandler = serviceProvider.GetRequiredService<IErrorHandler>();

        // Act & Assert
        var testException = new InvalidOperationException("Test exception");
        var userMessage = errorHandler.HandleExceptionAsync(testException, "Test context").Result;

        Assert.NotNull(userMessage);
        Assert.Contains("error", userMessage.ToLower());
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configurationData = new Dictionary<string, string?>();
        
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
    }
}