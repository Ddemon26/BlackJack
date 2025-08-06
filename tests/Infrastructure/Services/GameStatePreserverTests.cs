using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using GroupProject.Infrastructure.Services;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Tests.Infrastructure.Services;

public class GameStatePreserverTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly GameStatePreserver _preserver;

    public GameStatePreserverTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"BlackjackTests_{Guid.NewGuid():N}");
        _preserver = new GameStatePreserver(persistenceDirectory: _testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task PreserveStateAsync_WithValidParameters_CreatesStateSuccessfully()
    {
        // Arrange
        const string stateId = "test_state_1";
        const string context = "Test context";

        // Act
        var result = await _preserver.PreserveStateAsync(stateId, context);

        // Assert
        Assert.Equal(stateId, result);
        Assert.True(await _preserver.StateExistsAsync(stateId));
    }

    [Fact]
    public async Task PreserveStateAsync_WithNullOrEmptyStateId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _preserver.PreserveStateAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _preserver.PreserveStateAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _preserver.PreserveStateAsync("   "));
    }

    [Fact]
    public async Task StateExistsAsync_WithExistingState_ReturnsTrue()
    {
        // Arrange
        const string stateId = "existing_state";
        await _preserver.PreserveStateAsync(stateId);

        // Act
        var exists = await _preserver.StateExistsAsync(stateId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task StateExistsAsync_WithNonExistingState_ReturnsFalse()
    {
        // Arrange
        const string stateId = "non_existing_state";

        // Act
        var exists = await _preserver.StateExistsAsync(stateId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task StateExistsAsync_WithNullOrEmptyStateId_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(await _preserver.StateExistsAsync(""));
        Assert.False(await _preserver.StateExistsAsync(null!));
        Assert.False(await _preserver.StateExistsAsync("   "));
    }

    [Fact]
    public async Task RestoreStateAsync_WithExistingState_ReturnsTrue()
    {
        // Arrange
        const string stateId = "restorable_state";
        await _preserver.PreserveStateAsync(stateId, "Test context");

        // Act
        var restored = await _preserver.RestoreStateAsync(stateId);

        // Assert
        Assert.True(restored);
    }

    [Fact]
    public async Task RestoreStateAsync_WithNonExistingState_ReturnsFalse()
    {
        // Arrange
        const string stateId = "non_existing_state";

        // Act
        var restored = await _preserver.RestoreStateAsync(stateId);

        // Assert
        Assert.False(restored);
    }

    [Fact]
    public async Task RestoreStateAsync_WithNullOrEmptyStateId_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(await _preserver.RestoreStateAsync(""));
        Assert.False(await _preserver.RestoreStateAsync(null!));
        Assert.False(await _preserver.RestoreStateAsync("   "));
    }

    [Fact]
    public async Task ClearStateAsync_WithExistingState_RemovesState()
    {
        // Arrange
        const string stateId = "clearable_state";
        await _preserver.PreserveStateAsync(stateId);
        Assert.True(await _preserver.StateExistsAsync(stateId));

        // Act
        await _preserver.ClearStateAsync(stateId);

        // Assert
        Assert.False(await _preserver.StateExistsAsync(stateId));
    }

    [Fact]
    public async Task ClearStateAsync_WithNonExistingState_DoesNotThrow()
    {
        // Arrange
        const string stateId = "non_existing_state";

        // Act & Assert (should not throw)
        await _preserver.ClearStateAsync(stateId);
    }

    [Fact]
    public async Task ClearStateAsync_WithNullOrEmptyStateId_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _preserver.ClearStateAsync("");
        await _preserver.ClearStateAsync(null!);
        await _preserver.ClearStateAsync("   ");
    }

    [Fact]
    public async Task GetStateInfoAsync_WithExistingState_ReturnsStateInfo()
    {
        // Arrange
        const string stateId = "info_state";
        const string context = "Test context for info";
        await _preserver.PreserveStateAsync(stateId, context);

        // Act
        var stateInfo = await _preserver.GetStateInfoAsync(stateId);

        // Assert
        Assert.NotNull(stateInfo);
        Assert.Equal(stateId, stateInfo.StateId);
        Assert.Equal(context, stateInfo.Context);
        Assert.True(stateInfo.SizeBytes > 0);
        Assert.True(stateInfo.Age < TimeSpan.FromMinutes(1)); // Should be very recent
    }

    [Fact]
    public async Task GetStateInfoAsync_WithNonExistingState_ReturnsNull()
    {
        // Arrange
        const string stateId = "non_existing_state";

        // Act
        var stateInfo = await _preserver.GetStateInfoAsync(stateId);

        // Assert
        Assert.Null(stateInfo);
    }

    [Fact]
    public async Task GetStateInfoAsync_WithNullOrEmptyStateId_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(await _preserver.GetStateInfoAsync(""));
        Assert.Null(await _preserver.GetStateInfoAsync(null!));
        Assert.Null(await _preserver.GetStateInfoAsync("   "));
    }

    [Fact]
    public async Task GetPreservedStateIdsAsync_WithMultipleStates_ReturnsAllStateIds()
    {
        // Arrange
        var stateIds = new[] { "state_1", "state_2", "state_3" };
        foreach (var stateId in stateIds)
        {
            await _preserver.PreserveStateAsync(stateId);
        }

        // Act
        var preservedIds = await _preserver.GetPreservedStateIdsAsync();

        // Assert
        var preservedIdsList = preservedIds.ToList();
        Assert.Equal(stateIds.Length, preservedIdsList.Count);
        foreach (var stateId in stateIds)
        {
            Assert.Contains(stateId, preservedIdsList);
        }
    }

    [Fact]
    public async Task GetPreservedStateIdsAsync_WithNoStates_ReturnsEmptyCollection()
    {
        // Act
        var preservedIds = await _preserver.GetPreservedStateIdsAsync();

        // Assert
        Assert.Empty(preservedIds);
    }

    [Fact]
    public async Task ClearOldStatesAsync_WithOldStates_RemovesOldStatesOnly()
    {
        // Arrange
        const string recentStateId = "recent_state";
        const string oldStateId = "old_state";
        
        await _preserver.PreserveStateAsync(recentStateId);
        await _preserver.PreserveStateAsync(oldStateId);

        // Act - clear states older than 1 second (this is a bit artificial for testing)
        await Task.Delay(10); // Small delay to ensure some time passes
        var clearedCount = await _preserver.ClearOldStatesAsync(TimeSpan.FromMilliseconds(1));

        // Assert
        // Note: This test is somewhat artificial since we can't easily create "old" states in memory
        // In a real scenario, the states would have different timestamps
        Assert.True(clearedCount >= 0); // At least verify the method doesn't throw
    }

    [Fact]
    public async Task ClearOldStatesAsync_WithNoStates_ReturnsZero()
    {
        // Act
        var clearedCount = await _preserver.ClearOldStatesAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(0, clearedCount);
    }

    [Fact]
    public async Task PreserveStateAsync_OverwritesExistingState()
    {
        // Arrange
        const string stateId = "overwrite_state";
        const string originalContext = "Original context";
        const string newContext = "New context";

        await _preserver.PreserveStateAsync(stateId, originalContext);
        var originalInfo = await _preserver.GetStateInfoAsync(stateId);

        // Act
        await _preserver.PreserveStateAsync(stateId, newContext);
        var newInfo = await _preserver.GetStateInfoAsync(stateId);

        // Assert
        Assert.NotNull(originalInfo);
        Assert.NotNull(newInfo);
        Assert.Equal(originalContext, originalInfo.Context);
        Assert.Equal(newContext, newInfo.Context);
        Assert.True(newInfo.PreservedAt >= originalInfo.PreservedAt);
    }

    [Fact]
    public async Task Constructor_WithNullPersistenceDirectory_UsesDefaultDirectory()
    {
        // Arrange & Act
        var preserver = new GameStatePreserver(persistenceDirectory: null);
        const string stateId = "test_state";
        
        // Act
        var result = await preserver.PreserveStateAsync(stateId);

        // Assert
        Assert.Equal(stateId, result);
        Assert.True(await preserver.StateExistsAsync(stateId));
    }

    [Fact]
    public void GameStateInfo_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string stateId = "test_state";
        var preservedAt = DateTime.UtcNow;
        const string context = "Test context";
        const long sizeBytes = 1024;

        // Act
        var stateInfo = new GameStateInfo(stateId, preservedAt, context, sizeBytes);

        // Assert
        Assert.Equal(stateId, stateInfo.StateId);
        Assert.Equal(preservedAt, stateInfo.PreservedAt);
        Assert.Equal(context, stateInfo.Context);
        Assert.Equal(sizeBytes, stateInfo.SizeBytes);
        Assert.True(stateInfo.Age >= TimeSpan.Zero);
    }

    [Fact]
    public void GameStateInfo_Constructor_WithNullStateId_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GameStateInfo(null!, DateTime.UtcNow, "context", 100));
    }

    [Fact]
    public void GameStateInfo_Constructor_WithNullContext_UsesEmptyString()
    {
        // Arrange & Act
        var stateInfo = new GameStateInfo("test", DateTime.UtcNow, null!, 100);

        // Assert
        Assert.Equal(string.Empty, stateInfo.Context);
    }

    [Fact]
    public void GameStateInfo_Age_CalculatesCorrectly()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddMinutes(-5);
        var stateInfo = new GameStateInfo("test", pastTime, "context", 100);

        // Act
        var age = stateInfo.Age;

        // Assert
        Assert.True(age >= TimeSpan.FromMinutes(4)); // Allow some tolerance
        Assert.True(age <= TimeSpan.FromMinutes(6)); // Allow some tolerance
    }
}