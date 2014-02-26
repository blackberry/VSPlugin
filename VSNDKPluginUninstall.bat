echo off

REM     //********************************************************************************************
REM     // Batch Constants 
REM     // thisDir = working directory of current batch file.
REM     // buildresults = Directory containing the build results.
REM     **********************************************************************************************
set thisDir=%~dp0
set buildresults=%thisDir%..\buildresults
set ProgRoot=%ProgramFiles(x86)%
set AllUsers=%ALLUSERSPROFILE%
set AppData=%APPDATA%
set drive=%cd:~0,3%

if "%ProgramFiles(x86)%XXX"=="XXX" (
echo 32-bit
set ProgRoot=%ProgramFiles%
) 
echo %ProgRoot%

REM     //********************************************************************************************
REM     // Remove AddIn Files
REM     **********************************************************************************************
echo Deleting "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn" 
del "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn" 
echo Deleting "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll" 
del "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll" 

REM     //********************************************************************************************
REM     // Remove GDBParser and DebugEngine Files
REM     **********************************************************************************************
echo Deleting  "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBParser.dll"
del "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBParser.dll" 
echo Deleting "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBWrapper.exe"
del  "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBWrapper.exe" 
echo Deleting "%ProgRoot%\BlackBerry\VSPlugin-NDK\Instructions.txt"
del "%ProgRoot%\BlackBerry\VSPlugin-NDK\Instructions.txt" 
echo Deleting "%ProgRoot%\BlackBerry\VSPlugin-NDK\VSNDK.DebugEngine.dll"
del "%ProgRoot%\BlackBerry\VSPlugin-NDK\VSNDK.DebugEngine.dll" 
echo Remove Directory "%ProgRoot%\BlackBerry\VSPlugin-NDK"
rd "%ProgRoot%\BlackBerry\VSPlugin-NDK"

REM     //********************************************************************************************
REM     // Remove Package Files
REM     **********************************************************************************************
echo Deleting "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\extension.vsixmanifest"
del "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\extension.manifest" 
echo Deleting  "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
del "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
echo Deleting  "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef"
del "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef" 
echo Remove Directory "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin"
rd "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin"


REM     //********************************************************************************************
REM     // Remove MSBuild Files
REM     **********************************************************************************************
echo Delete BlackBerry MSBuild directory
rd "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry" /s /q
echo Delete BlackBerrySimulator MSBuild directory
rd "%buildresults%\BlackBerrySimulator" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator" /s /q

REM     //********************************************************************************************
REM     // Remove Template Files
REM     **********************************************************************************************
echo Delete BlackBerry VCWizards directory
rd "%ProgRoot%\Microsoft Visual Studio 10.0\VC\VCWizards\CodeWiz\BlackBerry" /s /q 

REM     //********************************************************************************************
REM     // Remove command line tools
REM     **********************************************************************************************
echo Delete %drive%bbndk_vs directory
rd "%drive%bbndk_vs" /s /q

REM     //********************************************************************************************
REM     // Remove qnx tools
REM     **********************************************************************************************
echo Delete "%ProgRoot%\BlackBerry\VSPlugin-NDK\qnxtools" directory
rd "%ProgRoot%\BlackBerry\VSPlugin-NDK\qnxtools" /s /q

REM     //********************************************************************************************
REM     // Register clases
REM     **********************************************************************************************
echo Unregistering - %thisDir%VSNDKPluginUninstall.reg
REGEDIT.EXE -S %thisDir%VSNDKPluginUninstall.reg



















