@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set nuget=C:\Tools\Nuget\nuget.exe

rd /q /s FluentFTP\bin

:: Build dll without strong name
%msbuild% /p:Configuration=Release FluentFTP\FluentFTP.csproj

:: Build signed DLL
rem %msbuild% /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="%snk%" FluentFTP\FluentFTP.csproj

%nuget% pack FluentFTP\FluentFTP.csproj -Prop Configuration=Release -OutputDirectory FluentFTP\nuget\
pause