// <copyright file="WorkbenchThemeCode.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Settings;

/// <summary>Maps supported workbench themes to stable storage identifiers.</summary>
public static class WorkbenchThemeCode
{
    /// <summary>The default theme identifier.</summary>
    public const string Default = "workbench-dark";

    /// <summary>Returns the stable identifier for a supported theme.</summary>
    /// <param name="theme">The theme.</param>
    /// <returns>The stable identifier.</returns>
    public static string ToCode(WorkbenchTheme theme) => theme switch
    {
        WorkbenchTheme.WorkbenchDark => Default,
        WorkbenchTheme.WorkbenchLight => "workbench-light",
        _ => throw new ArgumentOutOfRangeException(nameof(theme), "The workbench theme is not supported."),
    };

    /// <summary>Parses a stable supported theme identifier.</summary>
    /// <param name="value">The candidate identifier.</param>
    /// <returns>The supported theme.</returns>
    public static WorkbenchTheme Parse(string value) => value.ToLowerInvariant() switch
    {
        Default => WorkbenchTheme.WorkbenchDark,
        "workbench-light" => WorkbenchTheme.WorkbenchLight,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "The workbench theme is not supported."),
    };

    /// <summary>Attempts to parse a stable supported theme identifier.</summary>
    /// <param name="value">The candidate identifier.</param>
    /// <param name="theme">The parsed theme.</param>
    /// <returns><see langword="true"/> when the identifier is supported.</returns>
    public static bool TryParse(string? value, out WorkbenchTheme theme)
    {
        WorkbenchTheme? parsed = value?.ToLowerInvariant() switch
        {
            Default => WorkbenchTheme.WorkbenchDark,
            "workbench-light" => WorkbenchTheme.WorkbenchLight,
            _ => null,
        };

        theme = parsed.GetValueOrDefault();
        return parsed.HasValue;
    }
}
