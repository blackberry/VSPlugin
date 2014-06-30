#include "stdafx.h"
#include "Log.h"

#include <stdlib.h>
#include <stdio.h>
#include <strsafe.h>
#include <wtypes.h>


#if LOGS_ENABLED

static char path_log[_MAX_PATH]; // contains the path to the output log (needed in LogPrint())

void LogInitialize()
{
    GetEnvironmentVariableA("AppData", path_log, _countof(path_log));
    strcat_s(path_log, _countof(path_log), "\\BlackBerry\\wrapper.log");

    FILE* file = NULL;
    errno_t retCode;
    retCode = fopen_s(&file, path_log, "w"); // just to delete a possible existing file
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

    retCode = fopen_s(&file, path_log, "a");
    if (file != NULL && retCode == 0)
    {
        _ftprintf(file, _T("%s\r\n"), message);
        fclose(file);
    }
}

#endif /* LOGS_ENABLED */

/// <summary> 
/// Displays the error number and corresponding message.
/// </summary>
/// <param name="lpszFunctionName">Name of the API function, that failed</param>
void PrintError(LPCTSTR lpszFunctionName)
{
    HLOCAL lpvMessageBuffer = NULL;
    TCHAR szPrintBuffer[512];

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
            NULL, GetLastError(), MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) &lpvMessageBuffer, 0, NULL);

    _stprintf_s(szPrintBuffer, _countof(szPrintBuffer), _T("Error: API    = %s.\n   error code = %d.\n   message    = %s.\n"),
            lpszFunctionName, GetLastError(), (LPTSTR)lpvMessageBuffer);
    _tprintf(_T("%s\n"), szPrintBuffer);

    LocalFree(lpvMessageBuffer);
}

/// <summary> 
/// Retrieve the system error message for the last-error code. 
/// </summary>
/// <param name="lpszFunctionName">Name of the API function, that failed</param>
void ShowMessage(LPCTSTR lpszFunctionName, LPCTSTR arguments)
{ 
    HLOCAL lpMsgBuf;
    HLOCAL lpDisplayBuf;

    if (arguments == NULL)
        arguments = _T("");

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
            NULL, GetLastError(), MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR) &lpMsgBuf, 0, NULL);

    // Display the error message and exit the process
    lpDisplayBuf = LocalAlloc(LMEM_ZEROINIT, (_tcslen((LPCTSTR)lpMsgBuf) + _tcslen(lpszFunctionName) + 40 + _tcslen(arguments)) * sizeof(TCHAR));
    _stprintf_s((LPTSTR)lpDisplayBuf, LocalSize(lpDisplayBuf) / sizeof(TCHAR), _T("%s failed with error %d: %s\r\n%s"), lpszFunctionName,
                GetLastError(), lpMsgBuf, arguments);

    MessageBox(NULL, (LPCTSTR)lpDisplayBuf, _T("GDB Host Error"), MB_OK);

    LocalFree(lpMsgBuf);
    LocalFree(lpDisplayBuf);
}

