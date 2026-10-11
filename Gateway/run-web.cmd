@echo off
cd /d "%~dp0"
if not exist node_modules\ws\package.json call npm.cmd ci --no-audit --no-fund
if errorlevel 1 exit /b 1
node server.cjs
