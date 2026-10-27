@echo off
rem Double-click to publish the Web build to GitHub Pages (penmon-scramble-web).
rem Build Web directly into the pages clone next to the repo: <repo>\..\penmon-scramble-web
rem ASCII only: cmd.exe misparses UTF-8 Japanese in .bat files.

rem gh may be missing from PATH right after install until re-login
set "PATH=%PATH%;C:\Program Files\GitHub CLI"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release-web.ps1"

rem keep the window open so the result/URL can be read
pause
