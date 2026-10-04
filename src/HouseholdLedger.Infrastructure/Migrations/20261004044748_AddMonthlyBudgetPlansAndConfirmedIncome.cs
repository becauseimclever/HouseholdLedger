// <copyright file="20261004044748_AddMonthlyBudgetPlansAndConfirmedIncome.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddMonthlyBudgetPlansAndConfirmedIncome : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "schedule_id",
            table: "income_receipts",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.CreateTable(
            name: "monthly_budget_plans",
            columns: table => new
            {
                month = table.Column<DateOnly>(type: "date", nullable: false),
                expected_income = table.Column<decimal>(type: "numeric", nullable: false),
                intended_savings = table.Column<decimal>(type: "numeric", nullable: false),
                necessities = table.Column<decimal>(type: "numeric", nullable: false),
                optional = table.Column<decimal>(type: "numeric", nullable: false),
                culture = table.Column<decimal>(type: "numeric", nullable: false),
                unexpected = table.Column<decimal>(type: "numeric", nullable: false),
                last_revised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_monthly_budget_plans", x => x.month);
                table.CheckConstraint("ck_monthly_budget_plans_culture_range", "culture >= 0 AND culture <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_culture_scale", "culture = round(culture, 2)");
                table.CheckConstraint("ck_monthly_budget_plans_expected_income_range", "expected_income >= 0 AND expected_income <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_expected_income_scale", "expected_income = round(expected_income, 2)");
                table.CheckConstraint("ck_monthly_budget_plans_intended_savings_range", "intended_savings >= 0 AND intended_savings <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_intended_savings_scale", "intended_savings = round(intended_savings, 2)");
                table.CheckConstraint("ck_monthly_budget_plans_necessities_range", "necessities >= 0 AND necessities <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_necessities_scale", "necessities = round(necessities, 2)");
                table.CheckConstraint("ck_monthly_budget_plans_optional_range", "optional >= 0 AND optional <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_optional_scale", "optional = round(optional, 2)");
                table.CheckConstraint("ck_monthly_budget_plans_reconciled", "expected_income = intended_savings + necessities + optional + culture + unexpected");
                table.CheckConstraint("ck_monthly_budget_plans_unexpected_range", "unexpected >= 0 AND unexpected <= 9999999999999999.99");
                table.CheckConstraint("ck_monthly_budget_plans_unexpected_scale", "unexpected = round(unexpected, 2)");
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DO $$ BEGIN IF EXISTS (SELECT 1 FROM income_receipts WHERE schedule_id IS NULL) THEN RAISE EXCEPTION 'Cannot roll back while confirmed receipts have no schedule association.'; END IF; END $$;");

        migrationBuilder.DropTable(
            name: "monthly_budget_plans");

        migrationBuilder.AlterColumn<Guid>(
            name: "schedule_id",
            table: "income_receipts",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
