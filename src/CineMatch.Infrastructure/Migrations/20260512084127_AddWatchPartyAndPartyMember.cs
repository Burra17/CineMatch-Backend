using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchPartyAndPartyMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchParties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Genre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchParties_Users_HostId",
                        column: x => x.HostId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyMembers_WatchParties_WatchPartyId",
                        column: x => x.WatchPartyId,
                        principalTable: "WatchParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartyMembers_UserId_WatchPartyId",
                table: "PartyMembers",
                columns: new[] { "UserId", "WatchPartyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyMembers_WatchPartyId",
                table: "PartyMembers",
                column: "WatchPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchParties_HostId",
                table: "WatchParties",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchParties_JoinCode",
                table: "WatchParties",
                column: "JoinCode",
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyMembers");

            migrationBuilder.DropTable(
                name: "WatchParties");
        }
    }
}
