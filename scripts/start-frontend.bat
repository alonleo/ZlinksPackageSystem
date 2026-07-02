@echo off
echo Starting Zlinks Package System Frontend...
cd /d "%~dp0\frontend"

echo Checking Bun version...
bun --version

echo Installing dependencies...
bun install

echo Starting development server...
bun run dev

pause