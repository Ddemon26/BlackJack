using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using GroupProject.Application.Services;
using GroupProject.Application.Interfaces;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.ValueObjects;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Tests.Application
{
    /// <summary>
    /// Tests for the ErrorHandler class.
    /// </summary>
    public class ErrorHandlerTests
    {
        private readonly Mock<IOutputProvider> _mockOutputProvider;
        private readonly ErrorHandler _errorHandler;

        public ErrorHandlerTests()
        {
            _mockOutputProvider = new Mock<IOutputProvider>();
            _errorHandler = new ErrorHandler(_mockOutputProvider.Object);
        }

        [Fact]
        public void Constructor_WithNullOutputProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ErrorHandler(null!));
        }

        [Fact]
        public async Task HandleExceptionAsync_WithNullException_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _errorHandler.HandleExceptionAsync(null!));
        }

        [Fact]
        public async Task HandleExceptionAsync_WithValidException_ReturnsUserFriendlyMessage()
        {
            // Arrange
            var exception = new InvalidPlayerActionException("Test error");

            // Act
            var result = await _errorHandler.HandleExceptionAsync(exception, "Test context");

            // Assert
            Assert.Contains("Invalid action", result);
            _mockOutputProvider.Verify(x => x.WriteLineAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LogErrorAsync_WithNullException_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _errorHandler.LogErrorAsync(null!));
        }

        [Fact]
        public async Task LogErrorAsync_WithValidException_CallsOutputProvider()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");

            // Act
            await _errorHandler.LogErrorAsync(exception, "Test context");

            // Assert
            _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s.Contains("[ERROR]"))), Times.Once);
        }

        [Fact]
        public async Task LogErrorAsync_WhenOutputProviderFails_FallsBackToConsole()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");
            _mockOutputProvider.Setup(x => x.WriteLineAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Output failed"));

            // Act & Assert - Should not throw
            await _errorHandler.LogErrorAsync(exception, "Test context");
        }

        [Theory]
        [InlineData(typeof(InvalidPlayerActionException), true)]
        [InlineData(typeof(InvalidGameStateException), true)]
        [InlineData(typeof(ArgumentException), true)]
        [InlineData(typeof(ArgumentOutOfRangeException), true)]
        [InlineData(typeof(InvalidOperationException), true)]
        [InlineData(typeof(ArgumentNullException), false)]
        [InlineData(typeof(OutOfMemoryException), false)]
        [InlineData(typeof(StackOverflowException), false)]
        [InlineData(typeof(AccessViolationException), false)]
        public void IsRecoverableError_WithDifferentExceptionTypes_ReturnsExpectedResult(Type exceptionType, bool expectedResult)
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;

            // Act
            var result = _errorHandler.IsRecoverableError(exception);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void IsRecoverableError_WithNullException_ReturnsFalse()
        {
            // Act
            var result = _errorHandler.IsRecoverableError(null);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(InvalidPlayerActionException), "Invalid action")]
        [InlineData(typeof(InvalidGameStateException), "Game state error")]
        [InlineData(typeof(ArgumentOutOfRangeException), "not within the valid range")]
        [InlineData(typeof(System.IO.IOException), "problem reading or writing data")]
        [InlineData(typeof(TimeoutException), "took too long to complete")]
        [InlineData(typeof(NotSupportedException), "not supported")]
        public void GetUserFriendlyMessage_WithDifferentExceptionTypes_ReturnsExpectedMessage(Type exceptionType, string expectedMessagePart)
        {
            // Arrange
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test message")!;

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains(expectedMessagePart, result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithNullException_ReturnsGenericMessage()
        {
            // Act
            var result = _errorHandler.GetUserFriendlyMessage(null);

            // Assert
            Assert.Equal("An unknown error occurred.", result);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithArgumentExceptionContainingPlayerName_ReturnsSpecificMessage()
        {
            // Arrange
            var exception = new ArgumentException("Player name cannot be null or empty.");

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains("valid player name", result);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithArgumentExceptionContainingCount_ReturnsSpecificMessage()
        {
            // Arrange
            var exception = new ArgumentException("Count must be greater than zero.");

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains("valid number of players", result);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithInvalidOperationExceptionContainingEmpty_ReturnsSpecificMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("The shoe is empty.");

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains("no cards available", result);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithInvalidOperationExceptionContainingConfiguration_ReturnsSpecificMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("Invalid game configuration detected.");

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains("configuration error", result);
        }

        [Fact]
        public void GetUserFriendlyMessage_WithUnknownException_ReturnsGenericMessage()
        {
            // Arrange
            var exception = new Exception("Some unknown error");

            // Act
            var result = _errorHandler.GetUserFriendlyMessage(exception);

            // Assert
            Assert.Contains("unexpected error occurred", result);
        }

        [Fact]
        public async Task HandleExceptionAsync_WithInnerException_LogsInnerExceptionDetails()
        {
            // Arrange
            var innerException = new ArgumentNullException("innerParam", "Inner exception message");
            var outerException = new InvalidOperationException("Outer exception message", innerException);

            // Act
            await _errorHandler.HandleExceptionAsync(outerException, "Test context");

            // Assert
            _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
                s.Contains("Inner Exception") && s.Contains("ArgumentNullException"))), Times.Once);
        }
    }
}