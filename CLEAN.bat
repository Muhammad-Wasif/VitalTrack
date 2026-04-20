@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Clean

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack CLEAN SCRIPT
::  Removes all build artifacts (bin/, obj/, publish/)
::  Use before a fresh build or when switching configurations
:: ═══════════════════════════════════════════════════════════════════

echo.
echo  ╔══════════════════════════════════════════════════╗
echo  ║   VitalTrack  —  Clean Build Artifacts          ║
echo  ╚══════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    :: dotnet not available — do manual cleanup
    echo  [INFO] dotnet not found — cleaning manually...
    goto :ManualClean
)

echo  [INFO] Running dotnet clean...
dotnet clean "VitalTrack.sln" --verbosity minimal
if not errorlevel 1 (
    echo  [ OK ] dotnet clean complete
)

:ManualClean
echo  [INFO] Removing bin/ and obj/ folders...

for /d /r . %%d in (bin obj) do (
    if exist "%%d" (
        echo  Removing: %%d
        rd /s /q "%%d" 2>nul
    )
)

if exist "publish" (
    echo  [INFO] Removing publish/ folder...
    rd /s /q "publish" 2>nul
)

echo.
echo  [ OK ] Clean complete. Run BUILD.bat to rebuild.
echo.
pause
exit /b 0
