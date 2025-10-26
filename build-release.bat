@echo off
REM Simple batch file wrapper for the PowerShell build script
REM Usage: build-release.bat [version]

set VERSION=%1
if "%VERSION%"=="" set VERSION=1.0.0

echo.
echo =====================================
echo   TypeBeat Release Builder
echo =====================================
echo.
echo Building version: %VERSION%
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0build-release.ps1" -Version "%VERSION%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Press any key to exit...
    pause >nul
    exit /b 1
)

echo.
echo Build completed successfully!
echo Press any key to exit...
pause >nul
