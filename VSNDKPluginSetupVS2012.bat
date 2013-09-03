echo off

REM     //********************************************************************************************
REM     // Batch Constants 
REM     // thisDir = working directory of current batch file.
REM     // buildresults = Directory containing the build results.
REM     **********************************************************************************************
set thisDir=%~dp0
set buildresults=%thisDir%..\buildresults\VS2010
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
REM     // Copy AddIn Files
REM     **********************************************************************************************
echo Copy "%buildresults%\VSNDK.AddIn.AddIn" to "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn" 
copy "%buildresults%\VSNDK.AddIn.AddIn" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn" 
echo Copy "%buildresults%\VSNDK.AddIn.dll" to "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll" 
copy "%buildresults%\VSNDK.AddIn.dll" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll" 

REM     //********************************************************************************************
REM     // Copy GDBParser and DebugEngine Files
REM     **********************************************************************************************
echo Make Directory "%ProgRoot%\BlackBerry\VSPlugin-NDK"
md "%ProgRoot%\BlackBerry\VSPlugin-NDK"
echo Copy "%buildresults%\GDBParser.dll" to "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBParser.dll"
copy "%buildresults%\GDBParser.dll" "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBParser.dll" 
echo Copy "%buildresults%\GDBWrapper.exe" to "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBWrapper.exe"
copy "%buildresults%\GDBWrapper.exe" "%ProgRoot%\BlackBerry\VSPlugin-NDK\GDBWrapper.exe" 
echo Copy "%buildresults%\Instructions.txt" to "%ProgRoot%\BlackBerry\VSPlugin-NDK\Instructions.txt"
copy "%buildresults%\Instructions.txt" "%ProgRoot%\BlackBerry\VSPlugin-NDK\Instructions.txt" 
echo Copy "%buildresults%\VSNDK.DebugEngine.dll" to "%ProgRoot%\BlackBerry\VSPlugin-NDK\VSNDK.DebugEngine.dll"
copy "%buildresults%\VSNDK.DebugEngine.dll" "%ProgRoot%\BlackBerry\VSPlugin-NDK\VSNDK.DebugEngine.dll" 

REM     //********************************************************************************************
REM     // Copy Package Files
REM     **********************************************************************************************
echo Make Directory "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin"
md "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin"
echo "%buildresults%\extension.vsixmanifest" to "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\extension.vsixmanifest"
copy "%buildresults%\extension.vsixmanifest" "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\extension.vsixmanifest" 
echo "%buildresults%\VSNDK.Package.dll" to "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
copy "%buildresults%\VSNDK.Package.dll" "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
echo "%buildresults%\VSNDK.Package.pkgdef" to "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef"
copy "%buildresults%\VSNDK.Package.pkgdef" "%ProgRoot%\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef" 

REM     //********************************************************************************************
REM     // Copy MSBuild Files
REM     **********************************************************************************************
echo Copy BlackBerry MSBuild directory
xcopy "%buildresults%\BlackBerry" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry" /e /i /y
copy "%buildresults%\VSNDK.Tasks.dll" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry\VSNDK.Tasks.dll"
echo Copy GDBWrapper.exe
echo Copy BlackBerrySimulator MSBuild directory
xcopy "%buildresults%\BlackBerrySimulator" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator" /e /i /y
copy "%buildresults%\VSNDK.Tasks.dll" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator\VSNDK.Tasks.dll"

REM     //********************************************************************************************
REM     // Copy Template Files
REM     **********************************************************************************************
echo Copy BlackBerry VCWizards directory
xcopy "%buildresults%\Templates\VCWizards" "%ProgRoot%\Microsoft Visual Studio 11.0\VC\VCWizards\CodeWiz" /e /i /y

REM     //********************************************************************************************
REM     // Copy SDK Command Line Tool Files
REM     **********************************************************************************************
echo xcopy "%thisDir%bbndk_vs" "%drive%bbndk_vs" /e /i /y
md "%drive%bbndk_vs"
xcopy "%thisDir%bbndk_vs" "%drive%bbndk_vs" /e /i /y

REM     //********************************************************************************************
REM     // Copy QNX Tools
REM     **********************************************************************************************
echo xcopy "%thisDir%qnxtools" "%ProgRoot%\BlackBerry\VSPlugin-NDK\qnxtools" /e /i /y
md "%ProgRoot%\BlackBerry\VSPlugin-NDK\qnxtools"
xcopy "%thisDir%qnxtools" "%ProgRoot%\BlackBerry\VSPlugin-NDK\qnxtools" /e /i /y

REM     //********************************************************************************************
REM     // Register clases
REM     **********************************************************************************************
echo registering setup
REGEDIT.EXE /S "thisDir"VSNDKPluginSetup.reg




























