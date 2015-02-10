@echo off

SETLOCAL

REM ######################################
REM # Make sure git tools are accessible
REM # (assume default installation dir)
REM #

set ProgFilesRoot=%ProgramFiles(x86)%
if "%ProgFilesRoot%" == "" set ProgFilesRoot=%ProgramFiles%
set PATH=%PATH%;%ProgFilesRoot%\Git\bin;

echo Repository cleanup script
echo.

REM ######################################
REM # Reading names from:
REM #   - 'prune.tags.txt'
REM # and
REM #   removing all unnecessary tags
REM #

echo Removing tags...
FOR /F "eol=; delims==" %%i in (prune.tags.txt) do (
 echo Removing tag '%%i'...
 git tag -d "%%i"
)

echo [DONE]

REM #
REM ######################################

ENDLOCAL
