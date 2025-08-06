using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GroupProject.Application.Models;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using Xunit;

namespace GroupProject.Tests.Infrastructure.Providers;

/// <summary>
/// Tests for the ConfigurationManager class.
/// </summary>
public class ConfigurationManagerTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly ConfigurationManager _configManager;

    public ConfigurationManagerTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
        _configManager = new ConfigurationManager(_testConfigPath);
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }

        // Clean up any backup files
        var directory = Path.GetDirectoryName(_testConfigPath);
        if (directory != null)
        {
            var backupFiles = Directory.GetFiles(directory, Path.GetFileName(_testConfigPath) + ".backup.*");
            foreach (var backupFile in backupFiles)
            {
                try
                {
                    File.Delete(backupFile);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }

        _configManager.Dispose();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileDoesNotExist_ReturnsDefaultConfiguration()
    {
        // Act
        var config = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks);
        Assert.Equal(4, config.MaxPlayers);
        Assert.Equal(1, config.MinPlayers);
        Assert.True(config.AllowDoubleDown);
        Assert.False(config.AllowSplit);
        Assert.Equal(new Money(5m), config.MinimumBet);
        Assert.Equal(new Money(500m), config.MaximumBet);
        Assert.Equal(new Money(1000m), config.DefaultBankroll);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenFileDoesNotExist_CreatesConfigurationFile()
    {
        // Act
        await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.True(File.Exists(_testConfigPath));
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithValidConfiguration_SavesSuccessfully()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 8,
            MaxPlayers = 6,
            MinPlayers = 2,
            AllowDoubleDown = false,
            AllowSplit = true,
            MinimumBet = new Money(10m),
            MaximumBet = new Money(1000m),
            DefaultBankroll = new Money(2000m)
        };

        // Act
        await _configManager.SaveConfigurationAsync(config);

        // Assert
        Assert.True(File.Exists(_testConfigPath));
        var savedContent = await File.ReadAllTextAsync(_testConfigPath);
        Assert.Contains("\"numberOfDecks\": 8", savedContent);
        Assert.Contains("\"maxPlayers\": 6", savedContent);
        Assert.Contains("\"allowSplit\": true", savedContent);
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithInvalidConfiguration_ThrowsValidationException()
    {
        // Arrange
        var invalidConfig = new GameConfiguration
        {
            NumberOfDecks = 0, // Invalid
            MinimumBet = new Money(100m),
            MaximumBet = new Money(50m) // Less than minimum
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _configManager.SaveConfigurationAsync(invalidConfig));
        
        Assert.Contains("Cannot save invalid configuration", exception.Message);
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _configManager.SaveConfigurationAsync(null!));
    }

    [Fact]
    public async Task LoadConfigurationAsync_AfterSave_ReturnsCorrectConfiguration()
    {
        // Arrange
        var originalConfig = new GameConfiguration
        {
            NumberOfDecks = 4,
            MaxPlayers = 3,
            AllowDoubleDown = false,
            MinimumBet = new Money(25m),
            CardDisplayFormat = CardDisplayFormat.Text
        };

        // Act
        await _configManager.SaveConfigurationAsync(originalConfig);
        var loadedConfig = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.Equal(originalConfig.NumberOfDecks, loadedConfig.NumberOfDecks);
        Assert.Equal(originalConfig.MaxPlayers, loadedConfig.MaxPlayers);
        Assert.Equal(originalConfig.AllowDoubleDown, loadedConfig.AllowDoubleDown);
        Assert.Equal(originalConfig.MinimumBet, loadedConfig.MinimumBet);
        Assert.Equal(originalConfig.CardDisplayFormat, loadedConfig.CardDisplayFormat);
    }

    [Fact]
    public async Task GetSettingAsync_WithValidKey_ReturnsCorrectValue()
    {
        // Arrange
        var config = new GameConfiguration { NumberOfDecks = 8 };
        await _configManager.SaveConfigurationAsync(config);

        // Act
        var numberOfDecks = await _configManager.GetSettingAsync("NumberOfDecks", 6);

        // Assert
        Assert.Equal(8, numberOfDecks);
    }

    [Fact]
    public async Task GetSettingAsync_WithInvalidKey_ReturnsDefaultValue()
    {
        // Act
        var result = await _configManager.GetSettingAsync("NonExistentProperty", 42);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task GetSettingAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.GetSettingAsync<int>(null!, 0));
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.GetSettingAsync<int>("", 0));
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.GetSettingAsync<int>("   ", 0));
    }

    [Fact]
    public async Task SetSettingAsync_WithValidKeyAndValue_UpdatesConfiguration()
    {
        // Arrange
        await _configManager.LoadConfigurationAsync(); // Initialize with defaults

        // Act
        await _configManager.SetSettingAsync("NumberOfDecks", 8);

        // Assert
        var updatedValue = await _configManager.GetSettingAsync("NumberOfDecks", 6);
        Assert.Equal(8, updatedValue);
    }

    [Fact]
    public async Task SetSettingAsync_WithInvalidKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.SetSettingAsync("NonExistentProperty", 42));
        
        Assert.Contains("Property 'NonExistentProperty' not found", exception.Message);
    }

    [Fact]
    public async Task SetSettingAsync_WithNullOrEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.SetSettingAsync<int>(null!, 0));
        
        await Assert.ThrowsAsync<ArgumentException>(
            () => _configManager.SetSettingAsync<int>("", 0));
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ResetsConfigurationToDefaults()
    {
        // Arrange
        var customConfig = new GameConfiguration
        {
            NumberOfDecks = 8,
            MaxPlayers = 7,
            AllowSplit = true
        };
        await _configManager.SaveConfigurationAsync(customConfig);

        // Act
        await _configManager.ResetToDefaultsAsync();

        // Assert
        var resetConfig = await _configManager.LoadConfigurationAsync();
        var defaultConfig = new GameConfiguration();
        
        Assert.Equal(defaultConfig.NumberOfDecks, resetConfig.NumberOfDecks);
        Assert.Equal(defaultConfig.MaxPlayers, resetConfig.MaxPlayers);
        Assert.Equal(defaultConfig.AllowSplit, resetConfig.AllowSplit);
    }

    [Fact]
    public async Task ConfigurationExistsAsync_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        await _configManager.SaveConfigurationAsync(new GameConfiguration());

        // Act
        var exists = await _configManager.ConfigurationExistsAsync();

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ConfigurationExistsAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await _configManager.ConfigurationExistsAsync();

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var validConfig = new GameConfiguration();
        await _configManager.SaveConfigurationAsync(validConfig);

        // Act
        var errors = await _configManager.ValidateConfigurationAsync();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WithInvalidConfiguration_ReturnsErrors()
    {
        // Arrange - Create invalid config by directly writing JSON
        var invalidJson = """
        {
            "numberOfDecks": 0,
            "minPlayers": 5,
            "maxPlayers": 3,
            "minimumBet": { "amount": 100, "currency": "USD" },
            "maximumBet": { "amount": 50, "currency": "USD" }
        }
        """;
        
        await File.WriteAllTextAsync(_testConfigPath, invalidJson);

        // Act
        var errors = await _configManager.ValidateConfigurationAsync();

        // Assert
        Assert.NotEmpty(errors);
        var errorList = errors.ToList();
        Assert.Contains(errorList, e => e.Contains("Minimum bet must be less than maximum bet"));
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithCorruptedJson_CreatesBackupAndUsesDefaults()
    {
        // Arrange
        var corruptedJson = "{ invalid json content }";
        await File.WriteAllTextAsync(_testConfigPath, corruptedJson);

        // Act
        var config = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks); // Default value
        
        // Check that backup was created
        var directory = Path.GetDirectoryName(_testConfigPath);
        var backupFiles = Directory.GetFiles(directory!, Path.GetFileName(_testConfigPath) + ".backup.*");
        Assert.NotEmpty(backupFiles);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithEmptyFile_UsesDefaults()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "");

        // Act
        var config = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks); // Default value
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithWhitespaceOnlyFile_UsesDefaults()
    {
        // Arrange
        await File.WriteAllTextAsync(_testConfigPath, "   \n\t  ");

        // Act
        var config = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks); // Default value
    }

    [Fact]
    public async Task LoadConfigurationAsync_MultipleCalls_ReturnsSameConfiguration()
    {
        // Arrange
        var originalConfig = new GameConfiguration { NumberOfDecks = 8 };
        await _configManager.SaveConfigurationAsync(originalConfig);

        // Act
        var config1 = await _configManager.LoadConfigurationAsync();
        var config2 = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.Equal(config1.NumberOfDecks, config2.NumberOfDecks);
        Assert.NotSame(config1, config2); // Should be different instances (cloned)
    }

    [Fact]
    public async Task MoneyJsonConverter_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var config = new GameConfiguration
        {
            MinimumBet = new Money(25.50m, "EUR"),
            MaximumBet = new Money(1000m, "EUR"),
            DefaultBankroll = new Money(2500m, "EUR"),
            MinimumBankroll = new Money(100m, "EUR"),
            MaximumBankroll = new Money(10000m, "EUR")
        };

        // Act
        await _configManager.SaveConfigurationAsync(config);
        var loadedConfig = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.Equal(config.MinimumBet.Amount, loadedConfig.MinimumBet.Amount);
        Assert.Equal(config.MinimumBet.Currency, loadedConfig.MinimumBet.Currency);
        Assert.Equal(config.MaximumBet.Amount, loadedConfig.MaximumBet.Amount);
        Assert.Equal(config.MaximumBet.Currency, loadedConfig.MaximumBet.Currency);
        Assert.Equal(config.DefaultBankroll.Amount, loadedConfig.DefaultBankroll.Amount);
        Assert.Equal(config.DefaultBankroll.Currency, loadedConfig.DefaultBankroll.Currency);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WithPartiallyInvalidConfiguration_MergesWithDefaults()
    {
        // Arrange - Create config with some invalid values
        var partiallyInvalidJson = """
        {
            "numberOfDecks": 0,
            "maxPlayers": 6,
            "allowDoubleDown": true,
            "minimumBet": { "amount": 10, "currency": "USD" },
            "maximumBet": { "amount": 5, "currency": "USD" }
        }
        """;
        
        await File.WriteAllTextAsync(_testConfigPath, partiallyInvalidJson);

        // Act
        var config = await _configManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(config);
        Assert.Equal(6, config.NumberOfDecks); // Should be reset to default due to invalid value
        Assert.Equal(6, config.MaxPlayers); // Should preserve valid value
        Assert.True(config.AllowDoubleDown); // Should preserve valid value
        
        // Betting amounts should preserve valid values from JSON
        Assert.Equal(new Money(10m), config.MinimumBet);
        Assert.Equal(new Money(500m), config.MaximumBet); // Should be reset to default due to validation failure
    }

    [Fact]
    public async Task SetSettingAsync_WithMoneyValue_UpdatesCorrectly()
    {
        // Arrange
        var config = await _configManager.LoadConfigurationAsync(); // Initialize
        
        // Update all money values to use EUR to avoid currency mismatch
        config.MinimumBet = new Money(5m, "EUR");
        config.MaximumBet = new Money(500m, "EUR");
        config.DefaultBankroll = new Money(1000m, "EUR");
        config.MinimumBankroll = new Money(50m, "EUR");
        config.MaximumBankroll = new Money(10000m, "EUR");
        await _configManager.SaveConfigurationAsync(config);

        // Act
        await _configManager.SetSettingAsync("MinimumBet", new Money(15m, "EUR"));

        // Assert
        var updatedBet = await _configManager.GetSettingAsync("MinimumBet", new Money(5m));
        Assert.Equal(15m, updatedBet.Amount);
        Assert.Equal("EUR", updatedBet.Currency);
    }

    [Theory]
    [InlineData(CardDisplayFormat.Symbols)]
    [InlineData(CardDisplayFormat.Text)]
    public async Task SetSettingAsync_WithEnumValue_UpdatesCorrectly(CardDisplayFormat format)
    {
        // Arrange
        await _configManager.LoadConfigurationAsync(); // Initialize

        // Act
        await _configManager.SetSettingAsync("CardDisplayFormat", format);

        // Assert
        var updatedFormat = await _configManager.GetSettingAsync("CardDisplayFormat", CardDisplayFormat.Symbols);
        Assert.Equal(format, updatedFormat);
    }

    [Fact]
    public async Task ConfigurationManager_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subdir", "config.json");
        var configManager = new ConfigurationManager(nonExistentPath);

        try
        {
            // Act
            await configManager.SaveConfigurationAsync(new GameConfiguration());

            // Assert
            Assert.True(File.Exists(nonExistentPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(nonExistentPath))
            {
                File.Delete(nonExistentPath);
            }
            
            var directory = Path.GetDirectoryName(nonExistentPath);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            
            configManager.Dispose();
        }
    }
}