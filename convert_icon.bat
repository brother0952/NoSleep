@echo off
setlocal

:: 设置 .NET Framework 路径
set "NETFRAMEWORK=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319"
set "CSC=%NETFRAMEWORK%\csc.exe"

if not exist "%CSC%" (
    echo Error: C# compiler not found at %CSC%
    echo Please ensure .NET Framework is installed.
    exit /b 1
)

echo Compiling ConvertToIco.cs...
"%CSC%" /reference:System.Drawing.dll /out:ConvertToIco.exe ConvertToIco.cs

if errorlevel 1 (
    echo Failed to compile ConvertToIco.cs
    exit /b 1
)

echo Running icon converter...
ConvertToIco.exe

if errorlevel 1 (
    echo Failed to convert icon
    exit /b 1
)

echo Cleaning up...
del ConvertToIco.exe

echo Done! 