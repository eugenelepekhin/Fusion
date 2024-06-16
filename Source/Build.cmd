@echo off

if "%VSCMD_VER%" == "" (
	echo This .CMD file must be run from "Developer Command Prompt for VS 2022" or later
	exit /B
)

@echo on

msbuild -r -p:Configuration=Release Fusion.sln
rem @if "%ERRORLEVEL%" NEQ "0" (exit /B)
