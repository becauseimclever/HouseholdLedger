// <copyright file="ExpenseTransaction.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Transactions;

/// <summary>
/// Represents one USD expense recorded on a calendar date.
/// </summary>
public sealed class ExpenseTransaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseTransaction"/> class.
    /// </summary>
    /// <param name="id">The backend-generated transaction identifier.</param>
    /// <param name="date">The date on which the expense is recorded.</param>
    /// <param name="amount">The positive USD amount.</param>
    /// <param name="classification">The expense classification.</param>
    /// <param name="sequence">The backend-assigned creation sequence.</param>
    public ExpenseTransaction(
        Guid id,
        DateOnly date,
        decimal amount,
        ExpenseClassification classification,
        long sequence = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A transaction identifier is required.", nameof(id));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount cannot have more than two decimal places.");
        }

        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification), "The classification is not supported.");
        }

        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "The creation sequence cannot be negative.");
        }

        this.Id = id;
        this.Date = date;
        this.Amount = amount;
        this.Classification = classification;
        this.Sequence = sequence;
    }

    /// <summary>Gets the transaction identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the ledger date.</summary>
    public DateOnly Date { get; }

    /// <summary>Gets the USD amount.</summary>
    public decimal Amount { get; }

    /// <summary>Gets the expense classification.</summary>
    public ExpenseClassification Classification { get; }

    /// <summary>Gets the backend-assigned creation sequence.</summary>
    public long Sequence { get; private set; }
}
