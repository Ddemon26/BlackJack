using System;
using System.Globalization;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency and precision handling.
/// Provides arithmetic operations, comparison operators, and validation for financial calculations.
/// </summary>
/// <remarks>
/// This value object ensures consistent handling of monetary values throughout the system.
/// It uses decimal arithmetic for precision and supports currency validation.
/// All operations maintain immutability and provide overflow protection.
/// </remarks>
public readonly record struct Money
{
    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Initializes a new instance of the Money struct with validation.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code (defaults to USD).</param>
    /// <exception cref="ArgumentException">Thrown when currency is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount has more than 2 decimal places.</exception>
    public Money(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null, empty, or whitespace.", nameof(currency));

        // Validate decimal precision (max 2 decimal places for currency)
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount != rounded)
            throw new ArgumentOutOfRangeException(nameof(amount), 
                "Money amount cannot have more than 2 decimal places.");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
    /// <summary>
    /// Represents zero money in USD.
    /// </summary>
    public static Money Zero => new(0m);

    /// <summary>
    /// Gets a value indicating whether this money amount is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Gets a value indicating whether this money amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Gets a value indicating whether this money amount is negative.
    /// </summary>
    public bool IsNegative => Amount < 0;



    /// <summary>
    /// Adds two money amounts.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <returns>The sum of the two amounts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    /// <exception cref="OverflowException">Thrown when the result would overflow.</exception>
    public static Money operator +(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        
        try
        {
            return new Money(left.Amount + right.Amount, left.Currency);
        }
        catch (OverflowException)
        {
            throw new OverflowException("Money addition resulted in overflow.");
        }
    }

    /// <summary>
    /// Subtracts one money amount from another.
    /// </summary>
    /// <param name="left">The money amount to subtract from.</param>
    /// <param name="right">The money amount to subtract.</param>
    /// <returns>The difference between the two amounts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    /// <exception cref="OverflowException">Thrown when the result would overflow.</exception>
    public static Money operator -(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        
        try
        {
            return new Money(left.Amount - right.Amount, left.Currency);
        }
        catch (OverflowException)
        {
            throw new OverflowException("Money subtraction resulted in overflow.");
        }
    }

    /// <summary>
    /// Multiplies a money amount by a decimal multiplier.
    /// </summary>
    /// <param name="money">The money amount to multiply.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>The product of the money amount and multiplier.</returns>
    /// <exception cref="OverflowException">Thrown when the result would overflow.</exception>
    /// <exception cref="ArgumentException">Thrown when the result has more than 2 decimal places.</exception>
    public static Money operator *(Money money, decimal multiplier)
    {
        try
        {
            var result = money.Amount * multiplier;
            return new Money(Math.Round(result, 2, MidpointRounding.AwayFromZero), money.Currency);
        }
        catch (OverflowException)
        {
            throw new OverflowException("Money multiplication resulted in overflow.");
        }
    }

    /// <summary>
    /// Multiplies a money amount by a decimal multiplier.
    /// </summary>
    /// <param name="multiplier">The multiplier.</param>
    /// <param name="money">The money amount to multiply.</param>
    /// <returns>The product of the money amount and multiplier.</returns>
    public static Money operator *(decimal multiplier, Money money) => money * multiplier;

    /// <summary>
    /// Divides a money amount by a decimal divisor.
    /// </summary>
    /// <param name="money">The money amount to divide.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>The quotient of the money amount and divisor.</returns>
    /// <exception cref="DivideByZeroException">Thrown when divisor is zero.</exception>
    /// <exception cref="OverflowException">Thrown when the result would overflow.</exception>
    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        try
        {
            var result = money.Amount / divisor;
            return new Money(Math.Round(result, 2, MidpointRounding.AwayFromZero), money.Currency);
        }
        catch (OverflowException)
        {
            throw new OverflowException("Money division resulted in overflow.");
        }
    }

    /// <summary>
    /// Returns the unary negation of a money amount.
    /// </summary>
    /// <param name="money">The money amount to negate.</param>
    /// <returns>The negated money amount.</returns>
    public static Money operator -(Money money)
    {
        return new Money(-money.Amount, money.Currency);
    }

    /// <summary>
    /// Determines whether one money amount is greater than another.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <returns>True if left is greater than right; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public static bool operator >(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    /// <summary>
    /// Determines whether one money amount is less than another.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <returns>True if left is less than right; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public static bool operator <(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    /// <summary>
    /// Determines whether one money amount is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <returns>True if left is greater than or equal to right; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public static bool operator >=(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    /// <summary>
    /// Determines whether one money amount is less than or equal to another.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <returns>True if left is less than or equal to right; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public static bool operator <=(Money left, Money right)
    {
        ValidateSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    /// <summary>
    /// Creates a Money instance from a decimal amount in USD.
    /// </summary>
    /// <param name="amount">The amount in USD.</param>
    /// <returns>A Money instance representing the amount in USD.</returns>
    public static Money FromUsd(decimal amount) => new(amount, "USD");

    /// <summary>
    /// Creates a Money instance from a decimal amount in the specified currency.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>A Money instance representing the amount in the specified currency.</returns>
    public static Money FromCurrency(decimal amount, string currency) => new(amount, currency);

    /// <summary>
    /// Returns the absolute value of the money amount.
    /// </summary>
    /// <returns>A Money instance with the absolute value of the amount.</returns>
    public Money Abs() => new(Math.Abs(Amount), Currency);

    /// <summary>
    /// Converts the money amount to the specified currency.
    /// </summary>
    /// <param name="targetCurrency">The target currency code.</param>
    /// <param name="exchangeRate">The exchange rate from current currency to target currency.</param>
    /// <returns>A Money instance in the target currency.</returns>
    /// <exception cref="ArgumentException">Thrown when targetCurrency is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when exchangeRate is not positive.</exception>
    public Money ConvertTo(string targetCurrency, decimal exchangeRate)
    {
        if (string.IsNullOrWhiteSpace(targetCurrency))
            throw new ArgumentException("Target currency cannot be null, empty, or whitespace.", nameof(targetCurrency));

        if (exchangeRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(exchangeRate), "Exchange rate must be positive.");

        var convertedAmount = Amount * exchangeRate;
        return new Money(Math.Round(convertedAmount, 2, MidpointRounding.AwayFromZero), targetCurrency);
    }

    /// <summary>
    /// Returns a string representation of this money amount.
    /// </summary>
    /// <returns>A formatted string showing the amount and currency.</returns>
    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }

    /// <summary>
    /// Returns a string representation of this money amount using the specified format.
    /// </summary>
    /// <param name="format">The format string to use for the amount.</param>
    /// <returns>A formatted string showing the amount and currency.</returns>
    public string ToString(string format)
    {
        return $"{Amount.ToString(format, CultureInfo.InvariantCulture)} {Currency}";
    }

    /// <summary>
    /// Returns a string representation of this money amount using the specified format and culture.
    /// </summary>
    /// <param name="format">The format string to use for the amount.</param>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns>A formatted string showing the amount and currency.</returns>
    public string ToString(string format, IFormatProvider formatProvider)
    {
        return $"{Amount.ToString(format, formatProvider)} {Currency}";
    }

    /// <summary>
    /// Deconstructs the Money into its amount and currency components.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code.</param>
    public void Deconstruct(out decimal amount, out string currency)
    {
        amount = Amount;
        currency = Currency;
    }

    /// <summary>
    /// Validates that two money amounts have the same currency.
    /// </summary>
    /// <param name="left">The first money amount.</param>
    /// <param name="right">The second money amount.</param>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    private static void ValidateSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot perform operation on different currencies: {left.Currency} and {right.Currency}.");
        }
    }
}