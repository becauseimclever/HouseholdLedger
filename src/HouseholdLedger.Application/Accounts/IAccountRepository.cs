// <copyright file="IAccountRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Accounts;

using HouseholdLedger.Domain.Accounts;

/// <summary>Persists and queries accounts.</summary>
public interface IAccountRepository
{
    /// <summary>Finds an account by identifier.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or <see langword="null"/> when it does not exist.</returns>
    Task<Account?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>Attempts to add an account without violating durable name uniqueness.</summary>
    /// <param name="account">The account to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when added; otherwise, <see langword="false"/> for a duplicate name.</returns>
    Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken);

    /// <summary>Lists all persisted accounts.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted accounts.</returns>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);
}
