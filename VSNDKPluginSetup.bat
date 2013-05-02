echo off

REM     //********************************************************************************************
REM     // Batch Constants 
REM     // thisDir = working directory of current batch file.
REM     // buildresults = Directory containing the build results.
REM     **********************************************************************************************
Set thisDir=%~dp0
Set buildresults=%thisDir%..\buildresults
set ProgRoot=%ProgramFiles(x86)%
set AllUsers=%ALLUSERSPROFILE%
set AppData=%APPDATA%

if "%ProgramFiles(x86)%XXX"=="XXX" (
echo 32-bit
set ProgRoot=%ProgramFiles%
) 
echo %ProgRoot%

echo Kill GDBWrapper.exe
taskkill /f /im "GDBWrapper.exe"

REM     //********************************************************************************************
REM     // Create Files
REM     **********************************************************************************************
echo Read Me File > "%AppData%\Research In Motion\readme.txt"
REM     //********************************************************************************************
REM     // Copy Files
REM     **********************************************************************************************
echo Copy files to build results.
echo Copy GDBParser.dll
copy "%buildresults%\GDBParser.dll" "%ProgRoot%\Research In Motion\VSPlugin-NDK\GDBParser.dll"
echo Copy GDBWrapper.exe
copy "%buildresults%\GDBWrapper.exe" "%ProgRoot%\Research In Motion\VSPlugin-NDK\GDBWrapper.exe"
echo Copy Instructions.txt
copy "%buildresults%\Instructions.txt" "%ProgRoot%\Research In Motion\VSPlugin-NDK\Instructions.txt"
echo Copy VSNDK.DebugEngine.dll
copy "%buildresults%\VSNDK.DebugEngine.dll" "%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"
echo Copy VSNDK.AddIn.AddIn
copy "%buildresults%\VSNDK.AddIn.AddIn" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn"
echo Copy VSNDK.DebugEngine.dll
copy "%buildresults%\VSNDK.AddIn.dll" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll"
echo Copy extension.manifest
copy "%buildresults%\extension.manifest" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\extension.manifest"
echo Copy VSNDK.Package.dll
copy "%buildresults%\VSNDK.Package.dll" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
echo Copy VSNDK.Package.pkgdef
copy "%buildresults%\VSNDK.Package.pkgdef" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef"
echo Copy BlackBerry template directory
xcopy "%buildresults%\BlackBerry" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry" /e /i /y
echo Copy BlackBerrySimulator template directory
xcopy "%buildresults%\BlackBerrySimulator" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator" /e /i /y
































