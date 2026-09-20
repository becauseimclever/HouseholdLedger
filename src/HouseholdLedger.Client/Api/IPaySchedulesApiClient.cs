// <copyright file="IPaySchedulesApiClient.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Client.Api;

using HouseholdLedger.Api.Contracts;

/// <summary>Accesses recurring income schedules and materialized receipts.</summary>
public interface IPaySchedulesApiClient
{
    /// <summary>Lists configured pay schedules.</summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The configured schedules.</returns>
    Task<IReadOnlyList<PayScheduleResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Creates a pay schedule.</summary>
    /// <param name="request">The schedule values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created schedule.</returns>
    Task<PayScheduleResponse> CreateAsync(CreatePayScheduleRequest request, CancellationToken cancellationToken);

    /// <summary>Revises one schedule's future values.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="request">The replacement future values.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The revised schedule, or <see langword="null"/> when it no longer exists.</returns>
    Task<PayScheduleResponse?> ReviseAsync(Guid scheduleId, RevisePayScheduleRequest request, CancellationToken cancellationToken);

    /// <summary>Pauses one schedule.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns><see langword="true"/> when the schedule existed; otherwise, <see langword="false"/>.</returns>
    Task<bool> PauseAsync(Guid scheduleId, CancellationToken cancellationToken);

    /// <summary>Resumes one schedule without backfilling receipts.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="request">The resume date.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns><see langword="true"/> when the schedule existed; otherwise, <see langword="false"/>.</returns>
    Task<bool> ResumeAsync(Guid scheduleId, ResumePayScheduleRequest request, CancellationToken cancellationToken);

    /// <summary>Explicitly records receipts due on a date.</summary>
    /// <param name="payDate">The date to materialize.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The receipts created on that date.</returns>
    Task<IReadOnlyList<IncomeReceiptResponse>> MaterializeAsync(DateOnly payDate, CancellationToken cancellationToken);

    /// <summary>Lists receipts in an inclusive calendar range.</summary>
    /// <param name="from">The inclusive first date.</param>
    /// <param name="endDate">The inclusive final date.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The materialized receipts in the requested range.</returns>
    Task<IReadOnlyList<IncomeReceiptResponse>> ListReceiptsAsync(DateOnly from, DateOnly endDate, CancellationToken cancellationToken);
}
