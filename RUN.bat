@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Run

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack RUN SCRIPT
::  Checks prerequisites, validates .env, then launches the app
:: ═══════════════════════════════════════════════════════════════════

call :PrintBanner

cd /d "%~dp0"

:: ════════════════════════════════
::  STEP 1 — Check dotnet
:: ════════════════════════════════
call :PrintStep "1" "Checking .NET runtime"

where dotnet >nul 2>&1
if errorlevel 1 (
    call :PrintError "dotnet.exe not found."
    echo  Download .NET 8: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "SDK_VER=%%v"
call :PrintOK ".NET v%SDK_VER%"

:: ════════════════════════════════
::  STEP 2 — Check .env
:: ════════════════════════════════
call :PrintStep "2" "Checking .env file"

if not exist ".env" (
    echo  [WARN] .env not found — creating from .env.example
    if exist ".env.example" (
        copy ".env.example" ".env" >nul
        echo.
        echo  ┌─────────────────────────────────────────────────────┐
        echo  │  IMPORTANT: .env was just created from template.    │
        echo  │                                                     │
        echo  │  Please edit .env and set:                         │
        echo  │    DB_CONNECTION_STRING = your SQL Server string   │
        echo  │    HUGGINGFACE_API_KEY  = hf_...  (optional)       │
        echo  │    EXERCISEDB_API_KEY   = ...      (optional)       │
        echo  │                                                     │
        echo  │  Then run RUN.bat again.                           │
        echo  └─────────────────────────────────────────────────────┘
        echo.
        pause
        start notepad ".env"
        exit /b 0
    ) else (
        call :PrintError ".env.example missing. Cannot create .env."
        pause
        exit /b 1
    )
)
call :PrintOK ".env file found"

:: Read DB connection string from .env to validate it is set
set "DB_SET=0"
for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
    if /i "%%a"=="DB_CONNECTION_STRING" (
        if not "%%b"=="" set "DB_SET=1"
    )
)
if "%DB_SET%"=="0" (
    call :PrintError "DB_CONNECTION_STRING is empty in .env"
    echo  Edit .env and set a valid SQL Server connection string.
    pause
    exit /b 1
)
call :PrintOK "DB_CONNECTION_STRING is configured"

:: ════════════════════════════════
::  STEP 3 — Check build exists
:: ════════════════════════════════
call :PrintStep "3" "Locating compiled executable"

set "EXE_DEBUG=VitalTrack.UI\bin\Debug\net8.0-windows\VitalTrack.exe"
set "EXE_RELEASE=VitalTrack.UI\bin\Release\net8.0-windows\VitalTrack.exe"

:: Prefer Release, fall back to Debug
set "EXE_PATH="
if exist "%EXE_RELEASE%" (
    set "EXE_PATH=%EXE_RELEASE%"
    set "BUILD_TYPE=Release"
)
if exist "%EXE_DEBUG%" (
    if "%EXE_PATH%"=="" (
        set "EXE_PATH=%EXE_DEBUG%"
        set "BUILD_TYPE=Debug"
    )
)

if "%EXE_PATH%"=="" (
    call :PrintError "No compiled executable found."
    echo.
    echo  Run BUILD.bat first to compile the application.
    echo.
    set /p "DOBUILD=Would you like to build now? (Y/N): "
    if /i "!DOBUILD!"=="Y" (
        call BUILD.bat
        if errorlevel 1 exit /b 1
        :: Re-check after build
        if exist "%EXE_RELEASE%" (
            set "EXE_PATH=%EXE_RELEASE%"
            set "BUILD_TYPE=Release"
        ) else if exist "%EXE_DEBUG%" (
            set "EXE_PATH=%EXE_DEBUG%"
            set "BUILD_TYPE=Debug"
        ) else (
            call :PrintError "Still no executable after build. Check build errors."
            pause
            exit /b 1
        )
    ) else (
        exit /b 1
    )
)

call :PrintOK "Found [%BUILD_TYPE%]: %EXE_PATH%"

:: ════════════════════════════════
::  STEP 4 — Check SQL Server
:: ════════════════════════════════
call :PrintStep "4" "Checking SQL Server connectivity"

:: Try sqlcmd if available (non-blocking check)
where sqlcmd >nul 2>&1
if not errorlevel 1 (
    sqlcmd -Q "SELECT 1" -b -l 3 >nul 2>&1
    if errorlevel 1 (
        echo  [WARN] SQL Server may not be running on localhost.
        echo         The app will attempt to connect anyway and show
        echo         a friendly error dialog if it cannot reach the DB.
    ) else (
        call :PrintOK "SQL Server responded"
    )
) else (
    echo  [INFO] sqlcmd not found — skipping DB ping (app will check on startup)
)

:: ════════════════════════════════
::  STEP 5 — Launch
:: ════════════════════════════════
call :PrintStep "5" "Launching VitalTrack"
echo  Executable : %EXE_PATH%
echo  Build type : %BUILD_TYPE%
echo.
echo  [ INFO ] The app auto-migrates the database on first run.
echo  [ INFO ] Default login: admin / Admin@2025
echo.
echo  Starting...

:: Launch the app (not blocking — cmd returns immediately)
start "" "%~dp0%EXE_PATH%"

if errorlevel 1 (
    call :PrintError "Failed to start VitalTrack.exe"
    echo  Try running: dotnet run --project VitalTrack.UI
    pause
    exit /b 1
)

call :PrintOK "VitalTrack launched successfully!"
echo.
timeout /t 3 /nobreak >nul
exit /b 0

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
echo  Universal Health ^& Fitness Tracker  —  RUN SCRIPT
echo  ─────────────────────────────────────────────────
echo.
goto :eof

:PrintStep
echo.
echo  [STEP %~1] %~2
echo  ─────────────────────────────────────────────────────
goto :eof

:PrintOK
echo  [ OK ] %~1
goto :eof

:PrintError
echo.
echo  [FAIL] %~1
goto :eof
