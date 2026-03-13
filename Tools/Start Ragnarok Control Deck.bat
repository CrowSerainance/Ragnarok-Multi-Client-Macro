@echo off
setlocal
set "ROOT=%~dp0"
set "APP_DIR=%ROOT%Publish\win-x86"
set "APP_EXE=%APP_DIR%\RagnarokControlDeck.exe"

if not exist "%APP_EXE%" (
    echo Ragnarok Control Deck is not published yet.
    echo Run:
    echo powershell -ExecutionPolicy Bypass -File "%ROOT%publish-release.ps1"
    exit /b 1
)

start "" "%APP_EXE%"
