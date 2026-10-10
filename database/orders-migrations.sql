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
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerName] nvarchar(100) NOT NULL,
        [CustomerEmail] nvarchar(200) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [CardLast4] nvarchar(4) NOT NULL,
        [PaymentToken] nvarchar(32) NULL,
        [FailureReason] nvarchar(500) NULL,
        [PaymentId] uniqueidentifier NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [SimulateFailureAfterPayment] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Orders_CardLast4_Length] CHECK (LEN([CardLast4]) = 4),
        CONSTRAINT [CK_Orders_TotalAmount_NonNegative] CHECK ([TotalAmount] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [Type] nvarchar(100) NOT NULL,
        [RoutingKey] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [PublishedAtUtc] datetime2 NULL,
        [Attempts] int NOT NULL,
        [LastError] nvarchar(2000) NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
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
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductId] int NOT NULL,
        [Sku] nvarchar(32) NOT NULL,
        [ProductName] nvarchar(120) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderItems_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderItems_UnitPrice_NonNegative] CHECK ([UnitPrice] >= 0),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE TABLE [OrderStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] uniqueidentifier NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderStatusHistory_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderItems_OrderId_ProductId] ON [OrderItems] ([OrderId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_CorrelationId] ON [Orders] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_CreatedAtUtc] ON [Orders] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerEmail] ON [Orders] ([CustomerEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistory_OrderId_OccurredAtUtc] ON [OrderStatusHistory] ([OrderId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_OutboxMessages_PublishedAtUtc_OccurredAtUtc] ON [OutboxMessages] ([PublishedAtUtc], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261010181507_InitialOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261010181507_InitialOrders', N'10.0.0');
END;

COMMIT;
GO

