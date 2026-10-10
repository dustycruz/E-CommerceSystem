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
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE TABLE [ProcessedMessages] (
        [MessageId] uniqueidentifier NOT NULL,
        [Consumer] nvarchar(200) NOT NULL,
        [ProcessedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ProcessedMessages] PRIMARY KEY ([MessageId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE TABLE [StockItems] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Sku] nvarchar(32) NOT NULL,
        [QuantityOnHand] int NOT NULL,
        [QuantityReserved] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_StockItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockItems_Quantities] CHECK ([QuantityOnHand] >= 0 AND [QuantityReserved] >= 0 AND [QuantityReserved] <= [QuantityOnHand])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE TABLE [StockReservations] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_StockReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StockReservations_Quantity_Positive] CHECK ([Quantity] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ProductId', N'QuantityOnHand', N'QuantityReserved', N'Sku', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[StockItems]'))
        SET IDENTITY_INSERT [StockItems] ON;
    EXEC(N'INSERT INTO [StockItems] ([Id], [ProductId], [QuantityOnHand], [QuantityReserved], [Sku], [UpdatedAtUtc])
    VALUES (1, 1, 50, 0, N''SKU-KBD-001'', ''2026-01-01T00:00:00.0000000Z''),
    (2, 2, 80, 0, N''SKU-MSE-002'', ''2026-01-01T00:00:00.0000000Z''),
    (3, 3, 25, 0, N''SKU-HDP-003'', ''2026-01-01T00:00:00.0000000Z''),
    (4, 4, 10, 0, N''SKU-MON-004'', ''2026-01-01T00:00:00.0000000Z''),
    (5, 5, 40, 0, N''SKU-CAM-005'', ''2026-01-01T00:00:00.0000000Z''),
    (6, 6, 60, 0, N''SKU-SSD-006'', ''2026-01-01T00:00:00.0000000Z''),
    (7, 7, 15, 0, N''SKU-CHR-007'', ''2026-01-01T00:00:00.0000000Z''),
    (8, 8, 2, 0, N''SKU-SPK-008'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ProductId', N'QuantityOnHand', N'QuantityReserved', N'Sku', N'UpdatedAtUtc') AND [object_id] = OBJECT_ID(N'[StockItems]'))
        SET IDENTITY_INSERT [StockItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockItems_ProductId] ON [StockItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockItems_Sku] ON [StockItems] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockReservations_OrderId_ProductId] ON [StockReservations] ([OrderId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    CREATE INDEX [IX_StockReservations_Status] ON [StockReservations] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010175949_InitialInventory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010175949_InitialInventory', N'10.0.0');
END;

COMMIT;
GO

