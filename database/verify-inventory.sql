-- =====================================================================
-- verify-inventory.sql
-- Verifies ECommerceInventoryDb (Inventory Service).
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -W -i database\verify-inventory.sql
-- =====================================================================
SET NOCOUNT ON;

IF DB_ID(N'ECommerceInventoryDb') IS NULL
BEGIN
    PRINT 'FAIL: database ECommerceInventoryDb does not exist. Run "dotnet ef database update" in src\ECommerceSystem.InventoryService.';
    RETURN;
END;

USE [ECommerceInventoryDb];

PRINT '=== 1. Tables (expected ProcessedMessages, StockItems, StockReservations, __EFMigrationsHistory) ===';
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

PRINT '=== 2. Applied EF Core migrations ===';
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

PRINT '=== 3. Stock items (expected 8 rows; SKU-SPK-008 has 2 units) ===';
SELECT Id, ProductId, Sku, QuantityOnHand, QuantityReserved,
       QuantityOnHand - QuantityReserved AS QuantityAvailable,
       CONVERT(varchar(20), RowVersion, 1) AS RowVersionHex,
       UpdatedAtUtc
FROM dbo.StockItems
ORDER BY ProductId;

PRINT '=== 4. Reservations (expected 0 rows before any order) ===';
SELECT Id, OrderId, ProductId, Quantity, Status, CreatedAtUtc, UpdatedAtUtc
FROM dbo.StockReservations
ORDER BY CreatedAtUtc;

PRINT '=== 5. Processed messages (expected 0 rows before any order) ===';
SELECT MessageId, Consumer, ProcessedAtUtc
FROM dbo.ProcessedMessages
ORDER BY ProcessedAtUtc;

PRINT '=== 6. Indexes ===';
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.is_unique AS IsUnique
FROM sys.indexes AS i
WHERE i.object_id IN (OBJECT_ID(N'dbo.StockItems'), OBJECT_ID(N'dbo.StockReservations'), OBJECT_ID(N'dbo.ProcessedMessages'))
  AND i.name IS NOT NULL
ORDER BY TableName, IndexName;

PRINT '=== 7. Check constraints ===';
SELECT OBJECT_NAME(parent_object_id) AS TableName, name AS ConstraintName, definition AS Definition
FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID(N'dbo.StockItems'), OBJECT_ID(N'dbo.StockReservations'))
ORDER BY TableName;

PRINT '=== 8. Automated checks ===';
IF (SELECT COUNT(*) FROM dbo.StockItems) = 8
    PRINT 'PASS: 8 stock items seeded';
ELSE
    PRINT 'FAIL: expected 8 stock items';

IF EXISTS (SELECT 1 FROM dbo.StockItems WHERE Sku = N'SKU-SPK-008' AND QuantityOnHand = 2)
    PRINT 'PASS: low-stock product SKU-SPK-008 has exactly 2 units';
ELSE
    PRINT 'INFO: SKU-SPK-008 does not have exactly 2 units (expected 2 on a fresh database)';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockItems' AND COLUMN_NAME = 'RowVersion' AND DATA_TYPE = 'timestamp')
    PRINT 'PASS: StockItems.RowVersion is a rowversion (timestamp) column';
ELSE
    PRINT 'FAIL: StockItems.RowVersion is not a rowversion column';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockItems') AND name = N'IX_StockItems_ProductId' AND is_unique = 1)
    PRINT 'PASS: StockItems.ProductId is unique';
ELSE
    PRINT 'FAIL: unique index IX_StockItems_ProductId missing';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockReservations') AND name = N'IX_StockReservations_OrderId_ProductId' AND is_unique = 1)
    PRINT 'PASS: one reservation per (OrderId, ProductId)';
ELSE
    PRINT 'FAIL: unique index IX_StockReservations_OrderId_ProductId missing';

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_StockItems_Quantities')
    PRINT 'PASS: check constraint prevents reserving more than on hand';
ELSE
    PRINT 'FAIL: CK_StockItems_Quantities missing';

IF NOT EXISTS (SELECT 1 FROM dbo.StockItems WHERE QuantityReserved > QuantityOnHand OR QuantityReserved < 0)
    PRINT 'PASS: no oversold stock rows';
ELSE
    PRINT 'FAIL: a stock row has QuantityReserved > QuantityOnHand';