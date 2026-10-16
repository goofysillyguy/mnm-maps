@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
set "MAP_INSTALL_RESULT=%ERRORLEVEL%"
echo.
pause
exit /b %MAP_INSTALL_RESULT%
