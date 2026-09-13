// <copyright file="IncomeScheduleAllocationRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class IncomeScheduleAllocationRecord
{
    public Guid ScheduleId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }
}
