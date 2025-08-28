@echo off
SET workindir=%~2
echo %workindir%
IF %workindir:~-1%==\ SET workindir=%workindir:~0,-1%
echo %workindir%
"C:\Program Files (x86)\NSIS\makensis.exe" /DICONFILE="%1" /DWORKINGDIR="%workindir%" "%3"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%