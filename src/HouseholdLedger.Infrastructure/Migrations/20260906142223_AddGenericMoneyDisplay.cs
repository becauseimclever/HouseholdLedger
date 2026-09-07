// <copyright file="20260906142223_AddGenericMoneyDisplay.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddGenericMoneyDisplay : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_global_settings_display_currency",
            table: "global_settings");

        migrationBuilder.AddCheckConstraint(
            name: "ck_global_settings_display_currency",
            table: "global_settings",
            sql: "display_currency IN ('USD', 'CAD', 'EUR', 'GBP', 'AUD', 'XXX')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_global_settings_display_currency",
            table: "global_settings");

        migrationBuilder.Sql(
            "UPDATE global_settings SET display_currency = 'USD' WHERE display_currency = 'XXX'");

        migrationBuilder.AddCheckConstraint(
            name: "ck_global_settings_display_currency",
            table: "global_settings",
            sql: "display_currency IN ('USD', 'CAD', 'EUR', 'GBP', 'AUD')");
    }
}
