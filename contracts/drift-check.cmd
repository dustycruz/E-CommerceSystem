@echo off
setlocal EnableExtensions

rem =====================================================================
rem drift-check.cmd - compares contracts\<name>.yaml with the live Swagger
rem document of the running service (http://localhost:<port>/swagger/v1/swagger.json).
rem
rem Usage:    contracts\drift-check.cmd <contract-name> <port>
rem Examples: contracts\drift-check.cmd catalog 5001
rem           contracts\drift-check.cmd orders 5002
rem           contracts\drift-check.cmd inventory 5003
rem           contracts\drift-check.cmd payments 5004
rem
rem Compared: 1) operationIds  2) paths  3) response status codes (as a multiset).
rem Accepted differences (not compared): servers, descriptions, examples,
rem Location response headers, minimum on path ids, openapi 3.0.3 vs 3.0.4.
rem =====================================================================

if "%~2"=="" (
    echo Usage:   contracts\drift-check.cmd ^<contract-name^> ^<port^>
    echo Example: contracts\drift-check.cmd catalog 5001
    exit /b 2
)

cd /d "%~dp0.."

set "NAME=%~1"
set "PORT=%~2"
set "CONTRACT=contracts\%NAME%.yaml"
set "WORK=%TEMP%\ecommerce-drift-%NAME%"
set "LIVE=%WORK%\live-swagger.json"
set "URL=http://localhost:%PORT%/swagger/v1/swagger.json"
set "DRIFT=0"

if not exist "%CONTRACT%" (
    echo ERROR: %CONTRACT% not found.
    exit /b 2
)

if exist "%WORK%" rmdir /s /q "%WORK%"
mkdir "%WORK%"

curl.exe -s -f -o "%LIVE%" "%URL%"
if errorlevel 1 (
    echo ERROR: could not download %URL% - start the service first.
    exit /b 2
)

echo Contract : %CONTRACT%
echo Live     : %URL%
echo Work dir : %WORK%
echo.

rem ---- 1. operationIds ----
(for /f "tokens=2 delims=: " %%A in ('findstr /C:"operationId:" "%CONTRACT%"') do @echo %%A) > "%WORK%\contract-operationIds.tmp"
(for /f "tokens=2 delims=:, " %%A in ('findstr /C:"operationId" "%LIVE%"') do @echo %%~A) > "%WORK%\live-operationIds.tmp"
call :compare operationIds

rem ---- 2. paths ----
(for /f "tokens=1 delims=: " %%A in ('findstr /B /C:"  /" "%CONTRACT%"') do @echo %%A) > "%WORK%\contract-paths.tmp"
(for /f "tokens=1 delims=: " %%A in ('findstr /C:"/%NAME%/v1/" "%LIVE%"') do @echo %%~A) > "%WORK%\live-paths.tmp"
call :compare paths

rem ---- 3. response status codes ----
(for /f "tokens=1 delims=:' " %%A in ('findstr /R /C:"^        '[0-9][0-9][0-9]':" "%CONTRACT%"') do @echo %%A) > "%WORK%\contract-statusCodes.tmp"
(for /f "tokens=1 delims=: " %%A in ('findstr /R /C:"[0-9][0-9][0-9].: {" "%LIVE%"') do @echo %%~A) > "%WORK%\live-statusCodes.tmp"
call :compare statusCodes

echo.
if "%DRIFT%"=="0" (
    echo RESULT: NO DRIFT - the running service matches %CONTRACT%.
    exit /b 0
)

echo RESULT: DRIFT FOUND - fix the controller attributes or the contract, then run this again.
exit /b 1

:compare
sort "%WORK%\contract-%1.tmp" /O "%WORK%\contract-%1.txt"
sort "%WORK%\live-%1.tmp" /O "%WORK%\live-%1.txt"
fc /W "%WORK%\contract-%1.txt" "%WORK%\live-%1.txt" > nul
if errorlevel 1 (
    echo DRIFT: %1 differ. First list = contract, second list = live service:
    fc /W "%WORK%\contract-%1.txt" "%WORK%\live-%1.txt"
    set "DRIFT=1"
) else (
    echo PASS : %1 match.
)
echo.
exit /b 0