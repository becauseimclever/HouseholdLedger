// <copyright file="AccountServiceTests.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.UnitTests;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Accounts;
using Xunit;

/// <summary>Verifies account Application use cases.</summary>
public sealed class AccountServiceTests
{
    /// <summary>Verifies creation persists a trimmed account.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreatePersistsAndReturnsAccount()
    {
        var repository = new StubRepository();
        var service = new AccountService(repository);

        var result = await service.CreateAsync("  Household Checking  ", TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.True(result.IsCreated),
            () => Assert.NotEqual(Guid.Empty, result.Account!.Id),
            () => Assert.Equal("Household Checking", result.Account!.Name),
            () => Assert.Equal("HOUSEHOLD CHECKING", repository.Items.Single().NormalizedName));
    }

    /// <summary>Verifies durable duplicate rejection is exposed explicitly.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CreateReturnsDuplicateOutcome()
    {
        var repository = new StubRepository { RejectAdd = true };
        var service = new AccountService(repository);

        var result = await service.CreateAsync("cash wallet", TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.False(result.IsCreated),
            () => Assert.Null(result.Account),
            () => Assert.Empty(repository.Items));
    }

    /// <summary>Verifies listing is deterministic without inventing records.</summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ListOrdersByNameIgnoringCaseThenIdentifier()
    {
        var firstIdentifier = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var secondIdentifier = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var repository = new StubRepository();
        repository.Items.Add(new Account(Guid.NewGuid(), "Rainy Day Savings"));
        repository.Items.Add(new Account(secondIdentifier, "cash wallet"));
        repository.Items.Add(new Account(firstIdentifier, "Cash Wallet"));
        var service = new AccountService(repository);

        var results = await service.ListAsync(TestContext.Current.CancellationToken);

        Assert.Multiple(
            () => Assert.Equal(3, results.Count),
            () => Assert.Equal(firstIdentifier, results[0].Id),
            () => Assert.Equal(secondIdentifier, results[1].Id),
            () => Assert.Equal("Rainy Day Savings", results[2].Name));
    }

    private sealed class StubRepository : IAccountRepository
    {
        public List<Account> Items { get; } = [];

        public bool RejectAdd { get; init; }

        public Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (this.RejectAdd)
            {
                return Task.FromResult(false);
            }

            this.Items.Add(account);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Account>>(this.Items.ToArray());
        }
    }
}
