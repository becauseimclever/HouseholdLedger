// <copyright file="AccountRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Persists accounts through Entity Framework Core.</summary>
public sealed class AccountRepository(HouseholdLedgerDbContext dbContext) : IAccountRepository
{
    private const string NormalizedNameConstraint = "ux_accounts_normalized_name";

    /// <inheritdoc/>
    public async Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Add(account);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: NormalizedNameConstraint,
            })
        {
            dbContext.Entry(account).State = EntityState.Detached;
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.NormalizedName)
            .ThenBy(account => account.Id)
            .ToArrayAsync(cancellationToken);
    }
}
