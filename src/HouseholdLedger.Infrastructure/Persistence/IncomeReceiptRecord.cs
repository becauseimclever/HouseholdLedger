// <copyright file="IncomeReceiptRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class IncomeReceiptRecord
{
    public Guid Id { get; set; }

    public Guid ScheduleId { get; set; }

    public DateOnly PayDate { get; set; }

    public decimal NetIncome { get; set; }

    public List<IncomeReceiptAllocationRecord> Allocations { get; } = [];
}
