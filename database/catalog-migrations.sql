IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010174506_InitialCatalog'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Sku] nvarchar(32) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Category] nvarchar(60) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_Price_NonNegative] CHECK ([Price] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010174506_InitialCatalog'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'Description', N'ImageUrl', N'IsActive', N'Name', N'Price', N'Sku', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] ON;
    EXEC(N'INSERT INTO [Products] ([Id], [Category], [CreatedAtUtc], [Description], [ImageUrl], [IsActive], [Name], [Price], [Sku], [UpdatedAtUtc])
    VALUES (1, N''Peripherals'', ''2026-01-01T00:00:00.0000000Z'', N''Tenkeyless mechanical keyboard with hot-swappable brown switches and white backlight.'', N''https://placehold.co/600x400/png?text=Mechanical+Keyboard'', CAST(1 AS bit), N''Mechanical Keyboard'', 3499.0, N''SKU-KBD-001'', ''2026-01-01T00:00:00.0000000Z''),
    (2, N''Peripherals'', ''2026-01-01T00:00:00.0000000Z'', N''Ergonomic 2.4 GHz wireless mouse with silent clicks and 12-month battery life.'', N''https://placehold.co/600x400/png?text=Wireless+Mouse'', CAST(1 AS bit), N''Wireless Mouse'', 1299.0, N''SKU-MSE-002'', ''2026-01-01T00:00:00.0000000Z''),
    (3, N''Audio'', ''2026-01-01T00:00:00.0000000Z'', N''Over-ear Bluetooth headphones with active noise cancellation and 30-hour battery.'', N''https://placehold.co/600x400/png?text=Headphones'', CAST(1 AS bit), N''Noise-Cancelling Headphones'', 7999.0, N''SKU-HDP-003'', ''2026-01-01T00:00:00.0000000Z''),
    (4, N''Displays'', ''2026-01-01T00:00:00.0000000Z'', N''27-inch 2560x1440 IPS monitor, 165 Hz, height-adjustable stand.'', N''https://placehold.co/600x400/png?text=QHD+Monitor'', CAST(1 AS bit), N''27-inch QHD Monitor'', 15999.0, N''SKU-MON-004'', ''2026-01-01T00:00:00.0000000Z''),
    (5, N''Peripherals'', ''2026-01-01T00:00:00.0000000Z'', N''Full HD webcam with dual microphones and privacy shutter.'', N''https://placehold.co/600x400/png?text=Webcam'', CAST(1 AS bit), N''1080p Webcam'', 2499.0, N''SKU-CAM-005'', ''2026-01-01T00:00:00.0000000Z''),
    (6, N''Storage'', ''2026-01-01T00:00:00.0000000Z'', N''PCIe 4.0 NVMe solid state drive, up to 7000 MB/s read.'', N''https://placehold.co/600x400/png?text=NVMe+SSD'', CAST(1 AS bit), N''1TB NVMe SSD'', 4999.0, N''SKU-SSD-006'', ''2026-01-01T00:00:00.0000000Z''),
    (7, N''Furniture'', ''2026-01-01T00:00:00.0000000Z'', N''Mesh office chair with lumbar support, adjustable armrests and headrest.'', N''https://placehold.co/600x400/png?text=Office+Chair'', CAST(1 AS bit), N''Ergonomic Office Chair'', 8999.0, N''SKU-CHR-007'', ''2026-01-01T00:00:00.0000000Z''),
    (8, N''Audio'', ''2026-01-01T00:00:00.0000000Z'', N''Waterproof portable speaker with 12-hour battery. Limited stock (used for the out-of-stock demo).'', N''https://placehold.co/600x400/png?text=Bluetooth+Speaker'', CAST(1 AS bit), N''Portable Bluetooth Speaker'', 1999.0, N''SKU-SPK-008'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAtUtc', N'Description', N'ImageUrl', N'IsActive', N'Name', N'Price', N'Sku', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010174506_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010174506_InitialCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010174506_InitialCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010174506_InitialCatalog', N'10.0.0');
END;

COMMIT;
GO

