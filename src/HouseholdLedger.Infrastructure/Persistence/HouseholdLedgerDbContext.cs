// <copyright file="HouseholdLedgerDbContext.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Domain.Accounts;
using HouseholdLedger.Domain.Income;
using HouseholdLedger.Domain.Settings;
using HouseholdLedger.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Represents the HouseholdLedger PostgreSQL persistence boundary.
/// </summary>
/// <param name="options">The context configuration supplied by the composition root.</param>
public sealed class HouseholdLedgerDbContext(DbContextOptions<HouseholdLedgerDbContext> options)
    : DbContext(options)
{
    /// <summary>Gets the accounts.</summary>
    public DbSet<Account> Accounts => this.Set<Account>();

    /// <summary>Gets the expense transactions.</summary>
    public DbSet<ExpenseTransaction> ExpenseTransactions => this.Set<ExpenseTransaction>();

    /// <summary>Gets the singleton global settings.</summary>
    public DbSet<GlobalSettings> GlobalSettings => this.Set<GlobalSettings>();

    internal DbSet<PayScheduleRecord> PayScheduleRecords => this.Set<PayScheduleRecord>();

    internal DbSet<IncomeReceiptRecord> IncomeReceiptRecords => this.Set<IncomeReceiptRecord>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var account = modelBuilder.Entity<Account>();
        account.ToTable("accounts");
        account.HasKey(item => item.Id);
        account.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        account.Property(item => item.Name).HasColumnName("name").HasMaxLength(100);
        account.Property(item => item.NormalizedName).HasColumnName("normalized_name").HasMaxLength(100);
        account.HasIndex(item => item.NormalizedName).IsUnique().HasDatabaseName("ux_accounts_normalized_name");

        var transaction = modelBuilder.Entity<ExpenseTransaction>();
        transaction.ToTable(
            "expense_transactions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_expense_transactions_amount_positive", "amount > 0");
                tableBuilder.HasCheckConstraint(
                    "ck_expense_transactions_amount_maximum",
                    $"amount <= {ExpenseTransaction.MaximumAmount}");
                tableBuilder.HasCheckConstraint(
                    "ck_expense_transactions_amount_scale",
                    "amount = round(amount, 2)");
                tableBuilder.HasCheckConstraint(
                    "ck_expense_transactions_classification",
                    "classification IN ('Necessities', 'Optional', 'Culture', 'Unexpected')");
            });
        transaction.HasKey(item => item.Id);
        transaction.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        transaction.Property(item => item.AccountId).HasColumnName("account_id");
        transaction.Property(item => item.Date).HasColumnName("ledger_date").HasColumnType("date");
        transaction.Property(item => item.Amount).HasColumnName("amount").HasColumnType("numeric");
        transaction.Property(item => item.Classification).HasColumnName("classification").HasConversion<string>().HasMaxLength(32);
        transaction.Property(item => item.Sequence).HasColumnName("creation_sequence").ValueGeneratedOnAdd();
        transaction
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(item => item.AccountId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        transaction.HasIndex(item => item.Sequence).IsUnique();
        transaction.HasIndex(item => new { item.Date, item.Sequence });

        var settings = modelBuilder.Entity<GlobalSettings>();
        settings.ToTable(
            "global_settings",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_global_settings_singleton", "id = 1");
                tableBuilder.HasCheckConstraint(
                    "ck_global_settings_display_currency",
                    "display_currency IN ('USD', 'CAD', 'EUR', 'GBP', 'AUD', 'XXX')");
                tableBuilder.HasCheckConstraint(
                    "ck_global_settings_theme",
                    "theme IN ('workbench-dark', 'workbench-light')");
            });
        settings.HasKey(item => item.Id);
        settings.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        settings.Property(item => item.DisplayCurrency)
            .HasColumnName("display_currency")
            .HasConversion<string>()
            .HasMaxLength(3);
        settings.Property(item => item.Theme)
            .HasColumnName("theme")
            .HasConversion(
                theme => WorkbenchThemeCode.ToCode(theme),
                value => WorkbenchThemeCode.Parse(value))
            .HasMaxLength(32);

        var paySchedule = modelBuilder.Entity<PayScheduleRecord>();
        paySchedule.ToTable(
            "pay_schedules",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_pay_schedules_net_income_positive", "net_income > 0");
                tableBuilder.HasCheckConstraint("ck_pay_schedules_net_income_maximum", $"net_income <= {IncomeAmount.Maximum}");
                tableBuilder.HasCheckConstraint("ck_pay_schedules_net_income_scale", "net_income = round(net_income, 2)");
                tableBuilder.HasCheckConstraint("ck_pay_schedules_cadence", "cadence IN ('Weekly', 'Biweekly', 'Semimonthly', 'FourWeekly', 'Monthly')");
            });
        paySchedule.HasKey(item => item.Id);
        paySchedule.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        paySchedule.Property(item => item.Name).HasColumnName("name").HasMaxLength(100);
        paySchedule.Property(item => item.FirstPayDate).HasColumnName("first_pay_date").HasColumnType("date");
        paySchedule.Property(item => item.Cadence).HasColumnName("cadence").HasConversion<string>().HasMaxLength(32);
        paySchedule.Property(item => item.NetIncome).HasColumnName("net_income").HasColumnType("numeric");
        paySchedule.Property(item => item.SecondMonthlyPayDay).HasColumnName("second_monthly_pay_day");
        paySchedule.Property(item => item.IsPaused).HasColumnName("is_paused");
        paySchedule.Property(item => item.ReceiptEligibleFrom).HasColumnName("receipt_eligible_from").HasColumnType("date");

        var scheduleAllocation = modelBuilder.Entity<IncomeScheduleAllocationRecord>();
        scheduleAllocation.ToTable(
            "income_schedule_allocations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_income_schedule_allocations_amount_positive", "amount > 0");
                tableBuilder.HasCheckConstraint("ck_income_schedule_allocations_amount_maximum", $"amount <= {IncomeAmount.Maximum}");
                tableBuilder.HasCheckConstraint("ck_income_schedule_allocations_amount_scale", "amount = round(amount, 2)");
            });
        scheduleAllocation.HasKey(item => new { item.ScheduleId, item.AccountId });
        scheduleAllocation.Property(item => item.ScheduleId).HasColumnName("schedule_id");
        scheduleAllocation.Property(item => item.AccountId).HasColumnName("account_id");
        scheduleAllocation.Property(item => item.Amount).HasColumnName("amount").HasColumnType("numeric");
        scheduleAllocation.HasOne<PayScheduleRecord>().WithMany(item => item.Allocations).HasForeignKey(item => item.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        scheduleAllocation.HasOne<Account>().WithMany().HasForeignKey(item => item.AccountId).OnDelete(DeleteBehavior.Restrict);
        scheduleAllocation.HasIndex(item => new { item.ScheduleId, item.AccountId }).IsUnique().HasDatabaseName("ux_income_schedule_allocations_schedule_id_account_id");

        var receipt = modelBuilder.Entity<IncomeReceiptRecord>();
        receipt.ToTable(
            "income_receipts",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_income_receipts_net_income_positive", "net_income > 0");
                tableBuilder.HasCheckConstraint("ck_income_receipts_net_income_maximum", $"net_income <= {IncomeAmount.Maximum}");
                tableBuilder.HasCheckConstraint("ck_income_receipts_net_income_scale", "net_income = round(net_income, 2)");
            });
        receipt.HasKey(item => item.Id);
        receipt.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        receipt.Property(item => item.ScheduleId).HasColumnName("schedule_id");
        receipt.Property(item => item.PayDate).HasColumnName("pay_date").HasColumnType("date");
        receipt.Property(item => item.NetIncome).HasColumnName("net_income").HasColumnType("numeric");
        receipt.HasOne<PayScheduleRecord>().WithMany(item => item.Receipts).HasForeignKey(item => item.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        receipt.HasIndex(item => new { item.ScheduleId, item.PayDate }).IsUnique().HasDatabaseName("ux_income_receipts_schedule_id_pay_date");

        var receiptAllocation = modelBuilder.Entity<IncomeReceiptAllocationRecord>();
        receiptAllocation.ToTable(
            "income_receipt_allocations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint("ck_income_receipt_allocations_amount_positive", "amount > 0");
                tableBuilder.HasCheckConstraint("ck_income_receipt_allocations_amount_maximum", $"amount <= {IncomeAmount.Maximum}");
                tableBuilder.HasCheckConstraint("ck_income_receipt_allocations_amount_scale", "amount = round(amount, 2)");
            });
        receiptAllocation.HasKey(item => new { item.ReceiptId, item.AccountId });
        receiptAllocation.Property(item => item.ReceiptId).HasColumnName("receipt_id");
        receiptAllocation.Property(item => item.AccountId).HasColumnName("account_id");
        receiptAllocation.Property(item => item.Amount).HasColumnName("amount").HasColumnType("numeric");
        receiptAllocation.HasOne<IncomeReceiptRecord>().WithMany(item => item.Allocations).HasForeignKey(item => item.ReceiptId).OnDelete(DeleteBehavior.Restrict);
        receiptAllocation.HasOne<Account>().WithMany().HasForeignKey(item => item.AccountId).OnDelete(DeleteBehavior.Restrict);
        receiptAllocation.HasIndex(item => new { item.ReceiptId, item.AccountId }).IsUnique().HasDatabaseName("ux_income_receipt_allocations_receipt_id_account_id");
    }
}
