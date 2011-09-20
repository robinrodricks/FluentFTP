@echo off

set NET20PATH="%WINDIR%\Microsoft.NET\Framework\v2.0.50727"
set NET40PATH="%WINDIR%\Microsoft.NET\Framework\v4.0.30319"

if exist "%NET40PATH%" (
	set PATH=%PATH%;%NET40PATH%
) else (
	if exist "%NET20PATH%" (
		set PATH=%PATH%;%NET20PATH%
	) else (
		echo Could not locate a supported version of the framework.
		exit /b 1
	)
)

echo *** Removing old binaries...
if exist "bin" (
	rd /s /q "bin"
)
echo\

echo *** Building .NET4 Library
echo\

if exist "obj" (
	rd /s /q "obj"
)

msbuild System.Net.FtpClient.NET4.csproj /t:Rebuild /p:Configuration=Release

echo\
echo *** Building .NET2 Library
echo\
if exist "obj" (
	rd /s /q "obj"
)

msbuild System.Net.FtpClient.NET2.csproj /t:Rebuild /p:Configuration=Release

pause



