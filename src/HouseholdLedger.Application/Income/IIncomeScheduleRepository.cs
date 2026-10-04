// <copyright file="IIncomeScheduleRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Domain.Income;

/// <summary>Persists income expectation schedules and confirmed receipts.</summary>
public interface IIncomeScheduleRepository
{
    /// <summary>Adds a schedule.</summary>
    /// <param name="schedule">The schedule to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken);

    /// <summary>Finds one schedule by identifier.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching schedule, or <see langword="null"/>.</returns>
    Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>Lists all income expectation schedules.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The schedules.</returns>
    Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken);

    /// <summary>Lists confirmed receipts with dates in an inclusive calendar range.</summary>
    /// <param name="startDate">The inclusive first pay date.</param>
    /// <param name="endDate">The inclusive final pay date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching receipts with their allocation snapshots.</returns>
    Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>Finds one confirmed receipt by identifier.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching receipt, or <see langword="null"/>.</returns>
    Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken);

    /// <summary>Adds an income receipt explicitly confirmed by the user.</summary>
    /// <param name="receipt">The confirmed receipt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken);

    /// <summary>Atomically confirms a receipt, replaying an identical token or rejecting a conflicting payload.</summary>
    /// <param name="receipt">The confirmed receipt.</param>
    /// <param name="requestId">The optional client retry token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The original confirmed receipt on replay, otherwise the new receipt.</returns>
    Task<IncomeReceipt> ConfirmReceiptAsync(IncomeReceipt receipt, Guid? requestId, CancellationToken cancellationToken);

    /// <summary>Replaces one receipt and its allocation snapshot.</summary>
    /// <param name="receipt">The corrected receipt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the receipt existed.</returns>
    Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken);

    /// <summary>Deletes one receipt and its allocations, retaining any retry token tombstone.</summary>
    /// <param name="receiptId">The receipt identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Whether the receipt existed.</returns>
    Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken);

    /// <summary>Persists revised lifecycle or future schedule values.</summary>
    /// <param name="schedule">The schedule to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken);
}
