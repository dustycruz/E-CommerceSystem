using ECommerceSystem.CatalogService.Data.Entities;

namespace ECommerceSystem.CatalogService.Data.Seed;

/// <summary>
/// Constant seed data. Values must never change between builds (no DateTime.UtcNow, no Guid.NewGuid()),
/// otherwise EF Core reports pending model changes and Database.Migrate() fails.
/// Product Ids 1..8 and SKUs are mirrored by the Inventory Service seed (by value, not by foreign key).
/// </summary>
public static class CatalogSeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Product[] Products { get; } =
    [
        new Product
        {
            Id = 1,
            Sku = "SKU-KBD-001",
            Name = "Mechanical Keyboard",
            Description = "Tenkeyless mechanical keyboard with hot-swappable brown switches and white backlight.",
            Category = "Peripherals",
            Price = 3499.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Mechanical+Keyboard",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 2,
            Sku = "SKU-MSE-002",
            Name = "Wireless Mouse",
            Description = "Ergonomic 2.4 GHz wireless mouse with silent clicks and 12-month battery life.",
            Category = "Peripherals",
            Price = 1299.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Wireless+Mouse",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 3,
            Sku = "SKU-HDP-003",
            Name = "Noise-Cancelling Headphones",
            Description = "Over-ear Bluetooth headphones with active noise cancellation and 30-hour battery.",
            Category = "Audio",
            Price = 7999.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Headphones",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 4,
            Sku = "SKU-MON-004",
            Name = "27-inch QHD Monitor",
            Description = "27-inch 2560x1440 IPS monitor, 165 Hz, height-adjustable stand.",
            Category = "Displays",
            Price = 15999.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=QHD+Monitor",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 5,
            Sku = "SKU-CAM-005",
            Name = "1080p Webcam",
            Description = "Full HD webcam with dual microphones and privacy shutter.",
            Category = "Peripherals",
            Price = 2499.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Webcam",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 6,
            Sku = "SKU-SSD-006",
            Name = "1TB NVMe SSD",
            Description = "PCIe 4.0 NVMe solid state drive, up to 7000 MB/s read.",
            Category = "Storage",
            Price = 4999.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=NVMe+SSD",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 7,
            Sku = "SKU-CHR-007",
            Name = "Ergonomic Office Chair",
            Description = "Mesh office chair with lumbar support, adjustable armrests and headrest.",
            Category = "Furniture",
            Price = 8999.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Office+Chair",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        },
        new Product
        {
            Id = 8,
            Sku = "SKU-SPK-008",
            Name = "Portable Bluetooth Speaker",
            Description = "Waterproof portable speaker with 12-hour battery. Limited stock (used for the out-of-stock demo).",
            Category = "Audio",
            Price = 1999.00m,
            ImageUrl = "https://placehold.co/600x400/png?text=Bluetooth+Speaker",
            IsActive = true,
            CreatedAtUtc = SeededAtUtc,
            UpdatedAtUtc = SeededAtUtc
        }
    ];
}