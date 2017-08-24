SET PARAM=/clp:parameters:ErrorsOnly
SET MSBUILD=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe 
SET MSBUILD2017=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe 


%MSBUILD% /v:m msbuild_VS2012.proj

%MSBUILD2017% /v:m msbuild_VS2017.proj