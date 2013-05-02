echo off

REM     //********************************************************************************************
REM     // Batch Constants 
REM     // thisDir = working directory of current batch file.
REM     // dotNetFrameworkPath = path to the DotNet Framework libraries
REM     // buildLog = build results log file.
REM     // solutionPath = path to MDS.NET solution.
REM     **********************************************************************************************
Set thisDir=%~dp0
Set buildresults="%thisDir%\..\buildresults"
Set buildLog=%thisDir%\..\VDNDKPlugin_results.log
Set dotNetFrameworkPath="C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
Set solutionPath="%thisDir%\src\VSNDK.sln"
Set regAsmPath=C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727

REM     //********************************************************************************************
REM     // Clean up old bin folder
REM     **********************************************************************************************
echo Removing previous code
if not exist "%buildresults%" goto build
  echo Remove Build Results
  RMDIR /Q "%buildresults%" /S
:build  

REM     //********************************************************************************************
REM     // Build the .NET Solution 2008 and register files in GAC
REM     **********************************************************************************************
echo Building Solution for VS2010
%dotNetFrameworkPath% %solutionPath% /property:Configuration=Release /target:Clean;Build /p:OutputPath="%buildresults%" > %buildLog%
echo Build Success


