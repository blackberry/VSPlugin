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

// gdb-connect.cpp : Defines the entry point for the console application.
//
// Based on code sample from http://support.microsoft.com/kb/190351

#include "stdafx.h"

#include "gdb-connect.h"
#include <windows.h>
#include <strsafe.h>

#include <string>
#include <iostream>
#include <fstream>
#include <time.h>
#include <unordered_map>

#include <process.h>
#include < vcclr.h >

using namespace std;
using namespace System;

typedef struct ListeningThreadDataStruct // used to send parameters for the listening thread (the one that is listening for GDB responses)
{
	GDBConsole *console;
	string *parsingInstructions;
} ListeningThreadData;

typedef struct ParsingThreadDetailsStruct  //used to send parameters for the parsing threads.
{ 
	char* response;
	char* parsingInstruction;
	int* seqStamp;
} ParsingThreadDetails;

typedef struct GDBBufferStruct  //used to send parameters for the parsing threads.
{ 
	int seqId;
	int instructionCode;
	string param;
} GDBBufferEntry;

HANDLE hInputBufferMutex;
HANDLE hGDBBufferMutex;
HANDLE hOutputBufferMutex;
HANDLE inputEvent;
HANDLE asyncOutputEvent;

string outputText = ""; // used only for testing to generate the output file. Will be deleted later.

extern HMODULE _hModThis;

char outputBuffer[OutputBufferSize][ParsedMessageSize];		// Output buffer. Each entry has a parsed message. 
int out_OutputBuffer = SyncInstructionsSize;				// remove element from this position (outputBuffer). Input depends on the seqStamp, i.e., ordered according to GDB responses. Positions 0 to 49 are reserved for synchronous commands

char inputBuffer[InputBufferSize][GDBCommandSize];		// Input buffer (Circular). Each entry has a GDB command
int in_InputBuffer = 0;					// add element in this position (inputBuffer)
int out_InputBuffer = 0;				// remove element from this position (inputBuffer)


GDBConsole* GDBConsole::m_instance = NULL;
TCHAR* GDBConsole::m_pcGDBCmd = NULL;

struct GDBBufferMainStruct
{
	int quantity;
	GDBBufferEntry GDBBufferData[GDBBufferSize];
} GDBBuffer;
//GDBBufferEntry GDBBuffer[GDBBufferSize];		// Input buffer. Each entry has three data: the third is the command arguments, if needed.
//int GDBBuffer[GDBBufferSize][2];		// Input buffer. Each entry has two data: 
							// the first one corresponds to the sequencial number (seqID) of the instruction that was sent to GDB while the second corresponds to the ID of this instruction
bool exitGDB = false;

char path_log[_MAX_PATH]; // contains the path ot GDB output log, generated when GDB console is instantiated and needed in logPrint()

/// Generic function to print to a log file.
void logPrint(char* buffer) {	
	if (LOG_GDB_RAW_IO) {
		time_t tt = time(NULL);
		struct tm tm;
		char buf[32];

		tm = *localtime(&tt);

		strftime(buf, 31, "%H:%M:%S", &tm);

		FILE* file = fopen((char *)path_log, "a");
		fprintf(file, "%s - %s", buf, buffer);
		fclose(file);
	}
}

GDBConsole::GDBConsole() : 
	m_hInputRead(NULL), m_hInputWrite(NULL), m_hOutputRead(NULL), 
	m_hOutputWrite(NULL), m_hErrorWrite(NULL), m_isClosed(FALSE),
	m_hCleanupMutex(NULL)
{
    HANDLE hOutputReadTmp = NULL;
	HANDLE hInputWriteTmp = NULL;    
    SECURITY_ATTRIBUTES sa;

    out_OutputBuffer = SyncInstructionsSize;
    in_InputBuffer = 0;
	out_InputBuffer = 0;

	m_hCleanupMutex = CreateMutex(NULL, FALSE, NULL);

    // Set up the security attributes struct used when creating the pipes
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle = TRUE;

    // Create the child output pipe.
    if (!CreatePipe(&hOutputReadTmp, &m_hOutputWrite, &sa, 0))
        DisplayError(_T("CreatePipe"));

    // Create a duplicate of the output write handle for the std error
    // write handle. This is necessary in case the child application
    // closes one of its std output handles.
    if (!DuplicateHandle(GetCurrentProcess(), m_hOutputWrite,
                        GetCurrentProcess(), &m_hErrorWrite, 0,
                        TRUE, DUPLICATE_SAME_ACCESS))
        DisplayError(_T("DuplicateHandle"));


    // Create the child input pipe.
    if (!CreatePipe(&m_hInputRead, &hInputWriteTmp, &sa, 0))
        DisplayError(_T("CreatePipe"));


    // Create new output read handle and the input write handles. Set
    // the Properties to FALSE. Otherwise, the child inherits the
    // properties and, as a result, non-closeable handles to the pipes
    // are created.
    if (!DuplicateHandle(GetCurrentProcess(), hOutputReadTmp,
                        GetCurrentProcess(),
                        &m_hOutputRead, // Address of new handle.
                        0, FALSE, // Make it uninheritable.
                        DUPLICATE_SAME_ACCESS))
        DisplayError(_T("DuplicateHandle"));

    if (!DuplicateHandle(GetCurrentProcess(), hInputWriteTmp,
                        GetCurrentProcess(),
                        &m_hInputWrite, // Address of new handle.
                        0, FALSE, // Make it uninheritable.
                        DUPLICATE_SAME_ACCESS))
		DisplayError(_T("DuplicateHandle"));


    // Close inheritable copies of the handles you do not want to be
    // inherited.
    if (!CloseHandle(hOutputReadTmp)) DisplayError(_T("CloseHandle"));
    if (!CloseHandle(hInputWriteTmp)) DisplayError(_T("CloseHandle"));

    prepAndLaunchRedirectedChild();

    // Close pipe handles (do not continue to modify the parent).
    // You need to make sure that no handles to the write end of the
    // output pipe are maintained in this process or else the pipe will
    // not close when the child process exits and the ReadFile will hang.
    if (!CloseHandle(m_hOutputWrite)) DisplayError(_T("CloseHandle"));
    if (!CloseHandle(m_hInputRead )) DisplayError(_T("CloseHandle"));
    if (!CloseHandle(m_hErrorWrite)) DisplayError(_T("CloseHandle"));

//	WCHAR path_buffer[_MAX_PATH];
//	getThisDLLPath(path_buffer, L"gdb-output", L"log");	
		
	String^ tempPath = Environment::GetEnvironmentVariable("APPDATA"); 
	tempPath += "\\BlackBerry\\gdb-output.log";
	pin_ptr<const wchar_t> path_buffer = PtrToStringChars(tempPath);

	int ret = wcstombs ( path_log, path_buffer, _MAX_PATH );

	FILE* file = fopen(path_log, "w"); // just to delete a possible existing file
	fclose(file);
}

GDBConsole* GDBConsole::getInstance() {
	if (m_instance == NULL) {
		// Initialize it
		m_instance = new GDBConsole;
	}
	return m_instance;
}

void GDBConsole::shutdown() {
	if (m_instance != NULL) {
		m_instance->cleanup();
	}
}

void GDBConsole::setGDBPath(const TCHAR* pcGDBCmd) {	
	size_t numChars = _tcslen(pcGDBCmd) + 1;
		
	// The path length may have changed, so reallocate for this one
    if (m_pcGDBCmd) {	
		delete[] m_pcGDBCmd;		
	}
	m_pcGDBCmd = new TCHAR[numChars * sizeof(TCHAR)];

	_tcscpy_s(m_pcGDBCmd, numChars, pcGDBCmd);
}

BOOL GDBConsole::isClosed() {
	return m_isClosed;
}

void GDBConsole::cleanup() {	
	WaitForSingleObject(m_hCleanupMutex, INFINITE);
	// Check if cleanup required
	if (!m_isClosed) {
		m_isClosed = TRUE;

		if (m_pcGDBCmd != NULL) {
			delete[] m_pcGDBCmd;
			m_pcGDBCmd = NULL;
		}
    	
		// Clean up and kill GDBWrapper process
		if (!SetEvent(m_eventTerminate)) {
			// Force termination
			TerminateProcess(m_hProcess, 0);		
		}
	
		// Wait for process to terminate
		WaitForSingleObject(m_hProcess, INFINITE);
	
		if (!CloseHandle(m_hProcess)) DisplayError(_T("CloseHandle"));

		if (!CloseHandle(m_hOutputRead)) DisplayError(_T("CloseHandle"));
		if (!CloseHandle(m_hInputWrite)) DisplayError(_T("CloseHandle"));

		m_instance = NULL;
	}	
	ReleaseMutex(m_hCleanupMutex);	
}

GDBConsole::~GDBConsole() {
	cleanup();
}

/////////////////////////////////////////////////////////////////////// 
// PrepAndLaunchRedirectedChild
// Sets up STARTUPINFO structure, and launches redirected child.
/////////////////////////////////////////////////////////////////////// 
void GDBConsole::prepAndLaunchRedirectedChild(void)
{
	HANDLE stdHandles[3];
	LPCTSTR lpApplicationName = NULL;
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
	DWORD flags = 0;
	DWORD pid = GetCurrentProcessId();
	TCHAR eventCtrlCName[MAX_EVENT_NAME_LENGTH];
	TCHAR eventTerminateName[MAX_EVENT_NAME_LENGTH];

	_stprintf(eventCtrlCName, _T("Ctrl-C-%i"), pid);
	_stprintf(eventTerminateName, _T("Terminate-%i"), pid);

	// Create event for wrapper process to wait on for CTRL-C
	m_eventCtrlC = CreateEventW(NULL, FALSE, FALSE, eventCtrlCName);

	// Create event for wrapper process to wait on for termination
	m_eventTerminate = CreateEventW(NULL, FALSE, FALSE, eventTerminateName);

	// Set process information
	memset(&pi, 0, sizeof(pi));

    // Set up the start up info struct.
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
	si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
    si.hStdOutput = m_hOutputWrite;
    si.hStdInput  = m_hInputRead;
    si.hStdError  = m_hErrorWrite;
	si.wShowWindow = SW_HIDE;

	flags = CREATE_NEW_CONSOLE;	
	
	WCHAR path_buffer[_MAX_PATH];
	getThisDLLPath(path_buffer, L"GDBWrapper", L"exe");	

    if (!m_pcGDBCmd) {
		cleanup();
		ErrorExit(_T("m_pcGDBCmd is NULL"));
	}

	/* CreateProcess can modify pCmdLine thus we allocate memory */		
	size_t numChars = _tcslen(m_pcGDBCmd) + MAX_EVENT_NAME_LENGTH * 2 + 1;	
	TCHAR* pCmdLine = new TCHAR[numChars * sizeof(TCHAR)];

	if (pCmdLine == 0) {
        DisplayError(_T("prepAndLaunchRedirectedChild: failed to allocate memory for pCmdLine"));
    }    
	
	_stprintf(pCmdLine, _T("\"%s\" \"%s\" \"%s\" \"%s\""), path_buffer, m_pcGDBCmd, eventCtrlCName, eventTerminateName);
			
    // Launch the process (create a new console)
    if (!CreateProcess(NULL,			/* executable name */
						pCmdLine,		/* command line */
						NULL,			/* process security attribute */
						NULL,			/* thread security attribute */
						TRUE,			/* inherits system handles */
                        flags,			/* normal attached process */
						NULL,			/* environment block */ 
						NULL,			/* change to the new current directory */ 
						&si,			/* (in)  startup information */ 
						&pi))			/* (out) process information */
        ErrorExit(_T("CreateProcess"));

	if(NULL != pCmdLine) {
	    delete[] pCmdLine;
		pCmdLine = NULL;
	}		

	m_hProcess = pi.hProcess;

	// Close unneeded handles
    if (!CloseHandle(pi.hThread)) DisplayError(_T("CloseHandle"));			
}

/*
	Read the stdout of the GDB console process
	lpBuffer - buffer to store output
	bufSize - size of buffer in bytes (leave space for the null char)
	Returns:
		0 - Error
		1 - Success
		2 - Got part of the available read buffer (call again to get more)
*/

int GDBConsole::readOutput(CHAR* lpBuffer, int bufSize, int* nCharsRead)
{		
    DWORD nBytesRead;	

	if (isClosed()) {
		DisplayError(_T("readOutput: GDBConsole is closed"));
		return 0;
	}

	if (lpBuffer == NULL) {
		DisplayError(_T("readOutput: null buffer pointer"));
		return 0;
	}

	if (bufSize < 1) {
		DisplayError(_T("readOutput: needs bufSize > 1"));
		return 0;
	}

	if (!ReadFile(m_hOutputRead, lpBuffer, bufSize,
	                                &nBytesRead, NULL) || !nBytesRead)
    {
		if (GetLastError() == ERROR_BROKEN_PIPE) {
			// pipe done - normal exit path.
			cleanup();
			return 0;
		} else {
			ErrorExit(_T("ReadFile"));				
		}
	}
		
	*nCharsRead = nBytesRead / sizeof(CHAR);

	const char* pLastChar = lpBuffer + nBytesRead;
	pLastChar -= 2;		

	logPrint((char*) lpBuffer);

	if (strcmp("\r\n", pLastChar)) {
		// Didn't get end of line
		return 2;
	}
			 
	return 1;
}

/*
	Send a GDB command to the stdin of the GDB console process
	lpCmd - null-terminated string ending in \r\n	
	Returns:
		0 - Error
		1 - Success
*/
int GDBConsole::sendCommand(const CHAR* lpCmd)
{
    DWORD nBytesWrote;    
	int nCmdChars = strlen(lpCmd);

	if (isClosed()) {
		DisplayError(_T("readOutput: GDBConsole is closed"));
		return 0;
	}

	// Input validation	
	if (nCmdChars < 2) {
		DisplayError(_T("SendCommand: nCmdChars < 2"));	
	} else {		
		const char* pLastChar = strchr(lpCmd, '\0');
		pLastChar -= 2;		
		if (strcmp("\r\n", pLastChar)) {
			DisplayError(_T("SendCommand: lpCmd doesn't end in \\r\\n"));
		}
	}
		
    if (!WriteFile(m_hInputWrite, lpCmd, nCmdChars * sizeof(CHAR), &nBytesWrote, NULL))
    {			
		if (GetLastError() == ERROR_NO_DATA) {
			// Pipe was closed (normal exit path).
			cleanup();
			return 0;
		} else {
			DisplayError(_T("WriteFile"));
			return 0;
		}
    } else {		
		printf("Sending [%s] (%d chars)\n", lpCmd, nBytesWrote / sizeof(CHAR));
	}

	logPrint((char*) lpCmd);

    return 1;
}


HANDLE GDBConsole::getStdOutHandle() {
	return m_hOutputRead;
}

void GDBConsole::sendCtrlC() {
	if (!SetEvent(m_eventCtrlC)) {
		DisplayError(_T("SetEvent"));
	}
}

// Helper utility to get this binary's full-path
void getThisDLLPath(wchar_t* path_buffer, const wchar_t* fname, const wchar_t* ext) {   
    WCHAR wszThisFile[MAX_PATH + 1];
    GetModuleFileName(_hModThis, wszThisFile, MAX_PATH + 1);
	
	WCHAR drive[_MAX_DRIVE];
	WCHAR dir[_MAX_DIR];

	_wsplitpath(wszThisFile, drive, dir, NULL, NULL);
	_wmakepath(path_buffer, drive, dir, fname, ext);
}

/////////////////////////////////////////////////////////////////////// 
// DisplayError
// Displays the error number and corresponding message.
/////////////////////////////////////////////////////////////////////// 
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

// Check if there is more data to be read from GDB's stdout without removing it from the pipe's buffer
BOOL isMoreOutputAvailable (GDBConsole* console) {
	DWORD totalBytesAvail;	

	if (!PeekNamedPipe(console->getStdOutHandle(), NULL, NULL, NULL, &totalBytesAvail, NULL)) {
		TCHAR lpError[256];		
		_tprintf(lpError, _T("isMoreOutputAvailable: %s"), GetLastError());
		DisplayError(lpError);
		return false;
	}
		
	return (totalBytesAvail > 0);
}

// Print output received until the GDB prompt is ready
string waitForPrompt(GDBConsole* console, bool sync) {
	char lpBuffer [1024];
	int nCharsRead = 0;
	int rc = 0;
	BOOL isPromptReady = FALSE;
	const char* promptString = "(gdb) \r\n";
	const char* lpTmp;

	string message = "";

	do {
		memset(lpBuffer, 0, sizeof(lpBuffer));

		if (console->isClosed()) {
			break;
		}

		if (isMoreOutputAvailable(console))
		{
			rc = console->readOutput(lpBuffer, sizeof(lpBuffer) - 1, &nCharsRead);
			if (rc) {
				// Terminate the buffer

				lpBuffer[nCharsRead] = '\0';
			
				message += lpBuffer;

				if (nCharsRead >= strlen(promptString)) {
					lpTmp = lpBuffer + nCharsRead - strlen(promptString);
					if (!strcmp(lpTmp, promptString)) {
						isPromptReady = TRUE;
					}
				}
			} else {
				DisplayError(_T("waitForPrompt: readOutput got error"));
				message += "\n\nwaitForPrompt: readOutput got error\n\n";
				break;
			}
		}
		else if (!sync)
			break;
	} while (!isPromptReady && !console->isClosed());	

	return message;
}

/*bool isMoreInputAvailable()
{	// Check if there is any input from VS.
	// MUST BE IMPLEMENTED. THIS ONE IS ONLY FOR TESTING PURPUOSES
	int input;
	fflush(stdin);
	printf("\nIs there any input? (1-yes/0-no) \n");
	cin >> input;
	return(input);
}*/

void cleanBuffers()
{
	for (int i = 0; i < InputBufferSize; i++)
	{
		strcpy(inputBuffer[i],"");
	}
	for (int i = 0; i < GDBBufferSize; i++)
	{
		GDBBuffer.GDBBufferData[i].seqId = -1;
		GDBBuffer.GDBBufferData[i].param = "";
	}
	GDBBuffer.quantity = 0;
	for (int i = 0; i < OutputBufferSize; i++)
	{
		strcpy(outputBuffer[i], "");
	}
}

bool isGDBBufferEmpty()
{
	bool empty = true;
	WaitForSingleObject( hGDBBufferMutex, INFINITE );
	if (GDBBuffer.quantity == 0)
		empty = true;
	else
			empty = false;
	ReleaseMutex( hGDBBufferMutex);
	return empty;
}

bool addIntoGDBBuffer(int seq_id, int instructionCode, string param)
{
	bool got = false;
	int pos = seq_id % GDBBufferSize;
	WaitForSingleObject( hGDBBufferMutex, INFINITE );
	if (GDBBuffer.GDBBufferData[pos].seqId == -1)
	{
		GDBBuffer.GDBBufferData[pos].seqId = seq_id;
		GDBBuffer.GDBBufferData[pos].instructionCode = instructionCode;
		GDBBuffer.GDBBufferData[pos].param = param;
		GDBBuffer.quantity += 1;
		got = true;
	}
	ReleaseMutex( hGDBBufferMutex);
	return (got);
}

int removeFromGDBBuffer(int seq, string *param)
{
	int output = -1;
	if (seq < 0)
		if (seq == -2)  // asynchronous
			return 0;
		else if (seq == -3)  // -breakpoint-modified
			return 1;
		else
			return seq; // error

	if (seq >= SyncInstructionsSize)
		seq = (seq % SyncInstructionsSize) + SyncInstructionsSize;

	WaitForSingleObject( hGDBBufferMutex, INFINITE );
	if (GDBBuffer.GDBBufferData[seq].seqId != -1)
	{
		output = GDBBuffer.GDBBufferData[seq].instructionCode;
		*param = GDBBuffer.GDBBufferData[seq].param;
		GDBBuffer.GDBBufferData[seq].seqId = -1;
		GDBBuffer.quantity -= 1;
//		GDBBuffer[seq].param = "";
	}
	ReleaseMutex( hGDBBufferMutex);
	return output;
}

bool addIntoOutputBuffer(int seq_stamp, char *parsedMessage)
{
	bool got = false;
	int pos = seq_stamp % OutputBufferSize;
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[pos], "") == 0)
	{
		strcpy(outputBuffer[pos], parsedMessage);
		got = true;
		if (pos >= SyncInstructionsSize)
			SetEvent(asyncOutputEvent);
	}
	ReleaseMutex( hOutputBufferMutex);
	return (got);
}

void removeFromOutputBuffer(char * output)
{
//	char output[OutputBufferSize] = "";
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[out_OutputBuffer], "") != 0)
	{
//		output = new(char[ParsedMessageSize]);
		strcpy(output, outputBuffer[out_OutputBuffer]);
		strcpy(outputBuffer[out_OutputBuffer], "");
		out_OutputBuffer += 1;
		if (out_OutputBuffer == OutputBufferSize)
			out_OutputBuffer = SyncInstructionsSize; // positions 0 to 49 are reserved for synchronous commands.
	}
	ReleaseMutex( hOutputBufferMutex);
	if (strcmp(output, "") == 0) // means that the asynchronous part of output buffer is empty.
		WaitForSingleObject(asyncOutputEvent, WAIT_TIME);
	if (strcmp(output, "$#@EMPTY@#$") == 0)
		output = "";
//	return (output);
}

void removeSyncFromOutputBuffer(char * output, int ID)
{
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[ID], "") != 0)
	{
		strcpy(output, outputBuffer[ID]);
		strcpy(outputBuffer[ID], "");
	}
	ReleaseMutex( hOutputBufferMutex);
}

bool isInputBufferEmpty()
{
	bool empty;
	WaitForSingleObject( hInputBufferMutex, INFINITE );
	if (in_InputBuffer == out_InputBuffer)
		empty = true;
	else
		empty = false;
	ReleaseMutex( hInputBufferMutex);
	return empty;
}

bool addIntoInputBuffer(char GDBCommand[GDBCommandSize])
{
	bool got = false;
	bool fireEvent = false;
	WaitForSingleObject( hInputBufferMutex, INFINITE );
	if (!((in_InputBuffer == (InputBufferSize - 1) && out_InputBuffer == 0) || ((in_InputBuffer + 1) == out_InputBuffer)))
	{
		if (in_InputBuffer == out_InputBuffer)
			fireEvent = true;
		strcpy(inputBuffer[in_InputBuffer], GDBCommand);
		in_InputBuffer += 1;
		if (in_InputBuffer == InputBufferSize)
			in_InputBuffer = 0;
		got = true;
	}
	if (fireEvent)
		SetEvent(inputEvent);
	ReleaseMutex( hInputBufferMutex);
	return (got);
}

void removeFromInputBuffer(char * input)
{
//	char input[GDBCommandSize] = "";
	WaitForSingleObject( hInputBufferMutex, INFINITE );
	if (in_InputBuffer != out_InputBuffer)
	{
		strcpy(input, inputBuffer[out_InputBuffer]);
		strcpy(inputBuffer[out_InputBuffer],"");
		out_InputBuffer += 1;
		if (out_InputBuffer == InputBufferSize)
			out_InputBuffer = 0;
	}
	ReleaseMutex( hInputBufferMutex);
//	return input;
}


int getSeqID(string response)
{
	int end;
	int begin;
	char seqIDstr[11] = "";
	int seqID = -1;

	end = response.find("^done", 0);
	if (end == -1)
	{
		end = response.find("^running", 0);
		if (end == -1)
		{
			end = response.find("^error", 0);
			if (end == -1)
			{
				end = response.find("^connected", 0);
				if (end == -1)
				{
					end = response.find("*running", 0);
					if (end != -1)
						return -2;  // asynchronous
					else
					{
						end = response.find("*stopped", 0);
						if (end != -1)
						{
							return -2;   // asynchronous
						}
						else
						{
							if (response.substr(0, 20) == "=breakpoint-modified")
							{
								return -3;  //-breakpoint-modified
							}
							else
							{
								return -1;
							}
						}
					}
				}
			}
		}
	}
	if (end < 15) // SeqID is the first data in the string
		begin = 0;
	else
	{
		begin = end - 1;
		while ((response[begin] != '"') && (response[begin] != '}'))
			begin--;
		begin++;
	}
	response.copy(seqIDstr, end - begin, begin);
	seqID = atoi(seqIDstr);
	return(seqID); 
}

int getNextChar(char token, string txt, int pos)
{
	int end = pos;
	int slash, aux;
	do
	{
		slash = 0;
		end = txt.find(token, pos);
		if (end == -1)
			end = txt.length();
		aux = end - 1;
		while ((aux >= 0) && (txt[aux] == '\\'))
		{
			slash++;
			aux--;
		}
		pos = end + 1;
	} while ((slash % 2) != 0);
	return end;
}

int searchResponse(string response, string txt, int begin, int times, bool forward, char instruction)
{
	for (; times > 0; times--)
	{
		if ((begin >= response.length()) || (begin == -1))
			break;
		if (forward)
		{
			if (begin == 0) 
				if ((txt[0] == '\r') && (txt[1] == '\n'))
			{
				string check = txt.substr(2, txt.length() - 2);
				if (response.substr(0, check.length()) == check)
					continue;
			}
			begin = response.find(txt, begin);
			if ((begin != -1) && ((times != 1) || (instruction == '?')))
				begin += txt.length();
		}
		else
		{
			begin = response.rfind(txt, begin);
			if ((begin != -1) && (times == 1))
				begin += txt.length();
			if ((begin == -1) && ((txt[0] == '\r') && (txt[1] == '\n')))
				begin = 2; // size of "\r\n"
		}
	}
	if (times == 0)
		return begin;
	return -1;
}

string substituteVariables(string txt, char variables[10][128])
{
	do // substitute variables ($) by their values
	{
		int aux = getNextChar('$', txt, 0);
		if (aux < txt.length())
		{
			aux++;
			int aux2 = getNextChar('$', txt, aux);
			string index = txt.substr(aux, aux2 - aux);
			if (index == "EOL")
			{
				txt = txt.substr(0, aux - 1) + '\r' + '\n' + txt.substr(aux2 + 1, txt.length());
			}
			else
				txt = txt.substr(0, aux - 1) + variables[atoi(index.c_str())] + txt.substr(aux2 + 1, txt.length());
		}
		else
			break;
	} while (true);
	return txt;
}

int findClosing(char opening, char closing, string parsingInstruction, int ini)
{
	int end = getNextChar(closing, parsingInstruction, ini);
	int other = ini;
	bool found = true;
	do 
	{
		other = getNextChar(opening, parsingInstruction, other + 1);
		if (other < end)
		{
			end = getNextChar(closing, parsingInstruction, end + 1);
		}
		else
			found = false;
	} while (found);
	return end;
}

// Parse GDB response for a given command
string parseGDB(string *response, string parsingInstruction, int respBegin, bool repeat, char variables[10][128], string separator) 
{
	string result = "";
	string originalParsingInstruction = parsingInstruction;
	int parsePos = 0;
	int home = respBegin;
	int respEnd = 0;
	int limit = (*response).length();
	bool found = false;

	bool removeSelection = false;
	int removeFromHere = -2;    // -2 - not setted; -1 - indicates that it will be setted in the first '?' instruction; >=0 - already setted.

	if (separator == "$EOL$")
		separator = "\r\n";

	while ((parsePos < parsingInstruction.length()) || repeat)
	{
		if ((repeat) && (parsePos >= parsingInstruction.length()))  // repeat instructions till the end of the response
		{
			if (found)
			{
				if (result != "")
					result += separator;
				parsePos = 0;
				parsingInstruction = originalParsingInstruction;
				limit = (*response).length();
				found = false;
			}
			else
				repeat = false;
			continue;
		}

		if (parsingInstruction[parsePos] == '(')  // repeat instructions till the end of the response
		{  
			int end = parsePos;
			string repeatInstruction;

			end = findClosing('(', ')', parsingInstruction, parsePos);

			repeatInstruction = parsingInstruction.substr(parsePos + 1, end - (parsePos + 1));

			end++;
			if (parsingInstruction[end] == ':')
			{
				parsePos = end + 1;
				end = getNextChar(';', parsingInstruction, parsePos);
				separator = parsingInstruction.substr(parsePos, end - parsePos);
				int aux = separator.find("\\;");
				if (aux != -1)
					separator.erase(aux,1);
			}
			parsePos = end + 1;

			result += parseGDB(response, repeatInstruction, respBegin, true, variables, separator);

			continue;
		}

		if (parsingInstruction[parsePos] == '?') // search and set the beginning of the searched string. Return this position
		{
			int times = 1;
			bool forward = true;
			int end;
			int aux;
			string txt;

			parsePos++;
			if (parsingInstruction[parsePos] == '%')
			{
				removeFromHere = -1;
				parsePos++;
			}

			if (parsingInstruction[parsePos] == '<')
			{
				forward = false;
				parsePos++;
			}

			if (parsingInstruction[parsePos] != '?')
			{
				end = getNextChar('?', parsingInstruction, parsePos);
				times = atoi(parsingInstruction.substr(parsePos, end - parsePos).c_str());
				parsePos = end;
			}
			parsePos++;

			end = getNextChar(';', parsingInstruction, parsePos);
			
			txt = substituteVariables(parsingInstruction.substr(parsePos, end - parsePos), variables);

			aux = txt.find('\\');
			while (aux != -1)
			{   // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
				txt = txt.substr(0, aux) + txt.substr(aux + 1, txt.length());
				aux = txt.find('\\', aux + 1);
			};

			int previousRespBegin = respBegin;
			respBegin = searchResponse(*response, txt, respBegin, times, forward, '?');

			//		check if it was defined a set of instructions to be executed if the search is NOT valid, i.e., there is a '{' after the ';'.
			//		If the search is not valid, go to the '}' and keep going. If there is no '{}' and the search is not valid, just end evaluating these instructions.
			//		Normally, there is '{}' to allow handling gdb errors.
			if  (parsingInstruction[end + 1] == '{')
			{
				parsePos = end + 1;

				end = findClosing('{', '}', parsingInstruction, parsePos);

				if ((respBegin != -1) && (respBegin <= limit))
				{
					found = true;
					// the following instructions to be evaluated are between '{' and '}'.
					if (((end + 1) < parsingInstruction.length()) && (parsingInstruction[end+1] != '{'))
					{
						parsingInstruction = parsingInstruction.substr(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.substr(end + 2, parsingInstruction.length());
					// or erase, what is faster?
					// parsingInstruction.erase(parsePos,1); parsingInstruction.erase(end,1);
					}
					else
					{
						// if there is '{' after a '}', it means that there is an 'else' sentence.
						int else_end;
						else_end = findClosing('{', '}', parsingInstruction, end + 1);
						parsingInstruction = parsingInstruction.substr(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.substr(else_end + 2, parsingInstruction.length());
					}

					parsePos = 0;
					if (removeFromHere == -1)
						removeFromHere = respBegin - txt.length();
				}
				else
				{
					respBegin = previousRespBegin;
					removeFromHere = -2;
					if (((end + 1) < parsingInstruction.length()) && (parsingInstruction[end+1] != '{'))
					{
						parsePos = end + 2; // move it to the next instruction, after the "};"
					}
					else
					{
						parsePos = end + 1;
						end = findClosing('{', '}', parsingInstruction, parsePos);
						parsingInstruction = parsingInstruction.substr(parsePos + 1, end - (parsePos + 1)) + parsingInstruction.substr(end + 2, parsingInstruction.length());
						parsePos = 0;
					}
				}
			}
			else
			{
				if ((respBegin == -1) || (respBegin > limit))
				{
					if (respBegin > limit)
						respBegin = previousRespBegin;

					parsePos = parsingInstruction.length();
					removeFromHere = -2;
					removeSelection = false;
				}
				else
				{
					found = true;
					parsePos = end + 1;
					if (removeFromHere == -1)
						removeFromHere = respBegin - txt.length();
				}
			}

			continue;
		}

		if (parsingInstruction[parsePos] == '@') // till here: search and set the end of the searched string. Return the string between the last ? and this "till here" position
		{
			int times = 1;
			int end;
			string txt;
			int aux;

			parsePos++;

			if (parsingInstruction[parsePos] != '@')
			{
				end = getNextChar('@', parsingInstruction, parsePos);
				times = atoi(parsingInstruction.substr(parsePos, end - parsePos).c_str());
				parsePos = end;
			}
			parsePos++;

			end = getNextChar(';', parsingInstruction, parsePos);
			
			txt = substituteVariables(parsingInstruction.substr(parsePos, end - parsePos), variables);

			aux = txt.find('\\');
			while (aux != -1)
			{   // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
				txt = txt.substr(0, aux) + txt.substr(aux + 1, txt.length());
				aux = txt.find('\\', aux + 1);
			};

			respEnd = searchResponse(*response, txt, respBegin, times, true, '@');
			if ((respEnd == -1) || (respEnd > limit))
			{
				result = "";
				break;
			}
			if (!removeSelection)
				result += (*response).substr(respBegin, respEnd - respBegin);

			respBegin = respEnd;

			if (removeFromHere >= 0)
			{
				(*response).erase(removeFromHere, respEnd + txt.length() - removeFromHere);
				respBegin = removeFromHere;
				removeFromHere = -2;
				removeSelection = false;
			}

			parsePos = end + 1;
			continue;
		}

		if (parsingInstruction[parsePos] == '#') // ??? insert the following string into the result
		{
			int end = getNextChar(';', parsingInstruction, parsePos);
			parsePos++;
			
			string txt = parsingInstruction.substr(parsePos, end - parsePos);
			int aux = txt.find('\\');
			while (aux != -1)
			{   // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
				txt = txt.substr(0, aux) + txt.substr(aux + 1, txt.length());
				aux = txt.find('\\', aux + 1);
			};

			result += substituteVariables(txt, variables);

			parsePos = end + 1;
			continue;
		}

		if (parsingInstruction[parsePos] == '~') // specify a limit, so the search instructions will search till this position.
		{
			int end = getNextChar(';', parsingInstruction, parsePos);
			parsePos++;
			
			string txt = parsingInstruction.substr(parsePos, end - parsePos);
			int aux = txt.find('\\');
			while (aux != -1)
			{   // if a '\' is found, it is eliminated because it is considered an escape character. However, if '\' is needed, add two, i.e. '\\', because only one of them will be removed
				txt = txt.substr(0, aux) + txt.substr(aux + 1, txt.length());
				aux = txt.find('\\', aux + 1);
			};

			limit = searchResponse(*response, txt, respBegin, 1, true, '~');
			if (limit == -1)
			{
				limit = (*response).length();
			}

			parsePos = end + 1;
			continue;
		}

		if (parsingInstruction[parsePos] == '0') // set the response cursor position to home_pos (normally 0 - '(' sets a different value for hoje_pos)
		{
			respBegin = home;
			parsePos += 2; // jump the '0' and ';'
			continue;
		}

		if (parsingInstruction[parsePos] == '%') // delete the string (between ? and @) from response
		{ 
			removeSelection = true;
			removeFromHere = -1;
			parsePos++;
			continue;
		}

		if (parsingInstruction[parsePos] == '$') // create a variable to store a value from response string. This value is defined by the instructions between the '=' and the @ symbol (till here)
		{ // ???
			int end, varNumber, aux;

			parsePos++;
			end = getNextChar('=', parsingInstruction, parsePos);
			aux = getNextChar('$', parsingInstruction, parsePos);
			if ((aux > 0) && (aux < end))
				end = parsingInstruction.length();
			if (end < parsingInstruction.length())
			{
				varNumber = atoi(parsingInstruction.substr(parsePos, end - parsePos).c_str());

				parsePos = end + 1;
				end = parsingInstruction.find("$$", parsePos);
				string r = parseGDB(response, parsingInstruction.substr(parsePos, end - parsePos), respBegin, false, variables, "#");
				if (r == "")
					break;
				strcpy(variables[varNumber], r.c_str());

				parsePos = end + 3; // jump $$;
			}
			else
			{
				end = parsingInstruction.find('$', parsePos);
				if (parsingInstruction.substr(parsePos, end - parsePos) == "END")
					break;
				if (parsingInstruction.substr(parsePos, end - parsePos) == "EOR")  // move respBegin to the end of the response, probably to start looking from the end to the begin.
					respBegin = response->length() - 1;
				parsePos = end + 2;
			}
			continue;
		}

	}
	return(result);
}

string parseGDB(string response, string parsingInstruction) // call this function when starting GDB (this one doesn't use the OutputBuffer because it returns the parsed response)
{
	char variables[10][128];
	char result[ParsedMessageSize] = "";

	strcpy(result, parseGDB(&response, parsingInstruction, 0, false, variables, "#").substr(0, ParsedMessageSize - 1).c_str());

	return (result);
}

void parseGDB(string response, string parsingInstruction, int seqStamp) // call this function if not using multithreaded parsing
{
	char variables[10][128];
	char result[ParsedMessageSize] = "";

	strcpy(result, parseGDB(&response, parsingInstruction, 0, false, variables, "#").substr(0, ParsedMessageSize - 1).c_str());

	if (strcmp(result,"") == 0)
	{
		// ??? Unexpected response. Must handle it.
		outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *, OR =breakpoint-modified IN THIS BELOW RESPONSE.\n";
		outputText += response;
		outputText += "\n\n *************************************************************************\n\n";
		strcpy(result, "$#@EMPTY@#$");
	}

	while (!addIntoOutputBuffer(seqStamp, result))
		Sleep(0);

//	return (result);  // ??? returning string for testing purposes. When done, remove it and change this function a void one.
}

unsigned __stdcall parseGDBResponseThread(void *arg) // call this function if using multithreaded parsing
{
	ParsingThreadDetails *d = (ParsingThreadDetails *)arg;
	string response = d->response;
	string parsingInstruction = d->parsingInstruction;
	int seqStamp = *(d->seqStamp);

	char result[ParsedMessageSize] = "";
	char variables[10][128];

	strcpy(result, parseGDB(&response, parsingInstruction, 0, false, variables, "#").substr(0, ParsedMessageSize - 1).c_str());

	if (strcmp(result,"") == 0)
	{
		// ??? Unexpected response. Must handle it.
		outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *, OR =breakpoint-modified IN THIS BELOW RESPONSE.\n";
		outputText += response;
		outputText += "\n\n *************************************************************************\n\n";
		strcpy(result, "$#@EMPTY@#$");
	}

	while (!addIntoOutputBuffer(seqStamp, result))
		Sleep(0);

	return 0;
}

unsigned __stdcall listeningGDB(void *arg)
{
	ListeningThreadData *listen = (ListeningThreadData *)arg;
	
	int seqStamp = SyncInstructionsSize; // As positions 0 to 49 are reserved for synchronous commands

	string response = "";
	int getMoreData = 10;

	while(!exitGDB)
	{
		int instructionCode;
		int gdbPos;

//		if (!isMoreOutputAvailable(listen->console))
//			continue;
		response += waitForPrompt(listen->console, false);

		gdbPos = response.find("(gdb)");

		while (gdbPos == 0)
		{
			response.erase(0,5);
			gdbPos = response.find("(gdb)");
		}

		if ((gdbPos == -1) && (response.length() > 3))
			gdbPos = response.length();

		while (gdbPos != -1)  // just in case gdb was faster than our debugger. If that happens, there will be two or more responses in the buffer, so we have to parse each of them individually.
		{
			int asyncRunning = -1;
			int asyncStopped = -1;
			int seqId;
			string param = "";
			string oneResponse;

			oneResponse = response.substr(0, gdbPos);

			asyncRunning = oneResponse.find("*running");
			asyncStopped = oneResponse.find("*stopped");
			if ((asyncRunning != -1) && (asyncStopped != -1))
			{
				if (asyncRunning > asyncStopped)
					gdbPos = asyncRunning;
				else
					gdbPos = asyncStopped;
				oneResponse = oneResponse.substr(0, gdbPos);
			}

			seqId = getSeqID(oneResponse);
			instructionCode = removeFromGDBBuffer(seqId, &param);

			if (param != "")
			{
				oneResponse = oneResponse.erase(oneResponse.length() - 2, 2);
				oneResponse = oneResponse + param + '\r' + '\n';
			}

			if (instructionCode == -1)
			{
				getMoreData -= 1; // get more data from buffer to try to identify the type of the response
				if (getMoreData == 0)
					instructionCode = 0; // if it was not possible to identify the type of the response after reading buffer 5 times, consider it an asynchronous one. Check the outputBuffer for empty entries.
				else
					break; // don't erase the response... Trying to get more data.
			}

//			if (getMoreData != 3)
				getMoreData = 10;

			if (instructionCode != -1)
			{
				// create a thread to parse the response with parsingInstructions[instructionCode]. Send also a sequencial number, so VS can sort the results.
				// can use multiple parsing threads or not, depending on the const value MultipleThreads defined in gdb-connect.h
				if (MultipleThreads)
				{
					HANDLE ParsingThread = new(HANDLE);
					unsigned ParsingThreadID;
					
					ParsingThreadDetails *details = new(ParsingThreadDetails);
				
					details->parsingInstruction = new(char[listen->parsingInstructions[instructionCode].length()]);
					strcpy(details->parsingInstruction, listen->parsingInstructions[instructionCode].c_str());
					details->response = new(char[oneResponse.length()]);
					strcpy(details->response, oneResponse.c_str());
					details->seqStamp = new(int);
					if ((seqId >= 0) && (seqId < SyncInstructionsSize))
						*(details->seqStamp) = seqId;
					else				
						*(details->seqStamp) = seqStamp;

					ParsingThread = (HANDLE)_beginthreadex( NULL, 0, &parseGDBResponseThread, (void *)details, 0, &ParsingThreadID);
				}
				else  // without multiple parsing threads
				{
					if ((seqId >= 0) && (seqId < SyncInstructionsSize))
						parseGDB(oneResponse.c_str(), listen->parsingInstructions[instructionCode].c_str(), seqId);
					else				
						parseGDB(oneResponse.c_str(), listen->parsingInstructions[instructionCode].c_str(), seqStamp);
				}

				if ((seqId < 0) || (seqId >= SyncInstructionsSize))
				{
					seqStamp++;
					seqStamp = (seqStamp % SyncInstructionsSize) + SyncInstructionsSize;
//					if ((seqStamp % OutputBufferSize) == 0)  // position 0 is reserved for synchronous commands.
//						seqStamp++;
				}
			}
//			else
//			{
//				// ??? Unexpected response. Must handle it.
//				outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *, OR =breakpoint-modified IN THIS BELOW RESPONSE.\n";
//				outputText += oneResponse;
//				outputText += "\n\n *************************************************************************\n\n";
//				break;
//			}
			if ((asyncRunning == -1) || (asyncStopped == -1))
				gdbPos += 5;
			response.erase(0, gdbPos);
			gdbPos = response.find("(gdb)");

			if (gdbPos == -1) 
				if (response.length() > 3)
					gdbPos = response.length();
				else
					response = "";
		};
	}

	ofstream outputs;
	outputs.open ("outputs.txt");
	outputs << outputText.c_str();
	outputs.close();

    exitGDB = false;

	return 0;
}

int getInstructionCode(string command, unordered_map<string, int> map, string *param)
{
	int pos = command.find(" ");
	if (pos != -1)
	{
		*param = command.substr(pos, (command.length() -pos));
		(*param)[0] = ';';
		int i = (*param).find(" ");
		while (i != -1)
		{
			(*param)[i] = ';';
			i = (*param).find(" ");
		}
		command = command.substr(0, pos);
	}
	unordered_map<string, int>::iterator it = map.find(command);
	if (it != map.end())
	{
		if (it->second > 0)
		{
			*param = "";
		return (it->second);
		}
		else
		{
			return ((it->second) * -1);
		}
	}
	printf("Command code not found.");
	return -1;
}

bool insertingCommandCodes(unordered_map<string, int> *map, string parsingInstructions[NumberOfInstructions])
{
	WCHAR path_buffer[_MAX_PATH];
	getThisDLLPath(path_buffer, L"Instructions", L"txt");	

	ifstream inputs;
    inputs.open (path_buffer);

	if (!inputs.is_open())
    {
        cout << "\nCouldn't open Instructions.txt.\n";
		return 0;
    }
	int count = 0;
	while(true)
	{
		string instruction;
		int pos;
		getline(inputs, instruction);
		if (inputs.eof())
		{
			break;
		}
		
		pos = instruction.find(":->:");
		if (instruction[0] != '$')
		{
		(*map).insert(unordered_map<string, int>::value_type(instruction.substr(0, pos), count));
		}
		else // need to get the parameters from the GDB command and insert them into the GDB response. Count will be negative so sending and listening threads will know about that.
		{
			(*map).insert(unordered_map<string, int>::value_type(instruction.substr(1, pos - 1), count * -1));
		}
		instruction.erase(0, pos + 4);
		parsingInstructions[count] = instruction;
		count++;
	}

	inputs.close();
	return (1);
}

bool startGDB(GDBConsole *console, char* fileLocation, char* file, char* ip, unordered_map<string, int> map, string *parsingInstructions)
{
	string response;
	string command;
	string parsed = "";
	int instructionCode;
	string param = ""; // only to avoid problems with removeFromGDBBuffer

	for (int i=1; i<=4; i++)
	{
		while (!addIntoGDBBuffer(i, i, ""))   
		{
			printf("Input buffer element is already filled!"); // it should not happen, but it is better to verify it.
			Sleep(0);
		}
	}

	response = waitForPrompt(console, true);
	instructionCode = removeFromGDBBuffer(1, &param);
	if (instructionCode != -1)
	{
		parsed = parseGDB(response, parsingInstructions[instructionCode]);
	}
	else
	{
		// ??? Only to know if there is an unexpected response. It shouldn't have, but in case it has, it should be handled.
//		outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *";
		return 0;
	}

	if (ip[0] != '\0')
	{
		command = "2-target-select qnx ";
		command += ip;
		command += ":8000\r\n";
		(*console).sendCommand(command.c_str());
		response = waitForPrompt(console, true);
		response.erase(response.find("(gdb)"), 5);
		instructionCode = removeFromGDBBuffer(getSeqID(response), &param);
		if (instructionCode != -1)
		{
			parsed = parseGDB(response, parsingInstructions[instructionCode]);
		}
		else
		{
			// ??? Only to know if there is an unexpected response. It shouldn't have, but in case it has, it should be handled.
//			outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *";
			return 0;
		}

		if ((fileLocation[0] != '\0') && (file[0] != '\0'))
		{
			command = "3file ";
			command += fileLocation;
			if (fileLocation[strlen(fileLocation) -1] != '\\')
				command += "\\\\";
			command += file;
			command += "\r\n";
			(*console).sendCommand(command.c_str());
			response = waitForPrompt(console, true);
			response.erase(response.find("(gdb)"), 5);
			instructionCode = removeFromGDBBuffer(getSeqID(response), &param);
			if (instructionCode != -1)
			{
				parsed = parseGDB(response, parsingInstructions[instructionCode]);
			}
			else
			{
				//	??? ERRORS ARE NOT EXPECTED HERE because VS has already built the project, so the file exist
				// ??? Only to know if there is an unexpected response. It shouldn't have, but in case it has, it should be handled.
//				outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR *";
				return 0;
			}

			command = "4upload ";
			command += fileLocation;
			if (fileLocation[strlen(fileLocation) -1] != '\\')
				command += "\\\\";
			command += file;
			command += " /tmp/";
			command += file;
			command += "\r\n";
			(*console).sendCommand(command.c_str());
			response = waitForPrompt(console, true);
			response.erase(response.find("(gdb)"), 5);
			instructionCode = removeFromGDBBuffer(getSeqID(response), &param);
			if (instructionCode != -1)
			{
				parsed = parseGDB(response, parsingInstructions[instructionCode]);
			}
			else
			{
				//	??? ERRORS ARE NOT EXPECTED HERE because VS has already built the project, so the file exist
				// ??? Only to know if there is an unexpected response. It shouldn't have, but in case it has, it should be handled.
//				outputText += "ERROR! MAYBE A NEW VERSION OF GDB IS BEING USED BECAUSE THERE IS NO ^(done, running, connected, error), OR * AS THE TESTED ONE.";
				return 0;
			}
		}
		else
			printf("\n\n *********** Don't forget to upload a file using commands 'file' and 'upload'. ***********\n\n");
	}
	else
		printf("\n\n *********** Don't forget to assign to a target device/simulator using command 'target', and them upload a file using commands 'file' and 'upload'. ***********\n\n");
	return 1;
}

unsigned __stdcall sendingCommands2GDB(void *arg)
{
	GDBConsole *console = (GDBConsole *)arg;
	
	HANDLE listeningThread;
	unsigned threadID;
	ListeningThreadData listeningThreadData;
	void *listen = &listeningThreadData;
//	int seq_id = 5; // start with 5 because there was 4 previous instructions used to start gdb.
	int seq_id = SyncInstructionsSize; // Positions 0 to 49 are reserved for synchronous commands
	int countExecInterrupt = 0;

	hGDBBufferMutex = CreateMutex (NULL, FALSE, NULL);
	hOutputBufferMutex = CreateMutex (NULL, FALSE, NULL);
	hInputBufferMutex = CreateMutex (NULL, FALSE, NULL);
	inputEvent = CreateEventW(NULL, FALSE, FALSE, _T("inputEvent"));
	asyncOutputEvent = CreateEventW(NULL, FALSE, FALSE, _T("asyncOutputEvent"));

	GDBBuffer.quantity = 0;

	unordered_map<string, int> commandCodesMap;
	string parsingInstructions[NumberOfInstructions];

	if(!insertingCommandCodes(&commandCodesMap, parsingInstructions))
	{
		printf("Error initializing parsing data structures");
	}

	listeningThreadData.console = console;
	listeningThreadData.parsingInstructions = parsingInstructions;
	listeningThread = (HANDLE)_beginthreadex( NULL, 0, &listeningGDB, (void *)listen, 0, &threadID);

	char lastCommandSend[GDBCommandSize] = "";

	while (true)
	{
		char inputCommandSend[GDBCommandSize] = "";
		char commandSend[GDBCommandSize] = "";
		int instructionCodeSend;
		string param = "";
		bool sync = false;
		int previousSeqId;

		removeFromInputBuffer(inputCommandSend);
		while (strcmp(inputCommandSend,"") == 0)
		{
			if (in_InputBuffer == out_InputBuffer) // if true, input buffer is empty.
				WaitForSingleObject(inputEvent, WAIT_TIME);
			removeFromInputBuffer(inputCommandSend);
		}

		if ((inputCommandSend[0] >= '0') && (inputCommandSend[0] <= '4')) // means that it is a synchronous command.
		{
			sync = true;
			previousSeqId = seq_id;
			seq_id = ((inputCommandSend[0] - '0') * 10) + (inputCommandSend[1] - '0');
			char *aux_str = inputCommandSend + 2;
			strcpy(inputCommandSend, aux_str);
//			inputCommandSend += 1;
		}

		if (strcmp(inputCommandSend, "-exec-interrupt") == 0)
		{
			if ((strcmp(lastCommandSend, "-exec-interrupt") == 0) && (countExecInterrupt < 5))
			{
				countExecInterrupt++;
				console->sendCtrlC();
				continue;
			}
			countExecInterrupt = 0;
			// Wait for existing commands to finish
			while (!isGDBBufferEmpty())
				Sleep(0);

			console->sendCtrlC();
		}

		if (strcmp(inputCommandSend, "-gdb-exit") == 0)
		{
			break;
		}

		if ((strcmp(inputCommandSend, "-exec-continue") == 0) || (strcmp(inputCommandSend, "-exec-continue --thread 1") == 0))  // GDB some times lose the commands that were sent while it was running an -exec-continue, what causes a deadlock. So, waiting for this command to finish
		{
			while (!isGDBBufferEmpty())
				Sleep(0);
		}

		itoa(seq_id, commandSend, 10);

		strcat(commandSend, inputCommandSend);
		strcat(commandSend, "\r\n");

		instructionCodeSend = getInstructionCode(inputCommandSend, commandCodesMap, &param);
		if (instructionCodeSend == -1)
			break;

		int bufferPos = seq_id; // Calculate GDB buffer position. Had to do here because here I know if the instruction is synchronous or not.
	    if (bufferPos >= SyncInstructionsSize)    
	    {  // Avoid writing in the first 50 positions that are reserved for synchronous instructions.
			bufferPos = (bufferPos % SyncInstructionsSize) + SyncInstructionsSize; 
		}

		while (!addIntoGDBBuffer(bufferPos, instructionCodeSend, param))
		{
			printf("Position in GDB buffer is full!");
			Sleep(0);
		}

		console->sendCommand(commandSend);

		if ((strcmp(inputCommandSend, "-exec-continue") == 0) || (strcmp(inputCommandSend, "-exec-continue --thread 1") == 0))
		{
			while (!isGDBBufferEmpty())
				Sleep(0);
		}
		strcpy(lastCommandSend, inputCommandSend);

		if (!sync)
			seq_id++;
		else
			seq_id = previousSeqId;
	};

	exitGDB = true;
	WaitForSingleObject(listeningThread, INFINITE);

	// Once GDB exits, further attempts to read or write to the GDB console will result in errors	
	if (console != NULL) {
		delete console;
		console = NULL;
	}

	return 0;
}

GDBConsole* setupTest() {
	GDBConsole::setGDBPath(_T("c:\\bbndk\\host_127_0_1_204\\win32\\x86\\usr\\bin\\ntoarm-gdb.exe --interpreter=mi2"));
	GDBConsole* console = GDBConsole::getInstance();

	// Print output from GDB startup
	waitForPrompt(console, true);

	console->sendCommand("1-target-select qnx 169.254.0.1:8000\r\n");
	waitForPrompt(console, true);

	console->sendCommand("2-target-attach 113090587\r\n");
	waitForPrompt(console, true);

	console->sendCommand("3-file-exec-and-symbols c:\\\\dev\\\\vsplugin-ndk\\\\samples\\\\Square\\\\Square\\\\Device-Debug\\\\Square\r\n");
	waitForPrompt(console, true);

	console->sendCommand("4-exec-continue\r\n");
	waitForPrompt(console, true);

	return console;
}

void endTest(GDBConsole* console) {
	console->sendCommand("5-gdb-exit\r\n");
	//console->shutdown();
	delete console;
	console = NULL;
}

void terminateTest() {
	GDBConsole* console = setupTest();
	
	Sleep(5000);
	console->sendCtrlC();

	Sleep(5000);
	console->sendCtrlC();

	Sleep(5000);
	endTest(console);
}

void ctrlCTest() {
	GDBConsole* console = setupTest();

	Sleep(2000);
	console->sendCtrlC();

	Sleep(5000);
	endTest(console);
}

int _tmain(int argc, _TCHAR* argv[])
{
	//ctrlCTest();
	terminateTest();
	return 0;
}
