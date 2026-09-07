// <copyright file="DependencyInjection.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Application.Settings;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Registers HouseholdLedger infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the PostgreSQL persistence boundary with an explicit connection string.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string supplied by the composition root.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddHouseholdLedgerInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<HouseholdLedgerDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<DevelopmentDatabaseInitializer>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IExpenseTransactionRepository, ExpenseTransactionRepository>();
        services.AddScoped<IGlobalSettingsRepository, GlobalSettingsRepository>();

        return services;
    }
}
