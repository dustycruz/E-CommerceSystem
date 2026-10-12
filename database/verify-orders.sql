-- =====================================================================
-- verify-orders.sql
-- Verifies ECommerceOrderDb (Order Service) and shows the latest saga.
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -W -i database\verify-orders.sql
-- =====================================================================
SET NOCOUNT ON;

IF DB_ID(N'ECommerceOrderDb') IS NULL
BEGIN
    PRINT 'FAIL: database ECommerceOrderDb does not exist. Run "dotnet ef database update" in src\ECommerceSystem.OrderService.';
    RETURN;
END;

USE [ECommerceOrderDb];

PRINT '=== 1. Tables (expected OrderItems, Orders, OrderStatusHistory, OutboxMessages, ProcessedMessages, __EFMigrationsHistory) ===';
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

PRINT '=== 2. Applied EF Core migrations ===';
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

PRINT '=== 3. Row counts (all 0 on a fresh database - orders are created only by the saga) ===';
SELECT 'Orders' AS TableName, COUNT(*) AS RowsCount FROM dbo.Orders
UNION ALL SELECT 'OrderItems', COUNT(*) FROM dbo.OrderItems
UNION ALL SELECT 'OrderStatusHistory', COUNT(*) FROM dbo.OrderStatusHistory
UNION ALL SELECT 'OutboxMessages', COUNT(*) FROM dbo.OutboxMessages
UNION ALL SELECT 'ProcessedMessages', COUNT(*) FROM dbo.ProcessedMessages;

PRINT '=== 4. Money columns (expected decimal 18,2) ===';
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TotalAmount')
   OR (TABLE_NAME = 'OrderItems' AND COLUMN_NAME IN ('UnitPrice', 'LineTotal'))
ORDER BY TABLE_NAME, COLUMN_NAME;

PRINT '=== 5. Foreign keys (expected 2, both CASCADE, inside this database only) ===';
SELECT fk.name AS ForeignKeyName,
       OBJECT_NAME(fk.parent_object_id) AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
       fk.delete_referential_action_desc AS OnDelete
FROM sys.foreign_keys AS fk
ORDER BY fk.name;

PRINT '=== 6. Indexes ===';
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.is_unique AS IsUnique
FROM sys.indexes AS i
WHERE i.object_id IN (OBJECT_ID(N'dbo.Orders'), OBJECT_ID(N'dbo.OrderItems'), OBJECT_ID(N'dbo.OrderStatusHistory'),
                      OBJECT_ID(N'dbo.OutboxMessages'), OBJECT_ID(N'dbo.ProcessedMessages'))
  AND i.name IS NOT NULL
ORDER BY TableName, IndexName;

PRINT '=== 7. Automated checks ===';
IF (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'
    AND TABLE_NAME IN ('Orders', 'OrderItems', 'OrderStatusHistory', 'OutboxMessages', 'ProcessedMessages')) = 5
    PRINT 'PASS: all 5 Order tables exist';
ELSE
    PRINT 'FAIL: one or more Order tables are missing';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'TotalAmount'
           AND DATA_TYPE = 'decimal' AND NUMERIC_PRECISION = 18 AND NUMERIC_SCALE = 2)
    PRINT 'PASS: Orders.TotalAmount is decimal(18,2)';
ELSE
    PRINT 'FAIL: Orders.TotalAmount is not decimal(18,2)';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'RowVersion' AND DATA_TYPE = 'timestamp')
    PRINT 'PASS: Orders.RowVersion is a rowversion column';
ELSE
    PRINT 'FAIL: Orders.RowVersion is not a rowversion column';

IF (SELECT COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action_desc = 'CASCADE') = 2
    PRINT 'PASS: OrderItems and OrderStatusHistory cascade from Orders';
ELSE
    PRINT 'FAIL: expected 2 cascading foreign keys';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OutboxMessages') AND name = N'IX_OutboxMessages_PublishedAtUtc_OccurredAtUtc')
    PRINT 'PASS: outbox polling index exists';
ELSE
    PRINT 'FAIL: IX_OutboxMessages_PublishedAtUtc_OccurredAtUtc missing';

PRINT '=== 8. Saga inspection: latest 10 orders ===';
SELECT TOP (10) Id, CustomerEmail, Status, TotalAmount, Currency, CardLast4, PaymentId, FailureReason,
       SimulateFailureAfterPayment, CorrelationId, CreatedAtUtc, UpdatedAtUtc
FROM dbo.Orders
ORDER BY CreatedAtUtc DESC;

DECLARE @LatestOrderId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.Orders ORDER BY CreatedAtUtc DESC);

PRINT '=== 9. Timeline of the latest order ===';
SELECT h.OccurredAtUtc, h.Status, h.Description
FROM dbo.OrderStatusHistory AS h
WHERE h.OrderId = @LatestOrderId
ORDER BY h.OccurredAtUtc, h.Id;

PRINT '=== 10. Outbox (latest 20; PublishedAtUtc NULL = not yet confirmed by RabbitMQ) ===';
SELECT TOP (20) Id, Type, RoutingKey, CorrelationId, OccurredAtUtc, PublishedAtUtc, Attempts, LastError
FROM dbo.OutboxMessages
ORDER BY OccurredAtUtc DESC;

PRINT '=== 11. Unpublished outbox messages (expected 0 when RabbitMQ is up) ===';
SELECT COUNT(*) AS UnpublishedCount
FROM dbo.OutboxMessages
WHERE PublishedAtUtc IS NULL;