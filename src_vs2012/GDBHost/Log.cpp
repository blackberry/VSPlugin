#include "stdafx.h"
#include "Log.h"

#include <stdlib.h>
#include <stdio.h>
#include <strsafe.h>
#include <wtypes.h>


static bool gPrintConsole = true;

#if LOGS_ENABLED

static char gLogFilePath[_MAX_PATH]; // contains the path to the output log (needed in LogPrint())

void LogInitialize()
{
    GetEnvironmentVariableA("AppData", gLogFilePath, _countof(gLogFilePath));
    strcat_s(gLogFilePath, _countof(gLogFilePath), "\\BlackBerry\\wrapper.log");

    FILE* file = NULL;
    errno_t retCode;
    retCode = fopen_s(&file, gLogFilePath, "w"); // just to delete a possible existing file
    if (file != NULL && retCode == 0)
    {
        fclose(file);
    }
}

/// <summary> 
/// Generic function to print to a log file. 
/// </summary>
/// <param name="buffer"> Message to be printed to a log file. </param>
void LogPrint(TCHAR* message)
{
    FILE* file = NULL;
    errno_t retCode;

    retCode = fopen_s(&file, gLogFilePath, "a");
    if (file != NULL && retCode == 0)
    {
        _ftprintf(file, _T("%s\r\n"), message);
        fclose(file);
    }
}

#endif /* LOGS_ENABLED */

void DisableConsolePrinting()
{
    gPrintConsole = false;
}

static void VsPrintMessage(LPCTSTR format, va_list args)
{
    TCHAR buffer[1024] = _T("");

    _vstprintf_s(buffer, _countof(buffer), format, args);

    if (gPrintConsole)
    {
        _tprintf(buffer);
        fflush(stdout);
    }

    // and also put it into logs...
    LogPrint(buffer);
}

void PrintMessage(LPCTSTR format, ...)
{
    va_list list;

    va_start(list, format);
    VsPrintMessage(format, list);
    va_end(list);
}

/// <summary> 
/// Displays the error number and corresponding message.
/// </summary>
/// <param name="lpszFunctionName">Name of the API function, that failed</param>
void PrintError(LPCTSTR lpszFunctionName, DWORD lastError)
{
    HLOCAL lpvMessageBuffer = NULL;

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, lastError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) &lpvMessageBuffer, 0, NULL);
    PrintMessage(_T("Error: API    = %s.\n   error code = %d.\n   message    = %s.\n"), lpszFunctionName, lastError, (LPTSTR)lpvMessageBuffer);
    LocalFree(lpvMessageBuffer);
}

/// <summary> 
/// Retrieve the system error message for the last-error code. 
/// </summary>
/// <param name="lpszFunctionName">Name of the API function, that failed</param>
void ShowMessage(LPCTSTR lpszFunctionName, DWORD lastError, LPCTSTR arguments)
{ 
    HLOCAL lpMsgBuf;
    HLOCAL lpDisplayBuf;

    if (arguments == NULL)
        arguments = _T("");

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, lastError, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) &lpMsgBuf, 0, NULL);

    // Display the error message and exit the process
    lpDisplayBuf = LocalAlloc(LMEM_ZEROINIT, (_tcslen((LPCTSTR)lpMsgBuf) + _tcslen(lpszFunctionName) + 40 + _tcslen(arguments)) * sizeof(TCHAR));
    _stprintf_s((LPTSTR)lpDisplayBuf, LocalSize(lpDisplayBuf) / sizeof(TCHAR), _T("%s failed with error %d: %s\r\n%s"), lpszFunctionName,
        lastError, lpMsgBuf, arguments);

    PrintMessage(_T("%s"), (LPCTSTR)lpDisplayBuf);
    MessageBox(NULL, (LPCTSTR)lpDisplayBuf, _T("GDB Host Error"), MB_OK);

    LocalFree(lpMsgBuf);
    LocalFree(lpDisplayBuf);
}
