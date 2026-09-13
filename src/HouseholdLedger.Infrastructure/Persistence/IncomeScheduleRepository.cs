// <copyright file="IncomeScheduleRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Income;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Persists income schedules and materialized receipts through Entity Framework Core.</summary>
public sealed class IncomeScheduleRepository(HouseholdLedgerDbContext dbContext) : IIncomeScheduleRepository
{
    private const string ReceiptSchedulePayDateConstraint = "ux_income_receipts_schedule_id_pay_date";

    /// <inheritdoc/>
    public async Task AddScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        dbContext.PayScheduleRecords.Add(Map(schedule));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PaySchedule?> FindScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var record = await dbContext.PayScheduleRecords
            .AsNoTracking()
            .Include(item => item.Allocations)
            .SingleOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);

        return record is null ? null : Rehydrate(record);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PaySchedule>> ListSchedulesAsync(CancellationToken cancellationToken)
    {
        var records = await dbContext.PayScheduleRecords
            .AsNoTracking()
            .Include(item => item.Allocations)
            .OrderBy(item => item.Id)
            .ToArrayAsync(cancellationToken);

        return records.Select(Rehydrate).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IncomeReceipt>> ListReceiptsAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.IncomeReceiptRecords
            .AsNoTracking()
            .Include(item => item.Allocations)
            .Where(item => item.PayDate >= startDate && item.PayDate <= endDate)
            .OrderBy(item => item.PayDate)
            .ThenBy(item => item.ScheduleId)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);

        return records.Select(Rehydrate).ToArray();
    }

    /// <inheritdoc/>
    public async Task UpdateScheduleAsync(PaySchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var record = await dbContext.PayScheduleRecords
            .Include(item => item.Allocations)
            .SingleAsync(item => item.Id == schedule.Id, cancellationToken);

        record.Name = schedule.Name;
        record.FirstPayDate = schedule.FirstPayDate;
        record.Cadence = schedule.Cadence;
        record.NetIncome = schedule.NetIncome;
        record.SecondMonthlyPayDay = schedule.SecondMonthlyPayDay;
        record.IsPaused = schedule.IsPaused;
        record.ReceiptEligibleFrom = schedule.ReceiptEligibleFrom;
        record.Allocations.Clear();
        record.Allocations.AddRange(schedule.Allocations.Select(allocation => new IncomeScheduleAllocationRecord
        {
            ScheduleId = schedule.Id,
            AccountId = allocation.AccountId,
            Amount = allocation.Amount,
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IncomeReceipt?> GetOrAddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        var record = Map(receipt);
        dbContext.IncomeReceiptRecords.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return receipt;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ReceiptSchedulePayDateConstraint,
            })
        {
            dbContext.ChangeTracker.Clear();
            return null;
        }
    }

    private static PayScheduleRecord Map(PaySchedule schedule)
    {
        var record = new PayScheduleRecord
        {
            Id = schedule.Id,
            Name = schedule.Name,
            FirstPayDate = schedule.FirstPayDate,
            Cadence = schedule.Cadence,
            NetIncome = schedule.NetIncome,
            SecondMonthlyPayDay = schedule.SecondMonthlyPayDay,
            IsPaused = schedule.IsPaused,
            ReceiptEligibleFrom = schedule.ReceiptEligibleFrom,
        };
        record.Allocations.AddRange(schedule.Allocations.Select(allocation => new IncomeScheduleAllocationRecord
        {
            ScheduleId = schedule.Id,
            AccountId = allocation.AccountId,
            Amount = allocation.Amount,
        }));
        return record;
    }

    private static IncomeReceiptRecord Map(IncomeReceipt receipt)
    {
        var record = new IncomeReceiptRecord
        {
            Id = receipt.Id,
            ScheduleId = receipt.ScheduleId,
            PayDate = receipt.PayDate,
            NetIncome = receipt.NetIncome,
        };
        record.Allocations.AddRange(receipt.Allocations.Select(allocation => new IncomeReceiptAllocationRecord
        {
            ReceiptId = receipt.Id,
            AccountId = allocation.AccountId,
            Amount = allocation.Amount,
        }));
        return record;
    }

    private static PaySchedule Rehydrate(PayScheduleRecord record)
    {
        var schedule = new PaySchedule(
            record.Id,
            record.Name,
            record.FirstPayDate,
            record.Cadence,
            record.NetIncome,
            record.Allocations.Select(allocation => new IncomeAccountAllocation(allocation.AccountId, allocation.Amount)),
            record.SecondMonthlyPayDay);
        schedule.Resume(record.ReceiptEligibleFrom);
        if (record.IsPaused)
        {
            schedule.Pause();
        }

        return schedule;
    }

    private static IncomeReceipt Rehydrate(IncomeReceiptRecord record) => new(
        record.Id,
        record.ScheduleId,
        record.PayDate,
        record.NetIncome,
        record.Allocations.Select(allocation => new IncomeAccountAllocation(allocation.AccountId, allocation.Amount)));
}
