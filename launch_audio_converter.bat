@echo off
setlocal
cd /d "%~dp0"
if not defined WINDIR set "WINDIR=%SystemRoot%"

where dotnet >nul 2>&1
if not errorlevel 1 (
    dotnet --list-runtimes | findstr /b /c:"Microsoft.WindowsDesktop.App 8." >nul
    if not errorlevel 1 if exist "release\final\CodecTone.exe" (
        start "" /d "%~dp0release\final" "CodecTone.exe"
        exit /b 0
    )
)

if exist "release\final\CodecTone-Standalone.exe" (
    start "" /d "%~dp0release\final" "CodecTone-Standalone.exe"
    exit /b 0
)

echo No runnable CodecTone build was found.
echo Run build_executable.bat first.
pause
exit /b 1
