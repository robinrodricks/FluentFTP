@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set nuget=C:\Tools\Nuget\nuget.exe

REM rd /q /s FluentFTP\bin

:: clear all PDB files
cd FluentFTP\bin
del /s *.pdb
cd ../..

:: Build dll without strong name
REM %msbuild% /p:Configuration=Release FluentFTP\FluentFTP.csproj

:: Build signed DLL
rem %msbuild% /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="%snk%" FluentFTP\FluentFTP.csproj

:: PACK IN NUGET
:: %nuget% pack FluentFTP\FluentFTP.csproj -Prop Configuration=Release -OutputDirectory FluentFTP\nuget\
%nuget% pack FluentFTP\FluentFTP.nuspec -OutputDirectory FluentFTP\nuget\
pause