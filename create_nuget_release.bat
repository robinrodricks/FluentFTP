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

:: Build API Reference/CHM
cd Sandcastle
%msbuild% /p:Configuration=Release "API Reference.shfbproj"
cd ..

nuget pack System.Net.FtpClient\System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory "%packages%"
pause