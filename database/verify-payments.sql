-- =====================================================================
-- verify-payments.sql
-- Verifies ECommercePaymentDb (Payment Service).
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -W -i database\verify-payments.sql
-- =====================================================================
SET NOCOUNT ON;

IF DB_ID(N'ECommercePaymentDb') IS NULL
BEGIN
    PRINT 'FAIL: database ECommercePaymentDb does not exist. Run "dotnet ef database update" in src\ECommerceSystem.PaymentService.';
    RETURN;
END;

USE [ECommercePaymentDb];

PRINT '=== 1. Tables (expected Payments, __EFMigrationsHistory) ===';
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

PRINT '=== 2. Applied EF Core migrations ===';
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

PRINT '=== 3. Payments by status (empty on a fresh database) ===';
SELECT Status, COUNT(*) AS PaymentCount, SUM(Amount) AS TotalAmount
FROM dbo.Payments
GROUP BY Status
ORDER BY Status;

PRINT '=== 4. Latest 20 payments ===';
SELECT TOP (20) Id, OrderId, Amount, Currency, CardLast4, Status, FailureReason, CorrelationId,
       CreatedAtUtc, RefundedAtUtc
FROM dbo.Payments
ORDER BY CreatedAtUtc DESC;

PRINT '=== 5. Indexes ===';
SELECT i.name AS IndexName, i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.Payments') AND i.name IS NOT NULL
ORDER BY i.name;

PRINT '=== 6. Check constraints ===';
SELECT name AS ConstraintName, definition AS Definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Payments');

PRINT '=== 7. Automated checks ===';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'IX_Payments_OrderId' AND is_unique = 1)
    PRINT 'PASS: OrderId is unique (idempotency key)';
ELSE
    PRINT 'FAIL: unique index IX_Payments_OrderId missing';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Payments' AND COLUMN_NAME = 'Amount'
           AND DATA_TYPE = 'decimal' AND NUMERIC_PRECISION = 18 AND NUMERIC_SCALE = 2)
    PRINT 'PASS: Amount is decimal(18,2)';
ELSE
    PRINT 'FAIL: Amount is not decimal(18,2)';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Payments' AND COLUMN_NAME = 'RowVersion' AND DATA_TYPE = 'timestamp')
    PRINT 'PASS: RowVersion is a rowversion column';
ELSE
    PRINT 'FAIL: RowVersion is not a rowversion column';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Payments' AND COLUMN_NAME IN ('CardNumber', 'PaymentToken'))
    PRINT 'PASS: no full card number column is stored';
ELSE
    PRINT 'FAIL: a full card number column exists';

IF NOT EXISTS (SELECT OrderId FROM dbo.Payments GROUP BY OrderId HAVING COUNT(*) > 1)
    PRINT 'PASS: no order was charged twice';
ELSE
    PRINT 'FAIL: an order has more than one payment';