@echo off
setlocal EnableDelayedExpansion
title VitalTrack — Publish

:: ═══════════════════════════════════════════════════════════════════
::  VitalTrack PUBLISH SCRIPT
::  Produces a self-contained Windows x64 executable in /publish/
::  Users can run VitalTrack.exe without installing .NET separately.
:: ═══════════════════════════════════════════════════════════════════

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║   VitalTrack  —  Publish (Self-Contained Release)       ║
echo  ║   Output: publish\VitalTrack.exe                        ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.

cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  [FAIL] dotnet not found. Install .NET 8 SDK.
    pause
    exit /b 1
)

echo  [INFO] Restoring packages...
dotnet restore "VitalTrack.sln" --verbosity quiet
if errorlevel 1 ( echo [FAIL] Restore failed. & pause & exit /b 1 )

echo  [INFO] Publishing self-contained win-x64 build...
echo.

dotnet publish "VitalTrack.UI\VitalTrack.UI.csproj" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "publish" ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true ^
    --verbosity minimal

if errorlevel 1 (
    echo.
    echo  [FAIL] Publish failed. Check errors above.
    pause
    exit /b 1
)

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║   PUBLISH COMPLETE                                       ║
echo  ║                                                          ║
echo  ║   Output folder : publish\                              ║
echo  ║   Executable    : publish\VitalTrack.exe                ║
echo  ║                                                          ║
echo  ║   IMPORTANT: Copy your .env file into publish\          ║
echo  ║   before distributing or running from that folder.      ║
echo  ║                                                          ║
echo  ║   Users need SQL Server but NOT .NET installed.         ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.

:: Automatically copy .env to publish folder
if exist ".env" (
    copy ".env" "publish\.env" >nul
    echo  [INFO] .env copied to publish\
)
if exist ".env.example" (
    copy ".env.example" "publish\.env.example" >nul
)

pause
exit /b 0
