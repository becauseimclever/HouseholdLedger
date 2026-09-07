// <copyright file="AccountTransactionCriteria.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

using HouseholdLedger.Domain.Transactions;

/// <summary>Defines normalized criteria for one account's transaction history.</summary>
public sealed record AccountTransactionCriteria
{
    /// <summary>The maximum supported search length.</summary>
    public const int MaximumSearchLength = 100;

    private AccountTransactionCriteria(
        DateOnly? fromDate,
        DateOnly? toDate,
        ExpenseClassification? classification,
        decimal? minimumAmount,
        decimal? maximumAmount,
        string? search)
    {
        this.FromDate = fromDate;
        this.ToDate = toDate;
        this.Classification = classification;
        this.MinimumAmount = minimumAmount;
        this.MaximumAmount = maximumAmount;
        this.Search = search;
    }

    /// <summary>Gets the inclusive first ledger date.</summary>
    public DateOnly? FromDate { get; }

    /// <summary>Gets the inclusive final ledger date.</summary>
    public DateOnly? ToDate { get; }

    /// <summary>Gets the required expense classification.</summary>
    public ExpenseClassification? Classification { get; }

    /// <summary>Gets the inclusive minimum stored amount.</summary>
    public decimal? MinimumAmount { get; }

    /// <summary>Gets the inclusive maximum stored amount.</summary>
    public decimal? MaximumAmount { get; }

    /// <summary>Gets the trimmed search text, or <see langword="null"/> when empty.</summary>
    public string? Search { get; }

    /// <summary>Creates validated, normalized account-history criteria.</summary>
    /// <param name="fromDate">The inclusive first ledger date.</param>
    /// <param name="toDate">The inclusive final ledger date.</param>
    /// <param name="classification">The required expense classification.</param>
    /// <param name="minimumAmount">The inclusive minimum stored amount.</param>
    /// <param name="maximumAmount">The inclusive maximum stored amount.</param>
    /// <param name="search">The text to match against visible transaction fields.</param>
    /// <returns>The validated, normalized criteria.</returns>
    /// <exception cref="ArgumentException">A range is invalid or search text is too long.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An amount is negative.</exception>
    public static AccountTransactionCriteria Create(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        ExpenseClassification? classification = null,
        decimal? minimumAmount = null,
        decimal? maximumAmount = null,
        string? search = null)
    {
        if (fromDate > toDate)
        {
            throw new ArgumentException("From date must be on or before To date.", nameof(fromDate));
        }

        if (minimumAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAmount), "Minimum amount cannot be negative.");
        }

        if (maximumAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAmount), "Maximum amount cannot be negative.");
        }

        if (minimumAmount > maximumAmount)
        {
            throw new ArgumentException("Minimum amount must not exceed Maximum amount.", nameof(minimumAmount));
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (normalizedSearch?.Length > MaximumSearchLength)
        {
            throw new ArgumentException(
                $"Search must be {MaximumSearchLength} characters or fewer.",
                nameof(search));
        }

        return new AccountTransactionCriteria(
            fromDate,
            toDate,
            classification,
            minimumAmount,
            maximumAmount,
            normalizedSearch);
    }
}
