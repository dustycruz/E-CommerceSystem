using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerceSystem.InventoryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Consumer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "StockItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    QuantityReserved = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItems", x => x.Id);
                    table.CheckConstraint("CK_StockItems_Quantities", "[QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand]");
                });

            migrationBuilder.CreateTable(
                name: "StockReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservations", x => x.Id);
                    table.CheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0");
                });

            migrationBuilder.InsertData(
                table: "StockItems",
                columns: new[] { "Id", "ProductId", "QuantityOnHand", "QuantityReserved", "Sku", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, 1, 50, 0, "SKU-KBD-001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, 2, 80, 0, "SKU-MSE-002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, 3, 25, 0, "SKU-HDP-003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, 4, 10, 0, "SKU-MON-004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, 5, 40, 0, "SKU-CAM-005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, 6, 60, 0, "SKU-SSD-006", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, 7, 15, 0, "SKU-CHR-007", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, 8, 2, 0, "SKU-SPK-008", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_ProductId",
                table: "StockItems",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_Sku",
                table: "StockItems",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_OrderId_ProductId",
                table: "StockReservations",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_Status",
                table: "StockReservations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedMessages");

            migrationBuilder.DropTable(
                name: "StockItems");

            migrationBuilder.DropTable(
                name: "StockReservations");
        }
    }
}
