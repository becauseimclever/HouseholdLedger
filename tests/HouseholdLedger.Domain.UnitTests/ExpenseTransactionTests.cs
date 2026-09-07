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
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);

        var transaction = new ExpenseTransaction(id, accountId, date, 12.34m, ExpenseClassification.Necessities);

        Assert.Multiple(
            () => Assert.Equal(id, transaction.Id),
            () => Assert.Equal(accountId, transaction.AccountId),
            () => Assert.Equal(date, transaction.Date),
            () => Assert.Equal(12.34m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Necessities, transaction.Classification));
    }

    /// <summary>Verifies the persistence amount boundary is accepted exactly.</summary>
    [Fact]
    public void ConstructorAcceptsMaximumPersistableAmount()
    {
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            ExpenseTransaction.MaximumAmount,
            ExpenseClassification.Necessities);

        Assert.Equal(ExpenseTransaction.MaximumAmount, transaction.Amount);
    }

    /// <summary>Verifies invalid amounts are rejected.</summary>
    /// <param name="value">The invariant-culture amount text.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000000000.00")]
    public void ConstructorRejectsInvalidAmounts(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExpenseTransaction(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), amount, ExpenseClassification.Optional));
    }

    /// <summary>Verifies unsupported classifications are rejected.</summary>
    [Fact]
    public void ConstructorRejectsUnsupportedClassification()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExpenseTransaction(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), 1m, (ExpenseClassification)99));
    }

    /// <summary>Verifies an owning account is required.</summary>
    [Fact]
    public void ConstructorRejectsEmptyAccountIdentifier()
    {
        Assert.Throws<ArgumentException>(() =>
            new ExpenseTransaction(Guid.NewGuid(), Guid.Empty, new DateOnly(2026, 9, 1), 1m, ExpenseClassification.Optional));
    }

    /// <summary>Verifies correction changes only the allowed details.</summary>
    [Fact]
    public void ReviseChangesDetailsAndPreservesRecordIdentity()
    {
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var replacementAccountId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 1);
        var transaction = new ExpenseTransaction(id, accountId, date, 12.34m, ExpenseClassification.Necessities, 7);

        transaction.Revise(replacementAccountId, 45.67m, ExpenseClassification.Culture);

        Assert.Multiple(
            () => Assert.Equal(id, transaction.Id),
            () => Assert.Equal(date, transaction.Date),
            () => Assert.Equal(7, transaction.Sequence),
            () => Assert.Equal(replacementAccountId, transaction.AccountId),
            () => Assert.Equal(45.67m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Culture, transaction.Classification));
    }

    /// <summary>Verifies invalid correction leaves the transaction unchanged.</summary>
    /// <param name="value">The invariant-culture amount text.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public void ReviseRejectsInvalidAmountWithoutMutation(string value)
    {
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            12.34m,
            ExpenseClassification.Necessities);
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            transaction.Revise(Guid.NewGuid(), amount, ExpenseClassification.Culture));

        Assert.Multiple(
            () => Assert.Equal(12.34m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Necessities, transaction.Classification));
    }

    /// <summary>Verifies an invalid account correction leaves every detail unchanged.</summary>
    [Fact]
    public void ReviseRejectsEmptyAccountIdentifierWithoutMutation()
    {
        var accountId = Guid.NewGuid();
        var transaction = new ExpenseTransaction(
            Guid.NewGuid(),
            accountId,
            new DateOnly(2026, 9, 1),
            12.34m,
            ExpenseClassification.Necessities);

        Assert.Throws<ArgumentException>(() =>
            transaction.Revise(Guid.Empty, 45.67m, ExpenseClassification.Culture));

        Assert.Multiple(
            () => Assert.Equal(accountId, transaction.AccountId),
            () => Assert.Equal(12.34m, transaction.Amount),
            () => Assert.Equal(ExpenseClassification.Necessities, transaction.Classification));
    }
}
