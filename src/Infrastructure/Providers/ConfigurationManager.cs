using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupProject.Application.Models;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Infrastructure.Providers;

/// <summary>
/// File-based configuration manager that persists game configuration using JSON serialization.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly string _configurationFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private GameConfiguration? _cachedConfiguration;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationManager"/> class.
    /// </summary>
    /// <param name="configurationFilePath">The path to the configuration file. If null, uses default path.</param>
    public ConfigurationManager(string? configurationFilePath = null)
    {
        _configurationFilePath = configurationFilePath ?? GetDefaultConfigurationPath();
        _jsonOptions = CreateJsonSerializerOptions();
    }

    /// <summary>
    /// Loads the game configuration from persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded configuration.</returns>
    public async Task<GameConfiguration> LoadConfigurationAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (_cachedConfiguration != null)
            {
                return _cachedConfiguration.Clone();
            }

            if (!File.Exists(_configurationFilePath))
            {
                _cachedConfiguration = new GameConfiguration();
                await SaveConfigurationInternalAsync(_cachedConfiguration);
                return _cachedConfiguration.Clone();
            }

            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _cachedConfiguration = new GameConfiguration();
                await SaveConfigurationInternalAsync(_cachedConfiguration);
                return _cachedConfiguration.Clone();
            }

            try
            {
                var configuration = JsonSerializer.Deserialize<GameConfiguration>(jsonContent, _jsonOptions);
                if (configuration == null)
                {
                    _cachedConfiguration = new GameConfiguration();
                    await SaveConfigurationInternalAsync(_cachedConfiguration);
                    return _cachedConfiguration.Clone();
                }

                // Validate the loaded configuration
                var validationResults = configuration.Validate();
                if (validationResults.Any())
                {
                    // If validation fails, use defaults but preserve valid settings
                    _cachedConfiguration = MergeWithDefaults(configuration);
                    await SaveConfigurationInternalAsync(_cachedConfiguration);
                }
                else
                {
                    _cachedConfiguration = configuration;
                }

                return _cachedConfiguration.Clone();
            }
            catch (JsonException)
            {
                // If JSON is corrupted, create backup and use defaults
                await CreateBackupAsync();
                _cachedConfiguration = new GameConfiguration();
                await SaveConfigurationInternalAsync(_cachedConfiguration);
                return _cachedConfiguration.Clone();
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Saves the game configuration to persistent storage.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveConfigurationAsync(GameConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Validate configuration before saving
        var validationResults = configuration.Validate();
        if (validationResults.Any())
        {
            var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            throw new ValidationException($"Cannot save invalid configuration: {errors}");
        }

        await _fileLock.WaitAsync();
        try
        {
            await SaveConfigurationInternalAsync(configuration);
            _cachedConfiguration = configuration.Clone();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Gets a specific setting value from the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains the setting value.</returns>
    public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        var configuration = await LoadConfigurationAsync();
        
        try
        {
            var property = typeof(GameConfiguration).GetProperty(key);
            if (property == null || !property.CanRead)
            {
                return defaultValue;
            }

            var value = property.GetValue(configuration);
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert the value
            if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Sets a specific setting value in the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    public async Task SetSettingAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        var configuration = await LoadConfigurationAsync();
        
        var property = typeof(GameConfiguration).GetProperty(key);
        if (property == null || !property.CanWrite)
        {
            throw new ArgumentException($"Property '{key}' not found or is read-only.", nameof(key));
        }

        if (value != null && !property.PropertyType.IsAssignableFrom(typeof(T)))
        {
            throw new ArgumentException($"Value type '{typeof(T)}' is not compatible with property type '{property.PropertyType}'.", nameof(value));
        }

        property.SetValue(configuration, value);
        await SaveConfigurationAsync(configuration);
    }

    /// <summary>
    /// Resets the configuration to default values.
    /// </summary>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    public async Task ResetToDefaultsAsync()
    {
        var defaultConfiguration = new GameConfiguration();
        await SaveConfigurationAsync(defaultConfiguration);
    }

    /// <summary>
    /// Gets a value indicating whether a configuration file exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous check operation. The task result indicates whether the configuration exists.</returns>
    public Task<bool> ConfigurationExistsAsync()
    {
        return Task.FromResult(File.Exists(_configurationFilePath));
    }

    /// <summary>
    /// Validates the current configuration and returns any validation errors.
    /// </summary>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains validation errors if any.</returns>
    public async Task<IEnumerable<string>> ValidateConfigurationAsync()
    {
        var configuration = await LoadConfigurationAsync();
        var validationResults = configuration.Validate();
        return validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error");
    }

    /// <summary>
    /// Internal method to save configuration without locking (assumes caller has lock).
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    private async Task SaveConfigurationInternalAsync(GameConfiguration configuration)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_configurationFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var jsonContent = JsonSerializer.Serialize(configuration, _jsonOptions);
        await File.WriteAllTextAsync(_configurationFilePath, jsonContent);
    }

    /// <summary>
    /// Creates a backup of the current configuration file.
    /// </summary>
    /// <returns>A task that represents the asynchronous backup operation.</returns>
    private async Task CreateBackupAsync()
    {
        if (!File.Exists(_configurationFilePath))
            return;

        var backupPath = $"{_configurationFilePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
        try
        {
            await File.WriteAllTextAsync(backupPath, await File.ReadAllTextAsync(_configurationFilePath));
        }
        catch
        {
            // Ignore backup failures - not critical
        }
    }

    /// <summary>
    /// Merges a potentially invalid configuration with defaults, preserving valid settings.
    /// </summary>
    /// <param name="configuration">The configuration to merge.</param>
    /// <returns>A new configuration with valid settings preserved and invalid ones reset to defaults.</returns>
    private static GameConfiguration MergeWithDefaults(GameConfiguration configuration)
    {
        var defaultConfig = new GameConfiguration();
        var mergedConfig = new GameConfiguration();

        // Use reflection to copy valid properties
        var properties = typeof(GameConfiguration).GetProperties()
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(configuration);
                property.SetValue(mergedConfig, value);
                
                // Validate individual property by creating a temp config and checking
                var tempConfig = new GameConfiguration();
                property.SetValue(tempConfig, value);
                
                if (tempConfig.Validate().Any(vr => vr.MemberNames?.Contains(property.Name) == true))
                {
                    // If this property causes validation errors, use default
                    var defaultValue = property.GetValue(defaultConfig);
                    property.SetValue(mergedConfig, defaultValue);
                }
            }
            catch
            {
                // If any error occurs, use default value
                var defaultValue = property.GetValue(defaultConfig);
                property.SetValue(mergedConfig, defaultValue);
            }
        }

        return mergedConfig;
    }

    /// <summary>
    /// Gets the default configuration file path.
    /// </summary>
    /// <returns>The default path for the configuration file.</returns>
    private static string GetDefaultConfigurationPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "BlackjackGame");
        return Path.Combine(appFolder, "gameconfig.json");
    }

    /// <summary>
    /// Creates JSON serializer options for configuration serialization.
    /// </summary>
    /// <returns>Configured JSON serializer options.</returns>
    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        // Add custom converter for Money type
        options.Converters.Add(new MoneyJsonConverter());
        
        return options;
    }

    /// <summary>
    /// Disposes the configuration manager and releases resources.
    /// </summary>
    public void Dispose()
    {
        _fileLock?.Dispose();
    }
}

/// <summary>
/// JSON converter for the Money value object.
/// </summary>
public class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            decimal amount = 0m;
            string currency = "USD";

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName?.ToLowerInvariant())
                    {
                        case "amount":
                            amount = reader.GetDecimal();
                            break;
                        case "currency":
                            currency = reader.GetString() ?? "USD";
                            break;
                    }
                }
            }

            return new Money(amount, currency);
        }

        // Fallback: try to read as decimal (amount only)
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new Money(reader.GetDecimal());
        }

        throw new JsonException("Invalid Money format");
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}