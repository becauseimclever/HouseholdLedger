// <copyright file="20260906151001_AddWorkbenchTheme.cs" company="HouseholdLedger">
// Copyright (c) HouseholdLedger. All rights reserved.
// </copyright>

namespace HouseholdLedger.Infrastructure.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

/// <inheritdoc />
public partial class AddWorkbenchTheme : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "theme",
            table: "global_settings",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "workbench-dark");

        migrationBuilder.AddCheckConstraint(
            name: "ck_global_settings_theme",
            table: "global_settings",
            sql: "theme IN ('workbench-dark', 'workbench-light')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_global_settings_theme",
            table: "global_settings");

        migrationBuilder.DropColumn(
            name: "theme",
            table: "global_settings");
    }
}
