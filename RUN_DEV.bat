@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Dev Mode

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack DEV RUN SCRIPT
::  Runs via `dotnet run` — shows console output, faster iteration,
::  no pre-built exe required. Best for development and debugging.
:: ═══════════════════════════════════════════════════════════════════

echo.
echo  ╔══════════════════════════════════════════════════╗
echo  ║   VitalTrack  —  Developer Mode                 ║
echo  ║   Using: dotnet run --project VitalTrack.UI     ║
echo  ║   Console output visible below                  ║
echo  ╚══════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

:: Check dotnet
where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [FAIL] dotnet not found. Install .NET 8 SDK first.
    pause
    exit /b 1
)

:: Create .env from example if missing
if not exist ".env" (
    if exist ".env.example" (
        copy ".env.example" ".env" >nul
        echo  [INFO] Created .env from .env.example — edit before running.
        start notepad ".env"
        pause
    )
)

echo  [INFO] Restoring packages...
dotnet restore "VitalTrack.sln" --verbosity quiet

echo  [INFO] Building and running VitalTrack.UI...
echo  [INFO] Press Ctrl+C to stop.
echo.

dotnet run ^
    --project "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Debug ^
    --no-restore

echo.
echo  [INFO] Application exited.
pause
