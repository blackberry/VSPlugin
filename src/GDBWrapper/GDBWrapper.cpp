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

#include <windows.h>
#include <strsafe.h>

#include <string>
#include <iostream>
#include <fstream>
#include <unordered_map>

#include <process.h>

#include "GDBWrapper.h"
#include < vcclr.h >

// *******************************************************************
// TODO: Put the following functions in a library that both GDBWrapper
// and GDBConsole can use

/////////////////////////////////////////////////////////////////////// 
// DisplayError
// Displays the error number and corresponding message.
/////////////////////////////////////////////////////////////////////// 
using namespace std;
using namespace System;

char path_log[_MAX_PATH]; // contains the path to the Wrapper output log, needed in logPrint()

void DisplayError(LPCTSTR pszAPI)
{
    LPVOID lpvMessageBuffer;
    TCHAR szPrintBuffer[512];
	DWORD bufSize = 512 * sizeof(TCHAR);
    DWORD nCharsWritten;

    FormatMessage(
            FORMAT_MESSAGE_ALLOCATE_BUFFER|FORMAT_MESSAGE_FROM_SYSTEM,
            NULL, GetLastError(),
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
            (LPTSTR)&lpvMessageBuffer, 0, NULL);

    StringCbPrintf(szPrintBuffer, bufSize, 
        _T("ERROR: API    = %s.\n   error code = %d.\n   message    = %s.\n"),
            pszAPI, GetLastError(), (char *)lpvMessageBuffer);

    /*WriteConsole(GetStdHandle(STD_OUTPUT_HANDLE),szPrintBuffer,
                    lstrlen(szPrintBuffer),&nCharsWritten,NULL);
					*/
	_tprintf(_T("%s\n"), szPrintBuffer);

    LocalFree(lpvMessageBuffer);
    ExitProcess(GetLastError());
}

void ErrorExit(LPTSTR lpszFunction) 
{ 
    // Retrieve the system error message for the last-error code

    LPVOID lpMsgBuf;
    LPVOID lpDisplayBuf;
    DWORD dw = GetLastError(); 

    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | 
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        dw,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &lpMsgBuf,
        0, NULL );

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

void logPrint(TCHAR* buffer) {
	if (LOG_GDB_RAW_IO) {
		FILE* file = fopen(path_log, "a");
		_ftprintf(file, _T("%s\r\n"), buffer);
		fclose(file);
	}
}

// End region that needs to go into a library
// *******************************************************************

BOOL WINAPI ctrlHandler(DWORD dwCtrlType)
{
	logPrint(_T("ctrlHandler"));
	if (dwCtrlType == CTRL_C_EVENT) {
		logPrint(_T("ctrlHandler got CTRL_C_EVENT"));
		return true;
	}
	logPrint(_T("ctrlHandler returns false"));
	return false;
}

GDBWrapper::GDBWrapper(TCHAR* pcGDBCmd, HANDLE eventCtrlC, HANDLE eventTerminate) : 
	m_eventCtrlC(eventCtrlC), m_eventTerminate(eventTerminate), m_isClosed(FALSE)
{
    SECURITY_ATTRIBUTES sa;

	// Copy path to GDB
	size_t numChars = _tcslen(pcGDBCmd) + 1;
	m_pcGDBCmd = new TCHAR[numChars * sizeof(TCHAR)];	
	_tcscpy_s(m_pcGDBCmd, numChars, pcGDBCmd);

	// Register CTRL-C handler
	if (!SetConsoleCtrlHandler(ctrlHandler, TRUE)) {	
		DisplayError(_T("SetConsoleCtrlHandler"));
	}

    // Set up the security attributes struct used when creating the pipes
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle = TRUE;

    prepAndLaunchRedirectedChild();
}

void GDBWrapper::shutdown() {
	logPrint(_T("+shutdown"));
	m_isClosed = TRUE;

	if (m_pcGDBCmd != NULL) {
		delete[] m_pcGDBCmd;
		m_pcGDBCmd = NULL;
	}

	// Kill GDB process
	TerminateProcess(m_hProcess, 0);
    
	if (!CloseHandle(m_hProcess)) DisplayError(_T("CloseHandle"));
	logPrint(_T("-shutdown"));
}

GDBWrapper::~GDBWrapper() {
	logPrint(_T("+~GDBWrapper"));
	if (!m_isClosed) {
		shutdown();
	}
	logPrint(_T("-~GDBWrapper"));
}

void GDBWrapper::prepAndLaunchRedirectedChild(void)
{
	LPCTSTR lpApplicationName = NULL;
    PROCESS_INFORMATION pi;
    STARTUPINFO si;	
	DWORD flags = 0;

	memset(&pi, 0, sizeof(pi));

    // Set up the start up info struct.
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

	/* CreateProcessW can modify pCmdLine thus we allocate memory */	
	int pCmdSz = _tcslen(m_pcGDBCmd) + 1;
    LPTSTR pCmdLine = new TCHAR[pCmdSz];
    if (pCmdLine == 0) {
        DisplayError(_T("GDBWrapper::launch: failed to allocate memory for pCmdLine"));
    }
    _tcscpy_s(pCmdLine, pCmdSz, m_pcGDBCmd);
	
    // Launch the process
    if (!CreateProcess(NULL, pCmdLine, NULL, NULL, TRUE,						
                        flags, NULL, NULL, &si, &pi))
        ErrorExit(_T("CreateProcess"));
			
	m_hProcess = pi.hProcess;

	if (!CloseHandle(pi.hThread)) DisplayError(_T("CloseHandle"));
}

int _tmain(int argc, _TCHAR* argv[])
{	
	TCHAR msg[1024];
	memset(msg, 0, 1024);
	
    WCHAR wszThisFile[MAX_PATH + 1];
    GetModuleFileName(NULL, wszThisFile, MAX_PATH + 1);
	
//	WCHAR path_buffer[_MAX_PATH];
//	WCHAR drive[_MAX_DRIVE];
//	WCHAR dir[_MAX_DIR];
//	WCHAR filename[] = L"wrapper";
//	WCHAR ext[] = L"log";

	String^ tempPath = Environment::GetEnvironmentVariable("APPDATA"); 
	tempPath += "\\BlackBerry\\wrapper.log";
	pin_ptr<const wchar_t> path_buffer = PtrToStringChars(tempPath);

//	_wsplitpath(wszThisFile, drive, dir, NULL, NULL);
//	_wmakepath(path_buffer, drive, dir, filename, ext);
		
	int ret = wcstombs ( path_log, path_buffer, _MAX_PATH );

	FILE* file = fopen(path_log, "w"); // just to delete a possible existing file
	fclose(file);

	logPrint(_T("Starting"));

	HANDLE handles[2];
	handles[0] = OpenEventW(EVENT_ALL_ACCESS, TRUE, argv[2]); // Ctrl-C signal
	handles[1] = OpenEventW(EVENT_ALL_ACCESS, TRUE, argv[3]); // Signal to terminate the wrapper process


	_stprintf(msg, _T("args: %s %s %s %s\r\n"), argv[0], argv[1], argv[2], argv[3]);
	_tprintf(msg);
	logPrint(msg);

	_stprintf(msg, _T("Ctrl-C handler: name: %s handle: %p\r\n"), argv[2], handles[0]);
	_tprintf(msg);
	logPrint(msg);	

	_stprintf(msg, _T("Terminate handler: name: %s handle: %p\r\n"), argv[3], handles[1]);
	_tprintf(msg);
	logPrint(msg);	
	
	GDBWrapper* g = new GDBWrapper(argv[1], handles[0], handles[1]);

	BOOL exitProc = FALSE;
	while(!exitProc)
	{
		// Wait for a CTRL-C event indicating GDB should be interrupted
		logPrint(_T("WaitForMultipleObjects"));
		DWORD event = WaitForMultipleObjects(2, handles, false, INFINITE);
		switch (event)
		{
		case WAIT_OBJECT_0:
			logPrint(_T("WAIT_OBJECT_0 (Ctrl-C)"));			
			GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
			break;
		case WAIT_OBJECT_0 + 1:
			logPrint(_T("WAIT_OBJECT_0 + 1 (Terminate)"));			
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

	g->shutdown();

	return 0;
}
