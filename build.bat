@echo off
setlocal

set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
set CONFIG=Release
set RELEASE_DIR=release

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
if exist %RELEASE_DIR%\NoSleep.exe del %RELEASE_DIR%\NoSleep.exe
goto end

:rebuild
echo Rebuilding solution in %CONFIG% mode...
%MSBUILD% .\Sources\NoSleep\NoSleep.csproj /t:Clean,Rebuild /p:Configuration=%CONFIG%
goto copy_files

:build
echo Building solution in %CONFIG% mode...
%MSBUILD% .\Sources\NoSleep\NoSleep.csproj /t:Build /p:Configuration=%CONFIG%
goto copy_files

:copy_files
if not exist %RELEASE_DIR% mkdir %RELEASE_DIR%
copy /Y "Sources\NoSleep\bin\%CONFIG%\NoSleep.exe" "%RELEASE_DIR%\"
echo Files copied to %RELEASE_DIR%
goto end

:end
if errorlevel 1 (
    echo Build failed!
    exit /b 1
) else (
    echo Build succeeded!
    exit /b 0
) 