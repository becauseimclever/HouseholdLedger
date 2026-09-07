// <copyright file="ExpenseTransaction.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Transactions;

/// <summary>
/// Represents one USD expense recorded on a calendar date.
/// </summary>
public sealed class ExpenseTransaction
{
    /// <summary>The largest amount representable by the persistence contract.</summary>
    public const decimal MaximumAmount = 9999999999999999.99m;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpenseTransaction"/> class.
    /// </summary>
    /// <param name="id">The backend-generated transaction identifier.</param>
    /// <param name="accountId">The owning account identifier.</param>
    /// <param name="date">The date on which the expense is recorded.</param>
    /// <param name="amount">The positive USD amount.</param>
    /// <param name="classification">The expense classification.</param>
    /// <param name="sequence">The backend-assigned creation sequence.</param>
    public ExpenseTransaction(
        Guid id,
        Guid accountId,
        DateOnly date,
        decimal amount,
        ExpenseClassification classification,
        long sequence = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A transaction identifier is required.", nameof(id));
        }

        ValidateAccountId(accountId);
        ValidateDetails(amount, classification);

        if (sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "The creation sequence cannot be negative.");
        }

        this.Id = id;
        this.AccountId = accountId;
        this.Date = date;
        this.Amount = amount;
        this.Classification = classification;
        this.Sequence = sequence;
    }

    /// <summary>Gets the transaction identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning account identifier.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Gets the ledger date.</summary>
    public DateOnly Date { get; }

    /// <summary>Gets the USD amount.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Gets the expense classification.</summary>
    public ExpenseClassification Classification { get; private set; }

    /// <summary>Gets the backend-assigned creation sequence.</summary>
    public long Sequence { get; private set; }

    /// <summary>Replaces the correctable transaction details.</summary>
    /// <param name="accountId">The replacement owning account identifier.</param>
    /// <param name="amount">The positive USD amount.</param>
    /// <param name="classification">The expense classification.</param>
    public void Revise(Guid accountId, decimal amount, ExpenseClassification classification)
    {
        ValidateAccountId(accountId);
        ValidateDetails(amount, classification);
        this.AccountId = accountId;
        this.Amount = amount;
        this.Classification = classification;
    }

    private static void ValidateAccountId(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("An account identifier is required.", nameof(accountId));
        }
    }

    private static void ValidateDetails(decimal amount, ExpenseClassification classification)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount cannot have more than two decimal places.");
        }

        if (amount > MaximumAmount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"The amount cannot exceed {MaximumAmount}.");
        }

        if (!Enum.IsDefined(classification))
        {
            throw new ArgumentOutOfRangeException(nameof(classification), "The classification is not supported.");
        }
    }
}
