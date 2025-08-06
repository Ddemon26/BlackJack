using GroupProject.Application.Models;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for managing game configuration persistence and retrieval.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Loads the game configuration from persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded configuration.</returns>
    Task<GameConfiguration> LoadConfigurationAsync();

    /// <summary>
    /// Saves the game configuration to persistent storage.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveConfigurationAsync(GameConfiguration configuration);

    /// <summary>
    /// Gets a specific setting value from the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>A task that represents the asynchronous get operation. The task result contains the setting value.</returns>
    Task<T> GetSettingAsync<T>(string key, T defaultValue);

    /// <summary>
    /// Sets a specific setting value in the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A task that represents the asynchronous set operation.</returns>
    Task SetSettingAsync<T>(string key, T value);

    /// <summary>
    /// Resets the configuration to default values.
    /// </summary>
    /// <returns>A task that represents the asynchronous reset operation.</returns>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Gets a value indicating whether a configuration file exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous check operation. The task result indicates whether the configuration exists.</returns>
    Task<bool> ConfigurationExistsAsync();

    /// <summary>
    /// Validates the current configuration and returns any validation errors.
    /// </summary>
    /// <returns>A task that represents the asynchronous validation operation. The task result contains validation errors if any.</returns>
    Task<IEnumerable<string>> ValidateConfigurationAsync();
}