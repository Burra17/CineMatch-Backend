using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStartedToWatchParty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarted",
                table: "WatchParties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "WatchParties",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStarted",
                table: "WatchParties");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "WatchParties");
        }
    }
}
