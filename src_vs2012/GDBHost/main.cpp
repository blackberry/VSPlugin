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
    else
    {
        result[0] = '\0';
    }

    for (int i = argsFrom; i < argc; i++)
    {
        _tcscat_s(result, length, _T(" \""));
        _tcscat_s(result, length, argv[i]);
        _tcscat_s(result, length, _T("\""));
    }

    return result;
}

static BOOL FileExists(LPCTSTR lpszPath)
{
  DWORD attributes = GetFileAttributes(lpszPath);

  return (attributes != INVALID_FILE_ATTRIBUTES && !(attributes & FILE_ATTRIBUTE_DIRECTORY));
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
    SetConsoleTitle(_T("BlackBerry GDB Host Application"));

    LogInitialize();
    LogPrint(_T("Starting"));

    if (argc < 4)
    {
        PrintMessage(_T("Copyright (C) 2010-2014 Research in Motion Limited\r\n"));
        PrintMessage(_T("This application runs GDB in console mode and helps it handle Ctrl+C signal.\r\n"));
        PrintMessage(_T("Usage:\r\n"));
        PrintMessage(_T("  BlackBerry.GDBHost.exe <ctrl-c-event-name> <termination-event-name> (host-options) <path-to-GDB.exe> (<gdb-arguments>)*\r\n"));
        PrintMessage(_T("Where:\r\n"));
        PrintMessage(_T("  <ctrl-c-event-name>      - name of the global event, firing it will cause sending Ctrl+C to GDB\r\n"));
        PrintMessage(_T("  <termination-event-name> - name of the global event, firing it will cause this process to finish\r\n"));
        PrintMessage(_T("  <path-to-GDB.exe>        - path to GDB executable and its arguments\r\n"));
        PrintMessage(_T("  <gdb-arguments>          - additional arguments passed directly to GDB\r\n"));
        PrintMessage(_T("  (host-options)           - single parameter starting with '-', which defines custom behavior of the host process itself\r\n"));
        PrintMessage(_T("Host options:\r\n"));
        PrintMessage(_T("  s                        - [silent] - disable all custom console logs\r\n"));
        PrintMessage(_T("  c                        - skip checking GDB executable existance, before executing\r\n"));
        PrintMessage(_T("\r\n\r\n"));
        return 0;
    }

    HANDLE handleCtrlC;
    HANDLE handleTerminate;
    LPCTSTR hostOptions = argv[3];          // it's not a mistake, the hostOptions and gdbExecutablePath should point to the same 'opitional' parameter
    LPCTSTR gdbExecutablePath = argv[3];
    LPCTSTR eventNameCtrlC = argv[1];
    LPCTSTR eventNameTerminate = argv[2];
    int gdbArgsStartFrom = 4;
    BOOL checkGdbExistence = TRUE;

    // check, if we passed some optional arguments for the host...
    if (hostOptions[0] == '-')
    {
        gdbExecutablePath = argc >= 5 ? argv[4] : NULL;
        gdbArgsStartFrom = 5;

        // and parse host options:
        int optionsLength = _tcslen(hostOptions);
        for (int i = 1; i < optionsLength; i++)
        {
            switch(hostOptions[i])
            {
            case 's':
                DisableConsolePrinting();
                break;
            case 'c':
                checkGdbExistence = FALSE;
                break;
            }
        }
    }

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
    PrintMessage(_T("  Args: %s %s %s %s\r\n"), argv[0], argv[1], argv[2], argv[3], argc >= 5 ? argv[4] : _T(""), argc >= 6 ? argv[5] : _T(""));
    PrintMessage(_T("  Ctrl-C handler: name: \"%s\", handle: 0x%p\r\n"), eventNameCtrlC, handleCtrlC);
    PrintMessage(_T("  Terminate handler: name: \"%s\", handle: 0x%p\r\n"), eventNameTerminate, handleTerminate);

    if (gdbExecutablePath == NULL || gdbExecutablePath[0] == '\0' || (checkGdbExistence && !FileExists(gdbExecutablePath)))
    {
        PrintMessage(_T("Error: Unable to find GDB executable (%s)\r\n"), gdbExecutablePath != NULL ? gdbExecutablePath : _T("-missing path-"));
        return 1;
    }

    // Initialize GDB
    LPCTSTR gdbCommand = ConcatGdbCommand(argc, argv, gdbExecutablePath, gdbArgsStartFrom);
    GDBWrapper* gdb = new GDBWrapper(gdbCommand, handleCtrlC, handleTerminate);

    PrintMessage(_T("  GDB command: %s\r\n"), gdbCommand);
    PrintMessage(_T("\r\n\r\n"));

    delete[] gdbCommand;

    // Start GDB
    if (!gdb->StartProcess())
    {
        PrintMessage(_T("Error: Failed to start the GDB process (%s)\r\n"), gdbExecutablePath);
        return 2;
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
