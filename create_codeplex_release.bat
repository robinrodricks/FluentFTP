@echo off
setlocal

set haszip=0
set year=%DATE:~12,2%
set month=%DATE:~4,2%
set day=%DATE:~7,2%

if %month:~0,1% equ 0 set month=%month:~1,1%
if %day:~0,1% equ 0 set month=%day:~1,1%

set release=System.Net.FtpClient.%year%.%month%.%day%
set archive=%release%.zip

if exist "%release%" rd /s /q "%release%"
if exist "%archive%" del /q "%archive%"

md "%release%"
md "%release%\bin"
md "%release%\source"
md "%release%\examples"
md "%release%\help"

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