using GroupProject.Infrastructure.Validation;
using Xunit;

namespace GroupProject.Tests.Infrastructure;

/// <summary>
/// Unit tests for the InputValidator utility class.
/// </summary>
public class InputValidatorTests
{
    [Theory]
    [InlineData("ValidName", true)]
    [InlineData("Player1", true)]
    [InlineData("Mary-Jane", true)]
    [InlineData("O'Connor", true)]
    [InlineData("Test.User", true)]
    [InlineData("User_123", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("ThisNameIsTooLongForValidation", false)]
    [InlineData("Invalid@Name", false)]
    [InlineData("Name#WithSymbols", false)]
    public void IsValidPlayerName_WithVariousInputs_ReturnsExpectedResult(string? input, bool expected)
    {
        // Act
        var result = InputValidator.IsValidPlayerName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  Test  ", "Test")]
    [InlineData("Multiple   Spaces", "Multiple Spaces")]
    [InlineData("Tab\tCharacter", "Tab Character")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void SanitizeString_WithVariousInputs_ReturnsCleanedString(string? input, string expected)
    {
        // Act
        var result = InputValidator.SanitizeString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "Player name cannot be empty.")]
    [InlineData("   ", "Player name cannot be empty.")]
    [InlineData(null, "Player name cannot be empty.")]
    [InlineData("ThisNameIsTooLongForValidationPurposes", "Player name must be 20 characters or less.")]
    [InlineData("Invalid@Name", "Player name can only contain letters, numbers, spaces, hyphens, underscores, apostrophes, and periods.")]
    public void GetPlayerNameErrorMessage_WithInvalidInputs_ReturnsAppropriateMessage(string? input, string expectedMessage)
    {
        // Act
        var result = InputValidator.GetPlayerNameErrorMessage(input);

        // Assert
        Assert.Equal(expectedMessage, result);
    }

    [Theory]
    [InlineData(5, 1, 10, true)]
    [InlineData(1, 1, 10, true)]
    [InlineData(10, 1, 10, true)]
    [InlineData(0, 1, 10, false)]
    [InlineData(11, 1, 10, false)]
    [InlineData(-5, 1, 10, false)]
    public void IsInRange_WithVariousValues_ReturnsExpectedResult(int value, int min, int max, bool expected)
    {
        // Act
        var result = InputValidator.IsInRange(value, min, max);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("5", 1, 10, true, 5)]
    [InlineData("1", 1, 10, true, 1)]
    [InlineData("10", 1, 10, true, 10)]
    [InlineData("0", 1, 10, false, 0)]
    [InlineData("11", 1, 10, false, 0)]
    [InlineData("abc", 1, 10, false, 0)]
    [InlineData("", 1, 10, false, 0)]
    [InlineData("  5  ", 1, 10, true, 5)]
    [InlineData("5 ", 1, 10, true, 5)]
    [InlineData(null, 1, 10, false, 0)]
    public void TryParseIntInRange_WithVariousInputs_ReturnsExpectedResult(string? input, int min, int max, bool expectedSuccess, int expectedValue)
    {
        // Act
        var success = InputValidator.TryParseIntInRange(input, min, max, out var result);

        // Assert
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result);
        }
    }

    [Theory]
    [InlineData("Test Input", "testinput")]
    [InlineData("  UPPER case  ", "uppercase")]
    [InlineData("Mixed-Case_Input", "mixed-case_input")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("Spaces   Between", "spacesbetween")]
    public void NormalizeInput_WithVariousInputs_ReturnsNormalizedString(string? input, string expected)
    {
        // Act
        var result = InputValidator.NormalizeInput(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ValidName", "ValidName")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("Test", "Test")]
    public void IsValidNonEmptyString_WithVariousInputs_ReturnsExpectedResult(string? input, string fieldName)
    {
        // Act
        var result = InputValidator.IsValidNonEmptyString(input, fieldName);

        // Assert
        Assert.Equal(!string.IsNullOrWhiteSpace(input), result);
    }
}