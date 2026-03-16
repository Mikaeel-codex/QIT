using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointofSale.Migrations
{
    /// <inheritdoc />
    public partial class AddHeldReceiptsNoteStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "HeldReceipts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "HeldReceipts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "HeldReceipts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "HeldReceipts");
        }
    }
}
