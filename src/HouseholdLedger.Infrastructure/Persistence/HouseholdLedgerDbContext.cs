// <copyright file="HouseholdLedgerDbContext.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Persistence;

using HouseholdLedger.Domain.Accounts;
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
        transaction.ToTable("expense_transactions");
        transaction.HasKey(item => item.Id);
        transaction.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        transaction.Property(item => item.AccountId).HasColumnName("account_id");
        transaction.Property(item => item.Date).HasColumnName("ledger_date").HasColumnType("date");
        transaction.Property(item => item.Amount).HasColumnName("amount").HasPrecision(18, 2);
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
    }
}
