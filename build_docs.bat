@echo off
setlocal
set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

if exist System.Net.FtpClient\bin rd /q /s System.Net.FtpClient\bin
%msbuild% /p:Configuration=Release System.Net.FtpClient\System.Net.FtpClient.csproj

cd Sandcastle
if exist Help rd /q /s Help
%msbuild% /p:Configuration=Release "API Reference.shfbproj"
cd ..
pause