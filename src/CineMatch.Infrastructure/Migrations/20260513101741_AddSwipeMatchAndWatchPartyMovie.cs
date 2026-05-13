using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineMatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSwipeMatchAndWatchPartyMovie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovieId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsWatched = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    WatchedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    WatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Users_WatchedByUserId",
                        column: x => x.WatchedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_WatchParties_WatchPartyId",
                        column: x => x.WatchPartyId,
                        principalTable: "WatchParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Swipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovieId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLiked = table.Column<bool>(type: "boolean", nullable: false),
                    SwipedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Swipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Swipes_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Swipes_PartyMembers_PartyMemberId",
                        column: x => x.PartyMemberId,
                        principalTable: "PartyMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Swipes_WatchParties_WatchPartyId",
                        column: x => x.WatchPartyId,
                        principalTable: "WatchParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchPartyMovies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WatchPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovieId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchPartyMovies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchPartyMovies_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WatchPartyMovies_WatchParties_WatchPartyId",
                        column: x => x.WatchPartyId,
                        principalTable: "WatchParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MovieId",
                table: "Matches",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WatchedByUserId",
                table: "Matches",
                column: "WatchedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WatchPartyId_MovieId",
                table: "Matches",
                columns: new[] { "WatchPartyId", "MovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_MovieId",
                table: "Swipes",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_PartyMemberId_MovieId",
                table: "Swipes",
                columns: new[] { "PartyMemberId", "MovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_WatchPartyId",
                table: "Swipes",
                column: "WatchPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchPartyMovies_MovieId",
                table: "WatchPartyMovies",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchPartyMovies_WatchPartyId_MovieId",
                table: "WatchPartyMovies",
                columns: new[] { "WatchPartyId", "MovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WatchPartyMovies_WatchPartyId_OrderIndex",
                table: "WatchPartyMovies",
                columns: new[] { "WatchPartyId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Swipes");

            migrationBuilder.DropTable(
                name: "WatchPartyMovies");
        }
    }
}
