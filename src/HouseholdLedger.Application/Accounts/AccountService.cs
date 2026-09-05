// <copyright file="AccountService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Accounts;

using HouseholdLedger.Domain.Accounts;

/// <summary>Creates and lists named accounts.</summary>
public sealed class AccountService(IAccountRepository repository)
{
    /// <summary>Creates one account when its normalized name is unique.</summary>
    /// <param name="name">The account display name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account or a duplicate-name outcome.</returns>
    public async Task<AccountCreationResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var account = new Account(Guid.NewGuid(), name);
        var added = await repository.TryAddAsync(account, cancellationToken);
        return new AccountCreationResult(added ? Map(account) : null);
    }

    /// <summary>Lists accounts by display name and identifier.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deterministically ordered account catalog.</returns>
    public async Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await repository.ListAsync(cancellationToken);
        return accounts
            .OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(account => account.Id)
            .Select(Map)
            .ToArray();
    }

    private static AccountDto Map(Account account) => new(account.Id, account.Name);
}
