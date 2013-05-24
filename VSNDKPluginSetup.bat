REGEDIT4

; echo off

; REM     //********************************************************************************************
; REM     // Batch Constants 
; REM     // thisDir = working directory of current batch file.
; REM     // buildresults = Directory containing the build results.
; REM     **********************************************************************************************
; set thisDir=%~dp0
; set buildresults=%thisDir%..\buildresults
; set ProgRoot=%ProgramFiles(x86)%
; set AllUsers=%ALLUSERSPROFILE%
; set AppData=%APPDATA%

; if "%ProgramFiles(x86)%XXX"=="XXX" (
; echo 32-bit
; set ProgRoot=%ProgramFiles%
; ) 
; echo %ProgRoot%

; REM     //********************************************************************************************
; REM     // Copy AddIn Files
; REM     **********************************************************************************************
; echo Copy VSNDK.AddIn.AddIn
; copy "%buildresults%\VSNDK.AddIn.AddIn" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.AddIn"
; echo Copy VSNDK.DebugEngine.dll
; copy "%buildresults%\VSNDK.AddIn.dll" "%AllUsers%\Microsoft\MSEnvShared\Addins\VSNDK.AddIn.dll"

; REM     //********************************************************************************************
; REM     // Create BlackBerry directory
; REM     **********************************************************************************************
; echo Create BlackBerry App Directory
; md "%AppData%\BlackBerry"

; REM     //********************************************************************************************
; REM     // Copy GDBParser and DebugEngine Files
; REM     **********************************************************************************************
; echo Copy GDBParser.dll
; copy "%buildresults%\GDBParser.dll" "%ProgRoot%\Research In Motion\VSPlugin-NDK\GDBParser.dll"
; echo Copy GDBWrapper.exe
; copy "%buildresults%\GDBWrapper.exe" "%ProgRoot%\Research In Motion\VSPlugin-NDK\GDBWrapper.exe"
; echo Copy Instructions.txt
; copy "%buildresults%\Instructions.txt" "%ProgRoot%\Research In Motion\VSPlugin-NDK\Instructions.txt"
; echo Copy VSNDK.DebugEngine.dll
; copy "%buildresults%\VSNDK.DebugEngine.dll" "%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"

; REM     //********************************************************************************************
; REM     // Copy Package Files
; REM     **********************************************************************************************
; echo Copy extension.manifest
; copy "%buildresults%\extension.manifest" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\extension.manifest"
; echo Copy VSNDK.Package.dll
; copy "%buildresults%\VSNDK.Package.dll" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.dll"
; echo Copy VSNDK.Package.pkgdef
; copy "%buildresults%\VSNDK.Package.pkgdef" "%ProgRoot%\Microsoft Visual Studio 10.0\Common7\IDE\Extensions\NDK Plugin\VSNDK.Package.pkgdef"

; REM     //********************************************************************************************
; REM     // Copy MSBuild Files
; REM     **********************************************************************************************
; echo Copy BlackBerry MSBuild directory
; xcopy "%buildresults%\BlackBerry" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry" /e /i /y
; copy "%buildresults%\VSNDK.Tasks.dll" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerry\VSNDK.Tasks.dll"
; echo Copy GDBWrapper.exe
; echo Copy BlackBerrySimulator MSBuild directory
; xcopy "%buildresults%\BlackBerrySimulator" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator" /e /i /y
; copy "%buildresults%\VSNDK.Tasks.dll" "%ProgRoot%\MSBuild\Microsoft.Cpp\v4.0\Platforms\BlackBerrySimulator\VSNDK.Tasks.dll"

; REM     //********************************************************************************************
; REM     // Copy Template Files
; REM     **********************************************************************************************
; echo Copy BlackBerry vcprojectitems directory
; xcopy "%buildresults%\Templates\vcprojectitems" ""%ProgRoot%\Microsoft Visual Studio 10.0\VC\vcprojectitems" /e /i /y
; echo Copy BlackBerry VCWizards directory
; xcopy "%buildresults%\Templates\VCWizards" ""%ProgRoot%\Microsoft Visual Studio 10.0\VC\VCWizards\CodeWiz" /e /i /y

; REM     //********************************************************************************************
; REM     // Register clases
; REM     **********************************************************************************************
; @ECHO OFF
; CLS
; REGEDIT.EXE /S "%~f0"
; EXIT

[HKEY_CLASSES_ROOT]

[HKEY_CURRENT_USER]

[HKEY_CURRENT_USER\SOFTWARE]

[HKEY_CURRENT_USER\SOFTWARE\BlackBerry]

[HKEY_CURRENT_USER\SOFTWARE\BlackBerry\BlackBerryVSPlugin]
"device_password"="AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA7fSRRS7m1Em+yoK0ioAYhwQAAAACAAAAAAADZgAAwAAAABAAAAAb9QNNQPNG1qB5uDOFabBjAAAAAASAAACgAAAAEAAAAOu49T6nbn0mHnBMoGvre5cIAAAA2NG7RxOCGYgUAAAAi84MGQ9ELFYdXZ5pOhVSayDK2V4="
"device_IP"="169.254.0.1"
"simulator_password"="AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA7fSRRS7m1Em+yoK0ioAYhwQAAAACAAAAAAADZgAAwAAAABAAAAAb9QNNQPNG1qB5uDOFabBjAAAAAASAAACgAAAAEAAAAOu49T6nbn0mHnBMoGvre5cIAAAA2NG7RxOCGYgUAAAAi84MGQ9ELFYdXZ5pOhVSayDK2V4="
"simulator_IP"=""
"NDKHostPath"=""
"NDKTargetPath"=""

[HKEY_LOCAL_MACHINE]

[HKEY_LOCAL_MACHINE\SOFTWARE]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\AD7Metrics]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\AD7Metrics\Engine]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}]
"AddressBP"=dword:00000000
"AutoSelectPriority"=dword:00000004
"CallstackBP"=dword:00000001
"ProgramProvider"="{AD06FD46-C790-4D5C-A274-8815DF9511B8}"
"Attach"=dword:00000001
"CLSID"="{904AA6E0-942C-4D11-9094-7BAAEB3EE4B9}"
"AlwaysLoadLocal"="1"
"PortSupplier"="{92A2B753-00BD-40FF-9964-6AB64A1D6C9F}"
"Name"="VSNDK Debug Engine"
@="guidDebuggingSampleEngine"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}\IncompatibleList]
"guidScriptEng"="{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}"
"guidNativeOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidCOMPlusOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidCOMPlusNativeEng"="{92EF0900-2251-11D2-B72E-0000F87572EF}"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\CLSID]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\CLSID\{AD06FD46-C790-4D5C-A274-8815DF9511B8}]
"CodeBase"="%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"
"InprocServer32"="c:\\windows\\system32\\mscoree.dll"
"Class"="VSNDK.DebugEngine.AD7ProgramProvider"
"Assembly"="VSNDK.DebugEngine"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\CLSID\{904AA6E0-942C-4D11-9094-7BAAEB3EE4B9}]
"CodeBase"="%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"
"InprocServer32"="c:\\windows\\system32\\mscoree.dll"
"Class"="VSNDK.DebugEngine.AD7Engine"
"Assembly"="VSNDK.DebugEngine"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\CLSID\{BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6}]
@="BlackBerrySupplier"
"Assembly "="VSNDK.DebugEngine"
"Class "="VSNDK.DebugEngine.AD7PortSupplier"
"InprocServer32 "="c:\\windows\\system32\\mscoree.dll"
"CodeBase "="%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"
"ThreadingModel "="Free"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics\PortSupplier]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics\PortSupplier\{92A2B753-00BD-40FF-9964-6AB64A1D6C9F} ]
"Name"="BlackBerry"
"DisallowUserEnteredPorts"=dword:00000000
"CLSID"="{BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6}"
@="Visual Studio Plug-in for BlackBerry"
"PortPickerCLSID "="{3FAA02D6-72D8-4F69-A1E6-BB05ECB4E37A}"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics\Engine]

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}]
"AlwaysLoadLocal"="1"
"PortSupplier"="{BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6}"
"Name"="VSNDK Debug Engine"
"CallstackBP"=dword:00000001
"AutoSelectPriority"=dword:00000004
"AddressBP"=dword:00000000
"Attach"=dword:00000001
"ProgramProvider"="{AD06FD46-C790-4D5C-A274-8815DF9511B8}"
"CLSID"="{904AA6E0-942C-4D11-9094-7BAAEB3EE4B9}"
@="guidDebuggingSampleEngine"

[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\10.0\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}\IncompatibleList]
"guidScriptEng"="{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}"
"guidNativeOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidCOMPlusOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidCOMPlusNativeEng"="{92EF0900-2251-11D2-B72E-0000F87572EF}"

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\CLSID]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\CLSID\{AD06FD46-C790-4D5C-A274-8815DF9511B8}]
"CodeBase"="%ProgRoot%\Research In Motion\VSPlugin-NDK\VSNDK.DebugEngine.dll"
"InprocServer32"="c:\\windows\\system32\\mscoree.dll"
"Class"="VSNDK.DebugEngine.AD7ProgramProvider"
"Assembly"="VSNDK.DebugEngine"

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\CLSID\{904AA6E0-942C-4D11-9094-7BAAEB3EE4B9}]
"CodeBase"="[INSTALLDIR]VSNDK.DebugEngine.dll"
"Assembly"="VSNDK.DebugEngine"
"Class"="VSNDK.DebugEngine.AD7Engine"
"InprocServer32"="c:\\windows\\system32\\mscoree.dll"

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\AD7Metrics]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\AD7Metrics\Engine]

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}]
"Attach"=dword:00000001
"ProgramProvider"="{AD06FD46-C790-4D5C-A274-8815DF9511B8}"
"CLSID"="{904AA6E0-942C-4D11-9094-7BAAEB3EE4B9}"
@="guidDebuggingSampleEngine"
"AlwaysLoadLocal"="1"
"AutoSelectPriority"=dword:00000004
"PortSupplier"="{92A2B753-00BD-40FF-9964-6AB64A1D6C9F}"
"Name"="VSNDK Debug Engine"
"CallstackBP"=dword:00000001
"AddressBP"=dword:00000000

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\AD7Metrics\Engine\{E5A37609-2F43-4830-AA85-D94CFA035DD2}\IncompatibleList]
"guidNativeOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidCOMPlusOnlyEng"="{449EC4CC-30D2-4032-9256-EE18EB41B62B}"
"guidScriptEng"="{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}"
"guidCOMPlusNativeEng"="{92EF0900-2251-11D2-B72E-0000F87572EF}"

[HKEY_USERS]

[HKEY_USER_SELECTABLE]
























