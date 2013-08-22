echo off

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set thisDir=%~dp0
set buildresults="%thisDir%\..\buildresults"
set dotNetFrameworkPath="C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set solutionPath2010="%thisDir%\src\VSNDK.sln"
set solutionPath2012="%thisDir%\src_vs2012\VSNDK.sln"

REM ********************************************************************************************
REM Clean up old bin folder
REM ********************************************************************************************
echo Removing previous code
if not exist "%buildresults%" goto build
  echo Remove Build Results
  RMDIR /Q "%buildresults%" /S
:build  

REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
echo Building Solution for VS2010
%dotNetFrameworkPath% %solutionPath2010% /property:Configuration=Release /target:Clean;Build /p:OutputPath="%buildresults%\VS2010" > %buildresult%\VS2010_buildlog.txt
echo Build Success

REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
echo Building Solution for VS2012
%dotNetFrameworkPath% %solutionPath2012% /property:Configuration=Release /target:Clean;Build /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
echo Build Success


