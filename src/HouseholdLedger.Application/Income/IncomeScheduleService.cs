// <copyright file="IncomeScheduleService.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Application.Income;

using HouseholdLedger.Application.Accounts;
using HouseholdLedger.Domain.Income;

/// <summary>Manages recurring income schedules and materializes due income receipts.</summary>
public sealed class IncomeScheduleService(IIncomeScheduleRepository repository, IAccountRepository accountRepository)
{
    /// <summary>Creates a recurring income schedule after verifying every allocation destination.</summary>
    /// <param name="command">The proposed schedule values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted schedule.</returns>
    public async Task<IncomeScheduleDto> CreateAsync(IncomeScheduleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var schedule = await this.CreateValidatedScheduleAsync(Guid.NewGuid(), command, cancellationToken);
        await repository.AddScheduleAsync(schedule, cancellationToken);
        return Map(schedule);
    }

    /// <summary>Gets one recurring income schedule by identifier.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching schedule, or <see langword="null"/> when it does not exist.</returns>
    public async Task<IncomeScheduleDto?> GetAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.FindScheduleAsync(scheduleId, cancellationToken);
        return schedule is null ? null : Map(schedule);
    }

    /// <summary>Lists recurring income schedules.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The schedules.</returns>
    public async Task<IReadOnlyList<IncomeScheduleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var schedules = await repository.ListSchedulesAsync(cancellationToken);
        return schedules.Select(Map).ToArray();
    }

    /// <summary>Lists materialized income receipts in an inclusive calendar range.</summary>
    /// <param name="startDate">The inclusive first pay date.</param>
    /// <param name="endDate">The inclusive final pay date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The receipt snapshots in the requested range.</returns>
    public async Task<IReadOnlyList<IncomeReceiptDto>> ListReceiptsAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("The end date must not precede the start date.", nameof(endDate));
        }

        var receipts = await repository.ListReceiptsAsync(startDate, endDate, cancellationToken);
        return receipts.Select(Map).ToArray();
    }

    /// <summary>Revises a schedule's future occurrences without changing existing receipts.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="command">The replacement future schedule values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The revised schedule, or <see langword="null"/> when it does not exist.</returns>
    public async Task<IncomeScheduleDto?> ReviseAsync(Guid scheduleId, IncomeScheduleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var schedule = await repository.FindScheduleAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return null;
        }

        var allocations = await this.ValidateAllocationsAsync(command.Allocations, cancellationToken);
        schedule.Revise(command.Name, command.FirstPayDate, command.Cadence, command.NetIncome, allocations, command.SecondMonthlyPayDay);
        await repository.UpdateScheduleAsync(schedule, cancellationToken);
        return Map(schedule);
    }

    /// <summary>Pauses one schedule so it creates no new receipts.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the schedule exists.</returns>
    public async Task<bool> PauseAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.FindScheduleAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return false;
        }

        schedule.Pause();
        await repository.UpdateScheduleAsync(schedule, cancellationToken);
        return true;
    }

    /// <summary>Resumes one schedule from the supplied date without backfilling missed receipts.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="resumeDate">The date the schedule became active.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the schedule exists.</returns>
    public async Task<bool> ResumeAsync(Guid scheduleId, DateOnly resumeDate, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.FindScheduleAsync(scheduleId, cancellationToken);
        if (schedule is null)
        {
            return false;
        }

        schedule.Resume(resumeDate);
        await repository.UpdateScheduleAsync(schedule, cancellationToken);
        return true;
    }

    /// <summary>Determines a schedule's due dates in an inclusive calendar range.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="startDate">The inclusive first date.</param>
    /// <param name="endDate">The inclusive final date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The due dates, or an empty list when the schedule does not exist.</returns>
    public async Task<IReadOnlyList<DateOnly>> GetDuePayDatesAsync(Guid scheduleId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var schedule = await repository.FindScheduleAsync(scheduleId, cancellationToken);
        return schedule is null ? [] : PayScheduleCalendar.GetDuePayDates(schedule, startDate, endDate);
    }

    /// <summary>Materializes every active schedule that is due on the supplied pay date.</summary>
    /// <param name="payDate">The pay date to materialize.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The receipts newly materialized for the date.</returns>
    public async Task<IReadOnlyList<IncomeReceipt>> MaterializeDueReceiptsAsync(DateOnly payDate, CancellationToken cancellationToken = default)
    {
        var receipts = new List<IncomeReceipt>();
        var schedules = await repository.ListSchedulesAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            if (schedule.IsPaused || payDate < schedule.ReceiptEligibleFrom || PayScheduleCalendar.GetDuePayDates(schedule, payDate, payDate).Count == 0)
            {
                continue;
            }

            var receipt = new IncomeReceipt(Guid.NewGuid(), schedule.Id, payDate, schedule.NetIncome, schedule.Allocations);
            var storedReceipt = await repository.GetOrAddReceiptAsync(receipt, cancellationToken);
            if (storedReceipt is not null)
            {
                receipts.Add(storedReceipt);
            }
        }

        return receipts;
    }

    private static IncomeScheduleDto Map(PaySchedule schedule) => new(
        schedule.Id,
        schedule.Name,
        schedule.FirstPayDate,
        schedule.Cadence,
        schedule.NetIncome,
        schedule.Allocations.Select(allocation => new IncomeAllocationCommand(allocation.AccountId, allocation.Amount)).ToArray(),
        schedule.SecondMonthlyPayDay,
        schedule.IsPaused);

    private static IncomeReceiptDto Map(IncomeReceipt receipt) => new(
        receipt.Id,
        receipt.ScheduleId,
        receipt.PayDate,
        receipt.NetIncome,
        receipt.Allocations.Select(allocation => new IncomeAllocationDto(allocation.AccountId, allocation.Amount)).ToArray());

    private async Task<PaySchedule> CreateValidatedScheduleAsync(Guid id, IncomeScheduleCommand command, CancellationToken cancellationToken)
    {
        var allocations = await this.ValidateAllocationsAsync(command.Allocations, cancellationToken);
        return new PaySchedule(id, command.Name, command.FirstPayDate, command.Cadence, command.NetIncome, allocations, command.SecondMonthlyPayDay);
    }

    private async Task<IReadOnlyList<IncomeAccountAllocation>> ValidateAllocationsAsync(IReadOnlyList<IncomeAllocationCommand> commands, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var allocations = new List<IncomeAccountAllocation>(commands.Count);
        foreach (var command in commands)
        {
            if (command.AccountId == Guid.Empty || await accountRepository.FindAsync(command.AccountId, cancellationToken) is null)
            {
                throw new ArgumentException("Choose existing allocation accounts.", nameof(commands));
            }

            allocations.Add(new IncomeAccountAllocation(command.AccountId, command.Amount));
        }

        return allocations;
    }
}
