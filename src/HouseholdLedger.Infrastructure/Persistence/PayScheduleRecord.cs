// <copyright file="PayScheduleRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Domain.Income;

internal sealed class PayScheduleRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly FirstPayDate { get; set; }

    public PayPeriodCadence Cadence { get; set; }

    public decimal NetIncome { get; set; }

    public int? SecondMonthlyPayDay { get; set; }

    public bool IsPaused { get; set; }

    public DateOnly ReceiptEligibleFrom { get; set; }

    public List<IncomeScheduleAllocationRecord> Allocations { get; } = [];

    public List<IncomeReceiptRecord> Receipts { get; } = [];
}
