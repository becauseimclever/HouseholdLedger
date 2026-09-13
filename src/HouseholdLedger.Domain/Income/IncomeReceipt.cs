// <copyright file="IncomeReceipt.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Represents an immutable materialized income occurrence.</summary>
public sealed class IncomeReceipt
{
    private readonly IReadOnlyList<IncomeAccountAllocation> allocations;

    /// <summary>Initializes a new instance of the <see cref="IncomeReceipt"/> class.</summary>
    /// <param name="id">The backend-generated receipt identifier.</param>
    /// <param name="scheduleId">The originating pay schedule identifier.</param>
    /// <param name="payDate">The calendar day income was received.</param>
    /// <param name="netIncome">The positive income amount received.</param>
    /// <param name="sourceAllocations">The immutable account-allocation snapshot.</param>
    public IncomeReceipt(Guid id, Guid scheduleId, DateOnly payDate, decimal netIncome, IEnumerable<IncomeAccountAllocation> sourceAllocations)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An income receipt identifier is required.", nameof(id));
        }

        if (scheduleId == Guid.Empty)
        {
            throw new ArgumentException("A pay schedule identifier is required.", nameof(scheduleId));
        }

        ArgumentNullException.ThrowIfNull(sourceAllocations);
        IncomeAmount.Validate(netIncome, nameof(netIncome));
        var candidateAllocations = sourceAllocations.ToArray();
        if (candidateAllocations.Length == 0)
        {
            throw new ArgumentException("At least one account allocation is required.", nameof(sourceAllocations));
        }

        if (candidateAllocations.GroupBy(allocation => allocation.AccountId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("An account can only receive one allocation per receipt.", nameof(sourceAllocations));
        }

        if (candidateAllocations.Sum(allocation => allocation.Amount) != netIncome)
        {
            throw new ArgumentException("Account allocations must total the net income.", nameof(sourceAllocations));
        }

        this.Id = id;
        this.ScheduleId = scheduleId;
        this.PayDate = payDate;
        this.NetIncome = netIncome;
        this.allocations = Array.AsReadOnly(candidateAllocations);
    }

    /// <summary>Gets the backend-generated receipt identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the originating pay schedule identifier.</summary>
    public Guid ScheduleId { get; }

    /// <summary>Gets the calendar day income was received.</summary>
    public DateOnly PayDate { get; }

    /// <summary>Gets the positive income amount received.</summary>
    public decimal NetIncome { get; }

    /// <summary>Gets the immutable account-allocation snapshot.</summary>
    public IReadOnlyList<IncomeAccountAllocation> Allocations => this.allocations;
}
