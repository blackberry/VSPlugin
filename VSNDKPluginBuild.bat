@echo off
setlocal enableextensions

REM ********************************************************************************************
REM Allow selective build
REM ********************************************************************************************

:: Process parameters, to limit the solutions to build
if "%~1" == "" (
  set ActionClean=1
  set ActionBuildVS2010=1
  set ActionBuildVS2012=1
  set ActionBuildVS2013=1
) else (
  set ActionClean=1
  set ActionBuildVS2010=0
  set ActionBuildVS2012=0
  set ActionBuildVS2013=0
  for %%a in (%*) do (
    if /i "%%a" == "/ActionNoClean"  set ActionClean=0
    if /i "%%a" == "/no-clean" set ActionClean=0
    if /i "%%a" == "vs2010"    set ActionBuildVS2010=1
    if /i "%%a" == "vs2012"    set ActionBuildVS2012=1
    if /i "%%a" == "vs2013"    set ActionBuildVS2013=1
  )
)

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set /A actionNo=1
set thisDir=%~dp0
set BuildResults="%thisDir%_BuildResults"

set MsBuild="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set MsBuild2013="C:\Program Files (x86)\MSBuild\12.0\Bin\MsBuild.exe"
set MsBuildCmd=%MsBuild% /property:Configuration=Release /target:Rebuild
set MsBuild2013Cmd=%MsBuild2013% /property:Configuration=Release /target:Rebuild

set SolutionPath2010="%thisDir%\src\VSNDK.sln"
set SolutionPath2012="%thisDir%\src_vs2012\VSNDK.sln"
set SolutionPath2013="%thisDir%\src_vs2013\VSNDK.sln"

echo Starting...
echo Output folder: %BuildResults%

REM ********************************************************************************************
REM Clean up old bin folder
REM ********************************************************************************************
if %ActionClean% equ 0 (goto skip_clean)

echo %actionNo%: Removing previous code...
if not exist "%BuildResults%" (
  mkdir "%BuildResults%"
  echo %actionNo%: Created empty folder - DONE

) else (
  rmdir /Q "%BuildResults%" /S
  if errorlevel 1 ( exit /b %errorlevel% )
  
  mkdir "%BuildResults%"
  if errorlevel 1 ( exit /b %errorlevel% )
  echo %actionNo%: Cleanup - DONE
)
set /a actionNo += 1

:skip_clean
 
REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
if %ActionBuildVS2010% equ 0 (goto skip_vs2010)

echo %actionNo%: Building Solution for Visual Studio 2010
%MsBuildCmd% %SolutionPath2010% /p:OutputPath="%BuildResults%\VS2010" > %BuildResults%\VS2010_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2010

REM ********************************************************************************************
REM Build VS2012
REM ********************************************************************************************
if %ActionBuildVS2012% equ 0 (goto skip_vs2012)

echo %actionNo%: Building Solution for Visual Studio 2012
%MsBuildCmd% %SolutionPath2012% /p:OutputPath="%BuildResults%\VS2012" /p:VisualStudioVersion=11.0  > %BuildResults%\VS2012_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2012

REM ********************************************************************************************
REM Build VS2013
REM ********************************************************************************************
if %ActionBuildVS2013% equ 0 (goto skip_vs2013)

echo %actionNo%: Building Solution for Visual Studio 2013
%MsBuild2013Cmd% %SolutionPath2013% /p:OutputPath="%BuildResults%\VS2013" /p:VisualStudioVersion=12.0  > %BuildResults%\VS2013_buildlog.txt
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2013

REM ********************************************************************************************
REM DONE
REM ********************************************************************************************

:EOF
endlocal
echo [ALL DONE]
