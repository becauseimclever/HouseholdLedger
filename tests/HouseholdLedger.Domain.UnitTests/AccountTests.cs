// <copyright file="AccountTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.UnitTests;

using HouseholdLedger.Domain.Accounts;
using Xunit;

/// <summary>
/// Verifies account invariants.
/// </summary>
public sealed class AccountTests
{
    /// <summary>Verifies valid values create a trimmed account.</summary>
    [Fact]
    public void ConstructorAcceptsAndNormalizesValidValues()
    {
        var id = Guid.NewGuid();

        var account = new Account(id, "  Household Checking  ");

        Assert.Multiple(
            () => Assert.Equal(id, account.Id),
            () => Assert.Equal("Household Checking", account.Name),
            () => Assert.Equal("HOUSEHOLD CHECKING", account.NormalizedName));
    }

    /// <summary>Verifies an empty identifier is rejected.</summary>
    [Fact]
    public void ConstructorRejectsEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.Empty, "Cash Wallet"));
    }

    /// <summary>Verifies blank names are rejected.</summary>
    /// <param name="name">The invalid account name.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), name));
    }

    /// <summary>Verifies the maximum trimmed name length is accepted.</summary>
    [Fact]
    public void ConstructorAcceptsOneHundredCharacterName()
    {
        var account = new Account(Guid.NewGuid(), $" {new string('a', 100)} ");

        Assert.Equal(100, account.Name.Length);
    }

    /// <summary>Verifies an over-length trimmed name is rejected.</summary>
    [Fact]
    public void ConstructorRejectsNameOverOneHundredCharacters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Account(Guid.NewGuid(), new string('a', 101)));
    }
}
