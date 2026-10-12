@echo off
setlocal

rem =====================================================================
rem verify-all.cmd - runs every database verification script with sqlcmd.
rem Usage (from any folder):   database\verify-all.cmd
rem Optional server override:  database\verify-all.cmd "np:\\.\pipe\LOCALDB#XXXXXXXX\tsql\query"
rem =====================================================================

cd /d "%~dp0.."

set "SQLSERVER=(localdb)\MSSQLLocalDB"
if not "%~1"=="" set "SQLSERVER=%~1"

echo Using SQL Server instance: %SQLSERVER%

for %%F in (verify-all verify-catalog verify-inventory verify-orders verify-payments) do (
    echo.
    echo ==================================================================
    echo  %%F.sql
    echo ==================================================================
    sqlcmd -S "%SQLSERVER%" -E -W -i "database\%%F.sql"
)

echo.
echo Done. Look for any line starting with FAIL above.
endlocal