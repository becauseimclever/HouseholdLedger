// <copyright file="GlobalSettings.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Settings;

/// <summary>Stores application-wide settings with one stable identity.</summary>
public sealed class GlobalSettings
{
    /// <summary>The stable singleton row identifier.</summary>
    public const int SingletonId = 1;

    /// <summary>Initializes a new instance of the <see cref="GlobalSettings"/> class.</summary>
    /// <param name="displayCurrency">The supported display currency.</param>
    /// <param name="theme">The supported workbench theme.</param>
    public GlobalSettings(
        DisplayCurrency displayCurrency,
        WorkbenchTheme theme = WorkbenchTheme.WorkbenchDark)
    {
        EnsureSupported(displayCurrency);
        EnsureSupported(theme);
        this.Id = SingletonId;
        this.DisplayCurrency = displayCurrency;
        this.Theme = theme;
    }

    /// <summary>Gets the stable singleton row identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the global display currency.</summary>
    public DisplayCurrency DisplayCurrency { get; private set; }

    /// <summary>Gets the global workbench theme.</summary>
    public WorkbenchTheme Theme { get; private set; }

    /// <summary>Changes the global display currency.</summary>
    /// <param name="displayCurrency">The supported display currency.</param>
    public void ChangeDisplayCurrency(DisplayCurrency displayCurrency)
    {
        EnsureSupported(displayCurrency);
        this.DisplayCurrency = displayCurrency;
    }

    /// <summary>Changes the global workbench theme.</summary>
    /// <param name="theme">The supported workbench theme.</param>
    public void ChangeTheme(WorkbenchTheme theme)
    {
        EnsureSupported(theme);
        this.Theme = theme;
    }

    private static void EnsureSupported(DisplayCurrency displayCurrency)
    {
        if (!Enum.IsDefined(displayCurrency))
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayCurrency),
                "The display currency is not supported.");
        }
    }

    private static void EnsureSupported(WorkbenchTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), "The workbench theme is not supported.");
        }
    }
}
