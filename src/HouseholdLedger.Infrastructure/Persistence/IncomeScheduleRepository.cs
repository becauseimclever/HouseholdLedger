// <copyright file="IncomeScheduleRepository.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using System.Globalization;
using System.Text.Json;
using HouseholdLedger.Application.Income;
using HouseholdLedger.Domain.Income;
using Microsoft.EntityFrameworkCore;
using Npgsql;

/// <summary>Persists income expectations and user-confirmed receipts through Entity Framework Core.</summary>
public sealed class IncomeScheduleRepository(HouseholdLedgerDbContext dbContext) : IIncomeScheduleRepository
{
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
    public async Task<IncomeReceipt?> FindReceiptAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var record = await dbContext.IncomeReceiptRecords
            .AsNoTracking()
            .Include(item => item.Allocations)
            .SingleOrDefaultAsync(item => item.Id == receiptId, cancellationToken);
        return record is null ? null : Rehydrate(record);
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
    public async Task AddReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        dbContext.IncomeReceiptRecords.Add(Map(receipt));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IncomeReceipt> ConfirmReceiptAsync(
        IncomeReceipt receipt,
        Guid? requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        if (requestId is null)
        {
            await this.AddReceiptAsync(receipt, cancellationToken);
            return receipt;
        }

        var payload = SerializePayload(receipt);
        var existing = await dbContext.IncomeReceiptRequestRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.RequestId == requestId, cancellationToken);
        if (existing is not null)
        {
            return Replay(existing, payload);
        }

        var request = new IncomeReceiptRequestRecord
        {
            RequestId = requestId.Value,
            ReceiptId = receipt.Id,
            Payload = payload,
        };
        var record = Map(receipt);
        dbContext.IncomeReceiptRequestRecords.Add(request);
        dbContext.IncomeReceiptRecords.Add(record);
        try
        {
            // SaveChanges atomically commits both the token and receipt, including allocations.
            await dbContext.SaveChangesAsync(cancellationToken);
            return receipt;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "pk_income_receipt_requests",
            })
        {
            // The losing transaction was rolled back; discard only its pending insert graph.
            dbContext.Entry(request).State = EntityState.Detached;
            foreach (var allocation in record.Allocations.ToArray())
            {
                dbContext.Entry(allocation).State = EntityState.Detached;
            }

            dbContext.Entry(record).State = EntityState.Detached;
            var winner = await dbContext.IncomeReceiptRequestRecords.AsNoTracking()
                .SingleAsync(item => item.RequestId == requestId, cancellationToken);
            return Replay(winner, payload);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateReceiptAsync(IncomeReceipt receipt, CancellationToken cancellationToken)
    {
        var record = await dbContext.IncomeReceiptRecords.Include(item => item.Allocations)
            .SingleOrDefaultAsync(item => item.Id == receipt.Id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        record.PayDate = receipt.PayDate;
        record.NetIncome = receipt.NetIncome;
        var replacements = receipt.Allocations.ToDictionary(item => item.AccountId);
        foreach (var allocation in record.Allocations.ToArray())
        {
            if (replacements.Remove(allocation.AccountId, out var replacement))
            {
                allocation.Amount = replacement.Amount;
            }
            else
            {
                dbContext.Remove(allocation);
                record.Allocations.Remove(allocation);
            }
        }

        record.Allocations.AddRange(replacements.Values.Select(item => new IncomeReceiptAllocationRecord
        {
            ReceiptId = receipt.Id,
            AccountId = item.AccountId,
            Amount = item.Amount,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteReceiptAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var record = await dbContext.IncomeReceiptRecords.Include(item => item.Allocations)
            .SingleOrDefaultAsync(item => item.Id == receiptId, cancellationToken);
        if (record is null)
        {
            return false;
        }

        dbContext.RemoveRange(record.Allocations);
        dbContext.IncomeReceiptRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string SerializePayload(IncomeReceipt receipt) => JsonSerializer.Serialize(new IncomeReceiptCommand(
        receipt.PayDate,
        NormalizeAmount(receipt.NetIncome),
        receipt.Allocations.OrderBy(item => item.AccountId)
            .Select(item => new IncomeAllocationCommand(item.AccountId, NormalizeAmount(item.Amount))).ToArray()));

    private static decimal NormalizeAmount(decimal amount) =>
        decimal.Parse(amount.ToString("0.00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static IncomeReceipt Replay(IncomeReceiptRequestRecord record, string payload)
    {
        if (!string.Equals(record.Payload, payload, StringComparison.Ordinal))
        {
            throw new IncomeReceiptRequestConflictException();
        }

        var original = JsonSerializer.Deserialize<IncomeReceiptCommand>(record.Payload)!;
        return new IncomeReceipt(
            record.ReceiptId,
            null,
            original.ReceivedDate,
            original.Amount,
            original.Allocations.Select(item => new IncomeAccountAllocation(item.AccountId, item.Amount)));
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
