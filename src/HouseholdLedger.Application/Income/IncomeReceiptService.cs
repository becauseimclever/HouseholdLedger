// <copyright file="IncomeReceiptService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Income;

/// <summary>Records income only after the user confirms it was received.</summary>
public sealed class IncomeReceiptService(
    IIncomeScheduleRepository repository,
    IAccountRepository accountRepository,
    TimeProvider? timeProvider = null)
{
    /// <summary>Gets one confirmed receipt.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The receipt, or <see langword="null"/> when it does not exist.</returns>
    public async Task<IncomeReceiptDto?> GetAsync(Guid receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await repository.FindReceiptAsync(receiptId, cancellationToken);
        return receipt is null ? null : Map(receipt);
    }

    /// <summary>Records a confirmed receipt and its actual account destinations.</summary>
    /// <param name="command">The user-confirmed receipt values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved receipt.</returns>
    public async Task<IncomeReceiptDto> CreateAsync(
        IncomeReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        var receipt = await this.BuildReceiptAsync(Guid.NewGuid(), null, command, cancellationToken);
        var saved = await repository.ConfirmReceiptAsync(receipt, command.RequestId, cancellationToken);
        return Map(saved);
    }

    /// <summary>Corrects the actual values of an existing confirmed receipt.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="command">The corrected actual values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corrected receipt, or null when not found.</returns>
    public async Task<IncomeReceiptDto?> UpdateAsync(
        Guid receiptId,
        IncomeReceiptCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindReceiptAsync(receiptId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var receipt = await this.BuildReceiptAsync(receiptId, existing.ScheduleId, command, cancellationToken);
        return await repository.UpdateReceiptAsync(receipt, cancellationToken) ? Map(receipt) : null;
    }

    /// <summary>Removes a mistaken receipt and its allocation snapshot.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the receipt existed.</returns>
    public Task<bool> DeleteAsync(Guid receiptId, CancellationToken cancellationToken = default) =>
        repository.DeleteReceiptAsync(receiptId, cancellationToken);

    private static IncomeReceiptDto Map(IncomeReceipt receipt) => new(
        receipt.Id,
        receipt.ScheduleId,
        receipt.PayDate,
        receipt.NetIncome,
        receipt.Allocations.Select(allocation => new IncomeAllocationDto(allocation.AccountId, allocation.Amount)).ToArray());

    private async Task<IncomeReceipt> BuildReceiptAsync(
        Guid receiptId,
        Guid? scheduleId,
        IncomeReceiptCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Allocations);
        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetLocalNow().DateTime);
        if (command.ReceivedDate == default || command.ReceivedDate > today)
        {
            throw new ArgumentException("Income must have been received on a real past or current date.", nameof(command));
        }

        if (command.RequestId == Guid.Empty)
        {
            throw new ArgumentException("A request identifier, when supplied, cannot be empty.", nameof(command));
        }

        var allocations = new List<IncomeAccountAllocation>(command.Allocations.Count);
        foreach (var allocation in command.Allocations)
        {
            if (allocation is null)
            {
                throw new ArgumentException("Account allocations cannot contain null.", nameof(command));
            }

            if (allocation.AccountId == Guid.Empty)
            {
                throw new ArgumentException("An account identifier is required.", nameof(command));
            }

            if (await accountRepository.FindAsync(allocation.AccountId, cancellationToken) is null)
            {
                throw new ArgumentException("The selected account does not exist.", nameof(command));
            }

            allocations.Add(new IncomeAccountAllocation(allocation.AccountId, allocation.Amount));
        }

        return new IncomeReceipt(receiptId, scheduleId, command.ReceivedDate, command.Amount, allocations);
    }
}
