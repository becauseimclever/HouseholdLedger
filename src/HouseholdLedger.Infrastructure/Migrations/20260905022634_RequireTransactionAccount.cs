// <copyright file="20260905022634_RequireTransactionAccount.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

/// <inheritdoc />
public partial class RequireTransactionAccount : Migration
{
    private static readonly string[] DateSequenceColumns = ["ledger_date", "creation_sequence"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "expense_transactions");

        migrationBuilder.CreateTable(
            name: "expense_transactions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                ledger_date = table.Column<DateOnly>(type: "date", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                creation_sequence = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expense_transactions", x => x.id);
                table.ForeignKey(
                    name: "FK_expense_transactions_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_expense_transactions_account_id",
            table: "expense_transactions",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "IX_expense_transactions_creation_sequence",
            table: "expense_transactions",
            column: "creation_sequence",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_expense_transactions_ledger_date_creation_sequence",
            table: "expense_transactions",
            columns: DateSequenceColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "expense_transactions");

        migrationBuilder.CreateTable(
            name: "expense_transactions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                ledger_date = table.Column<DateOnly>(type: "date", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                creation_sequence = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_expense_transactions", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_expense_transactions_creation_sequence",
            table: "expense_transactions",
            column: "creation_sequence",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_expense_transactions_ledger_date_creation_sequence",
            table: "expense_transactions",
            columns: DateSequenceColumns);
    }
}
