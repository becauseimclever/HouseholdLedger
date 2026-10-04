// <copyright file="20261004142908_AddLedgerCorrectionsAndMonthlyReflections.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc/>
public partial class AddLedgerCorrectionsAndMonthlyReflections : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "expense_transactions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "income_receipt_requests",
            columns: table => new
            {
                request_id = table.Column<Guid>(type: "uuid", nullable: false),
                receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                payload = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_income_receipt_requests", x => x.request_id);
            });

        migrationBuilder.CreateTable(
            name: "monthly_reflections",
            columns: table => new
            {
                month = table.Column<DateOnly>(type: "date", nullable: false),
                what_worked = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                next_month_intention = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                last_revised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_monthly_reflections", x => x.month);
            });
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "income_receipt_requests");

        migrationBuilder.DropTable(
            name: "monthly_reflections");

        migrationBuilder.DropColumn(
            name: "description",
            table: "expense_transactions");
    }
}
