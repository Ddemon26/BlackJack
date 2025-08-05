using System;
using Xunit;
using GroupProject.Domain.Exceptions;

namespace GroupProject.Tests.Domain.Exceptions
{
    /// <summary>
    /// Tests for the GameException base class functionality.
    /// </summary>
    public class GameExceptionTests
    {
        /// <summary>
        /// Concrete implementation of GameException for testing purposes.
        /// </summary>
        private class TestGameException : GameException
        {
            public TestGameException(string message) : base(message) { }
            public TestGameException(string message, Exception innerException) : base(message, innerException) { }
        }

        [Fact]
        public void Constructor_WithMessage_SetsMessageProperty()
        {
            // Arrange
            const string expectedMessage = "Test game exception message";

            // Act
            var exception = new TestGameException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            const string expectedMessage = "Test game exception message";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new TestGameException(expectedMessage, innerException);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void GameException_InheritsFromException()
        {
            // Arrange & Act
            var exception = new TestGameException("Test message");

            // Assert
            Assert.IsAssignableFrom<Exception>(exception);
        }

        [Fact]
        public void GameException_IsAbstractClass()
        {
            // Arrange & Act
            var type = typeof(GameException);

            // Assert
            Assert.True(type.IsAbstract);
        }
    }
}