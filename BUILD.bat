@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Build

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack BUILD SCRIPT
::  Checks prerequisites, restores NuGet packages, builds the solution
:: ═══════════════════════════════════════════════════════════════════

call :PrintBanner

:: ── Change to the script's own directory ────────────────────────────
cd /d "%~dp0"

:: ════════════════════════════════
::  STEP 1 — Check .NET 8 SDK
:: ════════════════════════════════
call :PrintStep "1" "Checking .NET 8 SDK"

where dotnet >nul 2>&1
if errorlevel 1 (
    call :PrintError "dotnet.exe not found on PATH."
    echo.
    echo  Please install the .NET 8 SDK:
    echo  https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

:: Get SDK version
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "SDK_VER=%%v"
echo  [INFO] dotnet SDK found: v%SDK_VER%

:: Check it is at least 8.x
for /f "tokens=1 delims=." %%m in ("%SDK_VER%") do set "SDK_MAJOR=%%m"
if "%SDK_MAJOR%" LSS "8" (
    call :PrintError ".NET 8 SDK is required. Found: v%SDK_VER%"
    echo  Download: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
call :PrintOK ".NET SDK v%SDK_VER% — OK"

:: ════════════════════════════════
::  STEP 2 — Check solution file
:: ════════════════════════════════
call :PrintStep "2" "Verifying solution file"

if not exist "VitalTrack.sln" (
    call :PrintError "VitalTrack.sln not found."
    echo  Make sure you run BUILD.bat from inside the VitalTrack folder.
    pause
    exit /b 1
)
call :PrintOK "VitalTrack.sln found"

:: ════════════════════════════════
::  STEP 3 — Check .env file
:: ════════════════════════════════
call :PrintStep "3" "Checking .env configuration"

if not exist ".env" (
    echo  [WARN] .env file not found — copying from .env.example
    if exist ".env.example" (
        copy ".env.example" ".env" >nul
        call :PrintOK ".env created from .env.example (edit it before running!)"
    ) else (
        call :PrintError ".env.example not found either. Cannot create .env."
        pause
        exit /b 1
    )
) else (
    call :PrintOK ".env file present"
)

:: ════════════════════════════════
::  STEP 4 — Restore NuGet
:: ════════════════════════════════
call :PrintStep "4" "Restoring NuGet packages"

dotnet restore "VitalTrack.sln" --verbosity minimal
if errorlevel 1 (
    call :PrintError "NuGet restore failed."
    echo.
    echo  Check your internet connection and try again.
    echo  If using a corporate proxy set HTTPS_PROXY environment variable.
    pause
    exit /b 1
)
call :PrintOK "NuGet packages restored"

:: ════════════════════════════════
::  STEP 5 — Build Debug
:: ════════════════════════════════
call :PrintStep "5" "Building solution [Debug]"

dotnet build "VitalTrack.sln" ^
    --configuration Debug ^
    --no-restore ^
    --verbosity minimal
if errorlevel 1 (
    call :PrintError "Build FAILED. See errors above."
    echo.
    echo  Common fixes:
    echo    - Ensure .NET 8 SDK is installed (not just runtime)
    echo    - Check VitalTrack.UI\App.xaml resource dictionaries exist
    echo    - Verify all project references are intact
    pause
    exit /b 1
)

call :PrintOK "Build succeeded [Debug]"

:: ════════════════════════════════
::  STEP 6 — Build Release
:: ════════════════════════════════
call :PrintStep "6" "Building solution [Release]"

dotnet build "VitalTrack.sln" ^
    --configuration Release ^
    --no-restore ^
    --verbosity minimal
if errorlevel 1 (
    call :PrintError "Release build FAILED."
    pause
    exit /b 1
)
call :PrintOK "Build succeeded [Release]"

:: ════════════════════════════════
::  DONE
:: ════════════════════════════════
echo.
echo  ╔══════════════════════════════════════════════════════╗
echo  ║   BUILD COMPLETE — VitalTrack built successfully!   ║
echo  ║                                                      ║
echo  ║   Debug   : VitalTrack.UI\bin\Debug\net8.0-windows\ ║
echo  ║   Release : VitalTrack.UI\bin\Release\net8.0-windows\║
echo  ║                                                      ║
echo  ║   Run the app with:  RUN.bat                         ║
echo  ╚══════════════════════════════════════════════════════╝
echo.
pause
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
echo  Universal Health ^& Fitness Tracker  —  BUILD SCRIPT
echo  ────────────────────────────────────────────────────
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
