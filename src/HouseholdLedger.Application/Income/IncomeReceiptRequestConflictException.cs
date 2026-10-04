// <copyright file="IncomeReceiptRequestConflictException.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

/// <summary>Indicates a confirmation retry token was reused with a different payload.</summary>
public sealed class IncomeReceiptRequestConflictException()
    : InvalidOperationException("The request identifier has already been used for different receipt details.");
