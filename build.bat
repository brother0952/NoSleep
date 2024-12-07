@echo off
setlocal

set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
set CONFIG=Release

if "%1"=="debug" (
    set CONFIG=Debug
    shift
)

if "%1"=="clean" goto clean
if "%1"=="rebuild" goto rebuild
goto build

:clean
echo Cleaning solution...
%MSBUILD% .\Sources\NoSleep\NoSleep.csproj /t:Clean /p:Configuration=%CONFIG%
goto end

:rebuild
echo Rebuilding solution in %CONFIG% mode...
%MSBUILD% .\Sources\NoSleep\NoSleep.csproj /t:Clean,Rebuild /p:Configuration=%CONFIG%
goto end

:build
echo Building solution in %CONFIG% mode...
%MSBUILD% .\Sources\NoSleep\NoSleep.csproj /t:Build /p:Configuration=%CONFIG%

:end
if errorlevel 1 (
    echo Build failed!
    exit /b 1
) else (
    echo Build succeeded!
    exit /b 0
) 