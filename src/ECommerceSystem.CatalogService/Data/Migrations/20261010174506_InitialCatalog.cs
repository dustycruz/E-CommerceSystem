using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerceSystem.CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "Description", "ImageUrl", "IsActive", "Name", "Price", "Sku", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, "Peripherals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tenkeyless mechanical keyboard with hot-swappable brown switches and white backlight.", "https://placehold.co/600x400/png?text=Mechanical+Keyboard", true, "Mechanical Keyboard", 3499.00m, "SKU-KBD-001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Peripherals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ergonomic 2.4 GHz wireless mouse with silent clicks and 12-month battery life.", "https://placehold.co/600x400/png?text=Wireless+Mouse", true, "Wireless Mouse", 1299.00m, "SKU-MSE-002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Audio", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Over-ear Bluetooth headphones with active noise cancellation and 30-hour battery.", "https://placehold.co/600x400/png?text=Headphones", true, "Noise-Cancelling Headphones", 7999.00m, "SKU-HDP-003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Displays", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "27-inch 2560x1440 IPS monitor, 165 Hz, height-adjustable stand.", "https://placehold.co/600x400/png?text=QHD+Monitor", true, "27-inch QHD Monitor", 15999.00m, "SKU-MON-004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Peripherals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full HD webcam with dual microphones and privacy shutter.", "https://placehold.co/600x400/png?text=Webcam", true, "1080p Webcam", 2499.00m, "SKU-CAM-005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Storage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PCIe 4.0 NVMe solid state drive, up to 7000 MB/s read.", "https://placehold.co/600x400/png?text=NVMe+SSD", true, "1TB NVMe SSD", 4999.00m, "SKU-SSD-006", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Furniture", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mesh office chair with lumbar support, adjustable armrests and headrest.", "https://placehold.co/600x400/png?text=Office+Chair", true, "Ergonomic Office Chair", 8999.00m, "SKU-CHR-007", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Audio", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Waterproof portable speaker with 12-hour battery. Limited stock (used for the out-of-stock demo).", "https://placehold.co/600x400/png?text=Bluetooth+Speaker", true, "Portable Bluetooth Speaker", 1999.00m, "SKU-SPK-008", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
