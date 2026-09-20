// <copyright file="IIncomeScheduleRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Domain.Income;

/// <summary>Persists recurring income schedules and their materialized receipts.</summary>
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

    /// <summary>Lists all schedules considered for a pay-date materialization operation.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The schedules.</returns>
    Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken);

    /// <summary>Lists materialized receipts with pay dates in an inclusive calendar range.</summary>
    /// <param name="startDate">The inclusive first pay date.</param>
    /// <param name="endDate">The inclusive final pay date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching receipts with their allocation snapshots.</returns>
    Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>Persists revised lifecycle or future schedule values.</summary>
    /// <param name="schedule">The schedule to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken);

    /// <summary>Atomically stores a receipt if no receipt has the same schedule and pay date.</summary>
    /// <param name="receipt">The receipt to conditionally persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored receipt, or <see langword="null"/> when that occurrence already exists.</returns>
    Task<IncomeReceipt?> GetOrAddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken);
}
