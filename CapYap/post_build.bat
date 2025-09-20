@echo off
SET workindir=%~2
IF %workindir:~-1%==\ SET workindir=%workindir:~0,-1%\publish
echo %1%
echo %2%-1%publish
echo %3%
echo %4%
echo %5%
"C:\Program Files (x86)\NSIS\makensis.exe" /DICONFILE="%1" /DWORKINGDIR="%workindir%" /DAPP_VERSION="%4" /DPLATFORM="%5" "%3"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%