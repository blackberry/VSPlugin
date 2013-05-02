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

#pragma once

#include <windows.h>

#include "stdafx.h"
#include <strsafe.h>
#include <string>
#include <iostream>
#include <unordered_map>

using namespace std;

#define NumberOfInstructions 48 // Number of parsing instructions in Instructios.txt file.
#define InputBufferSize 50		// Input buffer is used by the debug engine to send GDB commands.
#define GDBBufferSize 100		// Intermediate buffer is used as a communication channel between 
								// sendingCommands2GDB and listeningGDB threads.
#define OutputBufferSize 100	// Output buffer is used to send the parsed GDB responses to the debug engine.
#define SyncInstructionsSize 50 // This number is used to separate the Output buffer in two parts: first part 
								// for synchronous commands; and second for asynchronous ones.
#define GDBCommandSize 256		// Maximum size for a GDB command.
#define ParsedMessageSize 8192	// Maximum size for a parsed GDB response.
#define MultipleThreads 0		// 1 - use multiple parsing threads, 0 - single parsing thread.
#define MAX_EVENT_NAME_LENGTH 256
#define WAIT_TIME 1000L			// max amount of time to wait for single events.

#define LOG_GDB_RAW_IO 1		// 1 - generate GDB output log file; 0 - do not generate.

class GDBConsole {

private:	
	static GDBConsole* m_instance;	// represents an instance of GDB debugger.
    HANDLE m_hInputRead, m_hInputWrite, m_hOutputRead, m_hOutputWrite, m_hErrorWrite;	
	BOOL m_isClosed;
	static TCHAR* m_pcGDBCmd;
	DWORD m_pid;	
	HANDLE m_hProcess;	
	HANDLE m_eventCtrlC;
	HANDLE m_eventTerminate;
	HANDLE m_hCleanupMutex;

	GDBConsole();		
	void cleanup();
	HANDLE getStdOutHandle();
	BOOL isClosed();
	BOOL isMoreOutputAvailable ();
	void logPrint(char*);
	void prepAndLaunchRedirectedChild();		
	int readOutput(CHAR*, int, int*);

public:	
	~GDBConsole();	
	static GDBConsole* getInstance();
	int sendCommand(const CHAR*);
	void sendCtrlC();
	static void setGDBPath(const TCHAR*);
	static void shutdown();
	string waitForPrompt(bool);
};

void DisplayError(LPCTSTR);
void ErrorExit(LPTSTR);

// Threads responsible for calling the right GDBConsole methods to send commands to GDB and to get responses from GDB. They are also 
// responsible for handling the messages that are sent to/received from GDB.
unsigned __stdcall listeningGDB(void *);
unsigned __stdcall sendingCommands2GDB(void *);

// Functions that perform some specific tasks to the above threads responsible for sending GDB commands and receiving GDB responses.
int getInstructionCode(string, unordered_map<string, int>, string*);
int getSeqID(string);
bool insertingCommandCodes(unordered_map<string, int> *, string [NumberOfInstructions]);

// Functions/thread responsible for parsing GDB responses.
unsigned __stdcall parseGDBResponseThread(void *);  // Call this function if using multithreaded parsing or (see below).
void parseGDB(string, string, int);	// call this function if not using multithreaded parsing.
string parseGDB(string, string);	// Call this function when starting GDB (this one doesn't use the OutputBuffer).
string parseGDB(string*, string, int, bool, char[10][128], string); // The above functions will call this one that is the one responsible for parsing the GDB response.

// Functions that perform some specific tasks to the one responsible for parsing GDB responses.
int findClosing(char, char, string, int);
int getNextChar(char, string, int);
int searchResponse(string, string, int, int, bool, char);
string substituteVariables(string, char[10][128]);

// The following methods are responsible for manipulating the buffers (Input, GDB and Output).
void cleanBuffers();
bool addIntoInputBuffer(char [GDBCommandSize]);
void removeFromInputBuffer(char *);
bool isInputBufferEmpty();
bool addIntoGDBBuffer(int, int, string);
int removeFromGDBBuffer(int, string*);
bool isGDBBufferEmpty();
bool addIntoOutputBuffer(int, char *);
void removeFromOutputBuffer(char *);
void removeSyncFromOutputBuffer(char *, int);