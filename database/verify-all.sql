-- =====================================================================
-- verify-all.sql
-- ADMIN-ONLY summary of all service databases (the services themselves never query across databases).
-- Run:  sqlcmd -S "(localdb)\MSSQLLocalDB" -E -W -i database\verify-all.sql
-- =====================================================================
SET NOCOUNT ON;

DECLARE @Results TABLE
(
    DatabaseName   sysname NOT NULL,
    OwnerService   nvarchar(50) NOT NULL,
    DatabaseExists bit NOT NULL,
    MigrationCount int NULL,
    MainTable      sysname NOT NULL,
    MainTableRows  int NULL,
    ExpectedRows   nvarchar(20) NOT NULL
);

DECLARE @Databases TABLE
(
    DatabaseName sysname NOT NULL,
    OwnerService nvarchar(50) NOT NULL,
    MainTable    sysname NOT NULL,
    ExpectedRows nvarchar(20) NOT NULL
);

INSERT INTO @Databases (DatabaseName, OwnerService, MainTable, ExpectedRows)
VALUES (N'ECommerceCatalogDb',   N'Catalog Service',   N'Products',   N'8'),
       (N'ECommerceInventoryDb', N'Inventory Service', N'StockItems', N'8'),
       (N'ECommerceOrderDb',     N'Order Service',     N'Orders',     N'0 or more'),
       (N'ECommercePaymentDb',   N'Payment Service',   N'Payments',   N'0 or more');

DECLARE @Db sysname, @Owner nvarchar(50), @Table sysname, @Expected nvarchar(20);
DECLARE @Sql nvarchar(max), @Migrations int, @Rows int;

DECLARE DbCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DatabaseName, OwnerService, MainTable, ExpectedRows FROM @Databases;

OPEN DbCursor;
FETCH NEXT FROM DbCursor INTO @Db, @Owner, @Table, @Expected;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@Db) IS NULL
    BEGIN
        INSERT INTO @Results VALUES (@Db, @Owner, 0, NULL, @Table, NULL, @Expected);
    END
    ELSE
    BEGIN
        SET @Migrations = NULL;
        SET @Rows = NULL;
        SET @Sql = N'SELECT @m = COUNT(*) FROM ' + QUOTENAME(@Db) + N'.dbo.__EFMigrationsHistory; '
                 + N'SELECT @r = COUNT(*) FROM ' + QUOTENAME(@Db) + N'.dbo.' + QUOTENAME(@Table) + N';';
        EXEC sys.sp_executesql @Sql, N'@m int OUTPUT, @r int OUTPUT', @m = @Migrations OUTPUT, @r = @Rows OUTPUT;
        INSERT INTO @Results VALUES (@Db, @Owner, 1, @Migrations, @Table, @Rows, @Expected);
    END;

    FETCH NEXT FROM DbCursor INTO @Db, @Owner, @Table, @Expected;
END;

CLOSE DbCursor;
DEALLOCATE DbCursor;

PRINT '=== Database-per-service summary ===';
SELECT DatabaseName, OwnerService, DatabaseExists, MigrationCount, MainTable, MainTableRows, ExpectedRows
FROM @Results
ORDER BY DatabaseName;

PRINT '=== Automated checks ===';
IF NOT EXISTS (SELECT 1 FROM @Results WHERE DatabaseExists = 0)
    PRINT 'PASS: all four service databases exist';
ELSE
    PRINT 'FAIL: at least one service database is missing (see DatabaseExists = 0 above)';

IF NOT EXISTS (SELECT 1 FROM @Results WHERE DatabaseExists = 1 AND ISNULL(MigrationCount, 0) < 1)
    PRINT 'PASS: every existing database has at least one applied migration';
ELSE
    PRINT 'FAIL: a database has no applied migrations';

IF EXISTS (SELECT 1 FROM @Results WHERE DatabaseName = N'ECommerceCatalogDb' AND MainTableRows = 8)
   AND EXISTS (SELECT 1 FROM @Results WHERE DatabaseName = N'ECommerceInventoryDb' AND MainTableRows = 8)
    PRINT 'PASS: Catalog and Inventory seed data present (8 + 8 rows)';
ELSE
    PRINT 'FAIL: Catalog or Inventory seed data missing';