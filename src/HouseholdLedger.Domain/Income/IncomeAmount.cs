// <copyright file="IncomeAmount.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Income;

/// <summary>Validates monetary amounts used by income records.</summary>
public static class IncomeAmount
{
    /// <summary>Gets the largest amount supported by the persistence contract.</summary>
    public const decimal Maximum = 9999999999999999.99m;

    /// <summary>Validates a positive two-decimal monetary amount.</summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="parameterName">The parameter name for validation failures.</param>
    public static void Validate(decimal amount, string parameterName)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The amount must be positive.");
        }

        if (amount > Maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"The amount cannot exceed {Maximum}.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The amount cannot have more than two decimal places.");
        }
    }
}
