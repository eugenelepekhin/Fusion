@echo off

if "%VSCMD_VER%" == "" (
	echo This .CMD file must be run from "Developer Command Prompt for VS 2022" or later
	exit /B
)

@echo on

msbuild -r -p:Configuration=Release Tools\ResourceWrapper.Generator\ResourceWrapper.Generator.csproj
@if "%ERRORLEVEL%" NEQ "0" (exit /B)

dotnet publish -c Release Fusion\Fusion.csproj
@if "%ERRORLEVEL%" NEQ "0" (exit /B)

msbuild -p:Configuration=Release -t:ZipResult Fusion
@if "%ERRORLEVEL%" NEQ "0" (exit /B)
