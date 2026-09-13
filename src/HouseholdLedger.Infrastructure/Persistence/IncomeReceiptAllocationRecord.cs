// <copyright file="IncomeReceiptAllocationRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class IncomeReceiptAllocationRecord
{
    public Guid ReceiptId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }
}
