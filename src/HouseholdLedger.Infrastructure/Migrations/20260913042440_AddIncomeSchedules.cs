// <copyright file="20260913042440_AddIncomeSchedules.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddIncomeSchedules : Migration
{
    private static readonly string[] IncomeReceiptAllocationColumns = ["receipt_id", "account_id"];

    private static readonly string[] IncomeReceiptColumns = ["schedule_id", "pay_date"];

    private static readonly string[] IncomeScheduleAllocationColumns = ["schedule_id", "account_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.CreateTable(
                name: "pay_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    first_pay_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cadence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    net_income = table.Column<decimal>(type: "numeric", nullable: false),
                    second_monthly_pay_day = table.Column<int>(type: "integer", nullable: true),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    receipt_eligible_from = table.Column<DateOnly>(type: "date", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pay_schedules", x => x.id);
                    table.CheckConstraint("ck_pay_schedules_cadence", "cadence IN ('Weekly', 'Biweekly', 'Semimonthly', 'FourWeekly', 'Monthly')");
                    table.CheckConstraint("ck_pay_schedules_net_income_maximum", "net_income <= 9999999999999999.99");
                    table.CheckConstraint("ck_pay_schedules_net_income_positive", "net_income > 0");
                    table.CheckConstraint("ck_pay_schedules_net_income_scale", "net_income = round(net_income, 2)");
                });

            migrationBuilder.CreateTable(
                name: "income_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pay_date = table.Column<DateOnly>(type: "date", nullable: false),
                    net_income = table.Column<decimal>(type: "numeric", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_income_receipts", x => x.id);
                    table.CheckConstraint("ck_income_receipts_net_income_maximum", "net_income <= 9999999999999999.99");
                    table.CheckConstraint("ck_income_receipts_net_income_positive", "net_income > 0");
                    table.CheckConstraint("ck_income_receipts_net_income_scale", "net_income = round(net_income, 2)");
                    table.ForeignKey(
                        name: "FK_income_receipts_pay_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "pay_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "income_schedule_allocations",
                columns: table => new
                {
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_income_schedule_allocations", x => new { x.schedule_id, x.account_id });
                    table.CheckConstraint("ck_income_schedule_allocations_amount_maximum", "amount <= 9999999999999999.99");
                    table.CheckConstraint("ck_income_schedule_allocations_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_income_schedule_allocations_amount_scale", "amount = round(amount, 2)");
                    table.ForeignKey(
                        name: "FK_income_schedule_allocations_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_income_schedule_allocations_pay_schedules_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "pay_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "income_receipt_allocations",
                columns: table => new
                {
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_income_receipt_allocations", x => new { x.receipt_id, x.account_id });
                    table.CheckConstraint("ck_income_receipt_allocations_amount_maximum", "amount <= 9999999999999999.99");
                    table.CheckConstraint("ck_income_receipt_allocations_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_income_receipt_allocations_amount_scale", "amount = round(amount, 2)");
                    table.ForeignKey(
                        name: "FK_income_receipt_allocations_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_income_receipt_allocations_income_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "income_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_income_receipt_allocations_account_id",
                table: "income_receipt_allocations",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ux_income_receipt_allocations_receipt_id_account_id",
                table: "income_receipt_allocations",
                columns: IncomeReceiptAllocationColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_income_receipts_schedule_id_pay_date",
                table: "income_receipts",
                columns: IncomeReceiptColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_income_schedule_allocations_account_id",
                table: "income_schedule_allocations",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ux_income_schedule_allocations_schedule_id_account_id",
                table: "income_schedule_allocations",
                columns: IncomeScheduleAllocationColumns,
                unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropTable(
                name: "income_receipt_allocations");

            migrationBuilder.DropTable(
                name: "income_schedule_allocations");

            migrationBuilder.DropTable(
                name: "income_receipts");

            migrationBuilder.DropTable(
                name: "pay_schedules");
    }
}
