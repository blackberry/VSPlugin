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

#include "stdafx.h"

#pragma managed(on)

#include <string>
#include < stdio.h >
#include < stdlib.h >
#include < vcclr.h >
using namespace System;
using namespace System::Text::RegularExpressions;
using namespace Microsoft::Win32;

#include "ProjInclude.h"

// GDBParser API header files (in alphabetical order)
#include "GDBParserAPI.h"

#include "gdb-connect.h"

using namespace std;
//#include "marshal.h"
//#include "marshal_cppstd.h"

// Set an auto pointer given a String^
CAutoPtr <char> convertToAutoPtrFromString(String^ str) {
	CAutoPtr <char> ap;

	pin_ptr<const wchar_t> pkwc = PtrToStringChars(str);
	char* pc = new char[(wcslen(pkwc) + 1) * sizeof(char)];	
	ap.Attach(pc);	

	// Convert wchar_t* to a char*
	size_t origsize = wcslen(pkwc) + 1;    
    size_t convertedChars = 0;
    wcstombs_s(&convertedChars, ap, origsize, pkwc, _TRUNCATE);

	return ap;
}

String^ GDBParser::removeGDBResponse()
{
	char output[ParsedMessageSize] = "";
	removeFromOutputBuffer(output);
	String ^systemstring = gcnew String(output);
	return systemstring;
}

bool GDBParser::addGDBCommand(String^ GDBCommand)
{
	CAutoPtr <char> apCmd = convertToAutoPtrFromString(GDBCommand);
	while (!addIntoInputBuffer(apCmd)); // keep trying to add into buffer
	return(true);
}

void GDBParser::exitGDB()
{
    addGDBCommand("-gdb-exit");
	GDBConsole::shutdown();
    s_running = false;
    if (m_BBConnectProcess)
        TerminateProcess(m_BBConnectProcess, 0);
}

bool GDBParser::is_Input_Buffer_Empty()
{
	return(isInputBufferEmpty());
}

String^ GDBParser::parseCommand(String^ GDBCommand, int ID)
{
	char output[ParsedMessageSize] = "";
	CAutoPtr <char> apCmd;
	if (ID < 10)
	    apCmd = convertToAutoPtrFromString("0" + ID + GDBCommand);
	else
		apCmd = convertToAutoPtrFromString(ID + GDBCommand);

	while (!addIntoInputBuffer(apCmd) && s_running)
		Sleep(0); // keep trying to add into buffer

	while (strcmp(output,"") == 0 && s_running)
		removeSyncFromOutputBuffer(output, ID);
	if (strcmp(output,"$#@EMPTY@#$") == 0)
		strcpy(output,"");

	String ^systemstring = gcnew String(output);
	return systemstring;
}

void GDBParser::setNDKVars(bool isSimulator) {
	String^ keyPath = "HKEY_CURRENT_USER\\Software\\BlackBerry\\BlackBerryVSPlugin";
	String^ ndkHostPath = (String^)Registry::GetValue(keyPath, "NDKHostPath", "");
	String^ ndkTargetPath = (String^)Registry::GetValue(keyPath, "NDKTargetPath", "");
			
	if (isSimulator) {		
		m_pcGDBCmd = ndkHostPath + "\\usr\\bin\\ntox86-gdb.exe --interpreter=mi2";
		m_libPaths[0] = ndkTargetPath + "\\x86\\lib";
		m_libPaths[1] = ndkTargetPath + "\\x86\\usr\\lib";
	} else {		
		m_pcGDBCmd = ndkHostPath + "\\usr\\bin\\ntoarm-gdb.exe --interpreter=mi2";
		m_libPaths[0] = ndkTargetPath + "\\armle-v7\\lib";
		m_libPaths[1] = ndkTargetPath + "\\armle-v7\\usr\\lib";
	}

    // Escape backslashes for strings that will be sent to GDB
    String^ pattern = "\\\\"; // one backslash
    String^ replacement = "\\\\\\\\"; // two backslashes
    Regex^ r = gcnew Regex(pattern);

	String^ regexResult;
	for (int i = 0; i < NUM_LIB_PATHS; i++) {
		regexResult = r->Replace(m_libPaths[i], replacement);
		m_libPaths[i] = regexResult;
	}
}

void GDBParser::BlackBerryConnect(String^ IPAddrStr, String^ toolsPath, String^ publicKeyPath, String^ password)
{
	CAutoPtr <char> apDevice = convertToAutoPtrFromString(IPAddrStr);
	CAutoPtr <char> apTools = convertToAutoPtrFromString(toolsPath);
	CAutoPtr <char> apKey = convertToAutoPtrFromString(publicKeyPath);

    CAutoPtr <char> apPass;
    if (!String::IsNullOrEmpty(password))
	    apPass = convertToAutoPtrFromString(password);

    LPCTSTR lpApplicationName = NULL;
    PROCESS_INFORMATION pi;
    STARTUPINFOA si;	
	DWORD flags = 0;

	memset(&pi, 0, sizeof(pi));

    // Set up the start up info struct.
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);    
#if !defined (SHOW_BB_CONNECT_WINDOW)
	si.dwFlags = STARTF_USESHOWWINDOW;
	si.wShowWindow = SW_HIDE;
#endif

    char args[256];
    if (!String::IsNullOrEmpty(password))
        sprintf(args, "java.exe -Xmx512M -jar \"%s\\..\\lib\\Connect.jar\" %s -password %s -sshPublicKey \"%s\"", apTools, apDevice, apPass, apKey);
    else
        sprintf(args, "java.exe -Xmx512M -jar \"%s\\..\\lib\\Connect.jar\" %s -sshPublicKey \"%s\"", apTools, apDevice, apKey);

    // Launch the process
    if (!CreateProcessA(NULL, args, NULL, NULL, TRUE,
                        flags, NULL, NULL, &si, &pi))
        ErrorExit(_T("CreateProcess: BlackBerry-Connect"));

	m_BBConnectProcess = pi.hProcess;
}

/// <summary>
/// Launch the GDB Debug Process
/// </summary>
bool GDBParser::LaunchProcess(String^ pidStr, String^ exeStr, String^ IPAddrStr, bool isSimulator, String^ toolsPath, String^ publicKeyPath, String^ password)
{
	string response;
	string parsed;
    s_running = true;
    m_BBConnectProcess = NULL;
	char pcCmd[256];

	// Run BlackBerryConnect
    BlackBerryConnect(IPAddrStr, toolsPath, publicKeyPath, password);

	// Get PID
	CAutoPtr <char> apPid = convertToAutoPtrFromString(pidStr);	
	long int pid = strtol(apPid, NULL, 10);	
	if (pid == 0) {
		return false;
	}

	// Get device (IP address)	
	CAutoPtr <char> apDevice = convertToAutoPtrFromString(IPAddrStr);

	// Get binary file path	
	CAutoPtr <char> apBinaryFile = convertToAutoPtrFromString(exeStr);

	// Set NDK Variables
	setNDKVars(isSimulator);	
	
	// Convert GDB command to wchar_t* since GDBWrapper.exe requires a Unicode argument
	pin_ptr<const wchar_t> pkwcGDBCmd = PtrToStringChars(m_pcGDBCmd);
    const size_t newsizew = (wcslen(pkwcGDBCmd) + 1) * sizeof(wchar_t);
    wchar_t* pwcGDBCmd = new wchar_t[newsizew];
    wcscpy_s(pwcGDBCmd, newsizew, pkwcGDBCmd);
	CAutoPtr <wchar_t> apGDBCmd;
	apGDBCmd.Attach(pwcGDBCmd);

	// Get GDB Console
	GDBConsole::setGDBPath(apGDBCmd);	
	GDBConsole* console = GDBConsole::getInstance();	

	// Convert library paths
	CAutoPtr <char> libPaths[NUM_LIB_PATHS];
	for (int i = 0; i < NUM_LIB_PATHS; i++) {
		libPaths[i] = convertToAutoPtrFromString(m_libPaths[i]);
	}
	
    // initializing the parsing data structures, buffers and thread
	HANDLE sendingThread;
	unsigned threadID;
	unordered_map<string, int> commandCodesMap;
	string parsingInstructions[NumberOfInstructions];

	cleanBuffers();
	if(!insertingCommandCodes(&commandCodesMap, parsingInstructions))
	{
		printf("Error initializing parsing data structures");
		return false;
	}

	/// Send intialization commands to GDB
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[2]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// ??? load output console window with the parsed message.
		return false;
	}

	sprintf(pcCmd, "1-gdb-set breakpoint pending on\r\n");
	console->sendCommand(pcCmd);
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[8]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// ??? load output console window with the parsed message.
		return false;
	}

	// Disable these shared libary commands for now.
	/*sprintf(pcCmd, "2-gdb-set auto-solib-add on\r\n");
	console->sendCommand(pcCmd);
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[8]);
	if (parsed[0] == '!') //found an error
	{
		// ??? load output console window with the parsed message.
		return false;
	}
	
	if (NUM_LIB_PATHS > 0) {
		sprintf(pcCmd, "set solib-search-path ");

		// This results in an extra semi-colon at the end but GDB should be okay with that
		for (int i = 0; i < NUM_LIB_PATHS; i++) {
			strcat(pcCmd, libPaths[i]);
			strcat(pcCmd, ";");
		}
		strcat(pcCmd, "\r\n");		

		console->sendCommand(pcCmd);
		response = waitForPrompt(console, true);
		parsed = parseGDB(response, parsingInstructions[9]);
		if (parsed[0] == '!') //found an error
		{
			// ??? load output console window with the parsed message.
			return false;
		}
	}*/

	sprintf(pcCmd, "4-target-select qnx %s:8000\r\n", apDevice);
	console->sendCommand(pcCmd);
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[3]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// ??? load output console window with the parsed message.
		return false;
	}

	sprintf(pcCmd, "5-file-exec-and-symbols %s\r\n", apBinaryFile);		
	console->sendCommand(pcCmd);
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[7]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		 //??? load output console window with the parsed message.

		return false;
	}
	
	sprintf(pcCmd, "6-target-attach %d\r\n", pid);
	console->sendCommand(pcCmd);
	response = waitForPrompt(console, true);
	parsed = parseGDB(response, parsingInstructions[6]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// ??? load output console window with the parsed message.
		return false;
	}
	
//	start thread responsible for reading InputBuffer and sending commands to GDB.
//	Thread must start after initializing GDB with the previous instructions because:
//  - this thread will start another one (listening GDB) that will consume the GDB responses first. So, the above waitForPrompt will never get a result and this method will freeze.
//  - the above commands don't use the GDB Buffer, so the results wouldn't be parsed correctly.
	sendingThread = (HANDLE)_beginthreadex( NULL, 0, &sendingCommands2GDB, (void *)console, 0, &threadID);

	return true;
}
