-- =====================================================================
-- verify-catalog.sql
-- Verifies ECommerceCatalogDb (Catalog Service).
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -W -i database\verify-catalog.sql
-- =====================================================================
SET NOCOUNT ON;

IF DB_ID(N'ECommerceCatalogDb') IS NULL
BEGIN
    PRINT 'FAIL: database ECommerceCatalogDb does not exist. Run "dotnet ef database update" in src\ECommerceSystem.CatalogService.';
    RETURN;
END;

USE [ECommerceCatalogDb];

PRINT '=== 1. Tables ===';
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

PRINT '=== 2. Applied EF Core migrations ===';
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

PRINT '=== 3. Products (expected 8 rows) ===';
SELECT Id, Sku, Name, Category, Price, IsActive, CreatedAtUtc
FROM dbo.Products
ORDER BY Id;

PRINT '=== 4. Products per category ===';
SELECT Category, COUNT(*) AS ProductCount
FROM dbo.Products
GROUP BY Category
ORDER BY Category;

PRINT '=== 5. Indexes on Products (expected unique IX_Products_Sku) ===';
SELECT i.name AS IndexName, i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.Products') AND i.name IS NOT NULL
ORDER BY i.name;

PRINT '=== 6. Money column type (expected decimal 18,2) ===';
SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION, NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Price';

PRINT '=== 7. Check constraints ===';
SELECT name AS ConstraintName, definition AS Definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.Products');

PRINT '=== 8. Automated checks ===';
IF (SELECT COUNT(*) FROM dbo.Products) = 8
    PRINT 'PASS: 8 products seeded';
ELSE
    PRINT 'FAIL: expected 8 products';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Products') AND name = N'IX_Products_Sku' AND is_unique = 1)
    PRINT 'PASS: Sku is unique';
ELSE
    PRINT 'FAIL: unique index IX_Products_Sku missing';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'Price' AND DATA_TYPE = 'decimal' AND NUMERIC_PRECISION = 18 AND NUMERIC_SCALE = 2)
    PRINT 'PASS: Price is decimal(18,2)';
ELSE
    PRINT 'FAIL: Price is not decimal(18,2)';

IF EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = N'SKU-SPK-008' AND IsActive = 1)
    PRINT 'PASS: low-stock demo product SKU-SPK-008 exists and is active';
ELSE
    PRINT 'FAIL: SKU-SPK-008 missing or inactive';