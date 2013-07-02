@echo off
setlocal
set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

if exist System.Net.FtpClient\bin rd /q /s System.Net.FtpClient\bin
%msbuild% /p:Configuration=Release System.Net.FtpClient\System.Net.FtpClient.csproj

cd Sandcastle
if exist CHM rd /q /s CHM
if exist HTML rd /q /s HTML
%msbuild% /p:Configuration=Release "API_Reference_CHM.shfbproj"
%msbuild% /p:Configuration=Release "API_Reference_HTML.shfbproj"
if exist CHM/LastBuild.log del /q /s CHM/LastBuild.log
if exist HTML/LastBuild.log del /q /s HTML/LastBuild.log
cd ..
pause