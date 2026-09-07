// <copyright file="ThemeCatalog.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

/// <summary>Describes the finite set of selectable workbench themes.</summary>
public static class ThemeCatalog
{
    /// <summary>The default theme identifier.</summary>
    public const string DefaultTheme = "workbench-dark";

    /// <summary>Gets the supported themes.</summary>
    public static IReadOnlyList<ThemeOption> SupportedThemes { get; } =
    [
        new(DefaultTheme, "Workbench Dark"),
        new("workbench-light", "Workbench Light"),
    ];

    /// <summary>Determines whether an identifier is supported.</summary>
    /// <param name="theme">The candidate identifier.</param>
    /// <returns><see langword="true"/> when the identifier is supported.</returns>
    public static bool IsSupported(string? theme) => SupportedThemes.Any(
        option => string.Equals(option.Identifier, theme, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets the user-facing name for a supported theme.</summary>
    /// <param name="theme">The supported identifier.</param>
    /// <returns>The theme name.</returns>
    public static string GetDisplayName(string theme) => SupportedThemes.Single(
        option => string.Equals(option.Identifier, theme, StringComparison.OrdinalIgnoreCase)).DisplayName;
}
