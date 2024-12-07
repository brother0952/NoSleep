@echo off
setlocal

set EXE_PATH=Sources\NoSleep\bin\Release\NoSleep.exe

if not exist %EXE_PATH% (
    echo NoSleep.exe not found in Release folder.
    echo Please build the project in Release mode first using: build.bat
    exit /b 1
)

echo Starting NoSleep...
start "" "%EXE_PATH%" 