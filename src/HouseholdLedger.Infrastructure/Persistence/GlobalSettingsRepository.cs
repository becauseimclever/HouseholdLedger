// <copyright file="GlobalSettingsRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Settings;
using HouseholdLedger.Domain.Settings;
using Microsoft.EntityFrameworkCore;

/// <summary>Persists the singleton global application settings.</summary>
public sealed class GlobalSettingsRepository(HouseholdLedgerDbContext dbContext)
    : IGlobalSettingsRepository
{
    /// <inheritdoc/>
    public async Task<GlobalSettings?> GetAsync(CancellationToken cancellationToken)
    {
        return await dbContext.GlobalSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GlobalSettings> UpsertCurrencyAsync(
        DisplayCurrency displayCurrency,
        CancellationToken cancellationToken)
    {
        var currencyCode = displayCurrency.ToString();
        var defaultTheme = WorkbenchThemeCode.Default;
        FormattableString command = $$"""
                        INSERT INTO global_settings (id, display_currency, theme)
                        VALUES ({{GlobalSettings.SingletonId}}, {{currencyCode}}, {{defaultTheme}})
                        ON CONFLICT (id) DO UPDATE
                        SET display_currency = EXCLUDED.display_currency
            """;
        await dbContext.Database.ExecuteSqlInterpolatedAsync(command, cancellationToken);

        return await this.GetPersistedAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GlobalSettings> UpsertThemeAsync(
        WorkbenchTheme theme,
        CancellationToken cancellationToken)
    {
        var themeCode = WorkbenchThemeCode.ToCode(theme);
        var defaultCurrency = DisplayCurrency.USD.ToString();
        FormattableString command = $$"""
                        INSERT INTO global_settings (id, display_currency, theme)
                        VALUES ({{GlobalSettings.SingletonId}}, {{defaultCurrency}}, {{themeCode}})
                        ON CONFLICT (id) DO UPDATE
                        SET theme = EXCLUDED.theme
            """;
        await dbContext.Database.ExecuteSqlInterpolatedAsync(command, cancellationToken);

        return await this.GetPersistedAsync(cancellationToken);
    }

    private async Task<GlobalSettings> GetPersistedAsync(CancellationToken cancellationToken) =>
        await dbContext.GlobalSettings
            .AsNoTracking()
            .SingleAsync(cancellationToken);
}
