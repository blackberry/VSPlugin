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

#define NumberOfInstructions 48
#define InputBufferSize 50
#define GDBBufferSize 100
#define OutputBufferSize 100  // ??? I think 50 would be enough. Left 500 only for testing purposes
#define SyncInstructionsSize 50
#define GDBCommandSize 256
#define ParsedMessageSize 8192
#define MultipleThreads 0 // 1 - use multiple parsing threads, 0 doesn't
#define MAX_EVENT_NAME_LENGTH 256
#define WAIT_TIME 1000L  // max amount of time to wait for single events.

#define LOG_GDB_RAW_IO 1

class GDBConsole {

private:	
	static GDBConsole* m_instance;
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
	void prepAndLaunchRedirectedChild();		

public:	
	~GDBConsole();	
	static void shutdown();
	static GDBConsole* getInstance();
	static void setGDBPath(const TCHAR*);
	BOOL isClosed();
	int readOutput(CHAR*, int, int*);
	int sendCommand(const CHAR*);
	HANDLE getStdOutHandle();

	void sendCtrlC(); // For testing
};

void getThisDLLPath(wchar_t*, const wchar_t*, const wchar_t*);
void ErrorExit(LPTSTR);
void DisplayError(LPCTSTR);
string waitForPrompt(GDBConsole*, bool);
unsigned __stdcall listeningGDB(void *);
unsigned __stdcall sendingCommands2GDB(void *);
unsigned __stdcall parseGDBResponseThread(void *);  // call this function if using multithreaded parsing or (see below)
void parseGDB(string, string, int);	// call this function if not using multithreaded parsing
string parseGDB(string, string);	// call this function when starting GDB (this one doesn't use the OutputBuffer)
void cleanBuffers();
bool addIntoGDBBuffer(int, int, string);
int removeFromGDBBuffer(int, string*);
bool isGDBBufferEmpty();
bool addIntoOutputBuffer(int, char *);
void removeFromOutputBuffer(char *);
void removeSyncFromOutputBuffer(char *, int);
bool addIntoInputBuffer(char [GDBCommandSize]);
void removeFromInputBuffer(char *);
bool isInputBufferEmpty();
bool insertingCommandCodes(unordered_map<string, int> *, string [NumberOfInstructions]);
