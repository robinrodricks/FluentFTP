@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set packages=NuGetPackages
set snk=..\..\..\Dropbox\Documents\System.Net.FtpClient-SNK\System.Net.FtpClient.snk

if not exist "%packages%" md "%packages%"

rd /q /s System.Net.FtpClient\bin
rd /q /s Sandcastle\Help

:: Build dll without strong name
rem %msbuild% /p:Configuration=Release System.Net.FtpClient\System.Net.FtpClient.csproj

:: Build signed DLL
%msbuild% /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile="%snk%" System.Net.FtpClient\System.Net.FtpClient.csproj

:: Build API Reference
cd Sandcastle
if exist CHM rd /q /s CHM
if exist HTML rd /q /s HTML
%msbuild% /p:Configuration=Release "API_Reference_CHM.shfbproj"
%msbuild% /p:Configuration=Release "API_Reference_HTML.shfbproj"
if exist CHM/LastBuild.log del /q /s CHM/LastBuild.log
if exist HTML/LastBuild.log del /q /s HTML/LastBuild.log
cd ..

nuget pack System.Net.FtpClient\System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory "%packages%"
pause