@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Database Migration

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack DB MIGRATION SCRIPT
::  Applies EF Core migrations directly using the dotnet-ef tool.
::  Use this if you need to manually create or update the database
::  without launching the full application.
:: ═══════════════════════════════════════════════════════════════════

echo.
echo  ╔══════════════════════════════════════════════════════╗
echo  ║   VitalTrack  —  Database Migration Tool            ║
echo  ╚══════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: ── Check dotnet ────────────────────────────────────────────────────
where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [FAIL] dotnet.exe not found. Install .NET 8 SDK.
    pause
    exit /b 1
)

:: ── Check dotnet-ef tool ────────────────────────────────────────────
dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo  [INFO] dotnet-ef tool not found. Installing globally...
    dotnet tool install --global dotnet-ef --version 8.*
    if errorlevel 1 (
        echo  [FAIL] Could not install dotnet-ef tool.
        echo  Try manually: dotnet tool install --global dotnet-ef
        pause
        exit /b 1
    )
)
for /f "tokens=*" %%v in ('dotnet ef --version 2^>nul') do set "EF_VER=%%v"
echo  [ OK ] dotnet-ef: %EF_VER%

:: ── Load DB connection string from .env ─────────────────────────────
if not exist ".env" (
    echo  [FAIL] .env not found. Run BUILD.bat first to create it.
    pause
    exit /b 1
)

set "DB_CONN="
for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
    if /i "%%a"=="DB_CONNECTION_STRING" set "DB_CONN=%%b"
)

if "%DB_CONN%"=="" (
    echo  [FAIL] DB_CONNECTION_STRING not set in .env
    pause
    exit /b 1
)
echo  [ OK ] Connection string found in .env
echo  [INFO] Target: %DB_CONN%
echo.

:: ── Menu ────────────────────────────────────────────────────────────
echo  What would you like to do?
echo.
echo   [1] Apply all pending migrations  (database update)
echo   [2] Show migration status         (list migrations)
echo   [3] Show generated SQL            (script migrations)
echo   [4] Drop entire database          (DESTRUCTIVE!)
echo   [5] Exit
echo.
set /p "CHOICE=Enter choice (1-5): "

if "%CHOICE%"=="1" goto :ApplyMigrations
if "%CHOICE%"=="2" goto :ListMigrations
if "%CHOICE%"=="3" goto :ScriptMigrations
if "%CHOICE%"=="4" goto :DropDatabase
if "%CHOICE%"=="5" exit /b 0

echo  Invalid choice.
pause
exit /b 1

:: ── Apply migrations ────────────────────────────────────────────────
:ApplyMigrations
echo.
echo  [INFO] Applying migrations to database...
echo.

dotnet ef database update ^
    --project "VitalTrack.Data\VitalTrack.Data.csproj" ^
    --startup-project "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Debug ^
    --verbose

if errorlevel 1 (
    echo.
    echo  [FAIL] Migration failed. Common causes:
    echo    - SQL Server not running
    echo    - Wrong connection string in .env
    echo    - SQL Server user lacks CREATE DATABASE permission
    pause
    exit /b 1
)
echo.
echo  [ OK ] Database is up to date!
pause
exit /b 0

:: ── List migrations ─────────────────────────────────────────────────
:ListMigrations
echo.
echo  [INFO] Migration status:
echo.
dotnet ef migrations list ^
    --project "VitalTrack.Data\VitalTrack.Data.csproj" ^
    --startup-project "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Debug
echo.
pause
exit /b 0

:: ── Script SQL ──────────────────────────────────────────────────────
:ScriptMigrations
echo.
echo  [INFO] Generating SQL script...
dotnet ef migrations script ^
    --project "VitalTrack.Data\VitalTrack.Data.csproj" ^
    --startup-project "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Debug ^
    --output "VitalTrackDb_Script.sql" ^
    --idempotent

if errorlevel 1 (
    echo  [FAIL] Script generation failed.
    pause
    exit /b 1
)
echo  [ OK ] SQL script saved to: VitalTrackDb_Script.sql
start notepad "VitalTrackDb_Script.sql"
pause
exit /b 0

:: ── Drop database ───────────────────────────────────────────────────
:DropDatabase
echo.
echo  [WARN] THIS WILL DELETE ALL DATA IN THE DATABASE.
echo.
set /p "CONFIRM=Type YES to confirm: "
if /i not "%CONFIRM%"=="YES" (
    echo  Cancelled.
    pause
    exit /b 0
)
dotnet ef database drop ^
    --project "VitalTrack.Data\VitalTrack.Data.csproj" ^
    --startup-project "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Debug ^
    --force

echo  [ OK ] Database dropped.
pause
exit /b 0
