@echo off
rem Double-click to publish a test build to GitHub Releases (becomes releases/latest).
rem Build folder is expected next to the repo: <repo>\..\APP_PENGUIN_SCRAMBLE_EXE
rem ASCII only: cmd.exe misparses UTF-8 Japanese in .bat files.

rem gh may be missing from PATH right after install until re-login
set "PATH=%PATH%;C:\Program Files\GitHub CLI"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release-windows.ps1" -BuildDir "%~dp0..\..\..\APP_PENGUIN_SCRAMBLE_EXE"

rem keep the window open so the result/URL can be read
pause
