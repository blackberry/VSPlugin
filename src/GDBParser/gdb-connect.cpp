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


/// <summary> 
/// Used to send parameters for the listeningGDB thread (the one that is listening GDB responses). 
/// </summary>
typedef struct ListeningThreadDataStruct 
{
	GDBConsole *console;			// Current instance of GDB debugger.
	string *parsingInstructions;	// Parsing instruction.
} ListeningThreadData;


/// <summary> 
/// Used to send parameters for the parsing threads, when using multi-threaded parsing. 
/// </summary>
typedef struct ParsingThreadDetailsStruct  
{
	char* response;				// GDB response.
	char* parsingInstruction;	// Parsing instruction.
	int* seqStamp;				// Output buffer index where the parsed response will be stored. 
} ParsingThreadDetails;


/// <summary> 
/// Used by sendingCommands2GDB thread to store data that listeningGDB thread will need while parsing the GDB response. 
/// </summary>
typedef struct GDBBufferStruct  
{
	int seqId;				// The sequencial number (seqID) of the instruction that was sent to GDB.
	int instructionCode;	// The instruction ID.
	string param;			// Parameter sent by sendingCommands2GDB thread to the listeningGDB one.
} GDBBufferEntry;


/// <summary> 
/// Mutex / Event objects 
/// </summary>
HANDLE hInputBufferMutex;	// Used to ensure that only one thread at a time will be manipulating the input buffer.
HANDLE hGDBBufferMutex;		// Used to ensure that only one thread at a time will be manipulating the GDB (intermediate) buffer.
HANDLE hOutputBufferMutex;	// Used to ensure that only one thread at a time will be manipulating the output buffer.
HANDLE inputEvent;			// Used to avoid sendingCommands2GDB thread to be pooling while the input buffer is empty.
HANDLE asyncOutputEvent;	// Used to avoid DebugEngine.EventDispatcher thread to be pooling while waiting for an asynchronous response from Output buffer

extern HMODULE _hModThis;


/// <summary> 
/// Output buffer. Each entry has a parsed GDB response. 
/// </summary>
char outputBuffer[OutputBufferSize][ParsedMessageSize];
int out_OutputBuffer = SyncInstructionsSize;				// Remove element from this position (outputBuffer). Input depends on the 
															// seqStamp, i.e., ordered according to GDB responses. Positions 0 to 49 
															// are reserved for synchronous commands.


/// <summary> 
/// Input buffer (Circular). Each entry has a command to be sent to GDB. 
/// </summary>
char inputBuffer[InputBufferSize][GDBCommandSize];
int in_InputBuffer = 0;								// Add element in this position (inputBuffer).
int out_InputBuffer = 0;							// Remove element from this position (inputBuffer).


/// <summary> 
/// An instance of GDB debugger. 
/// </summary>
GDBConsole* GDBConsole::m_instance = NULL;
TCHAR* GDBConsole::m_pcGDBCmd = NULL;


/// <summary> 
/// GDB buffer. Contains the GDBBufferEntries sent by sendingCommands2GDB therad to the listeningGDB one. 
/// </summary>
struct GDBBufferMainStruct
{
	int quantity;									// Contains the number of GDB entries currently filled.
	GDBBufferEntry GDBBufferData[GDBBufferSize];	// See GDBBufferEntry definition above.
} GDBBuffer;


/// <summary> 
/// Shared variable between SendingCommands2GDB and listeningGDB threads used to let the listeningGDB one know that it has
///  to terminate. SendingCommands2GDB thread sets this variable to true to terminate listeningGDB thread. 
/// </summary>
bool exitGDB = false;


/// <summary> 
/// Used to store the path to GDB output log, generated when GDB console is instantiated and needed in logPrint(). 
/// </summary>
char path_log[_MAX_PATH];


/// <summary> 
/// Generic function to print to a log file. 
/// </summary>
/// <param name="buffer"> Message to be printed to a log file. </param>
void GDBConsole::logPrint(char* buffer) {	
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


/// <summary> 
/// Constructor. 
/// </summary>
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

    // Set up the security attributes struct used when creating the pipes.
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
    // properties and, as a result, non-closeable handles to the pipes are created.
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

    // Close inheritable copies of the handles you do not want to be inherited.
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

	String^ tempPath = Environment::GetEnvironmentVariable("APPDATA"); 
	tempPath += "\\BlackBerry\\gdb-output.log";
	pin_ptr<const wchar_t> path_buffer = PtrToStringChars(tempPath);

	int ret = wcstombs ( path_log, path_buffer, _MAX_PATH );

	FILE* file = fopen(path_log, "w"); // Delete a possible existing file.
	fclose(file);
}


/// <summary> 
/// Get the current instance of GDB debugger, creating a new one if needed. 
/// </summary>
/// <returns> Returns the current instance of GDB debugger.	</returns>
GDBConsole* GDBConsole::getInstance() {
	if (m_instance == NULL) {
		// Initialize it.
		m_instance = new GDBConsole;
	}
	return m_instance;
}


/// <summary> 
/// Shut down GDB debugger. A command to exit GDB was already sent by GDBParser::exitGDB(): "-gdb-exit". However, still 
/// needs to "clean up": Update variables and terminate GDBWrapper process. 
/// </summary>
void GDBConsole::shutdown() {
	if (m_instance != NULL) {
		m_instance->cleanup();
	}
}


/// <summary> 
/// Set m_pcGDBCmd variable with the path and command to initialize GDB/MI. It will be used to instantiate GDBConsole.
/// For example: C:/bbndk/host_10_0_10_536/win32/x86\usr\bin\ntoarm-gdb.exe --interpreter=mi2. 
/// </summary>
/// <param name="pcGDBCmd"> String with full path and command to initialize GDB/MI. </param>
void GDBConsole::setGDBPath(const TCHAR* pcGDBCmd) {	
	size_t numChars = _tcslen(pcGDBCmd) + 1;
		
	// The path length may have changed, so reallocate for this one.
    if (m_pcGDBCmd) {	
		delete[] m_pcGDBCmd;		
	}
	m_pcGDBCmd = new TCHAR[numChars * sizeof(TCHAR)];

	_tcscpy_s(m_pcGDBCmd, numChars, pcGDBCmd);
}


/// <summary> 
/// Check if GDB is closed. 
/// </summary>
/// <returns> A boolean value: TRUE if GDB is closed; FALSE if not. </returns>
BOOL GDBConsole::isClosed() {
	return m_isClosed;
}


/// <summary> 
/// After exiting GDB, it is still needed to update some internal variables and terminate the GDBWrapper process. 
/// </summary>
void GDBConsole::cleanup() {	
	WaitForSingleObject(m_hCleanupMutex, INFINITE);
	// Check if cleanup required.
	if (!m_isClosed) {
		m_isClosed = TRUE;

		if (m_pcGDBCmd != NULL) {
			delete[] m_pcGDBCmd;
			m_pcGDBCmd = NULL;
		}
    	
		// Clean up and kill GDBWrapper process.
		if (!SetEvent(m_eventTerminate)) {
			// Force termination.
			TerminateProcess(m_hProcess, 0);		
		}
	
		// Wait for process to terminate.
		WaitForSingleObject(m_hProcess, INFINITE);
	
		if (!CloseHandle(m_hProcess)) DisplayError(_T("CloseHandle"));

		if (!CloseHandle(m_hOutputRead)) DisplayError(_T("CloseHandle"));
		if (!CloseHandle(m_hInputWrite)) DisplayError(_T("CloseHandle"));

		m_instance = NULL;
	}	
	ReleaseMutex(m_hCleanupMutex);	
}


/// <summary> 
/// Destructor. 
/// </summary>
GDBConsole::~GDBConsole() {
	cleanup();
}


/// <summary> 
/// Sets up STARTUPINFO structure and launches redirected child. 
/// </summary>
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

	// Create event for wrapper process to wait on for CTRL-C.
	m_eventCtrlC = CreateEventW(NULL, FALSE, FALSE, eventCtrlCName);

	// Create event for wrapper process to wait on for termination.
	m_eventTerminate = CreateEventW(NULL, FALSE, FALSE, eventTerminateName);

	// Set process information.
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
	
    String^ StringFilePath = Environment::GetFolderPath(Environment::SpecialFolder::ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\GDBWrapper.exe";
	pin_ptr<const wchar_t> FilePath = PtrToStringChars(StringFilePath);

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
	
	_stprintf(pCmdLine, _T("\"%s\" \"%s\" \"%s\" \"%s\""), FilePath, m_pcGDBCmd, eventCtrlCName, eventTerminateName);
			
    // Launch the process (create a new console).
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

	// Close unneeded handles.
    if (!CloseHandle(pi.hThread)) DisplayError(_T("CloseHandle"));			
}


/// <summary> 
/// Read the stdout of the GDB console process. 
/// </summary>
/// <param name="lpBuffer"> Buffer to store output. </param>
/// <param name="bufSize"> Size of buffer in bytes (leave space for the null char). </param>
/// <param name="nCharsRead"> Returns the number of characters that was read. </param>
/// <returns> One of these integer values: 0 - Error; 1 - Success; 2 - Read everything from buffer but the message was not written 
/// completed, maybe because the message is bigger than the buffer, maybe because GDB didn't finish writing the message. Call it 
/// again to keep reading. </returns>
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
			// Pipe done - normal exit path.
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
		// Didn't get end of line.
		return 2;
	}
			 
	return 1;
}


/// <summary> 
/// Send a GDB command to the stdin of the GDB console process. 
/// </summary>
/// <param name="lpCmd"> null-terminated string ending in \r\n </param>
/// <returns> 0 - Error; 1 - Success </returns>
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


/// <summary> 
/// Get handle m_hOutputRead. 
/// </summary>
/// <returns> Returns handle m_hOutputRead. </returns>
HANDLE GDBConsole::getStdOutHandle() {
	return m_hOutputRead;
}


/// <summary> 
/// Send a CtrlC event that will interrupt GDB execution. 
/// </summary>
void GDBConsole::sendCtrlC() {
	if (!SetEvent(m_eventCtrlC)) {
		DisplayError(_T("SetEvent"));
	}
}


/// <summary> 
/// Check if there is more data to be read from GDB's stdout without removing it from the pipe's buffer. 
/// </summary>
/// <param name="console"> Instance of GDB debugger. </param>
/// <returns> Returns TRUE (there is data to be read) or FALSE (there isn't). </returns>
BOOL GDBConsole::isMoreOutputAvailable () {
	DWORD totalBytesAvail;	

	if (!PeekNamedPipe(getStdOutHandle(), NULL, NULL, NULL, &totalBytesAvail, NULL)) {
		TCHAR lpError[256];		
		_tprintf(lpError, _T("isMoreOutputAvailable: %s"), GetLastError());
		DisplayError(lpError);
		return false;
	}
		
	return (totalBytesAvail > 0);
}


/// <summary> 
/// Returns the output received from GDB. 
/// </summary>
/// <param name="console"> Instance of GDB debugger. </param>
/// <param name="sync"> If TRUE, this method waits for the entire GDB output, i.e., until the GDB prompt is ready. It is TRUE when 
/// initializing GDB. If FALSE, that is the default behavior, returns the current GDB output or "" if there is no GDB output. </param>
/// <returns> Returns output received from GDB or "" if there is no GDB output. </returns>
string GDBConsole::waitForPrompt(bool sync) {
	char lpBuffer [1024];
	int nCharsRead = 0;
	int rc = 0;
	BOOL isPromptReady = FALSE;
	const char* promptString = "(gdb) \r\n";
	const char* lpTmp;

	string message = "";

	do {
		memset(lpBuffer, 0, sizeof(lpBuffer));

		if (isClosed()) {
			break;
		}

		if (isMoreOutputAvailable())
		{
			rc = readOutput(lpBuffer, sizeof(lpBuffer) - 1, &nCharsRead);
			if (rc) {
				// Terminate the buffer.

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
	} while (!isPromptReady && !isClosed());	

	return message;
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
    DWORD nCharsWritten;

    FormatMessage(
            FORMAT_MESSAGE_ALLOCATE_BUFFER|FORMAT_MESSAGE_FROM_SYSTEM,
            NULL, GetLastError(),
            MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
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

    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | 
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        dw,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &lpMsgBuf,
        0, NULL );

    // Display the error message and exit the process.

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


/// <summary> 
/// Initialize / clean all the existing buffers: inputBuffer, GDBBuffer, outputBuffer. 
/// </summary>
void cleanBuffers()
{
	for (int i = 0; i < InputBufferSize; i++)
	{
		strcpy(inputBuffer[i],"");
	}
	in_InputBuffer = 0;
	out_InputBuffer = 0;
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
	out_OutputBuffer = SyncInstructionsSize;
}


/// <summary> 
/// Verify if GDB buffer is empty. 
/// </summary>
/// <returns> Returns TRUE if empty; FALSE if not. </returns>
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


/// <summary> 
/// For every command sent to GDB, the following parameters must be added into GDBBuffer. 
/// </summary>
/// <param name="seq_id"> Sequential ID of the command sent to GDB. It is used to identify, when evaluating GDB responses, which command 
/// generated a given response. This ID is also used to specify the location in which this entry will be stored in this buffer. </param>
/// <param name="instructionCode"> Each command sent to GDB has an associated instruction code, that corresponds to the parsing 
/// instruction that will be used to parse the GDB response. </param>
/// <param name="param"> Used to store the GDB command parameters, so they could be used during the parsing task. There are some GDB 
/// commands, like -break-delete for example, that results in a simple "^done" GDB response. The parser can identify which command  
/// caused that response but cannot know what was affected by it (considering the -break-delete command, the parser will know that some
/// breakpoint was successfully deleted but won't know which of them was deleted). Using "param" helps the parser letting it knows
/// which parameters were sent together with a given GDB command. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
bool addIntoGDBBuffer(int seq_id, int instructionCode, string param)
{
	if (seq_id < 0)
		return false;
	bool got = false;
	int pos = seq_id % GDBBufferSize;
	WaitForSingleObject( hGDBBufferMutex, INFINITE );
	if (GDBBuffer.GDBBufferData[pos].seqId == -1) // -1 means that the [pos] entry is "empty" or "not used".
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


/// <summary> 
/// This method is called to find the right parsing instruction for a given GDB response.  
/// </summary>
/// <param name="seq"> Sequential ID of the GDB response. </param>
/// <param name="param"> Returns the parameters sent together with the GDB command that generated this response, if there are some. </param>
/// <returns> Returns the instruction code associated to the sequential id (seq). </returns>
int removeFromGDBBuffer(int seq, string *param)
{
	int output = -1;
	if (seq < 0)        // Means that: the GDB response is asynchronous or the response is not completed yet (GDB is still writing it)
		if (seq == -2)  // Means the GDB response is asynchronous and is not related to a GDB command. 
			return 0;	// 0 is the instruction code for asynchronous responses.
		else if (seq == -3)  // Means that a breakpoint was modified. It is one of the GDB asynchronous messages.
			return 1;	// 1 is the instruction code for modified breakpoints responses.
		else
			return seq; // GDB response is incomplete. 

	if (seq >= SyncInstructionsSize) // Means it was not a synchronous COMMAND. This number starts with SyncInstructionsSize 
									 // and is increased for each new non synchronous COMMAND that is sent to GDB.
		seq = (seq % SyncInstructionsSize) + SyncInstructionsSize; // seq must stay between SyncInstructionsSize and the buffer size.

	WaitForSingleObject( hGDBBufferMutex, INFINITE );
	if (GDBBuffer.GDBBufferData[seq].seqId != -1)
	{
		output = GDBBuffer.GDBBufferData[seq].instructionCode;
		*param = GDBBuffer.GDBBufferData[seq].param;
		GDBBuffer.GDBBufferData[seq].seqId = -1; // Marking this entry as "empty" or "not used".
		GDBBuffer.quantity -= 1;
	}
	ReleaseMutex( hGDBBufferMutex);
	return output;
}


/// <summary> 
/// Store the parsed GDB response in the "seq_stamp" position of the Output buffer. 
/// </summary>
/// <param name="seq_stamp"> Position in the Output buffer to store the parsed GDB response. </param>
/// <param name="parsedMessage"> Parsed GDB response. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
bool addIntoOutputBuffer(int seq_stamp, char *parsedMessage)
{
	if (seq_stamp < 0)
		return false;
	bool got = false;
	int pos = seq_stamp % OutputBufferSize;
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[pos], "") == 0)	// "" means that the [pos] entry is "empty" or "not used".
	{
		strcpy(outputBuffer[pos], parsedMessage);
		got = true;
		if (pos >= SyncInstructionsSize)
			SetEvent(asyncOutputEvent); // Wake up "removeFromOutputBuffer", if needed.
	}
	ReleaseMutex( hOutputBufferMutex);
	return (got);
}


/// <summary> 
/// This method is called to send back to the debug engine the next parsed GDB response (output) for an 
/// asynchronous GDB RESPONSE or a non synchronous GDB COMMAND. This managed part of Output buffer is circular. 
/// </summary>
/// <param name="output"> Returns the parsed GDB response. </param>
void removeFromOutputBuffer(char * output)
{
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[out_OutputBuffer], "") != 0)
	{
		strcpy(output, outputBuffer[out_OutputBuffer]);
		strcpy(outputBuffer[out_OutputBuffer], "");
		out_OutputBuffer += 1;
		if (out_OutputBuffer == OutputBufferSize)
			out_OutputBuffer = SyncInstructionsSize; // Positions 0 to "SyncInstructionsSize - 1" are reserved for synchronous commands.
	}
	ReleaseMutex( hOutputBufferMutex);
	if (strcmp(output, "") == 0) // Means that the asynchronous part of output buffer is empty.
		WaitForSingleObject(asyncOutputEvent, WAIT_TIME);
	if (strcmp(output, "$#@EMPTY@#$") == 0) // This string means that both GDB and the parser worked well and returned an empty string.
		output = "";
}


/// <summary> 
/// This method is called to send back to the debug engine the expected parsed GDB response (output) for a given synchronous 
/// GDB COMMAND (ID). Each entry in this part of Output buffer was previously reserved for each GDB command. There are some free entries 
/// that can be used by other GDB commands in the future. 
/// </summary>
/// <param name="output"> Returns the parsed GDB response. </param>
/// <param name="ID"> ID of the GDB command. </param>
void removeSyncFromOutputBuffer(char * output, int ID)
{
	if ((ID < 0) || (ID >= SyncInstructionsSize))
		return;
	WaitForSingleObject( hOutputBufferMutex, INFINITE );
	if (strcmp(outputBuffer[ID], "") != 0)
	{
		strcpy(output, outputBuffer[ID]);
		strcpy(outputBuffer[ID], "");
	}
	ReleaseMutex( hOutputBufferMutex);
}


/// <summary> 
/// Verify if Input buffer is empty. 
/// </summary>
/// <returns> Returns TRUE if empty; FALSE if not. </returns>
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


/// <summary> 
/// This method is called by the debug engine to store each command that must be sent to GDB. Input buffer is a circular one. 
/// </summary>
/// <param name="GDBCommand"> Command to be sent to GDB. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
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
		SetEvent(inputEvent); // Wake up "sendingCommands2GDB", if needed.
	ReleaseMutex( hInputBufferMutex);
	return (got);
}


/// <summary> 
/// Used by sendingCommands2GDB thread to get the next command to be sent to GDB. Input buffer is a circular one. 
/// </summary>
/// <param name="input"> Returns the next command to be sent to GDB. </param>
void removeFromInputBuffer(char * input)
{
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
}


/// <summary> 
/// Get the sequential ID of the response. 
/// </summary>
/// <param name="response"> The GDB response. </param>
/// <returns> If the response is synchronous, this ID corresponds to the sequential ID of the command sent to GDB that generated such 
/// response. If the response is asynchronous, there is no sequential ID in the response, so it returns:
///		-3 - when it shows that a breakpoint was modified;
///		-2 - for any other asynchronous response
///		-1 - when the response is not completed or in case of an error. </returns>
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
						return -2;  // Means the GDB response is asynchronous.
					else
					{
						end = response.find("*stopped", 0);
						if (end != -1)
						{
							return -2;   // Means the GDB response is asynchronous.
						}
						else
						{
							if (response.substr(0, 20) == "=breakpoint-modified")
							{
								return -3;  // Means that a breakpoint was modified.
							}
							else
							{
								return -1;	// Error / message incomplete.
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


/// <summary> 
/// Get the next position of a given character (token) in a string message (txt), starting from a given position (pos). 
/// </summary>
/// <param name="token"> Character to search for. </param>
/// <param name="txt"> String message in which it will search for the given character. </param>
/// <param name="pos"> Starting position in the string message. </param>
/// <returns> An integer that corresponds to the next position of the character in the string. If the character is not found, returns
/// the length of the string. </returns>
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


/// <summary> 
/// Get the position of a given string (txt) in the string GDB response (response), starting from a given position (begin). 
/// </summary>
/// <param name="response"> GDB string response where the search will be performed. </param>
/// <param name="txt"> String to search for in the GDB response. </param>
/// <param name="begin"> Starting position in the GDB string response. </param>
/// <param name="times"> Search for that string "times" times. Ex: I want to find the third occurrence of word "qaqa" in the GDB 
/// response. </param>
/// <param name="forward"> Direction: if true, search forwards; if not, search backwards. Only '?' instruction can search backwards. </param>
/// <param name="instruction"> The kind of parsing instruction that called this method. '?', '@', or '~'. Only '?' instruction can 
/// search backwards. </param>
/// <returns> An integer that corresponds to the next position of the string in the GDB response. -1 in case of an error. </returns>
int searchResponse(string response, string txt, int begin, int times, bool forward, char instruction)
{
	for (; times > 0; times--)
	{
		if ((begin >= response.length()) || (begin == -1))
			break;
		if (forward)
		{
			if (begin == 0) 
			{
				if ((txt[0] == '\r') && (txt[1] == '\n'))
				{
					string check = txt.substr(2, txt.length() - 2);
					if (response.substr(0, check.length()) == check)
						continue;
				}
			}
			begin = response.find(txt, begin);
			if ((begin != -1) && ((times != 1) || (instruction == '?')))
				begin += txt.length();
		}
		else
		{
			begin = response.rfind(txt, begin);
			if (begin != -1)
			{
				if (times == 1)
					begin += txt.length();
				else
					begin--;
			}
			if ((begin == -1) && ((txt[0] == '\r') && (txt[1] == '\n')))
				begin = 2; // 2 is the size of "\r\n".
		}
	}
	if (times == 0)
		return begin;
	return -1;
}


/// <summary> 
/// Substitute the existing variables in the string "txt" by their values, stored in the "variables" array. Each variable name 
/// has this format: $9$, where $ characters are used to identify the variable while the number corresponds to the variable ID, that also
/// corresponds to the array index. There is a special variable "$EOL$" that is substituted by "\r\n".
/// </summary>
/// <param name="txt"> String to search for variables. </param>
/// <param name="variables"> Array with the variable values. </param>
/// <returns> Returns the new modified string. </returns>
string substituteVariables(string txt, char variables[10][128])
{
	do
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


/// <summary> 
/// Find the position of the associated closing bracket/parenthesis. 
/// </summary>
/// <param name="opening"> The associated opening character ("(", "{" or "["). </param>
/// <param name="closing"> The associated closing character (")", "}" or "]"). </param>
/// <param name="parsingInstruction"> The string where the search will be made. </param>
/// <param name="ini"> Start position in the parsingInstruction string. This position normally corresponds to the position of the opening
/// character, but it could be smaller than that (never bigger!). However, if it is smaller, it cannot have the same character between
/// the 'ini' position and the corresponding one for the opening one. If need to have another one, precede it by the character '\'. </param>
/// <returns> Returns the position of the associated closing bracket/parenthesis. If it is not found, returns the length of the string. </returns>
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


/// <summary> 
/// Reponsible for parsing each GDB response. Depending on the parsing instruction, the parser can get the first occurrence  
/// from GDB response or get all of them that satisfies this parsing instruction. 
/// </summary>
/// <param name="response"> GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the respective GDB response. </param>
/// <param name="respBegin"> Current character to be read in GDB response. </param>
/// <param name="repeat"> If the parsing instruction specifies that it has to get all occurences from a GDB response, this value must
/// be true. </param>
/// <param name="variables"> A variable stores a string parsed from GDB response and can be used as many times as needed to create the 
///	parsed response. Up to 10 variables can be created for a given parsing instruction. </param>
/// <param name="separator"> If the parsing instruction allows the parser to get all occurrences from GDB response, this separator will 
/// be used at the end of each occurence. The default one is '#' but it can be specified in the parsing instruction. </param>
/// <returns> Returns the parsed GDB response. </returns>
string parseGDB(string *response, string parsingInstruction, int respBegin, bool repeat, char variables[10][128], string separator) 
{
	string result = "";			// This variable will be the parsed GDB response.
	string originalParsingInstruction = parsingInstruction;	// Backup of parsing instruction.
	int parsePos = 0;			// Current character to be read in parsing instruction.
	int home = respBegin;		// Save the current position in GDB response.
	int respEnd = 0;
	int limit = (*response).length();
	bool found = false;

	bool removeSelection = false;
	int removeFromHere = -2;    // -2 - not set; -1 - indicates that it will be set in the first '?' instruction; >=0 - already set.

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

		if (parsingInstruction[parsePos] == '?') // search and set the beginning of the searched string.
		{
			int times = 1;
			bool forward = true;
			int end;
			int aux;
			string txt;

			parsePos++;
			if (parsingInstruction[parsePos] == '%') // consider this position to extract and use the data and them remove this 
													 // one from the GDB response string. 
			{
				removeFromHere = -1;
				parsePos++;
			}

			if (parsingInstruction[parsePos] == '<') // means to search backwards.
			{
				forward = false;
				parsePos++;
			}

			if (parsingInstruction[parsePos] != '?') // the number of times to search for a given string.
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
//		If the search is not valid, go to the '}' and keep going. If there is no '{}' and the search is not valid, just end evaluating
//		 these instructions. Normally, there is '{}' to allow handling gdb errors.
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

		if (parsingInstruction[parsePos] == '@') // till here: search and set the end of the searched string. Return the string 
												 // between the last ? and this "till here" position
		{
			int times = 1;
			int end;
			string txt;
			int aux;

			parsePos++;

			if (parsingInstruction[parsePos] != '@') // get the number of times to search for a given string.
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

		if (parsingInstruction[parsePos] == '#') // insert the following string into the result
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

		if (parsingInstruction[parsePos] == '0') // set the response cursor position to home_pos (normally 0, but '(' can set a different 
												 // value for home_pos)
		{
			respBegin = home;
			parsePos += 2; // jump the '0' and ';' characters
			continue;
		}

		if (parsingInstruction[parsePos] == '%') // delete the string (between ? and @) from response
		{ 
			removeSelection = true;
			removeFromHere = -1;
			parsePos++;
			continue;
		}

		if (parsingInstruction[parsePos] == '$') // create a variable to store a value from response string. This value is defined by 
												 // the instructions between the '=' and the @ symbol (till here)
		{
			int end, varNumber, aux;

			parsePos++;
			end = getNextChar('=', parsingInstruction, parsePos);
			aux = getNextChar('$', parsingInstruction, parsePos);
			if ((aux > 0) && (aux < end))
				end = parsingInstruction.length();
			if (end < parsingInstruction.length()) // variable assignment
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
			else // pre-defined variable: Finish the parsing task or move to the end of the GDB response.
			{
				end = parsingInstruction.find('$', parsePos);
				if (parsingInstruction.substr(parsePos, end - parsePos) == "END")
					break;
				if (parsingInstruction.substr(parsePos, end - parsePos) == "EOR")  // move respBegin to the end of the response, probably
																				   // to start looking from the end to the begin.
					respBegin = response->length() - 1;
				parsePos = end + 2;
			}
			continue;
		}

	}
	return(result);
}


/// <summary> 
/// Function used to call the parser that will parse a GDB response according to the parsing instruction, returning this parsed 
/// response. Call this function when starting GDB (this one doesn't use the OutputBuffer because it returns the parsed response). 
/// </summary>
/// <param name="response"> GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the respective GDB response. </param>
/// <returns> Returns the parsed GDB response. </returns>
string parseGDB(string response, string parsingInstruction)
{
	char variables[10][128];
	char result[ParsedMessageSize] = "";

	strcpy(result, parseGDB(&response, parsingInstruction, 0, false, variables, "#").substr(0, ParsedMessageSize - 1).c_str());

	return (result);
}


/// <summary> 
/// Function used to call the parser that will parse a GDB response according to the parsing instruction, storing the parsed 
/// response in the Output Buffer. Call this function if NOT USING multithreaded parsing 
/// </summary>
/// <param name="response"> GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the respective GDB response. </param>
/// <param name="seqStamp"> Position in the Output Buffer in which the parsed response will be stored. </param>
void parseGDB(string response, string parsingInstruction, int seqStamp) 
{
	char variables[10][128];
	char result[ParsedMessageSize] = "";

	strcpy(result, parseGDB(&response, parsingInstruction, 0, false, variables, "#").substr(0, ParsedMessageSize - 1).c_str());

	if (strcmp(result,"") == 0)
	{
		// TODO? Unexpected response. Must handle it.
		strcpy(result, "$#@EMPTY@#$"); // Sending this string to be sure that the parser worked well and returned an empty string.
	}

	while (!addIntoOutputBuffer(seqStamp, result))
		Sleep(0);
}


/// <summary> 
/// Thread used to call the parser that will parse a GDB response according to the parsing instruction, storing the parsed 
/// response in the Output Buffer. Call this function if USING multithreaded parsing. One thread per each GDB response. 
/// </summary>
/// <param name="arg"> A ParsingThreadDetails data structure that stores the following data:	
///		- response (GDB response);
///		- parsingInstruction (Parsing instruction); and 
///		- seqStamp (Output buffer index where the parsed response will be stored). </param>
/// <returns> 0 </returns>
unsigned __stdcall parseGDBResponseThread(void *arg)
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
		// TODO? Unexpected response. Must handle it.
		strcpy(result, "$#@EMPTY@#$"); // Sending this string to be sure that parsed worked well and returned an empty string.
	}

	while (!addIntoOutputBuffer(seqStamp, result))
		Sleep(0);

	return 0;
}


/// <summary> 
/// Thread responsible for listening for GDB outputs. Thread sendingCommands2GDB start this one, that stays alive till the 
/// end of the debugging session. 
/// </summary>
/// <param name="arg"> A ListeningThreadData data structure that stores the following data:	
///		- console (Current instance of GDB debugger); and
///		- parsingInstructions (Parsing instruction). </param>
/// <returns> 0 </returns>
unsigned __stdcall listeningGDB(void *arg)
{
	ListeningThreadData *listen = (ListeningThreadData *)arg;
	
	int seqStamp = SyncInstructionsSize; // Positions 0 to 49 are reserved for synchronous commands

	string response = "";
	int getMoreData = 10; // used when the GDB output does not have a complete response.

	while(!exitGDB) // exitGDB is set to true by sendingCommands2GDB thread, when the debugging sessiong ends.
	{
		int instructionCode;
		int gdbPos;

		response += listen->console->waitForPrompt(false);

		gdbPos = response.find("(gdb)");

		while (gdbPos == 0)
		{
			response.erase(0,5);
			gdbPos = response.find("(gdb)");
		}

		if ((gdbPos == -1) && (response.length() > 3))
			gdbPos = response.length();

		while (gdbPos != -1)  // Just in case gdb was faster than our debugger. If that happens, there will be two or more responses 
							  // in the buffer, so we have to parse each of them individually.
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
					instructionCode = 0; // it was not possible to identify the type of the response after reading buffer 10 times.
										 // Consider it an asynchronous one.
				else
					break; // don't erase the response... Trying to get more data.
			}

			getMoreData = 10;

			if (instructionCode != -1)
			{
				// Create a thread to parse the response with parsingInstructions[instructionCode]. Send also a sequencial number, 
				// so VS can sort the results. Can use multiple parsing threads or not, depending on the const value MultipleThreads 
				// defined in gdb-connect.h
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
				else  // Without multiple parsing threads
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
				}
			}
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

    exitGDB = false;

	return 0;
}


/// <summary> 
/// Get the associated instruction ID from the command to be sent to GDB. 
/// </summary>
/// <param name="command"> Command to be sent to GDB. </param>
/// <param name="map"> Hash map data structure that contains the GDB commands recognized by the parser and their respectives 
/// instructions IDs. </param>
/// <param name="param"> When the instruction code stored in "map" is negative, it means that the commands parameters must be saved to 
/// be used by the listeningGDB thread. When that happens, update and return variable "param". </param>
/// <returns> Returns the instruction code for the command to be sent to GDB or -1 in case of an error. It can also return the command 
/// parameters, as it was described above. </returns>
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


/// <summary> 
/// Initialize the hash map data structure "map" in which each entry contains a GDB command that is currently recognized by 
/// the parser with its respective instruction ID. It loads these information from Instructions.txt file, in where each line has the 
/// following format: 
///     {$}gdb-command:->:parsing-instruction;
/// It must have the $ sign in front of the GDB command when it is needed to store the command parameters to be used later by the
/// listeningGDB thread. This usually happens whenever the GDB response for a given command does not contains enough information, like 
/// only "^done" for example, when the parser needs something else. The ":->:" separates the GDB command from the parsing Instruction. 
/// </summary>
/// <param name="map"> Hash map data structure that contains the GDB commands recognized by the parser and their respectives 
/// instructions IDs. </param>
/// <param name="parsingInstructions"> A string array with all of the currently recognized parsing instructions. The index position 
/// correponds to the instruction ID used in "map" for the respective GDB command. </param>
/// <returns> 1 in case of sucess; 0 if there is an error when opening the Instructions.txt file. </returns>
bool insertingCommandCodes(unordered_map<string, int> *map, string parsingInstructions[NumberOfInstructions])
{
    String^ StringFilePath = Environment::GetFolderPath(Environment::SpecialFolder::ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\Instructions.txt";
	pin_ptr<const wchar_t> FilePath = PtrToStringChars(StringFilePath);

	ifstream inputs;
    inputs.open (FilePath);

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


/// <summary> 
/// Thread responsible for sending commands to GDB. The GDBParserAPI LaunchProcess start this one, that stays alive until it receives 
/// a -gdb-exit instruction. This thread initializes the main data structures that are needed by teh parser and the listeningGDB thread. 
/// </summary>
/// <param name="arg"> A reference to the GDB console (Current instance of GDB debugger). </param>
/// <returns> 0 </returns>
unsigned __stdcall sendingCommands2GDB(void *arg)
{
	GDBConsole *console = (GDBConsole *)arg;
	
	// Initializing listeningGDB thread data.
	HANDLE listeningThread;
	unsigned threadID;
	ListeningThreadData listeningThreadData;
	void *listen = &listeningThreadData;

	int seq_id = SyncInstructionsSize; // Positions 0 to 49 are reserved for synchronous commands.
	int countExecInterrupt = 0; 

	// Initializing mutex and event objects.
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

	// Starting listeningGDB thread.
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

		if ((inputCommandSend[0] >= '0') && (inputCommandSend[0] <= '4')) // Means that it is a synchronous command and the instruction
																		  // already has the ID.
		{
			sync = true;
			previousSeqId = seq_id;
			seq_id = ((inputCommandSend[0] - '0') * 10) + (inputCommandSend[1] - '0');
			char *aux_str = inputCommandSend + 2;
			strcpy(inputCommandSend, aux_str);
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

		if ((strcmp(inputCommandSend, "-exec-continue") == 0) || (strcmp(inputCommandSend, "-exec-continue --thread 1") == 0))  
		// GDB some times lose the commands that were sent while it was running an -exec-continue, what can cause a deadlock. So, 
		// waiting for the previous commands to finish.
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

		int bufferPos = seq_id; // Calculate GDB buffer position. Had to do now because I know if the instruction is synchronous or not.
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