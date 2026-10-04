// <copyright file="MonthlyBudgetPlan.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Domain.Planning;

using HouseholdLedger.Domain.Transactions;

/// <summary>Represents one deliberate budget plan for a calendar month.</summary>
public sealed class MonthlyBudgetPlan
{
    /// <summary>Initializes a new instance of the <see cref="MonthlyBudgetPlan"/> class.</summary>
    /// <param name="month">The first day of the planned calendar month.</param>
    /// <param name="expectedIncome">The expected take-home income.</param>
    /// <param name="intendedSavings">The amount intended for savings.</param>
    /// <param name="necessities">The planned Necessities spending.</param>
    /// <param name="optional">The planned Optional spending.</param>
    /// <param name="culture">The planned Culture spending.</param>
    /// <param name="unexpected">The planned Unexpected spending.</param>
    /// <param name="lastRevisedAt">When the plan was last saved.</param>
    public MonthlyBudgetPlan(
        DateOnly month,
        decimal expectedIncome,
        decimal intendedSavings,
        decimal necessities,
        decimal optional,
        decimal culture,
        decimal unexpected,
        DateTimeOffset lastRevisedAt)
    {
        if (month.Day != 1)
        {
            throw new ArgumentException("The plan date must be the first day of a month.", nameof(month));
        }

        ValidateAmount(expectedIncome, nameof(expectedIncome));
        ValidateAmount(intendedSavings, nameof(intendedSavings));
        ValidateAmount(necessities, nameof(necessities));
        ValidateAmount(optional, nameof(optional));
        ValidateAmount(culture, nameof(culture));
        ValidateAmount(unexpected, nameof(unexpected));

        if (expectedIncome != intendedSavings + necessities + optional + culture + unexpected)
        {
            throw new ArgumentException(
                "Expected income must equal intended savings and all four planned classifications.",
                nameof(expectedIncome));
        }

        this.Month = month;
        this.ExpectedIncome = expectedIncome;
        this.IntendedSavings = intendedSavings;
        this.Necessities = necessities;
        this.Optional = optional;
        this.Culture = culture;
        this.Unexpected = unexpected;
        this.LastRevisedAt = lastRevisedAt;
    }

    /// <summary>Gets the first day of the planned month.</summary>
    public DateOnly Month { get; }

    /// <summary>Gets the expected take-home income.</summary>
    public decimal ExpectedIncome { get; }

    /// <summary>Gets the intended savings amount.</summary>
    public decimal IntendedSavings { get; }

    /// <summary>Gets the planned Necessities spending.</summary>
    public decimal Necessities { get; }

    /// <summary>Gets the planned Optional spending.</summary>
    public decimal Optional { get; }

    /// <summary>Gets the planned Culture spending.</summary>
    public decimal Culture { get; }

    /// <summary>Gets the planned Unexpected spending.</summary>
    public decimal Unexpected { get; }

    /// <summary>Gets the latest save time.</summary>
    public DateTimeOffset LastRevisedAt { get; }

    private static void ValidateAmount(decimal amount, string parameterName)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Planned amounts cannot be negative.");
        }

        if (amount > ExpenseTransaction.MaximumAmount)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"An amount cannot exceed {ExpenseTransaction.MaximumAmount}.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Amounts cannot have more than two decimal places.");
        }
    }
}
