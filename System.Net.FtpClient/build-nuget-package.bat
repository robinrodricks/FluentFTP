@echo off
if not exist "packages" md packages
nuget pack System.Net.FtpClient.csproj -Prop Configuration=Release -OutputDirectory packages
pause