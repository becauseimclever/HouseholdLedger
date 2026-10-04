// <copyright file="MoneyInput.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Formatting;

using System.Globalization;

/// <summary>Validates decimal values supplied by native numeric inputs.</summary>
internal static class MoneyInput
{
    internal const decimal Maximum = 9999999999999999.99m;

    internal static bool TryParse(string text, out decimal amount) =>
        decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount)
        && amount >= 0
        && amount <= Maximum
        && decimal.Round(amount, 2) == amount;

    internal static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
}
