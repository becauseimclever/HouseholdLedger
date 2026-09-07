// <copyright file="20260906132640_AddGlobalSettings.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddGlobalSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "global_settings",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                display_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_global_settings", x => x.id);
                table.CheckConstraint("ck_global_settings_display_currency", "display_currency IN ('USD', 'CAD', 'EUR', 'GBP', 'AUD')");
                table.CheckConstraint("ck_global_settings_singleton", "id = 1");
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "global_settings");
    }
}
