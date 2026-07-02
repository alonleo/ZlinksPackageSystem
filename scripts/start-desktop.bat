@echo off
echo Starting Zlinks Package System Desktop...
cd /d "%~dp0\desktop\ZlinksPackageSystem.Desktop"

echo Checking .NET version...
dotnet --version

echo Building and running desktop application...
dotnet run

pause