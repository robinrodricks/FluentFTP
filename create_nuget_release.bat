@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set packages=NuGetPackages
set snk=%USERPROFILE%\Dropbox\Documents\FluentFTP-SNK\FluentFTP.snk

if not exist "%packages%" md "%packages%"

rd /q /s FluentFTP\bin

:: Build dll without strong name
rem %msbuild% /p:Configuration=Release FluentFTP\FluentFTP.csproj

:: Build signed DLL
%msbuild% /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="%snk%" FluentFTP\FluentFTP.csproj

nuget pack FluentFTP\FluentFTP.csproj -Prop Configuration=Release -OutputDirectory "%packages%"
pause