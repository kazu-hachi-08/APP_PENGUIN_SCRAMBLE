@echo off
rem Double-click to check for a new version on GitHub Releases, update this folder, then start the game.
rem ASCII only: cmd.exe misparses UTF-8 Japanese in .bat files.
rem Keep powershell as the last line: cmd reads .bat files line by line, and update.ps1 may overwrite this file.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0update.ps1"
