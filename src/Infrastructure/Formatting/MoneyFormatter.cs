using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;

namespace GroupProject.Infrastructure.Formatting;

/// <summary>
/// High-performance money formatting utilities with caching and cultural formatting support.
/// Provides efficient methods for formatting monetary amounts for display with consistent styling.
/// </summary>
/// <remarks>
/// This class provides culturally-aware formatting for monetary amounts with support for
/// different display styles (symbol, detailed, compact). It uses StringBuilder pooling for
/// efficient multi-amount formatting operations and caches common currency symbols.
/// All formatting methods are thread-safe due to the immutable nature of the cached data.
/// </remarks>
public static class MoneyFormatter
{
    // Common currency symbols for better display
    private static readonly Dictionary<string, string> _currencySymbols = new()
    {
        { "USD", "$" },
        { "EUR", "€" },
        { "GBP", "£" },
        { "JPY", "¥" },
        { "CAD", "C$" },
        { "AUD", "A$" },
        { "CHF", "CHF" },
        { "CNY", "¥" },
        { "SEK", "kr" },
        { "NOK", "kr" },
        { "DKK", "kr" },
        { "PLN", "zł" },
        { "CZK", "Kč" },
        { "HUF", "Ft" },
        { "RUB", "₽" },
        { "BRL", "R$" },
        { "INR", "₹" },
        { "KRW", "₩" },
        { "SGD", "S$" },
        { "HKD", "HK$" },
        { "NZD", "NZ$" },
        { "MXN", "MX$" },
        { "ZAR", "R" },
        { "TRY", "₺" },
        { "ILS", "₪" }
    };

    // Culture info cache for performance
    private static readonly Dictionary<string, CultureInfo> _cultureCache = new();

    static MoneyFormatter()
    {
        InitializeCultureCache();
    }

    /// <summary>
    /// Formats money with currency symbol (e.g., "$25.50").
    /// Uses cached currency symbols for optimal performance.
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>A formatted string with currency symbol and amount.</returns>
    public static string FormatWithSymbol(Money money)
    {
        var symbol = GetCurrencySymbol(money.Currency);
        return $"{symbol}{money.Amount:F2}";
    }

    /// <summary>
    /// Formats money with currency code (e.g., "25.50 USD").
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>A formatted string with amount and currency code.</returns>
    public static string FormatWithCode(Money money)
    {
        return $"{money.Amount:F2} {money.Currency}";
    }

    /// <summary>
    /// Formats money in a detailed format (e.g., "25.50 US Dollars").
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>A detailed formatted string with amount and full currency name.</returns>
    public static string FormatDetailed(Money money)
    {
        var currencyName = GetCurrencyName(money.Currency);
        return $"{money.Amount:F2} {currencyName}";
    }

    /// <summary>
    /// Formats money in a compact format for display in limited space (e.g., "$25.5K").
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>A compact formatted string with abbreviated large amounts.</returns>
    public static string FormatCompact(Money money)
    {
        var symbol = GetCurrencySymbol(money.Currency);
        var amount = money.Amount;
        
        return Math.Abs(amount) switch
        {
            >= 1_000_000_000 => $"{symbol}{amount / 1_000_000_000:F1}B",
            >= 1_000_000 => $"{symbol}{amount / 1_000_000:F1}M",
            >= 1_000 => $"{symbol}{amount / 1_000:F1}K",
            _ => $"{symbol}{amount:F2}"
        };
    }

    /// <summary>
    /// Formats money using the culture-specific formatting for the currency.
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>A culture-specific formatted string.</returns>
    public static string FormatCultureSpecific(Money money)
    {
        var culture = GetCultureForCurrency(money.Currency);
        return money.Amount.ToString("C", culture);
    }

    /// <summary>
    /// Formats money for accounting display with parentheses for negative amounts.
    /// </summary>
    /// <param name="money">The money amount to format.</param>
    /// <returns>An accounting-style formatted string.</returns>
    public static string FormatAccounting(Money money)
    {
        var symbol = GetCurrencySymbol(money.Currency);
        var amount = Math.Abs(money.Amount);
        
        if (money.IsNegative)
            return $"({symbol}{amount:F2})";
        
        return $"{symbol}{amount:F2}";
    }

    /// <summary>
    /// Formats a collection of money amounts with consistent alignment.
    /// </summary>
    /// <param name="amounts">The money amounts to format.</param>
    /// <param name="style">The formatting style to use.</param>
    /// <param name="separator">The separator to use between amounts.</param>
    /// <returns>A formatted string representation of all amounts.</returns>
    public static string FormatMultiple(IEnumerable<Money> amounts, MoneyFormatStyle style = MoneyFormatStyle.WithSymbol, string separator = ", ")
    {
        if (amounts == null)
            return string.Empty;

        var sb = StringBuilderPool.Get();
        try
        {
            var first = true;
            foreach (var money in amounts)
            {
                if (!first)
                    sb.Append(separator);

                sb.Append(style switch
                {
                    MoneyFormatStyle.WithSymbol => FormatWithSymbol(money),
                    MoneyFormatStyle.WithCode => FormatWithCode(money),
                    MoneyFormatStyle.Detailed => FormatDetailed(money),
                    MoneyFormatStyle.Compact => FormatCompact(money),
                    MoneyFormatStyle.CultureSpecific => FormatCultureSpecific(money),
                    MoneyFormatStyle.Accounting => FormatAccounting(money),
                    _ => FormatWithSymbol(money)
                });

                first = false;
            }

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Formats a money amount with a label for display.
    /// </summary>
    /// <param name="label">The label to display.</param>
    /// <param name="money">The money amount to format.</param>
    /// <param name="style">The formatting style to use.</param>
    /// <returns>A formatted string with label and amount.</returns>
    public static string FormatWithLabel(string label, Money money, MoneyFormatStyle style = MoneyFormatStyle.WithSymbol)
    {
        var formattedAmount = style switch
        {
            MoneyFormatStyle.WithSymbol => FormatWithSymbol(money),
            MoneyFormatStyle.WithCode => FormatWithCode(money),
            MoneyFormatStyle.Detailed => FormatDetailed(money),
            MoneyFormatStyle.Compact => FormatCompact(money),
            MoneyFormatStyle.CultureSpecific => FormatCultureSpecific(money),
            MoneyFormatStyle.Accounting => FormatAccounting(money),
            _ => FormatWithSymbol(money)
        };

        return $"{label}: {formattedAmount}";
    }

    /// <summary>
    /// Formats a money difference showing the change with appropriate indicators.
    /// </summary>
    /// <param name="current">The current money amount.</param>
    /// <param name="previous">The previous money amount.</param>
    /// <returns>A formatted string showing the difference with indicators.</returns>
    public static string FormatDifference(Money current, Money previous)
    {
        if (current.Currency != previous.Currency)
            throw new InvalidOperationException("Cannot format difference for different currencies.");

        var difference = current - previous;
        var symbol = GetCurrencySymbol(current.Currency);
        
        if (difference.IsZero)
            return $"No change ({FormatWithSymbol(current)})";
        
        var indicator = difference.IsPositive ? "+" : "";
        var arrow = difference.IsPositive ? "↑" : "↓";
        
        return $"{FormatWithSymbol(current)} ({indicator}{symbol}{Math.Abs(difference.Amount):F2} {arrow})";
    }

    /// <summary>
    /// Gets the currency symbol for the specified currency code.
    /// </summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <returns>The currency symbol or the currency code if no symbol is available.</returns>
    private static string GetCurrencySymbol(string currencyCode)
    {
        return _currencySymbols.TryGetValue(currencyCode.ToUpperInvariant(), out var symbol) 
            ? symbol 
            : currencyCode;
    }

    /// <summary>
    /// Gets the full currency name for the specified currency code.
    /// </summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <returns>The full currency name.</returns>
    private static string GetCurrencyName(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            "USD" => "US Dollars",
            "EUR" => "Euros",
            "GBP" => "British Pounds",
            "JPY" => "Japanese Yen",
            "CAD" => "Canadian Dollars",
            "AUD" => "Australian Dollars",
            "CHF" => "Swiss Francs",
            "CNY" => "Chinese Yuan",
            "SEK" => "Swedish Kronor",
            "NOK" => "Norwegian Kronor",
            "DKK" => "Danish Kronor",
            "PLN" => "Polish Zloty",
            "CZK" => "Czech Koruna",
            "HUF" => "Hungarian Forint",
            "RUB" => "Russian Rubles",
            "BRL" => "Brazilian Real",
            "INR" => "Indian Rupees",
            "KRW" => "South Korean Won",
            "SGD" => "Singapore Dollars",
            "HKD" => "Hong Kong Dollars",
            "NZD" => "New Zealand Dollars",
            "MXN" => "Mexican Pesos",
            "ZAR" => "South African Rand",
            "TRY" => "Turkish Lira",
            "ILS" => "Israeli Shekels",
            _ => currencyCode
        };
    }

    /// <summary>
    /// Gets the appropriate culture info for the specified currency.
    /// </summary>
    /// <param name="currencyCode">The currency code.</param>
    /// <returns>The culture info for the currency.</returns>
    private static CultureInfo GetCultureForCurrency(string currencyCode)
    {
        return _cultureCache.TryGetValue(currencyCode.ToUpperInvariant(), out var culture)
            ? culture
            : CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// Initializes the culture cache with common currency-culture mappings.
    /// </summary>
    private static void InitializeCultureCache()
    {
        var cultureMappings = new Dictionary<string, string>
        {
            { "USD", "en-US" },
            { "EUR", "de-DE" },
            { "GBP", "en-GB" },
            { "JPY", "ja-JP" },
            { "CAD", "en-CA" },
            { "AUD", "en-AU" },
            { "CHF", "de-CH" },
            { "CNY", "zh-CN" },
            { "SEK", "sv-SE" },
            { "NOK", "nb-NO" },
            { "DKK", "da-DK" },
            { "PLN", "pl-PL" },
            { "CZK", "cs-CZ" },
            { "HUF", "hu-HU" },
            { "RUB", "ru-RU" },
            { "BRL", "pt-BR" },
            { "INR", "hi-IN" },
            { "KRW", "ko-KR" },
            { "SGD", "en-SG" },
            { "HKD", "zh-HK" },
            { "NZD", "en-NZ" },
            { "MXN", "es-MX" },
            { "ZAR", "af-ZA" },
            { "TRY", "tr-TR" },
            { "ILS", "he-IL" }
        };

        foreach (var mapping in cultureMappings)
        {
            try
            {
                _cultureCache[mapping.Key] = new CultureInfo(mapping.Value);
            }
            catch (CultureNotFoundException)
            {
                // Fall back to invariant culture if specific culture is not available
                _cultureCache[mapping.Key] = CultureInfo.InvariantCulture;
            }
        }
    }
}

/// <summary>
/// Defines the available money formatting styles.
/// </summary>
public enum MoneyFormatStyle
{
    /// <summary>
    /// Format with currency symbol (e.g., "$25.50").
    /// </summary>
    WithSymbol,

    /// <summary>
    /// Format with currency code (e.g., "25.50 USD").
    /// </summary>
    WithCode,

    /// <summary>
    /// Format with detailed currency name (e.g., "25.50 US Dollars").
    /// </summary>
    Detailed,

    /// <summary>
    /// Format in compact style for large amounts (e.g., "$25.5K").
    /// </summary>
    Compact,

    /// <summary>
    /// Format using culture-specific formatting.
    /// </summary>
    CultureSpecific,

    /// <summary>
    /// Format for accounting with parentheses for negative amounts.
    /// </summary>
    Accounting
}