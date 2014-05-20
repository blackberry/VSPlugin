@echo off
setlocal

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set thisDir=%~dp0
set BuildResults="%thisDir%_BuildResults"

set MsBuild="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set MsBuild2013="C:\Program Files (x86)\MSBuild\12.0\Bin\MsBuild.exe"
set MsBuildCmd=%MsBuild% /property:Configuration=Release /target:Clean;Build
set MsBuild2013Cmd=%MsBuild2013% /property:Configuration=Release /target:Clean;Build

set SolutionPath2010="%thisDir%\src\VSNDK.sln"
set SolutionPath2012="%thisDir%\src_vs2012\VSNDK.sln"
set SolutionPath2013="%thisDir%\src_vs2013\VSNDK.sln"

echo Starting...
echo Output folder: %buildresults%

REM ********************************************************************************************
REM Clean up old bin folder
REM ********************************************************************************************
echo 1) Removing previous code...
if not exist "%BuildResults%" goto :create
  rmdir /Q "%BuildResults%" /S
  if errorlevel 1 ( exit /b %errorlevel% )
  
  mkdir "%BuildResults%"
  if errorlevel 1 ( exit /b %errorlevel% )
  
  echo 1) Cleanup - DONE
  goto :removed
:create
  mkdir "%BuildResults%"
  echo 1) Created empty folder - DONE
:removed

REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
echo 2) Building Solution for Visual Studio 2010
%MsBuildCmd% %SolutionPath2010% /p:OutputPath="%buildresults%\VS2010" > %buildresults%\VS2010_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo 2) Build - DONE

REM ********************************************************************************************
REM Build VS2012
REM ********************************************************************************************
echo 3) Building Solution for Visual Studio 2012
%MsBuildCmd% %SolutionPath2012% /p:OutputPath="%buildresults%\VS2012" /p:VisualStudioVersion=11.0  > %buildresults%\VS2012_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo 3) Build - DONE

REM ********************************************************************************************
REM Build VS2013
REM ********************************************************************************************
echo 4) Building Solution for Visual Studio 2013
%MsBuildCmd% %SolutionPath2013% /p:OutputPath="%buildresults%\VS2013" /p:VisualStudioVersion=12.0  > %buildresults%\VS2013_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo 4) Build - DONE

:EOF
endlocal
echo [ALL DONE]
