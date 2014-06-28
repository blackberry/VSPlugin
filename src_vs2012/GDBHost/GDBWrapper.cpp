//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.


// GDBWrapper.cpp : Utility to run GDB in console mode so that CTRL-C works.
//

#include "stdafx.h"

#include <strsafe.h>
#include <process.h>

#include "GDBWrapper.h"
#include "Log.h"


/// <summary> 
/// CTRL-C handler. 
/// </summary>
/// <param name="dwCtrlType"> CTRL type. </param>
/// <returns> True; False. </returns>
static BOOL WINAPI GDBWrapperCtrlHandler(DWORD dwCtrlType)
{
    LogPrint(_T("ctrlHandler"));
    if (dwCtrlType == CTRL_C_EVENT)
    {
        LogPrint(_T("ctrlHandler got CTRL_C_EVENT"));
        return true;
    }

    LogPrint(_T("ctrlHandler returns false"));
    return false;
}

/// <summary> 
/// Constructor. 
/// </summary>
/// <param name="pcGDBCmd">String with full path and command to initialize GDB/MI.</param>
/// <param name="eventCtrlC">Ctrl-C event handle</param>
/// <param name="eventTerminate">Terminate event handle</param>
GDBWrapper::GDBWrapper(LPCTSTR lpszGdbCommandpcGDBCmd, HANDLE hEventCtrlC, HANDLE hEventTerminate)
    : m_lpszGdbCommand(NULL), m_eventCtrlC(hEventCtrlC), m_eventTerminate(hEventCtrlC), m_isClosed(FALSE), m_hProcess(NULL)
{
    // Copy path to GDB
    if (lpszGdbCommandpcGDBCmd != NULL)
    {
        size_t numChars = _tcslen(lpszGdbCommandpcGDBCmd) + 1;
        m_lpszGdbCommand = new TCHAR[numChars * sizeof(TCHAR)];

        if (m_lpszGdbCommand != NULL)
        {
            _tcscpy_s(m_lpszGdbCommand, numChars, lpszGdbCommandpcGDBCmd);
        }
    }

    // Register own CTRL-C handler
    if (!SetConsoleCtrlHandler(GDBWrapperCtrlHandler, TRUE))
    {
        PrintError(_T("SetConsoleCtrlHandler"));
    }
}

/// <summary> 
/// Destructor. 
/// </summary>
GDBWrapper::~GDBWrapper()
{
    LogPrint(_T("+~GDBWrapper"));
    if (!m_isClosed)
    {
        Shutdown();
    }
    LogPrint(_T("-~GDBWrapper"));
}

HANDLE GDBWrapper::GetProcessHandle()
{
    return m_hProcess;
}

/// <summary> 
/// Shut down GDB Wrapper: Update variables and terminate GDBWrapper process. 
/// </summary>
void GDBWrapper::Shutdown()
{
    LogPrint(_T("+shutdown"));
    m_isClosed = TRUE;

    delete[] m_lpszGdbCommand;
    m_lpszGdbCommand = NULL;

    // Kill GDB process
    if (m_hProcess != NULL)
    {
        TerminateProcess(m_hProcess, 0);

        if (!CloseHandle(m_hProcess))
        {
            PrintError(_T("CloseHandle"));
        }
        m_hProcess = NULL;
    }

    LogPrint(_T("-shutdown"));
}

/// <summary> 
/// Sets up STARTUPINFO structure and launches redirected child. 
/// </summary>
BOOL GDBWrapper::StartProcess()
{
    if (m_hProcess != NULL)
    {
        return FALSE;
    }

    LPCTSTR lpApplicationName = NULL;
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    DWORD flags = 0;

    // Set up the start up info struct.
    ZeroMemory(&pi, sizeof(pi));
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    si.dwFlags = STARTF_USESTDHANDLES;

    // Pass the redirected handles
    si.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
    si.hStdInput  = GetStdHandle(STD_INPUT_HANDLE);
    si.hStdError  = GetStdHandle(STD_ERROR_HANDLE);

    // Use this if you want to hide the child:
    //     si.wShowWindow = SW_HIDE;
    // Note that dwFlags must include STARTF_USESHOWWINDOW if you want to
    // use the wShowWindow flags.

    // Launch the process
    if (!CreateProcess(NULL, m_lpszGdbCommand, NULL, NULL, TRUE, flags, NULL, NULL, &si, &pi))
    {
        ShowMessage(_T("CreateProcess"));
        return FALSE;
    }

    m_hProcess = pi.hProcess;

    if (!CloseHandle(pi.hThread))
    {
        PrintError(_T("CloseHandle"));
        return FALSE;
    }

    return TRUE;
}
