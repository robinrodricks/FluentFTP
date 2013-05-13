@echo off

set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set packages=NuGetPackages

if not exist "%packages%" md "%packages%"

%msbuild% /p:Configuration=Release System.Net.FtpClient\System.Net.FtpClient.csproj

nuget pack System.Net.FtpClient\System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory "%packages%"
pause