#include "stdafx.h"
#include "GDBWrapper.h"
#include "Log.h"

#include <stdlib.h>


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
    TCHAR msg[1024];
    memset(msg, 0, 1024);

    LogInitialize();
    LogPrint(_T("Starting"));

    HANDLE handles[2];
    handles[0] = OpenEventW(EVENT_ALL_ACCESS, TRUE, argv[2]); // Ctrl-C signal
    handles[1] = OpenEventW(EVENT_ALL_ACCESS, TRUE, argv[3]); // Signal to terminate the wrapper process

    _stprintf_s(msg, _countof(msg), _T("args: %s %s %s %s\r\n"), argv[0], argv[1], argv[2], argv[3]);
    _tprintf(msg);
    LogPrint(msg);

    _stprintf_s(msg, _countof(msg), _T("Ctrl-C handler: name: %s handle: %p\r\n"), argv[2], handles[0]);
    _tprintf(msg);
    LogPrint(msg);

    _stprintf_s(msg, _countof(msg), _T("Terminate handler: name: %s handle: %p\r\n"), argv[3], handles[1]);
    _tprintf(msg);
    LogPrint(msg);

    GDBWrapper* g = new GDBWrapper(argv[1], handles[0], handles[1]);

    BOOL exitProc = FALSE;
    while(!exitProc)
    {
        // Wait for a CTRL-C event indicating GDB should be interrupted
        LogPrint(_T("WaitForMultipleObjects"));
        DWORD event = WaitForMultipleObjects(2, handles, false, INFINITE);
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
            case WAIT_FAILED:
                DisplayError(_T("WaitForSingleObject WAIT_FAILED"));
                exitProc = TRUE;
                break;
            default:
                exitProc = TRUE;
            break;
        }
    }

    g->Shutdown();
    return 0;
}
