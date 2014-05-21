@echo off
setlocal enableextensions

REM ********************************************************************************************
REM Allow selective build
REM ********************************************************************************************

:: Process parameters, to limit the solutions to build
if "%~1" == "" (
  set ActionClean=1
  set ActionGenScripts=1
  set ActionBuildVS2010=1
  set ActionBuildVS2012=1
  set ActionBuildVS2013=1
) else (
  set ActionClean=1
  set ActionGenScripts=0
  set ActionBuildVS2010=0
  set ActionBuildVS2012=0
  set ActionBuildVS2013=0
  for %%a in (%*) do (
    if /i "%%a" == "/noclean"    set ActionClean=0
    if /i "%%a" == "/no-clean"   set ActionClean=0
    if /i "%%a" == "/scripts"    set ActionGenScripts=1
    if /i "%%a" == "/noscripts"  set ActionGenScripts=0
    if /i "%%a" == "/no-scripts" set ActionGenScripts=0
    if /i "%%a" == "vs2010"      set ActionBuildVS2010=1 && set ActionGenScripts=1
    if /i "%%a" == "vs2012"      set ActionBuildVS2012=1 && set ActionGenScripts=1
    if /i "%%a" == "vs2013"      set ActionBuildVS2013=1 && set ActionGenScripts=1
  )
)

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set /A actionNo=1
set thisDir=%~dp0
set BuildResults="%thisDir%_BuildResults"

set ProgFilesRoot=%ProgramFiles(x86)%
if "%ProgFilesRoot%" == "" set ProgFilesRoot=%ProgramFiles%

set MsBuild="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set MsBuild2013="%ProgFilesRoot%\MSBuild\12.0\Bin\MsBuild.exe"
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
REM Create Installation/Uninstallation Registry Scripts
REM ********************************************************************************************
if %ActionGenScripts% equ 0 (goto skip_scripts)

echo %actionNo%: Creating installation scripts
call :processTemplates 2010 10.0
call :processTemplates 2012 11.0
call :processTemplates 2013 12.0

echo %actionNo%: Writing - DONE
set /a actionNo += 1

:skip_scripts

REM ********************************************************************************************
REM Helper functions
REM ********************************************************************************************
goto EOF

REM ********************************************************************************************
REM Generates a set of templates for specified Visual Studio version
REM $1 - Visual Studio number (2010)
REM $2 - Visual Studio version (10.0)
:processTemplates
setlocal EnableDelayedExpansion

set InstallTemplate="%thisDir%setup_install.template"
set UninstallTemplate="%thisDir%setup_uninstall.template"

set VSYear=%~1
set VSVersion=%~2
set PluginRegistryNodeName=BlackBerryVSPlugin
set PluginPathX86="C:\Program Files\BlackBerry\VSPlugin-NDK"
set PluginPathX64="C:\Program Files (x86)\BlackBerry\VSPlugin-NDK"

set InstallOutputFileX86="%BuildResults%\setup_VS%VSYear%_install_x86.reg"
set InstallOutputFileX64="%BuildResults%\setup_VS%VSYear%_install_x64.reg"
set UninstallOutputFile="%BuildResults%\setup_VS%VSYear%_uninstall.reg"

call :processTemplate %InstallTemplate% %InstallOutputFileX86% %VSYear% %VSVersion% %PluginRegistryNodeName% %PluginPathX86%
call :processTemplate %InstallTemplate% %InstallOutputFileX64% %VSYear% %VSVersion% %PluginRegistryNodeName% %PluginPathX64%

call :processTemplate %UninstallTemplate% %UninstallOutputFile% %VSYear% %VSVersion% %PluginRegistryNodeName% ""

endlocal
exit /b

REM ********************************************************************************************
REM Generates single file from specified template
REM $1 - input template path
REM $2 - output name
REM $3 - Visual Studio number (2010)
REM $4 - Visual Studio version (10.0)
REM $5 - registry node name of the plugin (BlackBerryVSPlugin)
REM $6 - plugin installation directory (C:\Program Files (x86)\BlackBerry\VSPlugin-NDK)
:processTemplate
setlocal EnableDelayedExpansion

set InputFile=%~1
set OutputFile=%~2
set OutputShortFileName=%~n2%~x2
set VSYear=%~3
set VSVersion=%~4
set PluginRegistryNodeName=%~5
set PluginPath=%~6

REM Tweak a bit the path, to be better consumed by delayed evaluation
REM and printed correctly into output (preserve parenthesis and double path-chars)
set PluginPath=!PluginPath:\=\\!
set PluginPath=!PluginPath:(=^^(!
set PluginPath=!PluginPath:)=^^)!

REM Create empty file
type nul > %OutputFile%

for /f "tokens=* delims=" %%l in (%InputFile%) do (
  set line=%%l

  REM Inject empty line, so the sections are easier visible
  set first=!line:~0,1!
  if "!first!" == "[" echo.>> %OutputFile%
  
  REM Replace markers within the template
  set line=!line:#VSVersion#=%VSVersion%!
  set line=!line:#VSYear#=%VSYear%!
  set line=!line:#PluginRegistryNodeName#=%PluginRegistryNodeName%!
  set line=!line:#PluginPath#=%PluginPath%!
  echo.!line! >> %OutputFile%
)
echo.>> %OutputFile%
echo Completed template "%OutputShortFileName%"

endlocal
exit /b

REM ********************************************************************************************
REM DONE
REM ********************************************************************************************

:EOF
endlocal
echo [ALL DONE]
