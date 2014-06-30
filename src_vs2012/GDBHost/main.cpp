#include "stdafx.h"
#include "GDBWrapper.h"
#include "Log.h"

#include <stdlib.h>


/// <summary>
/// This method concatenates arguments sent to GDB.
/// The executable is given at 'exeAt' index, and the rest args start at 'argsFrom'.
/// It will return NULL, if anything failed.
/// </summary>
static LPCTSTR ConcatGdbCommand(int argc, _TCHAR* argv[], LPCTSTR exePath, int argsFrom)
{
    size_t length = 1; // for NUL-terminator

    if (exePath != NULL)
    {
        length += _tcslen(exePath);
        length += 2; // for apostrophs
    }

    for (int i = argsFrom; i < argc; i++)
    {
        length += 3; // for space between args and apostrophs
        length += _tcslen(argv[i]);
    }

    if (length == 0)
        return NULL;

    // alloc result:
    TCHAR *result = new TCHAR[length];
    if (result == NULL)
        return NULL;

    // concatenate result string:
    if (exePath != NULL)
    {
        _tcscpy_s(result, length, _T("\""));
        _tcscat_s(result, length, exePath);
        _tcscat_s(result, length, _T("\""));
    }

    for (int i = argsFrom; i < argc; i++)
    {
        _tcscat_s(result, length, _T(" \""));
        _tcscat_s(result, length, argv[i]);
        _tcscat_s(result, length, _T("\""));
    }

    return result;
}

/// <summary> 
/// GDBWrapper Main function. 
/// </summary>
/// <param name="argc"> Not used. </param>
/// <param name="argv"> argv[0] -> Full path of the GDBWrapper executable file.
/// argv[1] -> String with full path and command to initialize GDB/MI
/// argv[2] -> Ctrl-C handler;
/// argv[3] -> Terminate handler. </param>
/// <returns> 0 </returns>
int _tmain(int argc, _TCHAR* argv[])
{
    LogInitialize();
    LogPrint(_T("Starting"));

    if (argc < 4)
    {
        PrintMessage(_T("Copyright (C) 2010-2014 Research in Motion Limited\r\n"));
        PrintMessage(_T("This application runs GDB in console mode and helps it handle Ctrl+C signal.\r\n"));
        PrintMessage(_T("Usage:\r\n"));
        PrintMessage(_T("  BlackBerry.GDBHost.exe <ctrl-c-event-name> <termination-event-name> <path-to-GDB.exe> (<gdb-arguments>)*\r\n"));
        PrintMessage(_T("Where:\r\n"));
        PrintMessage(_T("  <ctrl-c-event-name>      - name of the global event, firing it will cause sending Ctrl+C to GDB\r\n"));
        PrintMessage(_T("  <termination-event-name> - name of the global event, firing it will cause this process to finish\r\n"));
        PrintMessage(_T("  <path-to-GDB.exe>        - path to GDB executable and its arguments\r\n"));
        PrintMessage(_T("  <gdb-arguments>          - additional arguments passed directly to GDB\r\n"));
        PrintMessage(_T("\r\n\r\n"));
        return 0;
    }

    HANDLE handleCtrlC;
    HANDLE handleTerminate;
    LPCTSTR eventNameGdbPath = argv[3];
    LPCTSTR eventNameCtrlC = argv[1];
    LPCTSTR eventNameTerminate = argv[2];


    handleCtrlC     = OpenEventW(EVENT_ALL_ACCESS, TRUE, eventNameCtrlC); // Ctrl-C signal
    handleTerminate = OpenEventW(EVENT_ALL_ACCESS, TRUE, eventNameTerminate); // Signal to terminate the wrapper process

    // If opening failed, create by itself events with the same names:
    if (handleCtrlC == NULL)
    {
        PrintMessage(_T("Error: Unable to open Ctrl+C event (%s), creating new one\r\n"), eventNameCtrlC);
        handleCtrlC = CreateEvent(NULL, FALSE, FALSE, eventNameCtrlC);
    }
    if (handleTerminate == NULL)
    {
        PrintMessage(_T("Error: Unable to open termination event (%s), creating new one\r\n"), eventNameTerminate);
        handleTerminate = CreateEvent(NULL, FALSE, FALSE, eventNameTerminate);
    }

    // Print status
    PrintMessage(_T("STARTUP INFO:\r\n"));
    PrintMessage(_T("  Args: %s %s %s %s\r\n"), argv[0], argv[1], argv[2], argv[3]);
    PrintMessage(_T("  Ctrl-C handler: name: \"%s\", handle: 0x%p\r\n"), eventNameCtrlC, handleCtrlC);
    PrintMessage(_T("  Terminate handler: name: \"%s\", handle: 0x%p\r\n"), eventNameTerminate, handleTerminate);

    // Initialize GDB
    LPCTSTR gdbCommand = ConcatGdbCommand(argc, argv, eventNameGdbPath, 4);
    GDBWrapper* gdb = new GDBWrapper(gdbCommand, handleCtrlC, handleTerminate);

    PrintMessage(_T("  GDB command: %s\r\n"), gdbCommand);
    PrintMessage(_T("\r\n\r\n"));

    delete[] gdbCommand;

    // Start GDB
    if (!gdb->StartProcess())
    {
        PrintMessage(_T("Error: Failed to start the GDB process (%s)\r\n"), eventNameGdbPath);
        return 1;
    }

    HANDLE handles[] = { handleCtrlC, handleTerminate, gdb->GetProcessHandle() };
    BOOL exitProc = FALSE;

    // Main loop
    while (!exitProc)
    {
        // Wait for a CTRL-C event indicating GDB should be interrupted
        LogPrint(_T("WaitForMultipleObjects"));
        DWORD event = WaitForMultipleObjects(_countof(handles), handles, false, INFINITE);
        switch (event)
        {
            case WAIT_OBJECT_0:
                LogPrint(_T("WAIT_OBJECT_0 (Ctrl-C)"));
                GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
                break;
            case WAIT_OBJECT_0 + 1:
                LogPrint(_T("WAIT_OBJECT_0 + 1 (Terminate)"));
                exitProc = TRUE;
                break;
            case WAIT_OBJECT_0 + 2:
                LogPrint(_T("WAIT_OBJECT_0 + 2 (GDB Terminated)"));
                PrintMessage(_T("GDB process terminated!"));
                exitProc = TRUE;
                break;
            case WAIT_FAILED:
                PrintError(_T("WaitForSingleObject WAIT_FAILED"), GetLastError());
                exitProc = TRUE;
                break;
            default:
                exitProc = TRUE;
            break;
        }
    }

    // Clean-up
    gdb->Shutdown();
    PrintMessage(_T("Finished"));
    return 0;
}
