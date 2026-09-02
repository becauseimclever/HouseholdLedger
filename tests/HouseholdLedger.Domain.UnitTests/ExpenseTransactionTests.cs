// <copyright file="ExpenseTransactionTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Transactions;
using Xunit;

/// <summary>
/// Verifies expense transaction invariants.
/// </summary>
public sealed class ExpenseTransactionTests
{
    /// <summary>Verifies that approved values create a transaction.</summary>
    [Fact]
    public void ConstructorAcceptsApprovedValues()
    {
        var id = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        var transaction = new ExpenseTransaction(id, date, 12.34m, ExpenseClassification.Necessities);

        Assert.Multiple(
            () => Assert.Equal(id, transaction.Id),
            () => Assert.Equal(date, transaction.Date),
            () => Assert.Equal(12.34m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Necessities, transaction.Classification));
    }

    /// <summary>Verifies invalid amounts are rejected.</summary>
    /// <param name="value">The invariant-culture amount text.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public void ConstructorRejectsInvalidAmounts(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 9, 1), amount, ExpenseClassification.Optional));
    }

    /// <summary>Verifies unsupported classifications are rejected.</summary>
    [Fact]
    public void ConstructorRejectsUnsupportedClassification()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExpenseTransaction(Guid.NewGuid(), new DateOnly(2026, 9, 1), 1m, (ExpenseClassification)99));
    }
}
