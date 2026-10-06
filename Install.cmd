@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "%~dp0Easy-Setup.ps1" -InitialGameRoot "%~dp0."
if errorlevel 1 pause
