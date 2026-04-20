@echo off
setlocal EnableDelayedExpansion
title VitalTrack — First-Time Setup

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack SETUP WIZARD
::  Run this ONCE after cloning or extracting the project.
::  Checks .NET SDK, SQL Server, creates .env, and builds the app.
:: ═══════════════════════════════════════════════════════════════════

call :PrintBanner

cd /d "%~dp0"

set "ERRORS=0"

:: ════════════════════════════════
::  CHECK 1 — .NET 8 SDK
:: ════════════════════════════════
call :PrintStep "Checking .NET 8 SDK"

where dotnet >nul 2>&1
if errorlevel 1 (
    call :Fail ".NET SDK not found"
    echo.
    echo  Please install the .NET 8 Desktop SDK:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    echo  (choose: .NET Desktop Runtime or SDK for Windows)
    echo.
    set /p "OPEN=Open download page in browser? (Y/N): "
    if /i "!OPEN!"=="Y" start https://dotnet.microsoft.com/download/dotnet/8.0
    set /a ERRORS+=1
) else (
    for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "SDK_VER=%%v"
    for /f "tokens=1 delims=." %%m in ("!SDK_VER!") do set "SDK_MAJOR=%%m"
    if "!SDK_MAJOR!" LSS "8" (
        call :Fail ".NET !SDK_VER! found but .NET 8+ required"
        set /a ERRORS+=1
    ) else (
        call :Pass ".NET SDK v!SDK_VER!"
    )
)

:: ════════════════════════════════
::  CHECK 2 — WPF support
:: ════════════════════════════════
call :PrintStep "Checking WPF / Windows Desktop support"

dotnet --list-sdks 2>nul | findstr /i "8\." >nul
if errorlevel 1 (
    call :Fail "No .NET 8 SDK listed. Re-install with Desktop workload."
    set /a ERRORS+=1
) else (
    call :Pass "Windows Desktop development target available"
)

:: ════════════════════════════════
::  CHECK 3 — SQL Server
:: ════════════════════════════════
call :PrintStep "Checking SQL Server"

set "SQLSERVER_FOUND=0"

:: Check SQL Server Windows service
sc query MSSQLSERVER >nul 2>&1
if not errorlevel 1 (
    set "SQLSERVER_FOUND=1"
    call :Pass "SQL Server (MSSQLSERVER) service found"
)

:: Check SQL Server Express
sc query MSSQL$SQLEXPRESS >nul 2>&1
if not errorlevel 1 (
    set "SQLSERVER_FOUND=1"
    call :Pass "SQL Server Express (SQLEXPRESS) service found"
)

:: Check LocalDB
sqllocaldb info >nul 2>&1
if not errorlevel 1 (
    set "SQLSERVER_FOUND=1"
    call :Pass "SQL Server LocalDB found"
)

if "!SQLSERVER_FOUND!"=="0" (
    echo  [WARN] No SQL Server instance detected.
    echo.
    echo  Options:
    echo    1. SQL Server Express (free, full-featured):
    echo       https://aka.ms/sqlexpress
    echo    2. SQL Server LocalDB (lightweight, dev-only):
    echo       Install via Visual Studio Installer
    echo    3. SQL Server Developer Edition (free):
    echo       https://www.microsoft.com/sql-server/sql-server-downloads
    echo.
    set /p "OPEN=Open SQL Server Express download page? (Y/N): "
    if /i "!OPEN!"=="Y" start https://aka.ms/sqlexpress
    echo.
    echo  [INFO] You can continue setup — the app will prompt on first run.
)

:: ════════════════════════════════
::  CHECK 4 — SSMS (optional)
:: ════════════════════════════════
call :PrintStep "Checking SQL Server Management Studio (optional)"

if exist "%ProgramFiles(x86)%\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe" (
    call :Pass "SSMS 20 found"
) else if exist "%ProgramFiles(x86)%\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe" (
    call :Pass "SSMS 19 found"
) else (
    echo  [INFO] SSMS not found (optional — you can manage DB without it)
    echo         Download: https://aka.ms/ssmsfullsetup
)

:: ════════════════════════════════
::  STEP 5 — Create .env
:: ════════════════════════════════
call :PrintStep "Setting up .env configuration"

if exist ".env" (
    call :Pass ".env already exists"
) else (
    if exist ".env.example" (
        copy ".env.example" ".env" >nul
        call :Pass ".env created from template"
        echo.
        echo  ╔─────────────────────────────────────────────────────────╗
        echo  │  ACTION REQUIRED: Configure your .env file              │
        echo  │                                                         │
        echo  │  The file has been opened in Notepad.                  │
        echo  │  Set DB_CONNECTION_STRING to your SQL Server.          │
        echo  │                                                         │
        echo  │  Examples:                                              │
        echo  │  Local (Windows Auth):                                 │
        echo  │    Server=localhost;Database=VitalTrackDb;              │
        echo  │    Trusted_Connection=True;TrustServerCertificate=True; │
        echo  │                                                         │
        echo  │  LocalDB:                                               │
        echo  │    Server=(localdb)\MSSQLLocalDB;                       │
        echo  │    Database=VitalTrackDb;Trusted_Connection=True;       │
        echo  │                                                         │
        echo  │  Save the file and press any key to continue.          │
        echo  ╚─────────────────────────────────────────────────────────╝
        echo.
        start notepad ".env"
        pause
    ) else (
        call :Fail ".env.example not found"
        set /a ERRORS+=1
    )
)

:: ════════════════════════════════
::  STEP 6 — Install dotnet-ef
:: ════════════════════════════════
call :PrintStep "Installing dotnet-ef migration tool"

dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo  [INFO] Installing dotnet-ef globally...
    dotnet tool install --global dotnet-ef --version 8.*
    if errorlevel 1 (
        echo  [WARN] Could not install dotnet-ef. DB_MIGRATE.bat may not work.
        echo  Manual install: dotnet tool install --global dotnet-ef
    ) else (
        call :Pass "dotnet-ef installed"
    )
) else (
    for /f "tokens=*" %%v in ('dotnet ef --version 2^>nul') do set "EF_VER=%%v"
    call :Pass "dotnet-ef v!EF_VER! already installed"
)

:: ════════════════════════════════
::  STEP 7 — NuGet restore + Build
:: ════════════════════════════════
if "!ERRORS!" GTR "0" (
    echo.
    echo  [WARN] !ERRORS! prerequisite issue(s) detected above.
    set /p "CONTINUE=Continue to build anyway? (Y/N): "
    if /i not "!CONTINUE!"=="Y" goto :Summary
)

call :PrintStep "Restoring NuGet packages"
dotnet restore "VitalTrack.sln" --verbosity minimal
if errorlevel 1 (
    call :Fail "NuGet restore failed"
    set /a ERRORS+=1
) else (
    call :Pass "NuGet packages restored"
)

call :PrintStep "Building solution"
dotnet build "VitalTrack.sln" --configuration Debug --no-restore --verbosity minimal
if errorlevel 1 (
    call :Fail "Build failed — check errors above"
    set /a ERRORS+=1
) else (
    call :Pass "Build successful [Debug]"
)

:: ════════════════════════════════
::  SUMMARY
:: ════════════════════════════════
:Summary
echo.
if "!ERRORS!"=="0" (
    echo  ╔══════════════════════════════════════════════════════════╗
    echo  ║   SETUP COMPLETE — VitalTrack is ready to run!          ║
    echo  ║                                                          ║
    echo  ║   Next steps:                                            ║
    echo  ║     1. Double-click  RUN.bat      to launch the app     ║
    echo  ║     2. Login with:   admin / Admin@2025                 ║
    echo  ║     3. The database is auto-created on first launch     ║
    echo  ║                                                          ║
    echo  ║   Other scripts:                                         ║
    echo  ║     BUILD.bat      — rebuild the solution               ║
    echo  ║     RUN_DEV.bat    — run with console output            ║
    echo  ║     PUBLISH.bat    — create distributable .exe          ║
    echo  ║     DB_MIGRATE.bat — manage database migrations         ║
    echo  ║     CLEAN.bat      — remove build artifacts             ║
    echo  ╚══════════════════════════════════════════════════════════╝
) else (
    echo  ╔══════════════════════════════════════════════════════════╗
    echo  ║   SETUP FINISHED WITH !ERRORS! WARNING(S)                       ║
    echo  ║                                                          ║
    echo  ║   Resolve the issues above, then run SETUP.bat again.  ║
    echo  ║   Or proceed with RUN.bat if issues are non-critical.  ║
    echo  ╚══════════════════════════════════════════════════════════╝
)
echo.
pause
exit /b !ERRORS!

:: ════════════════ SUBROUTINES ════════════════

:PrintBanner
echo.
echo  ██╗   ██╗██╗████████╗ █████╗ ██╗     ████████╗██████╗  █████╗  ██████╗██╗  ██╗
echo  ██║   ██║██║╚══██╔══╝██╔══██╗██║        ██║   ██╔══██╗██╔══██╗██╔════╝██║ ██╔╝
echo  ██║   ██║██║   ██║   ███████║██║        ██║   ██████╔╝███████║██║     █████╔╝
echo  ╚██╗ ██╔╝██║   ██║   ██╔══██║██║        ██║   ██╔══██╗██╔══██║██║     ██╔═██╗
echo   ╚████╔╝ ██║   ██║   ██║  ██║███████╗   ██║   ██║  ██║██║  ██║╚██████╗██║  ██╗
echo    ╚═══╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝
echo.
echo  Universal Health ^& Fitness Tracker  —  SETUP WIZARD
echo  ──────────────────────────────────────────────────
echo.
goto :eof

:PrintStep
echo.
echo  ▶ %~1
echo  ─────────────────────────────────────────────────────
goto :eof

:Pass
echo  [ OK ] %~1
goto :eof

:Fail
echo  [FAIL] %~1
goto :eof
