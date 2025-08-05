using System;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.ValueObjects;

public class MoneyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAmountAndCurrency_InitializesCorrectly()
    {
        // Arrange & Act
        var money = new Money(25.50m, "USD");

        // Assert
        Assert.Equal(25.50m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_WithDefaultCurrency_UsesUSD()
    {
        // Arrange & Act
        var money = new Money(100m);

        // Assert
        Assert.Equal(100m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_WithLowercaseCurrency_ConvertsToUppercase()
    {
        // Arrange & Act
        var money = new Money(50m, "eur");

        // Assert
        Assert.Equal("EUR", money.Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCurrency_ThrowsArgumentException(string currency)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Money(100m, currency));
        Assert.Contains("Currency cannot be null, empty, or whitespace", exception.Message);
    }

    [Theory]
    [InlineData(25.123)]
    [InlineData(100.999)]
    [InlineData(0.001)]
    public void Constructor_WithMoreThanTwoDecimalPlaces_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount));
        Assert.Contains("Money amount cannot have more than 2 decimal places", exception.Message);
    }

    [Theory]
    [InlineData(25.12)]
    [InlineData(100.00)]
    [InlineData(0.01)]
    [InlineData(0.00)]
    public void Constructor_WithValidDecimalPlaces_DoesNotThrow(decimal amount)
    {
        // Act & Assert
        var money = new Money(amount);
        Assert.Equal(amount, money.Amount);
    }

    #endregion

    #region Static Properties Tests

    [Fact]
    public void Zero_ReturnsZeroUSD()
    {
        // Act
        var zero = Money.Zero;

        // Assert
        Assert.Equal(0m, zero.Amount);
        Assert.Equal("USD", zero.Currency);
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(100.50, true)]
    [InlineData(0.01, true)]
    [InlineData(0.00, false)]
    [InlineData(-50.25, false)]
    public void IsPositive_WithVariousAmounts_ReturnsCorrectValue(decimal amount, bool expected)
    {
        // Arrange
        var money = new Money(amount);

        // Act & Assert
        Assert.Equal(expected, money.IsPositive);
    }

    [Theory]
    [InlineData(0.00, true)]
    [InlineData(100.50, false)]
    [InlineData(-50.25, false)]
    public void IsZero_WithVariousAmounts_ReturnsCorrectValue(decimal amount, bool expected)
    {
        // Arrange
        var money = new Money(amount);

        // Act & Assert
        Assert.Equal(expected, money.IsZero);
    }

    [Theory]
    [InlineData(-100.50, true)]
    [InlineData(-0.01, true)]
    [InlineData(0.00, false)]
    [InlineData(50.25, false)]
    public void IsNegative_WithVariousAmounts_ReturnsCorrectValue(decimal amount, bool expected)
    {
        // Arrange
        var money = new Money(amount);

        // Act & Assert
        Assert.Equal(expected, money.IsNegative);
    }

    #endregion

    #region Arithmetic Operator Tests

    [Fact]
    public void Addition_WithSameCurrency_ReturnsCorrectSum()
    {
        // Arrange
        var money1 = new Money(25.50m, "USD");
        var money2 = new Money(10.25m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        Assert.Equal(35.75m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var usd = new Money(25.50m, "USD");
        var eur = new Money(10.25m, "EUR");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => usd + eur);
        Assert.Contains("Cannot perform operation on different currencies", exception.Message);
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ReturnsCorrectDifference()
    {
        // Arrange
        var money1 = new Money(25.50m, "USD");
        var money2 = new Money(10.25m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        Assert.Equal(15.25m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Subtraction_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var usd = new Money(25.50m, "USD");
        var eur = new Money(10.25m, "EUR");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => usd - eur);
        Assert.Contains("Cannot perform operation on different currencies", exception.Message);
    }

    [Theory]
    [InlineData(100.00, 2.0, 200.00)]
    [InlineData(25.50, 1.5, 38.25)]
    [InlineData(10.00, 0.5, 5.00)]
    [InlineData(33.33, 3.0, 99.99)]
    public void Multiplication_WithDecimalMultiplier_ReturnsCorrectProduct(decimal amount, decimal multiplier, decimal expected)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = money * multiplier;

        // Assert
        Assert.Equal(expected, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Multiplication_CommutativeProperty_ReturnsCorrectProduct()
    {
        // Arrange
        var money = new Money(25.50m, "USD");
        var multiplier = 2.0m;

        // Act
        var result1 = money * multiplier;
        var result2 = multiplier * money;

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(51.00m, result1.Amount);
    }

    [Theory]
    [InlineData(100.00, 2.0, 50.00)]
    [InlineData(25.50, 1.5, 17.00)]
    [InlineData(10.00, 4.0, 2.50)]
    public void Division_WithDecimalDivisor_ReturnsCorrectQuotient(decimal amount, decimal divisor, decimal expected)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = money / divisor;

        // Assert
        Assert.Equal(expected, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Division_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        var money = new Money(100m, "USD");

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => money / 0);
    }

    [Theory]
    [InlineData(100.50)]
    [InlineData(-75.25)]
    [InlineData(0.00)]
    public void UnaryNegation_ReturnsNegatedAmount(decimal amount)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = -money;

        // Assert
        Assert.Equal(-amount, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    #endregion

    #region Comparison Operator Tests

    [Theory]
    [InlineData(100.00, 50.00, true)]
    [InlineData(50.00, 100.00, false)]
    [InlineData(75.50, 75.50, false)]
    public void GreaterThan_WithSameCurrency_ReturnsCorrectResult(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act & Assert
        Assert.Equal(expected, money1 > money2);
    }

    [Fact]
    public void GreaterThan_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var usd = new Money(100m, "USD");
        var eur = new Money(50m, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => usd > eur);
    }

    [Theory]
    [InlineData(50.00, 100.00, true)]
    [InlineData(100.00, 50.00, false)]
    [InlineData(75.50, 75.50, false)]
    public void LessThan_WithSameCurrency_ReturnsCorrectResult(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act & Assert
        Assert.Equal(expected, money1 < money2);
    }

    [Theory]
    [InlineData(100.00, 50.00, true)]
    [InlineData(50.00, 100.00, false)]
    [InlineData(75.50, 75.50, true)]
    public void GreaterThanOrEqual_WithSameCurrency_ReturnsCorrectResult(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act & Assert
        Assert.Equal(expected, money1 >= money2);
    }

    [Theory]
    [InlineData(50.00, 100.00, true)]
    [InlineData(100.00, 50.00, false)]
    [InlineData(75.50, 75.50, true)]
    public void LessThanOrEqual_WithSameCurrency_ReturnsCorrectResult(decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, "USD");
        var money2 = new Money(amount2, "USD");

        // Act & Assert
        Assert.Equal(expected, money1 <= money2);
    }

    #endregion

    #region Static Factory Method Tests

    [Fact]
    public void FromUsd_CreatesMoneyInUSD()
    {
        // Act
        var money = Money.FromUsd(125.75m);

        // Assert
        Assert.Equal(125.75m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void FromCurrency_CreatesMoneyInSpecifiedCurrency()
    {
        // Act
        var money = Money.FromCurrency(200.50m, "EUR");

        // Assert
        Assert.Equal(200.50m, money.Amount);
        Assert.Equal("EUR", money.Currency);
    }

    #endregion

    #region Instance Method Tests

    [Theory]
    [InlineData(100.50, 100.50)]
    [InlineData(-75.25, 75.25)]
    [InlineData(0.00, 0.00)]
    public void Abs_ReturnsAbsoluteValue(decimal amount, decimal expected)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = money.Abs();

        // Assert
        Assert.Equal(expected, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void ConvertTo_WithValidParameters_ReturnsConvertedMoney()
    {
        // Arrange
        var usd = new Money(100m, "USD");
        var exchangeRate = 0.85m; // USD to EUR

        // Act
        var eur = usd.ConvertTo("EUR", exchangeRate);

        // Assert
        Assert.Equal(85.00m, eur.Amount);
        Assert.Equal("EUR", eur.Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertTo_WithInvalidCurrency_ThrowsArgumentException(string targetCurrency)
    {
        // Arrange
        var money = new Money(100m, "USD");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => money.ConvertTo(targetCurrency, 1.0m));
        Assert.Contains("Target currency cannot be null, empty, or whitespace", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void ConvertTo_WithInvalidExchangeRate_ThrowsArgumentOutOfRangeException(decimal exchangeRate)
    {
        // Arrange
        var money = new Money(100m, "USD");

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => money.ConvertTo("EUR", exchangeRate));
        Assert.Contains("Exchange rate must be positive", exception.Message);
    }

    #endregion

    #region ToString Tests

    [Theory]
    [InlineData(100.50, "USD", "100.50 USD")]
    [InlineData(25.00, "EUR", "25.00 EUR")]
    [InlineData(0.00, "GBP", "0.00 GBP")]
    [InlineData(-75.25, "CAD", "-75.25 CAD")]
    public void ToString_WithVariousAmounts_ReturnsCorrectFormat(decimal amount, string currency, string expected)
    {
        // Arrange
        var money = new Money(amount, currency);

        // Act
        var result = money.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToString_WithFormat_ReturnsFormattedString()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = money.ToString("N2");

        // Assert
        Assert.Equal("1,234.56 USD", result);
    }

    [Fact]
    public void ToString_WithFormatAndProvider_ReturnsFormattedString()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");
        var culture = new System.Globalization.CultureInfo("en-US");

        // Act
        var result = money.ToString("C", culture);

        // Assert
        Assert.Contains("1,234.56", result);
        Assert.Contains("USD", result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameAmountAndCurrency_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act & Assert
        Assert.Equal(money1, money2);
        Assert.True(money1.Equals(money2));
    }

    [Fact]
    public void Equals_WithDifferentAmount_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(75.25m, "USD");

        // Act & Assert
        Assert.NotEqual(money1, money2);
        Assert.False(money1.Equals(money2));
    }

    [Fact]
    public void Equals_WithDifferentCurrency_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "EUR");

        // Act & Assert
        Assert.NotEqual(money1, money2);
        Assert.False(money1.Equals(money2));
    }

    [Fact]
    public void GetHashCode_WithSameAmountAndCurrency_ReturnsSameHashCode()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act
        var hash1 = money1.GetHashCode();
        var hash2 = money2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentMoney_ReturnsDifferentHashCodes()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(75.25m, "EUR");

        // Act
        var hash1 = money1.GetHashCode();
        var hash2 = money2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Value Type Tests

    [Fact]
    public void Money_IsValueType()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = money1; // Copy

        // Act
        money2 = new Money(75.25m, "EUR"); // Reassign

        // Assert
        Assert.Equal(100.50m, money1.Amount); // Original unchanged
        Assert.Equal("USD", money1.Currency);
        Assert.Equal(75.25m, money2.Amount);
        Assert.Equal("EUR", money2.Currency);
    }

    #endregion

    #region Deconstruction Tests

    [Fact]
    public void Money_SupportsDeconstruction()
    {
        // Arrange
        var money = new Money(125.75m, "EUR");

        // Act
        var (amount, currency) = money;

        // Assert
        Assert.Equal(125.75m, amount);
        Assert.Equal("EUR", currency);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void Money_SupportsPatternMatching()
    {
        // Arrange
        var money = new Money(100.00m, "USD");

        // Act & Assert
        var result = money switch
        {
            { Amount: 100.00m, Currency: "USD" } => "One hundred USD",
            { Amount: 100.00m, Currency: "EUR" } => "One hundred EUR",
            _ => "Other amount"
        };

        Assert.Equal("One hundred USD", result);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Money_WithMaxDecimalValue_DoesNotThrow()
    {
        // Arrange & Act
        var money = new Money(decimal.MaxValue, "USD");

        // Assert
        Assert.Equal(decimal.MaxValue, money.Amount);
    }

    [Fact]
    public void Money_WithMinDecimalValue_DoesNotThrow()
    {
        // Arrange & Act
        var money = new Money(decimal.MinValue, "USD");

        // Assert
        Assert.Equal(decimal.MinValue, money.Amount);
    }

    [Fact]
    public void Addition_WithOverflow_ThrowsOverflowException()
    {
        // Arrange
        var money1 = new Money(decimal.MaxValue, "USD");
        var money2 = new Money(1m, "USD");

        // Act & Assert
        Assert.Throws<OverflowException>(() => money1 + money2);
    }

    [Fact]
    public void Subtraction_WithOverflow_ThrowsOverflowException()
    {
        // Arrange
        var money1 = new Money(decimal.MinValue, "USD");
        var money2 = new Money(1m, "USD");

        // Act & Assert
        Assert.Throws<OverflowException>(() => money1 - money2);
    }

    [Fact]
    public void Multiplication_WithOverflow_ThrowsOverflowException()
    {
        // Arrange
        var money = new Money(decimal.MaxValue, "USD");

        // Act & Assert
        Assert.Throws<OverflowException>(() => money * 2m);
    }

    [Fact]
    public void Division_WithVerySmallDivisor_ThrowsOverflowException()
    {
        // Arrange
        var money = new Money(decimal.MaxValue, "USD");

        // Act & Assert - Division by very small number causes overflow
        Assert.Throws<OverflowException>(() => money / 0.0000000001m);
    }

    #endregion
}