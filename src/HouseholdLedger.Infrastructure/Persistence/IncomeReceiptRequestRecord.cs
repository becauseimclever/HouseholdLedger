// <copyright file="IncomeReceiptRequestRecord.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

internal sealed class IncomeReceiptRequestRecord
{
    public Guid RequestId { get; set; }

    public Guid ReceiptId { get; set; }

    public string Payload { get; set; } = string.Empty;
}
