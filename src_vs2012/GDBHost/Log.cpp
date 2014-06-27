#include "stdafx.h"
#include "Log.h"

#include <stdlib.h>
#include <stdio.h>
#include <strsafe.h>
#include <wtypes.h>

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
#if LOG_GDB_RAW_IO
    FILE* file = NULL;
    errno_t retCode;

    retCode = fopen_s(&file, path_log, "a");
    if (file != NULL && retCode == 0)
    {
        _ftprintf(file, _T("%s\r\n"), message);
        fclose(file);
    }
#endif
}

/// <summary> 
/// Displays the error number and corresponding message. 
/// </summary>
/// <param name="pszAPI"> Error message. </param>
void DisplayError(LPCTSTR pszAPI)
{
    LPVOID lpvMessageBuffer;
    TCHAR szPrintBuffer[512];
    DWORD bufSize = 512 * sizeof(TCHAR);

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
            NULL, GetLastError(), MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            (LPTSTR)&lpvMessageBuffer, 0, NULL);

    StringCbPrintf(szPrintBuffer, bufSize, 
        _T("ERROR: API    = %s.\n   error code = %d.\n   message    = %s.\n"),
            pszAPI, GetLastError(), (char *)lpvMessageBuffer);

    _tprintf(_T("%s\n"), szPrintBuffer);

    LocalFree(lpvMessageBuffer);
    ExitProcess(GetLastError());
}


/// <summary> 
/// Retrieve the system error message for the last-error code. 
/// </summary>
/// <param name="lpszFunction"> Error message. </param>
void ErrorExit(LPTSTR lpszFunction) 
{ 
    LPVOID lpMsgBuf;
    LPVOID lpDisplayBuf;
    DWORD dw = GetLastError(); 

    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, dw, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &lpMsgBuf, 0, NULL);

    // Display the error message and exit the process

    lpDisplayBuf = (LPVOID)LocalAlloc(LMEM_ZEROINIT, 
        (lstrlen((LPCTSTR)lpMsgBuf) + lstrlen((LPCTSTR)lpszFunction) + 40) * sizeof(TCHAR)); 
    StringCchPrintf((LPTSTR)lpDisplayBuf, 
        LocalSize(lpDisplayBuf) / sizeof(TCHAR),
        TEXT("%s failed with error %d: %s"), 
        lpszFunction, dw, lpMsgBuf); 
    MessageBox(NULL, (LPCTSTR)lpDisplayBuf, TEXT("Error"), MB_OK); 

    LocalFree(lpMsgBuf);
    LocalFree(lpDisplayBuf);
    ExitProcess(dw); 
}

