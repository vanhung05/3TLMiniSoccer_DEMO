using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3TLMiniSoccer.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionEnd",
                table: "BookingSessions");

            migrationBuilder.DropColumn(
                name: "SessionStart",
                table: "BookingSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInTime",
                table: "BookingSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "BookingSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "BookingSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "BookingSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionOrders",
                columns: table => new
                {
                    SessionOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOrders", x => x.SessionOrderId);
                    table.ForeignKey(
                        name: "FK_SessionOrders_BookingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BookingSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionOrders_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateTable(
                name: "SessionOrderItems",
                columns: table => new
                {
                    SessionOrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOrderItems", x => x.SessionOrderItemId);
                    table.ForeignKey(
                        name: "FK_SessionOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionOrderItems_SessionOrders_SessionOrderId",
                        column: x => x.SessionOrderId,
                        principalTable: "SessionOrders",
                        principalColumn: "SessionOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSessions_StaffId",
                table: "BookingSessions",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOrderItems_ProductId",
                table: "SessionOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOrderItems_SessionOrderId",
                table: "SessionOrderItems",
                column: "SessionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOrders_OrderId",
                table: "SessionOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionOrders_SessionId",
                table: "SessionOrders",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSessions_Users_StaffId",
                table: "BookingSessions",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingSessions_Users_StaffId",
                table: "BookingSessions");

            migrationBuilder.DropTable(
                name: "SessionOrderItems");

            migrationBuilder.DropTable(
                name: "SessionOrders");

            migrationBuilder.DropIndex(
                name: "IX_BookingSessions_StaffId",
                table: "BookingSessions");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "BookingSessions");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "BookingSessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "BookingSessions");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "BookingSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionEnd",
                table: "BookingSessions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStart",
                table: "BookingSessions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
