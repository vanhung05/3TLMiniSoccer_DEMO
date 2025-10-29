using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TLMiniSoccer.Migrations
{
    /// <inheritdoc />
    public partial class RemovePricePerHourFromFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "Fields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Fields",
                type: "decimal(10,2)",
                nullable: true);
        }
    }
}
