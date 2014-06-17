@echo off
setlocal EnableExtensions

REM ********************************************************************************************
REM Allow selective installation/uninstallation
REM ********************************************************************************************

:: Process parameters, to limit setup activities or directions
if "%~1" == "" (
  set ActionVS2010=1
  set ActionVS2012=1
  set ActionVS2013=1
  set ActionUninstall=0
  set ActionSkipTools=0
) else (
  set ActionVS2010=0
  set ActionVS2012=0
  set ActionVS2013=0
  set ActionUninstall=0
  set ActionSkipTools=0
  for %%a in (%*) do (
    if /i "%%a" == "/all"          set ActionVS2010=1 && set ActionVS2012=1 && set ActionVS2013=1
    if /i "%%a" == "/uninstall"    set ActionUninstall=1
    if /i "%%a" == "/u"            set ActionUninstall=1
    if /i "%%a" == "/delete"       set ActionUninstall=1
    if /i "%%a" == "/del"          set ActionUninstall=1
    if /i "%%a" == "/skip-tools"   set ActionSkipTools=1
    if /i "%%a" == "/no-tools"     set ActionSkipTools=1
    if /i "%%a" == "/notools"      set ActionSkipTools=1
    if /i "%%a" == "vs2010"        set ActionVS2010=1
    if /i "%%a" == "vs2012"        set ActionVS2012=1
    if /i "%%a" == "vs2013"        set ActionVS2013=1
  )
)

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set /A actionNo=1
set thisDir=%~dp0
set thisDir=%thisDir:~0,-1%
set BuildResults=%thisDir%\_BuildResults

set ProgFilesRoot=%ProgramFiles(x86)%
set SystemArch=x64
if "%ProgFilesRoot%" == "" set "ProgFilesRoot=%ProgramFiles%" && set SystemArch=x86

set AllUsersRoot=%ALLUSERSPROFILE%
set PluginRoot=%ProgFilesRoot%\BlackBerry\VSPlugin-NDK

if %ActionUninstall% neq 0 (echo Performing REMOVAL action...) else (echo Performing INSTALLATION action...)

REM ********************************************************************************************
REM Visual Studio 2010
REM ********************************************************************************************
if %ActionVS2010% equ 0 (goto skip_vs2010)

echo %actionNo%: Processing Setup for Visual Studio 2010
call :processSetup 2010 10.0
if errorlevel 1 ( exit /b %errorlevel% )
if %ActionUninstall% neq 0 (echo %actionNo%: Removal - DONE) else (echo %actionNo%: Installation - DONE)
set /a actionNo += 1

:skip_vs2010

REM ********************************************************************************************
REM Visual Studio 2012
REM ********************************************************************************************
if %ActionVS2012% equ 0 (goto skip_vs2012)

echo %actionNo%: Processing Setup for Visual Studio 2012
call :processSetup 2012 11.0 V110
if errorlevel 1 ( exit /b %errorlevel% )
if %ActionUninstall% neq 0 (echo %actionNo%: Removal - DONE) else (echo %actionNo%: Installation - DONE)
set /a actionNo += 1

:skip_vs2012

REM ********************************************************************************************
REM Visual Studio 2013
REM ********************************************************************************************
if %ActionVS2013% equ 0 (goto skip_vs2013)

echo %actionNo%: Processing Setup for Visual Studio 2013
call :processSetup 2013 12.0 V120
if errorlevel 1 ( exit /b %errorlevel% )
if %ActionUninstall% neq 0 (echo %actionNo%: Removal - DONE) else (echo %actionNo%: Installation - DONE)
set /a actionNo += 1

:skip_vs2013

REM ********************************************************************************************
REM Helper functions
REM ********************************************************************************************
goto EOF

REM ********************************************************************************************
REM Aggregate function to perform all actions needed to correctly deploy plugin
REM $1 - Visual Studio number (2012)
REM $2 - Visual Studio version (12.0)
REM $3 - Visual Studio selector (V120) # optional
:processSetup
setlocal EnableDelayedExpansion

set VSYear=%1
set VSVersion=%~2
set VSSelector=%~3
set BuildPath=%BuildResults%\VS%VSYear%
set VSPluginPath=%ProgFilesRoot%\Microsoft Visual Studio %VSVersion%\Common7\IDE\Extensions\NDK Plugin
set VSWizardsPath=%ProgFilesRoot%\Microsoft Visual Studio %VSVersion%\VC\VCWizards\CodeWiz

set MSBuildTargetPath=%ProgFilesRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms
if not "%VSSelector%" == "" set MSBuildTargetPath=%ProgFilesRoot%\MSBuild\Microsoft.Cpp\v4.0\%VSSelector%\Platforms

REM Skip installation of custom tools (in case upgrading only plugin during development)
if %ActionSkipTools% neq 0 goto skip_tools
call :processTools "%thisDir%" "%PluginRoot%" "%SystemDrive%"
set /a actionNo += 1
:skip_tools

call :processPlugin "%BuildPath%" "%PluginRoot%" "%VSPluginPath%" "%MSBuildTargetPath%" "%VSWizardsPath%"
set /a actionNo += 1

if %ActionUninstall% neq 0 (goto uninstall_reg)

REM Registering debug-ending and other support classes
echo %actionNo%: Registering debug-engine [%BuildResults%\setup_VS%VSYear%_install_%SystemArch%.reg]
REGEDIT.EXE /S "%BuildResults%\setup_VS%VSYear%_install_%SystemArch%.reg"
goto processSetup_End

:uninstall_reg
echo %actionNo%: Unregistering debug-engine [%BuildResults%\setup_VS%VSYear%_uninstall.reg]
REGEDIT.EXE /S "%BuildResults%\setup_VS%VSYear%_uninstall.reg"

:processSetup_End
endlocal
exit /b

REM ********************************************************************************************
REM Copy GDBParser and DebugEngine Files
REM ********************************************************************************************
REM $1 - from
REM $2 - to (plugin path)
REM $3 - to (Visual Studio path)
REM $4 - MSBuild Targets path
REM $5 - Wizards path
:processPlugin
setlocal EnableDelayedExpansion

set InputPath=%~1
set OutputPluginPath=%~2
set OutputVsPath=%~3
set OutputMsBuildTargetsPath=%~4
set OutputWizardsPath=%~5

if %ActionUninstall% neq 0 (goto uninstall_Plugin)

REM Create folders
echo %actionNo%: Installing plugin binaries
echo Make Directory "%OutputPluginPath%"
md "%OutputPluginPath%"
echo Make Directory "%OutputVsPath%"
md "%OutputVsPath%"

REM Install
echo Copy "%InputPath%\GDBParser.dll" to "%OutputVsPath%\GDBParser.dll"
copy "%InputPath%\GDBParser.dll" "%OutputVsPath%\GDBParser.dll" 
echo Copy "%InputPath%\GDBWrapper.exe" to "%OutputPluginPath%\GDBWrapper.exe"
copy "%InputPath%\GDBWrapper.exe" "%OutputPluginPath%\GDBWrapper.exe" 
echo Copy "%InputPath%\Instructions.txt" to "%OutputPluginPath%\Instructions.txt"
copy "%InputPath%\Instructions.txt" "%OutputPluginPath%\Instructions.txt" 
echo Copy "%InputPath%\BlackBerry.DebugEngine.dll" to "%OutputPluginPath%\BlackBerry.DebugEngine.dll"
copy "%InputPath%\BlackBerry.DebugEngine.dll" "%OutputPluginPath%\BlackBerry.DebugEngine.dll" 

REM Install Package Files
echo "%InputPath%\extension.vsixmanifest" to "%OutputVsPath%\extension.vsixmanifest"
copy "%InputPath%\extension.vsixmanifest" "%OutputVsPath%\extension.vsixmanifest" 
echo "%InputPath%\BlackBerry.NativeCore.dll" to "%OutputVsPath%\BlackBerry.NativeCore.dll"
copy "%InputPath%\BlackBerry.NativeCore.dll" "%OutputVsPath%\BlackBerry.NativeCore.dll"
echo "%InputPath%\BlackBerry.Package.dll" to "%OutputVsPath%\BlackBerry.Package.dll"
copy "%InputPath%\BlackBerry.Package.dll" "%OutputVsPath%\BlackBerry.Package.dll"
echo "%InputPath%\BlackBerry.Package.pkgdef" to "%OutputVsPath%\BlackBerry.Package.pkgdef"
copy "%InputPath%\BlackBerry.Package.pkgdef" "%OutputVsPath%\BlackBerry.Package.pkgdef" 

REM MSBuild Files
echo Copy BlackBerry MSBuild directory [%OutputMsBuildTargetsPath%]
xcopy "%InputPath%\BlackBerry" "%OutputMsBuildTargetsPath%\BlackBerry" /e /i /y
copy "%InputPath%\BlackBerry.BuildTasks.dll" "%OutputMsBuildTargetsPath%\BlackBerry\BlackBerry.BuildTasks.dll"
echo Copy BlackBerrySimulator MSBuild directory [%OutputMsBuildTargetsPath%]
xcopy "%InputPath%\BlackBerrySimulator" "%OutputMsBuildTargetsPath%\BlackBerrySimulator" /e /i /y
copy "%InputPath%\BlackBerry.BuildTasks.dll" "%OutputMsBuildTargetsPath%\BlackBerrySimulator\BlackBerry.BuildTasks.dll"

REM Templates
echo Copy BlackBerry VCWizards directory
xcopy "%InputPath%\Templates\VCWizards" "%OutputWizardsPath%" /e /i /y


goto processPlugin_End

:uninstall_Plugin

REM Uninstall
echo %actionNo%: Removing plugin binaries
echo Deleting "%OutputVsPath%\GDBParser.dll"
del "%OutputVsPath%\GDBParser.dll" 
echo Deleting "%OutputPluginPath%\GDBWrapper.exe"
del  "%OutputPluginPath%\GDBWrapper.exe" 
echo Deleting "%OutputPluginPath%\Instructions.txt"
del "%OutputPluginPath%\Instructions.txt" 
echo Deleting "%OutputPluginPath%\BlackBerry.DebugEngine.dll"
del "%OutputPluginPath%\BlackBerry.DebugEngine.dll" 

REM Uninstall Package Files
echo Deleting "%OutputVsPath%\extension.vsixmanifest"
del "%OutputVsPath%\extension.vsixmanifest" 
echo Deleting  "%OutputVsPath%\BlackBerry.NativeCore.dll"
del "%OutputVsPath%\BlackBerry.NativeCore.dll"
echo Deleting  "%OutputVsPath%\BlackBerry.Package.dll"
del "%OutputVsPath%\BlackBerry.Package.dll"
echo Deleting  "%OutputVsPath%\BlackBerry.Package.pkgdef"
del "%OutputVsPath%\BlackBerry.Package.pkgdef" 

REM Remove folders
echo Remove Directory "%OutputPluginPath%"
rd "%OutputPluginPath%"
echo Remove Directory "%OutputVsPath%"
rd "%OutputVsPath%"

REM Remove MSBuild Files
echo Delete BlackBerry MSBuild directory
rd "%OutputMsBuildTargetsPath%\BlackBerry" /s /q
echo Delete BlackBerrySimulator MSBuild directory
rd "%OutputMsBuildTargetsPath%\BlackBerrySimulator" /s /q

REM Remove Templates
echo Delete BlackBerry VCWizards directory
rd "%OutputWizardsPath%\BlackBerry" /s /q 

:processPlugin_End
endlocal
exit /b

REM ********************************************************************************************
REM NDK & QNX toolset
REM ********************************************************************************************
REM $1 - from
REM $2 - to (plugin path)
REM $3 - NDK path
:processTools
setlocal EnableDelayedExpansion

set InputPath=%~1
set OutputPath=%~2
set OutputNdkPath=%~3

if %ActionUninstall% neq 0 (goto uninstall_Tools)

REM Install
echo %actionNo%: Installing toolset
echo xcopy "%InputPath%\bbndk_vs" "%OutputNdkPath%\bbndk_vs" /e /i /y
md "%OutputNdkPath%\bbndk_vs"
xcopy "%InputPath%\bbndk_vs" "%OutputNdkPath%\bbndk_vs" /e /i /y

echo xcopy "%InputPath%\qnxtools" "%OutputPath%\qnxtools" /e /i /y
md "%OutputPath%\qnxtools"
xcopy "%InputPath%\qnxtools" "%OutputPath%\qnxtools" /e /i /y
goto processTools_End

:uninstall_Tools

REM Remove
echo %actionNo%: Removing toolset
echo Delete "%OutputNdkPath%\bbndk_vs" directory
rd "%OutputNdkPath%\bbndk_vs" /s /q
echo Delete "%OutputPath%\qnxtools" directory
rd "%OutputPath%\qnxtools" /s /q

:processTools_End
endlocal
exit /b

REM ********************************************************************************************
REM END
REM ********************************************************************************************

:EOF
endlocal
echo [ALL DONE]
