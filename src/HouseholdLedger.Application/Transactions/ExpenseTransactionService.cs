// <copyright file="ExpenseTransactionService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Transactions;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Transactions;

/// <summary>
/// Creates and lists date-scoped expense transactions.
/// </summary>
public sealed class ExpenseTransactionService(
    IExpenseTransactionRepository repository,
    IAccountRepository accountRepository)
{
    /// <summary>Creates one expense for a selected date.</summary>
    /// <param name="ledgerDate">The selected ledger date.</param>
    /// <param name="accountId">The owning account identifier.</param>
    /// <param name="amount">The positive USD amount.</param>
    /// <param name="classification">The expense classification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created transaction.</returns>
    public async Task<ExpenseTransactionDto> CreateAsync(
        DateOnly ledgerDate,
        Guid accountId,
        decimal amount,
        ExpenseClassification classification,
        CancellationToken cancellationToken = default)
    {
        var account = await this.FindRequiredAccountAsync(accountId, cancellationToken);
        var transaction = new ExpenseTransaction(Guid.NewGuid(), account.Id, ledgerDate, amount, classification);
        await repository.AddAsync(transaction, cancellationToken);
        return Map(transaction, account.Name);
    }

    /// <summary>Lists expenses for one date in backend creation order.</summary>
    /// <param name="ledgerDate">The ledger date to query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transactions recorded on the date.</returns>
    public async Task<IReadOnlyList<ExpenseTransactionDto>> ListAsync(
        DateOnly ledgerDate,
        CancellationToken cancellationToken = default)
    {
        return await repository.ListByDateAsync(ledgerDate, cancellationToken);
    }

    /// <summary>Gets an account and its complete newest-first transaction history.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account history, or <see langword="null"/> when the account does not exist.</returns>
    public async Task<AccountTransactionHistoryDto?> GetAccountHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await this.GetAccountHistoryAsync(
            accountId,
            AccountTransactionCriteria.Create(),
            cancellationToken);
    }

    /// <summary>Gets an account and its matching newest-first transaction history.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="criteria">The validated account-history criteria.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account history, or <see langword="null"/> when the account does not exist.</returns>
    public async Task<AccountTransactionHistoryDto?> GetAccountHistoryAsync(
        Guid accountId,
        AccountTransactionCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var account = await accountRepository.FindAsync(accountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var transactions = await repository.ListByAccountAsync(accountId, criteria, cancellationToken);
        return new AccountTransactionHistoryDto(account.Id, account.Name, transactions);
    }

    /// <summary>Summarizes daily and cumulative expenses for one month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One expense summary for each day in the month.</returns>
    public async Task<IReadOnlyList<DailyExpenseSummaryDto>> SummarizeMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var firstDate = new DateOnly(year, month, 1);
        var lastDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var transactions = await repository.ListByDateRangeAsync(firstDate, lastDate, cancellationToken);
        var totalsByDate = transactions
            .GroupBy(transaction => transaction.Date)
            .ToDictionary(group => group.Key, group => group.Sum(transaction => transaction.Amount));
        var summaries = new List<DailyExpenseSummaryDto>(DateTime.DaysInMonth(year, month));
        var monthToDateTotal = 0m;

        for (var day = 1; day <= lastDate.Day; day++)
        {
            var date = new DateOnly(year, month, day);
            var dailyTotal = totalsByDate.GetValueOrDefault(date);
            monthToDateTotal += dailyTotal;
            summaries.Add(new DailyExpenseSummaryDto(date, dailyTotal, monthToDateTotal));
        }

        return summaries;
    }

    /// <summary>Revises the correctable details of one date-scoped expense.</summary>
    /// <param name="ledgerDate">The transaction's ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="accountId">The replacement owning account identifier.</param>
    /// <param name="amount">The replacement positive USD amount.</param>
    /// <param name="classification">The replacement expense classification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The revised transaction, or <see langword="null"/> when it does not exist under the date.</returns>
    public async Task<ExpenseTransactionDto?> ReviseAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        Guid accountId,
        decimal amount,
        ExpenseClassification classification,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FindAsync(ledgerDate, transactionId, cancellationToken);
        if (transaction is null)
        {
            return null;
        }

        var account = await this.FindRequiredAccountAsync(accountId, cancellationToken);
        transaction.Revise(account.Id, amount, classification);
        await repository.UpdateAsync(transaction, cancellationToken);
        return Map(transaction, account.Name);
    }

    /// <summary>Removes one date-scoped expense.</summary>
    /// <param name="ledgerDate">The transaction's ledger date.</param>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveAsync(
        DateOnly ledgerDate,
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FindAsync(ledgerDate, transactionId, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        await repository.RemoveAsync(transaction, cancellationToken);
        return true;
    }

    private static ExpenseTransactionDto Map(ExpenseTransaction transaction, string accountName) => new(
        transaction.Id,
        transaction.AccountId,
        accountName,
        transaction.Date,
        transaction.Amount,
        transaction.Classification);

    private async Task<Domain.Accounts.Account> FindRequiredAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("An account identifier is required.", nameof(accountId));
        }

        return await accountRepository.FindAsync(accountId, cancellationToken)
            ?? throw new ArgumentException("Choose an existing account.", nameof(accountId));
    }
}
