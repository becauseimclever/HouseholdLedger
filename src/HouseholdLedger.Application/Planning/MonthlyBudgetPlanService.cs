// <copyright file="MonthlyBudgetPlanService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Planning;

using HouseholdLedger.Application.Income;
using HouseholdLedger.Application.Transactions;
using HouseholdLedger.Domain.Planning;
using HouseholdLedger.Domain.Transactions;

/// <summary>Creates deliberate monthly plans and compares them with ledger actuals.</summary>
public sealed class MonthlyBudgetPlanService(
    IMonthlyBudgetPlanRepository planRepository,
    IExpenseTransactionRepository expenseRepository,
    IIncomeScheduleRepository incomeRepository,
    TimeProvider clock)
{
    /// <summary>Gets a plan for one month, if one exists.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved plan, or <see langword="null"/>.</returns>
    public Task<MonthlyBudgetPlan?> GetAsync(int year, int month, CancellationToken cancellationToken = default) =>
        planRepository.FindAsync(GetMonth(year, month), cancellationToken);

    /// <summary>Creates or deliberately revises a plan for one month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="command">The replacement plan amounts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved plan.</returns>
    public async Task<MonthlyBudgetPlan> SaveAsync(
        int year,
        int month,
        MonthlyBudgetPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var plan = new MonthlyBudgetPlan(
            GetMonth(year, month),
            command.ExpectedIncome,
            command.IntendedSavings,
            command.Necessities,
            command.Optional,
            command.Culture,
            command.Unexpected,
            clock.GetUtcNow());
        await planRepository.UpsertAsync(plan, cancellationToken);
        return plan;
    }

    /// <summary>Builds a review from saved intention and persisted monthly activity.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The monthly plan and actuals comparison.</returns>
    public async Task<MonthlyBudgetReviewDto> ReviewAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var firstDate = GetMonth(year, month);
        var lastDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var plan = await planRepository.FindAsync(firstDate, cancellationToken);
        var expenses = await expenseRepository.ListByDateRangeAsync(firstDate, lastDate, cancellationToken);
        var receipts = await incomeRepository.ListReceiptsAsync(firstDate, lastDate, cancellationToken);
        var actualIncome = receipts.Sum(receipt => receipt.NetIncome);
        var actualByClassification = expenses
            .GroupBy(expense => expense.Classification)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount));
        var plannedByClassification = plan is null
            ? new Dictionary<ExpenseClassification, decimal>()
            : new Dictionary<ExpenseClassification, decimal>
            {
                [ExpenseClassification.Necessities] = plan.Necessities,
                [ExpenseClassification.Optional] = plan.Optional,
                [ExpenseClassification.Culture] = plan.Culture,
                [ExpenseClassification.Unexpected] = plan.Unexpected,
            };
        var classifications = Enum.GetValues<ExpenseClassification>()
            .Select(classification =>
            {
                var planned = plannedByClassification.GetValueOrDefault(classification);
                var actual = actualByClassification.GetValueOrDefault(classification);
                return new MonthlyClassificationReviewDto(classification, planned, actual, actual - planned);
            })
            .ToArray();
        var actualExpenses = actualByClassification.Values.Sum();
        var unallocatedRemainder = Math.Max(0m, actualIncome - actualExpenses);

        return new MonthlyBudgetReviewDto(
            plan?.ExpectedIncome,
            plan?.IntendedSavings,
            actualIncome,
            plan is null ? null : actualIncome - plan.ExpectedIncome,
            unallocatedRemainder,
            plan?.LastRevisedAt,
            classifications,
            actualIncome - actualExpenses);
    }

    private static DateOnly GetMonth(int year, int month)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(year is < 1 or > 9999 ? nameof(year) : nameof(month));
        }

        return new DateOnly(year, month, 1);
    }
}
