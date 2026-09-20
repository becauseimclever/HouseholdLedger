// <copyright file="PaySchedule.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Represents a recurring pay schedule and its account allocations.</summary>
public sealed class PaySchedule
{
    private IReadOnlyList<IncomeAccountAllocation> allocations = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="PaySchedule"/> class.
    /// </summary>
    /// <param name="id">The backend-generated schedule identifier.</param>
    /// <param name="name">The user-visible schedule name.</param>
    /// <param name="firstPayDate">The first pay date.</param>
    /// <param name="cadence">The recurrence cadence.</param>
    /// <param name="netIncome">The positive net income for each occurrence.</param>
    /// <param name="allocations">The account allocations for each occurrence.</param>
    /// <param name="secondMonthlyPayDay">The second monthly pay day for a semimonthly schedule.</param>
    public PaySchedule(
        Guid id,
        string name,
        DateOnly firstPayDate,
        PayPeriodCadence cadence,
        decimal netIncome,
        IEnumerable<IncomeAccountAllocation> allocations,
        int? secondMonthlyPayDay = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A pay schedule identifier is required.", nameof(id));
        }

        this.Id = id;
        this.Apply(name, firstPayDate, cadence, netIncome, allocations, secondMonthlyPayDay);
    }

    /// <summary>Gets the backend-generated schedule identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the user-visible schedule name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the first pay date.</summary>
    public DateOnly FirstPayDate { get; private set; }

    /// <summary>Gets the schedule recurrence cadence.</summary>
    public PayPeriodCadence Cadence { get; private set; }

    /// <summary>Gets the positive net income for each occurrence.</summary>
    public decimal NetIncome { get; private set; }

    /// <summary>Gets the account allocations for each occurrence.</summary>
    public IReadOnlyList<IncomeAccountAllocation> Allocations => this.allocations;

    /// <summary>Gets the second configured monthly pay day for a semimonthly schedule.</summary>
    public int? SecondMonthlyPayDay { get; private set; }

    /// <summary>Gets a value indicating whether future receipt creation is paused.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Gets the earliest date on which a receipt may be materialized.</summary>
    public DateOnly ReceiptEligibleFrom { get; private set; }

    /// <summary>Revises the values used for future income receipts.</summary>
    /// <param name="name">The user-visible schedule name.</param>
    /// <param name="firstPayDate">The first future pay date.</param>
    /// <param name="cadence">The recurrence cadence.</param>
    /// <param name="netIncome">The positive net income for each future occurrence.</param>
    /// <param name="allocations">The account allocations for each future occurrence.</param>
    /// <param name="secondMonthlyPayDay">The second monthly pay day for a semimonthly schedule.</param>
    public void Revise(
        string name,
        DateOnly firstPayDate,
        PayPeriodCadence cadence,
        decimal netIncome,
        IEnumerable<IncomeAccountAllocation> allocations,
        int? secondMonthlyPayDay = null) =>
        this.Apply(name, firstPayDate, cadence, netIncome, allocations, secondMonthlyPayDay);

    /// <summary>Prevents future income receipt creation.</summary>
    public void Pause() => this.IsPaused = true;

    /// <summary>Allows future income receipt creation.</summary>
    public void Resume() => this.IsPaused = false;

    /// <summary>Allows future receipt creation without backfilling pay dates missed while paused.</summary>
    /// <param name="resumeDate">The date on which the schedule resumed.</param>
    public void Resume(DateOnly resumeDate)
    {
        this.IsPaused = false;
        this.ReceiptEligibleFrom = resumeDate > this.FirstPayDate ? resumeDate : this.FirstPayDate;
    }

    /// <summary>Validates and applies schedule values used for future receipts.</summary>
    /// <param name="name">The user-visible schedule name.</param>
    /// <param name="firstPayDate">The first pay date.</param>
    /// <param name="cadence">The recurrence cadence.</param>
    /// <param name="netIncome">The positive net income for each occurrence.</param>
    /// <param name="sourceAllocations">The account allocations for each occurrence.</param>
    /// <param name="secondMonthlyPayDay">The second monthly pay day for a semimonthly schedule.</param>
    private void Apply(
        string name,
        DateOnly firstPayDate,
        PayPeriodCadence cadence,
        decimal netIncome,
        IEnumerable<IncomeAccountAllocation> sourceAllocations,
        int? secondMonthlyPayDay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sourceAllocations);
        if (name.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(name), "The schedule name cannot exceed 100 characters.");
        }

        if (!Enum.IsDefined(cadence))
        {
            throw new ArgumentOutOfRangeException(nameof(cadence), "The pay cadence is not supported.");
        }

        if (cadence == PayPeriodCadence.Semimonthly)
        {
            if (secondMonthlyPayDay is < 1 or > 31 || secondMonthlyPayDay == firstPayDate.Day)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondMonthlyPayDay),
                    "A semimonthly schedule requires a distinct second pay day from 1 through 31.");
            }
        }
        else if (secondMonthlyPayDay is not null)
        {
            throw new ArgumentException(
                "Only semimonthly schedules can specify a second monthly pay day.",
                nameof(secondMonthlyPayDay));
        }

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

        var total = candidateAllocations.Sum(allocation => allocation.Amount);
        if (total != netIncome)
        {
            throw new ArgumentException("Account allocations must total the net income.", nameof(sourceAllocations));
        }

        this.Name = name.Trim();
        this.FirstPayDate = firstPayDate;
        this.Cadence = cadence;
        this.NetIncome = netIncome;
        this.allocations = candidateAllocations;
        this.SecondMonthlyPayDay = secondMonthlyPayDay;
        this.ReceiptEligibleFrom = firstPayDate;
    }
}
