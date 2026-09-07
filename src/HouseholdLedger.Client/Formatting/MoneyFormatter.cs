// <copyright file="MoneyFormatter.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Formatting;

using System.Globalization;

/// <summary>Formats ledger amounts using the selected global display currency.</summary>
public static class MoneyFormatter
{
    /// <summary>The default currency code used when no setting row exists.</summary>
    public const string DefaultCurrencyCode = "USD";

    /// <summary>Gets the supported named currencies.</summary>
    public static IReadOnlyList<DisplayCurrencyOption> SupportedCurrencies { get; } =
    [
        new("USD", "US Dollar", "en-US"),
        new("CAD", "Canadian Dollar", "en-CA"),
        new("EUR", "Euro", "de-DE"),
        new("GBP", "Pound Sterling", "en-GB"),
        new("AUD", "Australian Dollar", "en-AU"),
        new("XXX", "No currency", "en-US"),
    ];

    /// <summary>Determines whether a currency code is supported.</summary>
    /// <param name="currencyCode">The candidate code.</param>
    /// <returns><see langword="true"/> when the code is supported.</returns>
    public static bool IsSupported(string? currencyCode) => SupportedCurrencies.Any(
        option => string.Equals(option.Code, currencyCode, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets the compact label used beside monetary controls.</summary>
    /// <param name="currencyCode">The supported currency code.</param>
    /// <returns>The named code or generic currency sign.</returns>
    public static string GetDisplayLabel(string currencyCode) =>
        string.Equals(currencyCode, "XXX", StringComparison.OrdinalIgnoreCase) ? "¤" : currencyCode.ToUpperInvariant();

    /// <summary>Gets the user-facing name for a supported display currency.</summary>
    /// <param name="currencyCode">The supported currency code.</param>
    /// <returns>The configured option's display name.</returns>
    public static string GetDisplayName(string currencyCode) => SupportedCurrencies.Single(
        option => string.Equals(option.Code, currencyCode, StringComparison.OrdinalIgnoreCase)).DisplayName;

    /// <summary>Formats an amount without changing its numeric value.</summary>
    /// <param name="amount">The ledger amount.</param>
    /// <param name="currencyCode">The supported currency code.</param>
    /// <returns>The currency-formatted text.</returns>
    public static string Format(decimal amount, string currencyCode)
    {
        var option = SupportedCurrencies.Single(
            item => string.Equals(item.Code, currencyCode, StringComparison.OrdinalIgnoreCase));
        var culture = CultureInfo.GetCultureInfo(option.CultureName);
        if (!string.Equals(option.Code, "XXX", StringComparison.Ordinal))
        {
            return amount.ToString("C", culture);
        }

        var genericFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        genericFormat.CurrencySymbol = "¤";
        return amount.ToString("C", genericFormat);
    }
}
