@echo off
title AetherDesk - Public Internet Relay Launcher
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Public-Relay.ps1"
pause
