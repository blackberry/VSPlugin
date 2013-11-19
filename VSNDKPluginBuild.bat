echo off

REM ********************************************************************************************
REM Declare Constants
REM ********************************************************************************************
set thisDir=%~dp0
set buildresults="%thisDir%\..\buildresults"
set dotNetFrameworkPath="C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MsBuild.exe"
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

REM ********************************************************************************************
REM Build VS2010
REM ********************************************************************************************
echo Building Solution for VS2010
%dotNetFrameworkPath% %solutionPath2010% /property:Configuration=Release /target:Clean;Build /p:OutputPath="%buildresults%\VS2010" > %buildresult%\VS2010_buildlog.txt
echo Build Success

REM ********************************************************************************************
REM Build VS2012
REM ********************************************************************************************
echo Building Solution for VS2012
%dotNetFrameworkPath% %AddIn2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
%dotNetFrameworkPath% %DebugEngine2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
%dotNetFrameworkPath% %GDWrapper2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
%dotNetFrameworkPath% %VSPackage2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
%dotNetFrameworkPath% %VSTasks2012% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=11.0 /p:OutputPath="%buildresults%\VS2012" > %buildresult%\VS2012_buildlog.txt
echo Build Success

REM ********************************************************************************************
REM Build VS2013
REM ********************************************************************************************
echo Building Solution for VS2013
%dotNetFrameworkPath% %AddIn2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresult%\VS2013_buildlog.txt
%dotNetFrameworkPath% %DebugEngine2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresult%\VS2013_buildlog.txt
%dotNetFrameworkPath% %GDWrapper2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresult%\VS2013_buildlog.txt
%dotNetFrameworkPath% %VSPackage2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresult%\VS2013_buildlog.txt
%dotNetFrameworkPath% %VSTasks2013% /property:Configuration=Release /target:Clean;Build /p:VisualStudioVersion=12.0 /p:OutputPath="%buildresults%\VS2013" > %buildresult%\VS2013_buildlog.txt
echo Build Success

