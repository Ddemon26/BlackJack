using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GroupProject.Infrastructure.Validation;

/// <summary>
/// Provides utility methods for input validation and sanitization.
/// </summary>
public static class InputValidator
{
    /// <summary>
    /// Validates that a string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="input">The input string to validate.</param>
    /// <param name="fieldName">The name of the field being validated (for error messages).</param>
    /// <returns>True if the input is valid, false otherwise.</returns>
    public static bool IsValidNonEmptyString(string? input, string fieldName = "Input")
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    /// <summary>
    /// Sanitizes a string by removing control characters and trimming whitespace.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string SanitizeString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove control characters but preserve normal whitespace
        var sanitized = new string(input.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
        
        // Trim and normalize whitespace
        return Regex.Replace(sanitized.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Validates that a player name meets the requirements.
    /// </summary>
    /// <param name="name">The player name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValidPlayerName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var sanitized = SanitizeString(name);
        
        // Check length constraints
        if (sanitized.Length < 1 || sanitized.Length > 20)
            return false;

        // Check for valid characters (letters, numbers, spaces, basic punctuation)
        return Regex.IsMatch(sanitized, @"^[a-zA-Z0-9\s\-_'.]+$");
    }

    /// <summary>
    /// Gets a user-friendly error message for invalid player names.
    /// </summary>
    /// <param name="name">The invalid player name.</param>
    /// <returns>A descriptive error message.</returns>
    public static string GetPlayerNameErrorMessage(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Player name cannot be empty.";

        var sanitized = SanitizeString(name);
        
        if (sanitized.Length > 20)
            return "Player name must be 20 characters or less.";

        if (!Regex.IsMatch(sanitized, @"^[a-zA-Z0-9\s\-_'.]+$"))
            return "Player name can only contain letters, numbers, spaces, hyphens, underscores, apostrophes, and periods.";

        return "Player name is invalid.";
    }

    /// <summary>
    /// Validates that an integer is within the specified range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <returns>True if the value is within range, false otherwise.</returns>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Attempts to parse a string as an integer with bounds checking.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="result">The parsed integer value if successful.</param>
    /// <returns>True if parsing was successful and the value is within bounds, false otherwise.</returns>
    public static bool TryParseIntInRange(string? input, int min, int max, out int result)
    {
        result = 0;
        
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var sanitized = SanitizeString(input).Replace(" ", "");
        
        if (!int.TryParse(sanitized, out result))
            return false;

        return IsInRange(result, min, max);
    }

    /// <summary>
    /// Normalizes user input for consistent comparison.
    /// </summary>
    /// <param name="input">The input to normalize.</param>
    /// <returns>The normalized input string.</returns>
    public static string NormalizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return SanitizeString(input).ToLowerInvariant().Replace(" ", "");
    }
}