// <copyright file="20260907191945_EnforceExpenseTransactionIntegrity.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class EnforceExpenseTransactionIntegrity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "amount",
            table: "expense_transactions",
            type: "numeric",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2);

        migrationBuilder.AddCheckConstraint(
            name: "ck_expense_transactions_amount_maximum",
            table: "expense_transactions",
            sql: "amount <= 9999999999999999.99");

        migrationBuilder.AddCheckConstraint(
            name: "ck_expense_transactions_amount_positive",
            table: "expense_transactions",
            sql: "amount > 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_expense_transactions_amount_scale",
            table: "expense_transactions",
            sql: "amount = round(amount, 2)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_expense_transactions_classification",
            table: "expense_transactions",
            sql: "classification IN ('Necessities', 'Optional', 'Culture', 'Unexpected')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_expense_transactions_amount_maximum",
            table: "expense_transactions");

        migrationBuilder.DropCheckConstraint(
            name: "ck_expense_transactions_amount_positive",
            table: "expense_transactions");

        migrationBuilder.DropCheckConstraint(
            name: "ck_expense_transactions_amount_scale",
            table: "expense_transactions");

        migrationBuilder.DropCheckConstraint(
            name: "ck_expense_transactions_classification",
            table: "expense_transactions");

        migrationBuilder.AlterColumn<decimal>(
            name: "amount",
            table: "expense_transactions",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric");
    }
}
