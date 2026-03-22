using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PointofSale.Migrations
{
    /// <inheritdoc />
    public partial class RenameCostPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgUnitCost",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "OrderCost",
                table: "Products",
                newName: "CostPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "Products",
                newName: "OrderCost");

            migrationBuilder.AddColumn<decimal>(
                name: "AvgUnitCost",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
