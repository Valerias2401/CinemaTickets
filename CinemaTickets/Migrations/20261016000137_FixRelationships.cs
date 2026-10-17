using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTickets.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Movies_MovieId1",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Seats_SeatId1",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Sessions_SessionId1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SeatId1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SessionId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SessionId1",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_MovieId1",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "SeatId1",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SessionId1",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MovieId1",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SessionId_SeatId",
                table: "Tickets",
                columns: new[] { "SessionId", "SeatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_SessionId_SeatId",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "SeatId1",
                table: "Tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId1",
                table: "Tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MovieId1",
                table: "Sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SeatId1",
                table: "Tickets",
                column: "SeatId1");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SessionId",
                table: "Tickets",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SessionId1",
                table: "Tickets",
                column: "SessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_MovieId1",
                table: "Sessions",
                column: "MovieId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Movies_MovieId1",
                table: "Sessions",
                column: "MovieId1",
                principalTable: "Movies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Seats_SeatId1",
                table: "Tickets",
                column: "SeatId1",
                principalTable: "Seats",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Sessions_SessionId1",
                table: "Tickets",
                column: "SessionId1",
                principalTable: "Sessions",
                principalColumn: "Id");
        }
    }
}
