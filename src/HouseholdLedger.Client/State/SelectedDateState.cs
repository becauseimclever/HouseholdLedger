// <copyright file="SelectedDateState.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.State;

/// <summary>
/// Shares the transient selected calendar date within the Client scope.
/// </summary>
public sealed class SelectedDateState
{
    /// <summary>Occurs after the selected date changes.</summary>
    public event Action<DateOnly>? Changed;

    /// <summary>Occurs after transactions for a date may have changed.</summary>
    public event Action<DateOnly>? TransactionsChanged;

    /// <summary>Gets the selected date, or <see langword="null"/> before selection.</summary>
    public DateOnly? Value { get; private set; }

    /// <summary>Selects a calendar date.</summary>
    /// <param name="ledgerDate">The date to select.</param>
    public void Select(DateOnly ledgerDate)
    {
        this.Value = ledgerDate;
        this.Changed?.Invoke(ledgerDate);
    }

    /// <summary>Notifies calendar consumers that a date's transaction summary changed.</summary>
    /// <param name="ledgerDate">The affected ledger date.</param>
    public void NotifyTransactionsChanged(DateOnly ledgerDate)
    {
        this.TransactionsChanged?.Invoke(ledgerDate);
    }
}
