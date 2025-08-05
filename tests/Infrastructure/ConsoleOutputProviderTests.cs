using System;
using System.Threading.Tasks;
using GroupProject.Infrastructure.Providers;
using Xunit;

namespace GroupProject.Tests.Infrastructure
{
    public class ConsoleOutputProviderTests
    {
        private readonly ConsoleOutputProvider _outputProvider;

        public ConsoleOutputProviderTests()
        {
            _outputProvider = new ConsoleOutputProvider();
        }

        [Fact]
        public async Task WriteLineAsync_WithNullMessage_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteLineAsync(null);
        }

        [Fact]
        public async Task WriteLineAsync_WithEmptyMessage_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteLineAsync("");
        }

        [Fact]
        public async Task WriteLineAsync_WithValidMessage_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteLineAsync("Test message");
        }

        [Fact]
        public async Task WriteAsync_WithNullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _outputProvider.WriteAsync(null!));
        }

        [Fact]
        public async Task WriteAsync_WithEmptyMessage_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteAsync("");
        }

        [Fact]
        public async Task WriteAsync_WithValidMessage_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteAsync("Test message");
        }

        [Fact]
        public async Task ClearAsync_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.ClearAsync();
        }

        [Fact]
        public async Task WriteLineAsync_WithFormat_WithNullFormat_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _outputProvider.WriteLineAsync(null!, "arg1"));
        }

        [Fact]
        public async Task WriteLineAsync_WithFormat_WithNullArgs_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _outputProvider.WriteLineAsync("Test {0}", null!));
        }

        [Fact]
        public async Task WriteLineAsync_WithFormat_WithValidParameters_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteLineAsync("Test {0} {1}", "arg1", "arg2");
        }

        [Fact]
        public async Task WriteAsync_WithFormat_WithNullFormat_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _outputProvider.WriteAsync(null!, "arg1"));
        }

        [Fact]
        public async Task WriteAsync_WithFormat_WithNullArgs_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _outputProvider.WriteAsync("Test {0}", null!));
        }

        [Fact]
        public async Task WriteAsync_WithFormat_WithValidParameters_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _outputProvider.WriteAsync("Test {0} {1}", "arg1", "arg2");
        }

        [Fact]
        public async Task MultipleOperations_ExecuteConcurrently_DoNotThrow()
        {
            // Arrange
            var tasks = new Task[10];

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    await _outputProvider.WriteLineAsync($"Message {index}");
                    await _outputProvider.WriteAsync($"Inline {index}");
                });
            }

            // Assert - Should not throw
            await Task.WhenAll(tasks);
        }
    }
}