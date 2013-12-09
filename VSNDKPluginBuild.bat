echo off

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set thisDir=%~dp0
set buildresults="%thisDir%\..\buildresults"
set dotNetFrameworkPath="C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
set dotNetFrameworkPath2013="C:\Program Files (x86)\MSBuild\12.0\Bin\MsBuild.exe"
set solutionPath2010="%thisDir%\src\VSNDK.sln"
set AddIn2012="%thisDir%\src_vs2012\VSNDK.AddIn\VSNDK.AddIn.csproj"
set DebugEngine2012="%thisDir%\src_vs2012\VSNDK.DebugEngine\VSNDK.DebugEngine.csproj"
set GDWrapper2012="%thisDir%\src_vs2012\GDBWrapper\GDBWrapper.vcxproj"
set VSPackage2012="%thisDir%\src_vs2012\VSNDK.Package\VSNDK.Package.csproj"
set VSTasks2012="%thisDir%\src_vs2012\VSNDK.Tasks\VSNDK.Tasks.csproj"
set AddIn2013="%thisDir%\src_vs2013\VSNDK.AddIn\VSNDK.AddIn.csproj"
set DebugEngine2013="%thisDir%\src_vs2013\VSNDK.DebugEngine\VSNDK.DebugEngine.csproj"
set GDWrapper2013="%thisDir%\src_vs2013\GDBWrapper\GDBWrapper.vcxproj"
set VSPackage2013="%thisDir%\src_vs2013\VSNDK.Package\VSNDK.Package.csproj"
set VSTasks2013="%thisDir%\src_vs2013\VSNDK.Tasks\VSNDK.Tasks.csproj"

REM ********************************************************************************************
REM Clean up old bin folder
REM ********************************************************************************************
echo Removing previous code
if not exist "%buildresults%" goto build
  echo Remove Build Results
  RMDIR /Q "%buildresults%" /S
:build  

mkdir "%buildresults%"

REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
REM echo Building Solution for VS2010
REM %dotNetFrameworkPath% %solutionPath2010% /property:Configuration=Release /target:Clean;Build /p:OutputPath="%buildresults%\VS2010" > %buildresult%\VS2010_buildlog.txt
REM echo Build Success

REM ********************************************************************************************
REM Build VS2012
REM ********************************************************************************************
REM echo Building Solution for VS2012
REM %dotNetFrameworkPath% %AddIn2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog_AddIn.txt
REM %dotNetFrameworkPath% %DebugEngine2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog_DBEngine.txt
REM %dotNetFrameworkPath% %GDWrapper2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog_GDBWrapper.txt
REM %dotNetFrameworkPath% %VSPackage2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog_Package.txt
REM %dotNetFrameworkPath% %VSTasks2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog_Tasks.txt
REM echo Build Success

REM ********************************************************************************************
REM Build VS2013
REM ********************************************************************************************
echo Building Solution for VS2013
%dotNetFrameworkPath2013% %VSTasks2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresults%\VS2013_buildlog_Tasks.txt
%dotNetFrameworkPath2013% %AddIn2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresults%\VS2013_buildlog_AddIn.txt
%dotNetFrameworkPath2013% %VSPackage2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresults%\VS2013_buildlog_Package.txt
%dotNetFrameworkPath2013% %GDWrapper2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresults%\VS2013_buildlog_GDBWrapper.txt
%dotNetFrameworkPath2013% %DebugEngine2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresults%\VS2013_buildlog_DBEngine.txt
echo Build Success

