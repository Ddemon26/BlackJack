using System;
using System.Collections.Generic;
using System.Globalization;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Formatting;
using Xunit;

namespace GroupProject.Tests.Infrastructure;

public class MoneyFormatterTests
{
    #region FormatWithSymbol Tests

    [Theory]
    [InlineData(25.50, "USD", "$25.50")]
    [InlineData(100.00, "EUR", "€100.00")]
    [InlineData(75.25, "GBP", "£75.25")]
    [InlineData(1000.99, "JPY", "¥1000.99")]
    [InlineData(0.00, "USD", "$0.00")]
    [InlineData(-50.75, "USD", "$-50.75")]
    public void FormatWithSymbol_WithVariousCurrencies_ReturnsCorrectFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatWithSymbol(money);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatWithSymbol_WithUnknownCurrency_UsesCurrencyCode()
    {
        // Arrange
        var money = new Money(100.50m, "XYZ");

        // Act
        var result = MoneyFormatter.FormatWithSymbol(money);

        // Assert
        Assert.Equal("XYZ100.50", result);
    }

    #endregion

    #region FormatWithCode Tests

    [Theory]
    [InlineData(25.50, "USD", "25.50 USD")]
    [InlineData(100.00, "EUR", "100.00 EUR")]
    [InlineData(75.25, "GBP", "75.25 GBP")]
    [InlineData(0.00, "JPY", "0.00 JPY")]
    [InlineData(-50.75, "CAD", "-50.75 CAD")]
    public void FormatWithCode_WithVariousCurrencies_ReturnsCorrectFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatWithCode(money);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatDetailed Tests

    [Theory]
    [InlineData(25.50, "USD", "25.50 US Dollars")]
    [InlineData(100.00, "EUR", "100.00 Euros")]
    [InlineData(75.25, "GBP", "75.25 British Pounds")]
    [InlineData(1000.99, "JPY", "1000.99 Japanese Yen")]
    [InlineData(50.00, "CAD", "50.00 Canadian Dollars")]
    public void FormatDetailed_WithKnownCurrencies_ReturnsDetailedFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatDetailed(money);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDetailed_WithUnknownCurrency_UsesCurrencyCode()
    {
        // Arrange
        var money = new Money(100.50m, "XYZ");

        // Act
        var result = MoneyFormatter.FormatDetailed(money);

        // Assert
        Assert.Equal("100.50 XYZ", result);
    }

    #endregion

    #region FormatCompact Tests

    [Theory]
    [InlineData(25.50, "USD", "$25.50")]
    [InlineData(1500.00, "USD", "$1.5K")]
    [InlineData(1500000.00, "USD", "$1.5M")]
    [InlineData(2500000000.00, "USD", "$2.5B")]
    [InlineData(999.99, "EUR", "€999.99")]
    [InlineData(1000.00, "EUR", "€1.0K")]
    public void FormatCompact_WithVariousAmounts_ReturnsCompactFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatCompact(money);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1500.00, "USD", "$-1.5K")]
    [InlineData(-1500000.00, "USD", "$-1.5M")]
    [InlineData(-2500000000.00, "USD", "$-2.5B")]
    public void FormatCompact_WithNegativeAmounts_ReturnsCorrectFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatCompact(money);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatCultureSpecific Tests

    [Fact]
    public void FormatCultureSpecific_WithUSD_ReturnsUSFormat()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = MoneyFormatter.FormatCultureSpecific(money);

        // Assert
        Assert.Contains("1,234.56", result);
    }

    [Fact]
    public void FormatCultureSpecific_WithUnknownCurrency_ReturnsInvariantFormat()
    {
        // Arrange
        var money = new Money(1234.56m, "XYZ");

        // Act
        var result = MoneyFormatter.FormatCultureSpecific(money);

        // Assert
        Assert.Contains("1,234.56", result); // Invariant culture includes comma separator
    }

    #endregion

    #region FormatAccounting Tests

    [Theory]
    [InlineData(100.50, "USD", "$100.50")]
    [InlineData(0.00, "USD", "$0.00")]
    [InlineData(-75.25, "USD", "($75.25)")]
    [InlineData(-100.00, "EUR", "(€100.00)")]
    public void FormatAccounting_WithVariousAmounts_ReturnsAccountingFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatAccounting(money);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatMultiple Tests

    [Fact]
    public void FormatMultiple_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var amounts = new List<Money>();

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatMultiple_WithNullCollection_ReturnsEmptyString()
    {
        // Act
        var result = MoneyFormatter.FormatMultiple(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatMultiple_WithSingleAmount_ReturnsFormattedAmount()
    {
        // Arrange
        var amounts = new List<Money> { new Money(100.50m, "USD") };

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts);

        // Assert
        Assert.Equal("$100.50", result);
    }

    [Fact]
    public void FormatMultiple_WithMultipleAmounts_ReturnsCommaSeparatedList()
    {
        // Arrange
        var amounts = new List<Money>
        {
            new Money(100.50m, "USD"),
            new Money(75.25m, "USD"),
            new Money(200.00m, "USD")
        };

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts);

        // Assert
        Assert.Equal("$100.50, $75.25, $200.00", result);
    }

    [Fact]
    public void FormatMultiple_WithCustomSeparator_UsesCustomSeparator()
    {
        // Arrange
        var amounts = new List<Money>
        {
            new Money(100.50m, "USD"),
            new Money(75.25m, "USD")
        };

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts, MoneyFormatStyle.WithSymbol, " | ");

        // Assert
        Assert.Equal("$100.50 | $75.25", result);
    }

    [Theory]
    [InlineData(MoneyFormatStyle.WithSymbol, "$100.50")]
    [InlineData(MoneyFormatStyle.WithCode, "100.50 USD")]
    [InlineData(MoneyFormatStyle.Detailed, "100.50 US Dollars")]
    [InlineData(MoneyFormatStyle.Compact, "$100.50")]
    [InlineData(MoneyFormatStyle.Accounting, "$100.50")]
    public void FormatMultiple_WithDifferentStyles_ReturnsCorrectFormat(MoneyFormatStyle style, string expected)
    {
        // Arrange
        var amounts = new List<Money> { new Money(100.50m, "USD") };

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts, style);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatWithLabel Tests

    [Theory]
    [InlineData("Balance", 100.50, "USD", MoneyFormatStyle.WithSymbol, "Balance: $100.50")]
    [InlineData("Total", 75.25, "EUR", MoneyFormatStyle.WithCode, "Total: 75.25 EUR")]
    [InlineData("Amount", 200.00, "GBP", MoneyFormatStyle.Detailed, "Amount: 200.00 British Pounds")]
    [InlineData("Cost", 1500.00, "USD", MoneyFormatStyle.Compact, "Cost: $1.5K")]
    [InlineData("Debt", -100.00, "USD", MoneyFormatStyle.Accounting, "Debt: ($100.00)")]
    public void FormatWithLabel_WithVariousStyles_ReturnsLabeledFormat(string label, decimal amount, string currency, MoneyFormatStyle style, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = MoneyFormatter.FormatWithLabel(label, money, style);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatDifference Tests

    [Fact]
    public void FormatDifference_WithPositiveChange_ReturnsUpwardIndicator()
    {
        // Arrange
        var current = new Money(150.00m, "USD");
        var previous = new Money(100.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatDifference(current, previous);

        // Assert
        Assert.Contains("$150.00", result);
        Assert.Contains("+$50.00", result);
        Assert.Contains("↑", result);
    }

    [Fact]
    public void FormatDifference_WithNegativeChange_ReturnsDownwardIndicator()
    {
        // Arrange
        var current = new Money(75.00m, "USD");
        var previous = new Money(100.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatDifference(current, previous);

        // Assert
        Assert.Contains("$75.00", result);
        Assert.Contains("$25.00", result);
        Assert.Contains("↓", result);
        Assert.DoesNotContain("+", result);
    }

    [Fact]
    public void FormatDifference_WithNoChange_ReturnsNoChangeMessage()
    {
        // Arrange
        var current = new Money(100.00m, "USD");
        var previous = new Money(100.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatDifference(current, previous);

        // Assert
        Assert.Contains("No change", result);
        Assert.Contains("$100.00", result);
    }

    [Fact]
    public void FormatDifference_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var current = new Money(100.00m, "USD");
        var previous = new Money(100.00m, "EUR");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => MoneyFormatter.FormatDifference(current, previous));
        Assert.Contains("Cannot format difference for different currencies", exception.Message);
    }

    #endregion

    #region MoneyFormatStyle Enum Tests

    [Fact]
    public void MoneyFormatStyle_HasAllExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.WithSymbol));
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.WithCode));
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.Detailed));
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.Compact));
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.CultureSpecific));
        Assert.True(Enum.IsDefined(typeof(MoneyFormatStyle), MoneyFormatStyle.Accounting));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FormatWithSymbol_WithZeroAmount_ReturnsZeroFormat()
    {
        // Arrange
        var money = Money.Zero;

        // Act
        var result = MoneyFormatter.FormatWithSymbol(money);

        // Assert
        Assert.Equal("$0.00", result);
    }

    [Fact]
    public void FormatCompact_WithExactThousand_ReturnsKFormat()
    {
        // Arrange
        var money = new Money(1000.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatCompact(money);

        // Assert
        Assert.Equal("$1.0K", result);
    }

    [Fact]
    public void FormatCompact_WithExactMillion_ReturnsMFormat()
    {
        // Arrange
        var money = new Money(1000000.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatCompact(money);

        // Assert
        Assert.Equal("$1.0M", result);
    }

    [Fact]
    public void FormatCompact_WithExactBillion_ReturnsBFormat()
    {
        // Arrange
        var money = new Money(1000000000.00m, "USD");

        // Act
        var result = MoneyFormatter.FormatCompact(money);

        // Assert
        Assert.Equal("$1.0B", result);
    }

    [Theory]
    [InlineData("CAD", "C$")]
    [InlineData("AUD", "A$")]
    [InlineData("CHF", "CHF")]
    [InlineData("CNY", "¥")]
    [InlineData("SEK", "kr")]
    [InlineData("NOK", "kr")]
    [InlineData("DKK", "kr")]
    [InlineData("PLN", "zł")]
    [InlineData("CZK", "Kč")]
    [InlineData("HUF", "Ft")]
    [InlineData("RUB", "₽")]
    [InlineData("BRL", "R$")]
    [InlineData("INR", "₹")]
    [InlineData("KRW", "₩")]
    [InlineData("SGD", "S$")]
    [InlineData("HKD", "HK$")]
    [InlineData("NZD", "NZ$")]
    [InlineData("MXN", "MX$")]
    [InlineData("ZAR", "R")]
    [InlineData("TRY", "₺")]
    [InlineData("ILS", "₪")]
    public void FormatWithSymbol_WithVariousInternationalCurrencies_ReturnsCorrectSymbol(string currency, string expectedSymbol)
    {
        // Arrange
        var money = new Money(100.00m, currency);

        // Act
        var result = MoneyFormatter.FormatWithSymbol(money);

        // Assert
        Assert.Equal($"{expectedSymbol}100.00", result);
    }

    [Theory]
    [InlineData("CAD", "Canadian Dollars")]
    [InlineData("AUD", "Australian Dollars")]
    [InlineData("CHF", "Swiss Francs")]
    [InlineData("CNY", "Chinese Yuan")]
    [InlineData("SEK", "Swedish Kronor")]
    [InlineData("NOK", "Norwegian Kronor")]
    [InlineData("DKK", "Danish Kronor")]
    [InlineData("PLN", "Polish Zloty")]
    [InlineData("CZK", "Czech Koruna")]
    [InlineData("HUF", "Hungarian Forint")]
    [InlineData("RUB", "Russian Rubles")]
    [InlineData("BRL", "Brazilian Real")]
    [InlineData("INR", "Indian Rupees")]
    [InlineData("KRW", "South Korean Won")]
    [InlineData("SGD", "Singapore Dollars")]
    [InlineData("HKD", "Hong Kong Dollars")]
    [InlineData("NZD", "New Zealand Dollars")]
    [InlineData("MXN", "Mexican Pesos")]
    [InlineData("ZAR", "South African Rand")]
    [InlineData("TRY", "Turkish Lira")]
    [InlineData("ILS", "Israeli Shekels")]
    public void FormatDetailed_WithVariousInternationalCurrencies_ReturnsCorrectName(string currency, string expectedName)
    {
        // Arrange
        var money = new Money(100.00m, currency);

        // Act
        var result = MoneyFormatter.FormatDetailed(money);

        // Assert
        Assert.Equal($"100.00 {expectedName}", result);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void FormatWithSymbol_CalledMultipleTimes_PerformsConsistently()
    {
        // Arrange
        var money = new Money(100.50m, "USD");

        // Act & Assert - Should not throw and should return consistent results
        for (int i = 0; i < 1000; i++)
        {
            var result = MoneyFormatter.FormatWithSymbol(money);
            Assert.Equal("$100.50", result);
        }
    }

    [Fact]
    public void FormatMultiple_WithLargeCollection_CompletesSuccessfully()
    {
        // Arrange
        var amounts = new List<Money>();
        for (int i = 0; i < 100; i++)
        {
            amounts.Add(new Money(i * 10.50m, "USD"));
        }

        // Act
        var result = MoneyFormatter.FormatMultiple(amounts);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("$0.00", result);
        Assert.Contains("$1039.50", result); // 99 * 10.50 = 1039.50
    }

    #endregion
}