using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupProject.Infrastructure.Services;

/// <summary>
/// Implementation of IGameStatePreserver that stores game state snapshots in memory and optionally persists to disk.
/// Provides fast access for error recovery while maintaining persistence for longer-term recovery scenarios.
/// </summary>
public class GameStatePreserver : IGameStatePreserver
{
    private readonly ILogger<GameStatePreserver>? _logger;
    private readonly ConcurrentDictionary<string, PreservedState> _stateCache;
    private readonly string _persistenceDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the GameStatePreserver class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    /// <param name="persistenceDirectory">Directory for persisting state snapshots. If null, only in-memory storage is used.</param>
    public GameStatePreserver(ILogger<GameStatePreserver>? logger = null, string? persistenceDirectory = null)
    {
        _logger = logger;
        _stateCache = new ConcurrentDictionary<string, PreservedState>();
        _persistenceDirectory = persistenceDirectory ?? Path.Combine(Path.GetTempPath(), "BlackjackGameStates");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Ensure persistence directory exists
        if (!string.IsNullOrEmpty(_persistenceDirectory))
        {
            try
            {
                Directory.CreateDirectory(_persistenceDirectory);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create persistence directory: {Directory}", _persistenceDirectory);
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> PreserveStateAsync(string stateId, string context = "")
    {
        if (string.IsNullOrWhiteSpace(stateId))
            throw new ArgumentException("State ID cannot be null or empty.", nameof(stateId));

        try
        {
            // For now, we'll create a placeholder state object
            // In a real implementation, this would capture the actual game state
            var stateData = new
            {
                StateId = stateId,
                Timestamp = DateTime.UtcNow,
                Context = context,
                // TODO: Add actual game state properties when game state objects are available
                Placeholder = "Game state would be captured here"
            };

            var json = JsonSerializer.Serialize(stateData, _jsonOptions);
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);

            var preservedState = new PreservedState(
                stateId,
                DateTime.UtcNow,
                context,
                json,
                sizeBytes
            );

            // Store in memory cache
            _stateCache.AddOrUpdate(stateId, preservedState, (key, oldValue) => preservedState);

            // Optionally persist to disk
            if (!string.IsNullOrEmpty(_persistenceDirectory))
            {
                await PersistStateToDiskAsync(stateId, json);
            }

            _logger?.LogDebug("Preserved game state: {StateId} (Context: {Context}, Size: {Size} bytes)", 
                stateId, context, sizeBytes);

            return stateId;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to preserve game state: {StateId}", stateId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreStateAsync(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return false;

        try
        {
            // Try to get from memory cache first
            if (_stateCache.TryGetValue(stateId, out var cachedState))
            {
                _logger?.LogDebug("Restored game state from cache: {StateId}", stateId);
                // TODO: Apply the restored state to the actual game objects
                return true;
            }

            // Try to load from disk if not in cache
            if (!string.IsNullOrEmpty(_persistenceDirectory))
            {
                var json = await LoadStateFromDiskAsync(stateId);
                if (!string.IsNullOrEmpty(json))
                {
                    // Deserialize and cache the state
                    var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(json);
                    var restoredState = new PreservedState(
                        stateId,
                        DateTime.UtcNow, // We don't have the original timestamp from disk
                        "Restored from disk",
                        json,
                        sizeBytes
                    );

                    _stateCache.TryAdd(stateId, restoredState);
                    
                    _logger?.LogDebug("Restored game state from disk: {StateId}", stateId);
                    // TODO: Apply the restored state to the actual game objects
                    return true;
                }
            }

            _logger?.LogWarning("Game state not found for restoration: {StateId}", stateId);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to restore game state: {StateId}", stateId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ClearStateAsync(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return;

        try
        {
            // Remove from memory cache
            _stateCache.TryRemove(stateId, out _);

            // Remove from disk if it exists
            if (!string.IsNullOrEmpty(_persistenceDirectory))
            {
                await DeleteStateFromDiskAsync(stateId);
            }

            _logger?.LogDebug("Cleared game state: {StateId}", stateId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear game state: {StateId}", stateId);
        }
    }

    /// <inheritdoc />
    public Task<bool> StateExistsAsync(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return Task.FromResult(false);

        // Check memory cache first
        if (_stateCache.ContainsKey(stateId))
            return Task.FromResult(true);

        // Check disk if persistence is enabled
        if (!string.IsNullOrEmpty(_persistenceDirectory))
        {
            var filePath = GetStateFilePath(stateId);
            return Task.FromResult(File.Exists(filePath));
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<GameStateInfo?> GetStateInfoAsync(string stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
            return Task.FromResult<GameStateInfo?>(null);

        if (_stateCache.TryGetValue(stateId, out var state))
        {
            var info = new GameStateInfo(
                state.StateId,
                state.PreservedAt,
                state.Context,
                state.SizeBytes
            );
            return Task.FromResult<GameStateInfo?>(info);
        }

        return Task.FromResult<GameStateInfo?>(null);
    }

    /// <inheritdoc />
    public Task<int> ClearOldStatesAsync(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var clearedCount = 0;

        try
        {
            // Clear old states from memory cache
            var oldStates = _stateCache
                .Where(kvp => kvp.Value.PreservedAt < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var stateId in oldStates)
            {
                if (_stateCache.TryRemove(stateId, out _))
                {
                    clearedCount++;
                }
            }

            // Clear old states from disk
            if (!string.IsNullOrEmpty(_persistenceDirectory) && Directory.Exists(_persistenceDirectory))
            {
                var files = Directory.GetFiles(_persistenceDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTimeUtc < cutoffTime)
                        {
                            File.Delete(file);
                            clearedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to delete old state file: {File}", file);
                    }
                }
            }

            _logger?.LogDebug("Cleared {Count} old game states older than {MaxAge}", clearedCount, maxAge);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear old game states");
        }

        return Task.FromResult(clearedCount);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetPreservedStateIdsAsync()
    {
        var stateIds = _stateCache.Keys.ToList();
        return Task.FromResult<IEnumerable<string>>(stateIds);
    }

    /// <summary>
    /// Persists a state snapshot to disk.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <param name="json">The JSON representation of the state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task PersistStateToDiskAsync(string stateId, string json)
    {
        try
        {
            var filePath = GetStateFilePath(stateId);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to persist state to disk: {StateId}", stateId);
        }
    }

    /// <summary>
    /// Loads a state snapshot from disk.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>A task that returns the JSON representation of the state, or null if not found.</returns>
    private async Task<string?> LoadStateFromDiskAsync(string stateId)
    {
        try
        {
            var filePath = GetStateFilePath(stateId);
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load state from disk: {StateId}", stateId);
        }

        return null;
    }

    /// <summary>
    /// Deletes a state snapshot from disk.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteStateFromDiskAsync(string stateId)
    {
        try
        {
            var filePath = GetStateFilePath(stateId);
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete state from disk: {StateId}", stateId);
        }
    }

    /// <summary>
    /// Gets the file path for a state snapshot.
    /// </summary>
    /// <param name="stateId">The state identifier.</param>
    /// <returns>The full file path for the state snapshot.</returns>
    private string GetStateFilePath(string stateId)
    {
        var sanitizedId = string.Join("_", stateId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_persistenceDirectory, $"{sanitizedId}.json");
    }

    /// <summary>
    /// Represents a preserved game state.
    /// </summary>
    private class PreservedState
    {
        public PreservedState(string stateId, DateTime preservedAt, string context, string jsonData, long sizeBytes)
        {
            StateId = stateId;
            PreservedAt = preservedAt;
            Context = context;
            JsonData = jsonData;
            SizeBytes = sizeBytes;
        }

        public string StateId { get; }
        public DateTime PreservedAt { get; }
        public string Context { get; }
        public string JsonData { get; }
        public long SizeBytes { get; }
    }
}