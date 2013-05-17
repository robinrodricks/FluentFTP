@echo off
setlocal

set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set year=%DATE:~12,2%
set month=%DATE:~4,2%
set day=%DATE:~7,2%
set release=System.Net.FtpClient.%year%.%month%.%day%
set archive=%release%.zip

if exist "%release%" rd /s /q "%release%"
if exist "%archive%" del /q "%archive%"

md "%release%"
md "%release%\bin"
md "%release%\source"
md "%release%\examples"
md "%release%\help"

rd /q /s System.Net.FtpClient\bin

%msbuild% /p:Configuration=Debug System.Net.FtpClient\System.Net.FtpClient.csproj
%msbuild% /p:Configuration=Release System.Net.FtpClient\System.Net.FtpClient.csproj

xcopy /s "System.Net.FtpClient\bin" "%release%\bin\"
xcopy "System.Net.FtpClient\*.cs" "%release%\source\"
xcopy "System.Net.FtpClient\*.csproj" "%release%\source\"
xcopy "Examples\*.cs" "%release%\examples\"
xcopy "Examples\*.csproj" "%release%\examples\"
xcopy "Sandcastle\Help\System.Net.FtpClient.chm" "%release%\help\"
xcopy "LICENSE.TXT" "%release%\"

cd "%release%" 
7za.exe a -tzip "..\%archive%" *
cd ..
rd /s /q "%release%"

pause