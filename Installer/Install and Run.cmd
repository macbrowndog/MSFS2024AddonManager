@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-MSFS2024AddonManager.ps1"
if errorlevel 1 (
    echo.
    echo Installation or startup did not complete.
    pause
)
endlocal
