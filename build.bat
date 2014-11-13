@echo off
echo Starting...
setlocal EnableExtensions

REM ********************************************************************************************
REM Allow selective build
REM ********************************************************************************************

:: Process parameters, to limit the solutions to build
if "%~1" == "" (
  set ActionClean=1
  set ActionBuildPackage=0
  set ActionBuildVS2010=1
  set ActionBuildVS2012=1
  set ActionBuildVS2013=1
  set PackageVersion=
) else (
  set ActionClean=1
  set ActionBuildPackage=0
  set ActionBuildVS2010=0
  set ActionBuildVS2012=0
  set ActionBuildVS2013=0
  set PackageVersion=
)

:args_parsing
set arg=%~1
if "%arg%" == ""                  (goto args_parsing_done)
if /i "%arg%" == "/all"           set ActionBuildVS2010=1 && set ActionBuildVS2012=1 && set ActionBuildVS2013=1
if /i "%arg%" == "/noclean"       set ActionClean=0
if /i "%arg%" == "/no-clean"      set ActionClean=0
if /i "%arg%" == "vs2010"         set ActionBuildVS2010=1
if /i "%arg%" == "vs2012"         set ActionBuildVS2012=1
if /i "%arg%" == "vs2013"         set ActionBuildVS2013=1
if /i "%arg:~0,5%" == "/out:"     set CustomOutputDir=%arg:~5%
if /i "%arg:~0,8%" == "/package"  set ActionClean=1 && set ActionBuildVS2010=1 && set ActionBuildVS2012=1 && set ActionBuildVS2013=1 && set ActionBuildPackage=1
if /i "%arg:~0,9%" == "/package:" set PackageVersion=%arg:~9%

shift /1
goto args_parsing
    
:args_parsing_done
set arg=

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set /A actionNo=1
set thisDir=%~dp0
set thisDir=%thisDir:~0,-1%

REM Make sure version of the release package is set
if "%PackageVersion%" == "" set PackageVersion=1.0.0-alpha
if "%PackageVersion:~0,1%" == "v"  set PackageVersion=%PackageVersion:~1%
if "%PackageVersion:~0,1%" == "V"  set PackageVersion=%PackageVersion:~1%

REM Allow to override the BuildReults path, in case someone dislikes the default one
set BuildResults=%thisDir%\_BuildResults
if "%CustomOutputDir%" == "" goto skip_buildoutput_override
  set BuildResults=%CustomOutputDir%
  if "%BuildResults:~-1%" == "\" set BuildResults=%BuildResults:~0,-1%
:skip_buildoutput_override

set ProgFilesRoot=%ProgramFiles(x86)%
if "%ProgFilesRoot%" == "" set ProgFilesRoot=%ProgramFiles%
set QnxToolsDir=%thisDir%\qnxtools

set ZipTool=%thisDir%\ext\7zip\7z.exe
set MsBuild="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set MsBuild2013="%ProgFilesRoot%\MSBuild\12.0\Bin\MsBuild.exe"
set MsBuildCmd=%MsBuild% /property:Configuration=Release /target:Rebuild
set MsBuild2013Cmd=%MsBuild2013% /property:Configuration=Release /target:Rebuild

set SolutionPath2010="%thisDir%\src_vs2010\BlackBerry.NativePlugin.sln"
set SolutionPath2012="%thisDir%\src_vs2012\BlackBerry.NativePlugin.sln"
set SolutionPath2013="%thisDir%\src_vs2013\BlackBerry.NativePlugin.sln"

set PackageResults=%BuildResults%\Package
set PackageNamePrefix=BBNDK-

echo Current folder: "%thisDir%"
echo Output folder: "%BuildResults%"

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
%MsBuildCmd% %SolutionPath2010% /p:OutputPath="%BuildResults%\VS2010" > "%BuildResults%\VS2010_buildlog.txt"
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2010

REM ********************************************************************************************
REM Build VS2012
REM ********************************************************************************************
if %ActionBuildVS2012% equ 0 (goto skip_vs2012)

echo %actionNo%: Building Solution for Visual Studio 2012
%MsBuildCmd% %SolutionPath2012% /p:OutputPath="%BuildResults%\VS2012" /p:VisualStudioVersion=11.0 > "%BuildResults%\VS2012_buildlog.txt"
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2012

REM ********************************************************************************************
REM Build VS2013
REM ********************************************************************************************
if %ActionBuildVS2013% equ 0 (goto skip_vs2013)

echo %actionNo%: Building Solution for Visual Studio 2013
%MsBuild2013Cmd% %SolutionPath2013% /p:OutputPath="%BuildResults%\VS2013" /p:VisualStudioVersion=12.0 > "%BuildResults%\VS2013_buildlog.txt"
if errorlevel 1 ( exit /b %errorlevel% )
echo %actionNo%: Build - DONE
set /a actionNo += 1

:skip_vs2013

REM ********************************************************************************************
REM Release Package ZIP files creation
REM ********************************************************************************************
if %ActionBuildPackage% equ 0 (goto skip_package)

if exist "%PackageResults%" rmdir /Q "%PackageResults%" /S
mkdir "%PackageResults%"

echo %actionNo%: Creating release package version: v%PackageVersion% (MSBuild platforms part)
if exist "%PackageResults%\BlackBerry" rmdir /Q "%PackageResults%\BlackBerry" /S
if exist "%PackageResults%\Microsoft.Cpp" rmdir /Q "%PackageResults%\Microsoft.Cpp" /S

echo   Creating folder structure compatible with MSBuild v4.0
xcopy "%QnxToolsDir%" "%PackageResults%\BlackBerry\QnxTools" /e /i /y /q
if exist "%BuildResults%\VS2010\BlackBerry" xcopy "%BuildResults%\VS2010\BlackBerry" "%PackageResults%\Microsoft.Cpp\v4.0\Platforms\BlackBerry\" /e /i /y /q
if exist "%BuildResults%\VS2012\BlackBerry" xcopy "%BuildResults%\VS2012\BlackBerry" "%PackageResults%\Microsoft.Cpp\v4.0\V110\Platforms\BlackBerry\" /e /i /y /q
if exist "%BuildResults%\VS2013\BlackBerry" xcopy "%BuildResults%\VS2013\BlackBerry" "%PackageResults%\Microsoft.Cpp\v4.0\V120\Platforms\BlackBerry\" /e /i /y /q

echo   Compressing...
%ZipTool% a -tzip -mx9 "%PackageResults%\MSBuild_Platforms_v%PackageVersion%.zip" "%PackageResults%\BlackBerry" "%PackageResults%\Microsoft.Cpp" > nul
if errorlevel 1 ( exit /b %errorlevel% )
echo   Created ZIP archive

if exist "%PackageResults%\BlackBerry" rmdir /Q "%PackageResults%\BlackBerry" /S
if exist "%PackageResults%\Microsoft.Cpp" rmdir /Q "%PackageResults%\Microsoft.Cpp" /S
echo %actionNo%: Package - MSBuild - DONE
set /a actionNo += 1

echo %actionNo%: Copying VSIX packages...

REM Copy VSIX packages:
if exist "%BuildResults%\VS2010\BlackBerry.Package.vsix" (
  copy /B "%BuildResults%\VS2010\BlackBerry.Package.vsix" "%PackageResults%\%PackageNamePrefix%plugin_vs2010_v%PackageVersion%.vsix"
  if errorlevel 1 ( exit /b %errorlevel% )
)
if exist "%BuildResults%\VS2012\BlackBerry.Package.vsix" (
  copy /B "%BuildResults%\VS2012\BlackBerry.Package.vsix" "%PackageResults%\%PackageNamePrefix%plugin_vs2012_v%PackageVersion%.vsix"
  if errorlevel 1 ( exit /b %errorlevel% )
)
if exist "%BuildResults%\VS2013\BlackBerry.Package.vsix" (
  copy /B "%BuildResults%\VS2013\BlackBerry.Package.vsix" "%PackageResults%\%PackageNamePrefix%plugin_vs2013_v%PackageVersion%.vsix"
  if errorlevel 1 ( exit /b %errorlevel% )
)

echo %actionNo%: VSIX - DONE
set /a actionNo += 1

:skip_package

goto EOF

REM ********************************************************************************************
REM DONE
REM ********************************************************************************************

:EOF
endlocal
echo [ALL DONE]
