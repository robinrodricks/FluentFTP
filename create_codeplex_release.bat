@echo off
setlocal

set msbuild=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
set year=%DATE:~12,2%
set month=%DATE:~4,2%
set day=%DATE:~7,2%
set release=FluentFTP.%year%.%month%.%day%
set archive=%release%.zip

if exist "%release%" rd /s /q "%release%"
if exist "%archive%" del /q "%archive%"

md "%release%"
md "%release%\bin"
md "%release%\source"
md "%release%\source\Extensions"
md "%release%\examples"
md "%release%\help"

rd /q /s FluentFTP\bin

%msbuild% /p:Configuration=Debug "FluentFTP\FluentFTP.csproj"
%msbuild% /p:Configuration=Release "FluentFTP\FluentFTP.csproj"

xcopy /s "FluentFTP\bin" "%release%\bin\"
xcopy "FluentFTP\*.cs" "%release%\source\"
xcopy "FluentFTP\Extensions\*.cs" "%release%\source\Extensions\"
xcopy "FluentFTP\*.csproj" "%release%\source\"
xcopy "Examples\*.cs" "%release%\examples\"
xcopy "Examples\*.csproj" "%release%\examples\"
xcopy "Sandcastle\README.txt" "%release%\help\"
xcopy "Sandcastle\CHM\FluentFTP.chm" "%release%\help\"
xcopy /s "Sandcastle\HTML\*" "%release%\help\html\"
xcopy "LICENSE.TXT" "%release%\"

cd "%release%" 
"C:\Program Files\7-Zip\7z.exe" a -tzip "..\%archive%" *
cd ..
rd /s /q "%release%"

pause