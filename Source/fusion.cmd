@echo off
set location=%~dp0
dotnet "%location%Fusion.dll" "%~f1" "%~f2"
