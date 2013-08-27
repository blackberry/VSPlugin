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


/// <summary> 
/// Set an auto pointer given a String^. 
/// </summary>
/// <param name="str"> String. </param>
/// <returns> Auto pointer. </returns>
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


/// <summary> 
/// Gets a parsed asynchronous GDB response from Output Buffer. Also can get a parsed GDB response for a non-synchronous GDB 
/// COMMAND from Output buffer. This method is called by the Event Dispather of the Debug Engine. 
/// </summary>
/// <returns> Parsed GDB response. </returns>
String^ GDBParser::removeGDBResponse()
{
	char output[ParsedMessageSize] = "";
	removeFromOutputBuffer(output);
	String ^systemstring = gcnew String(output);
	return systemstring;
}


/// <summary> 
/// Sends a non-synchronous GDB command by adding it into the Input Buffer. This method is called by the Debug Engine whenever 
/// it needs to send a GDB command without having to wait for the respective GDB response. 
/// </summary>
/// <param name="GDBCommand"> Command to be sent to GDB. </param>
/// <returns> True. </returns>
bool GDBParser::addGDBCommand(String^ GDBCommand)
{
	CAutoPtr <char> apCmd = convertToAutoPtrFromString(GDBCommand);
	while (!addIntoInputBuffer(apCmd)); // Keep trying until it is added into Input buffer
	return(true);
}


/// <summary> 
/// Exits GDB. This method is called to end the debug session by Event Dispatcher (Debug Engine) or directly by the debug engine 
/// when it fails to launch a new debug session. 
/// </summary>
void GDBParser::exitGDB()
{
	// Sends the command that exits GDB immediately. 
    // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Miscellaneous-Commands.html)
    addGDBCommand("-gdb-exit");
	GDBConsole::shutdown();
    s_running = false;
    if (m_BBConnectProcess)
        TerminateProcess(m_BBConnectProcess, 0);
}


/// <summary> 
/// Verify if Input buffer is empty. 
/// </summary>
/// <returns> Returns TRUE if empty; FALSE if not. </returns>
bool GDBParser::is_Input_Buffer_Empty()
{
	return(isInputBufferEmpty());
}


/// <summary> 
/// Sends a synchronous GDB command by adding it into the Input Buffer and waiting for the respective GDB response. This 
/// method is called by the Debug Engine whenever it needs a GDB response for a given GDB command. 
/// </summary>
/// <param name="GDBCommand"> Command to be sent to GDB. </param>
/// <param name="ID"> Instruction ID. </param>
/// <returns> Parsed GDB response. </returns>
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
	if (strcmp(output,"$#@EMPTY@#$") == 0) // This string means that both GDB and the parser worked well and returned an empty string.
		strcpy(output,"");

	String ^systemstring = gcnew String(output);
	return systemstring;
}


/// <summary> 
/// Sets the NDK variables. 
/// </summary>
/// <param name="isSimulator"> Boolean value that indicates which of the simulator/device is being used:
/// True -> simulator; 
/// False -> Device. </param>
void GDBParser::setNDKVars(bool isSimulator) {
	String^ keyPath = "HKEY_CURRENT_USER\\Software\\BlackBerry\\BlackBerryVSPlugin";
	String^ ndkHostPath = (String^)Registry::GetValue(keyPath, "NDKHostPath", "");
	String^ ndkTargetPath = (String^)Registry::GetValue(keyPath, "NDKTargetPath", "");
	m_remotePath = (String^)Registry::GetValue(keyPath, "NDKRemotePath", "");
			
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


/// <summary> 
/// Run BlackBerryConnect to stablish a connection with the device/simulator. 
/// </summary>
/// <param name="IPAddrStr"> Device/Simulator IP. </param>
/// <param name="toolsPath"> NDK full path. </param>
/// <param name="publicKeyPath"> Public key full path. </param>
/// <param name="password"> Device/simulator password. </param>
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
/// Execute GDB only to get the list of running processes in the Device/Simulator
/// </summary>
/// <param name="IP"> Device/Simulator IP. </param>
/// <param name="password"> Device/simulator password. </param>
/// <param name="isSimulator"> TRUE when using the Simulator, FALSE when using the Device. </param>
/// <param name="toolsPath"> NDK full path. </param>
/// <param name="publicKeyPath"> Public key full path. </param>
/// <param name="timeout"> How many seconds to wait for connecting to the device. </param>
/// <returns> A string with the list of running processes. </returns>
String^ GDBParser::GetPIDsThroughGDB(String^ IP, String^ password, bool isSimulator, String^ toolsPath, String^ publicKeyPath, int timeout)
{
	string response;
	string parsed;
    m_BBConnectProcess = NULL;
	char pcCmd[256];
	
	// Run BlackBerryConnect
    BlackBerryConnect(IP, toolsPath, publicKeyPath, password);

	// Get device (IP address)	
	CAutoPtr <char> ip = convertToAutoPtrFromString(IP);

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
	
	/// Send intialization commands to GDB
	response = console->waitForPrompt(true);
	if ((response == "") || (response[0] == '!')) //found an error
	{
		// ??? load output console window with the parsed message.
		response = "";
	}

	if (response != "")
	{
//		sprintf(pcCmd, "set debug nto-debug 2\r\n");
//		console->sendCommand(pcCmd);
//		response = console->waitForPrompt(true);

		sprintf(pcCmd, "1-target-select qnx %s:8000\r\n", ip);
		console->sendCommand(pcCmd);
		response = console->waitForPromptWithTimeout(timeout);
		if ((response == "") || (response[0] == '!')) //found an error
		{
			// ??? load output console window with the parsed message.
			response = "";
		}

		if ((response != "") && (response != "TIMEOUT!") && (response.find("1^error,msg=",0) == -1)) //there is no error from previous response
		{
			sprintf(pcCmd, "info pidlist\r\n");
			console->sendCommand(pcCmd);
			response = console->waitForPrompt(true);
			if ((response == "") || (response[0] == '!')) //found an error
			{
				// ??? load output console window with the parsed message.
				response = "";
			}
		}
	}
	exitGDB();
	String ^systemString = gcnew String(response.c_str());
	return systemString;
}


/// <summary> 
/// Launch the GDB Debug Process. 
/// </summary>
/// <param name="pidStr"> ID of the running process to be debugged. </param>
/// <param name="exeStr"> Full path of the executable file. </param>
/// <param name="IPAddrStr"> Device/Simulator IP. </param>
/// <param name="isSimulator"> TRUE when using the Simulator, FALSE when using the Device. </param>
/// <param name="toolsPath"> NDK full path. </param>
/// <param name="publicKeyPath"> Public key full path. </param>
/// <param name="password"> Device/simulator password. </param>
/// <returns> True -> succeeded;
/// False -> failed. </returns>
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
	CAutoPtr <char> apBinaryFile;
	if (exeStr == "CannotAttachToRunningProcess")
		apBinaryFile = convertToAutoPtrFromString("");
	else
		apBinaryFile = convertToAutoPtrFromString(exeStr);

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
	response = console->waitForPrompt(true);
	parsed = parseGDB(response, parsingInstructions[2]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// TODO: load output console window with the response.
		return false;
	}

//	sprintf(pcCmd, "set debug nto-debug 2\r\n");
//	console->sendCommand(pcCmd);
//	response = console->waitForPrompt(true);

	sprintf(pcCmd, "1-gdb-set breakpoint pending on\r\n");
	console->sendCommand(pcCmd);
	response = console->waitForPrompt(true);
	parsed = parseGDB(response, parsingInstructions[8]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// TODO: load output console window with the response.
		return false;
	}

	sprintf(pcCmd, "4-target-select qnx %s:8000\r\n", apDevice);
	console->sendCommand(pcCmd);
	response = console->waitForPrompt(true);
	parsed = parseGDB(response, parsingInstructions[3]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// TODO: load output console window with the response.
		return false;
	}

	sprintf(pcCmd, "5-file-exec-and-symbols %s\r\n", apBinaryFile);		
	console->sendCommand(pcCmd);
	response = console->waitForPrompt(true);
	parsed = parseGDB(response, parsingInstructions[7]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// TODO: load output console window with the response.
		return false;
	}
	
	if (m_remotePath != "")
	{
	  CAutoPtr <char> apPath = convertToAutoPtrFromString(m_remotePath);
	  sprintf(pcCmd, "6set solib-search-path %s\r\n", apPath);    
	  console->sendCommand(pcCmd);
	  response = console->waitForPrompt(true);
	  parsed = parseGDB(response, parsingInstructions[8]);
	  if ((parsed == "") || (parsed[0] == '!')) //found an error
	  {
		// TODO: load output console window with the response.
		return false;
	  }
	}
  
	sprintf(pcCmd, "7-target-attach %d\r\n", pid);
	console->sendCommand(pcCmd);
	response = console->waitForPrompt(true);
	parsed = parseGDB(response, parsingInstructions[6]);
	if ((parsed == "") || (parsed[0] == '!')) //found an error
	{
		// TODO: load output console window with the response.
		return false;
	}
	
//	start thread responsible for reading InputBuffer and sending commands to GDB.
//	Thread must start after initializing GDB with the previous instructions because:
//  - this thread will start another one (listening GDB) that will start consuming GDB responses. So, the above waitForPrompt would 
//	  never get a result and this method would freeze.
//  - the previous commands don't use the GDB Buffer, so the results wouldn't be parsed correctly.
	sendingThread = (HANDLE)_beginthreadex( NULL, 0, &sendingCommands2GDB, (void *)console, 0, &threadID);

	return true;
}




// From this point on it is implemented the interface that allows creating unit tests for gdb-connect functions. Don't use these methods
// for something else because they need some resources that are allocated correctly when launching GDB the right way, i.e., by using the 
// above GDBParser::LaunchProcess method. To be able to run the unit tests, it is needed to create mock resources or call 
// GDBParser::LaunchProcess method first, because it will allocate the needed resources and will start GDB. 


/// <summary> 
/// Get the associated instruction ID from the command that would be sent to GDB. 
/// </summary>
/// <param name="command"> Command that would be sent to GDB. </param>
/// <param name="param"> When the instruction code stored in "map" is negative, it means that the commands parameters should be saved to 
/// be used by the listeningGDB thread. When that happens, update and return variable "param". </param>
/// <returns> Returns the instruction code for the command that would be sent to GDB or -1 in case of an error. It can also return the 
/// command parameters, as it was described above. </returns>
int GDBParserUnitTests::get_Instruction_Code(String^ command, [Runtime::InteropServices::Out] String^ %param)
{
	CAutoPtr <char> apCmd = convertToAutoPtrFromString(command);
	string c = apCmd;
	string p = "";

	unordered_map<string, int> map;
	string v[NumberOfInstructions];

	// First create the hash table map.
	bool result = insertingCommandCodes(&map, v);

	int code = -2;

	if (result == 1)
	{
		code = getInstructionCode(c, map, &p);
		param = gcnew String(p.c_str());
	}
	return code;
}


/// <summary> 
/// Get the sequential ID of the supposed response. 
/// </summary>
/// <param name="response"> The supposed GDB response. </param>
/// <returns> If the supposed response is synchronous, this ID corresponds to the sequential ID of the command that should be sent to GDB 
/// that generated such supposed response. If the supposed response is asynchronous, there is no sequential ID, so it returns:
///		-3 - when it shows that a supposed breakpoint was modified;
///		-2 - for any other supposed asynchronous response
///		-1 - when the supposed response is not completed or in case of an error. </returns>
int GDBParserUnitTests::get_Seq_ID(String^ response)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(response);
	string r = apResp;
	return getSeqID(r);
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
/// <param name="map"> Array that contains all the data from the Hash map created by insertingCommandCodes() function, that are the GDB 
/// commands recognized by the parser and their respectives instructions IDs. </param>
/// <param name="parsingInstructions"> A string array with all of the currently recognized parsing instructions. The index position 
/// correponds to the instruction ID used in "map" for the respective GDB command. </param>
/// <returns> 1 in case of sucess; 0 if there is an error when opening the Instructions.txt file. </returns>
bool GDBParserUnitTests::inserting_Command_Codes(array<String^, 2>^ map, array<String^>^ parsingInstructions)
{
	unordered_map<string, int> m;
	string v[NumberOfInstructions];

	bool result = insertingCommandCodes(&m, v);

	for (int i = 0; i < NumberOfInstructions; i++)
	{
		parsingInstructions[i] = gcnew String(v[i].c_str());
	}

	int i = 0;
	for (unordered_map<string, int>::iterator it = m.begin(); it != m.end(); ++it) 
	{
		map[i,0] = gcnew String(it->first.c_str());
		map[i,1] = Convert::ToString(it->second);
		i++;
	}

	return result;
}


/// <summary> 
/// Function used to call the parser that will parse a supposed GDB response according to the supposed parsing instruction, returning 
/// this parsed response. This function is called when starting GDB (this one doesn't use the OutputBuffer because it returns the supposed 
/// parsed response). 
/// </summary>
/// <param name="response"> Supposed GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the supposed GDB response. </param>
/// <returns> Returns the parsed GDB response. </returns>
String^ GDBParserUnitTests::parse_GDB(String^ response, String^ parsingInstruction)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(response);
	string r = apResp;
	CAutoPtr <char> apPars = convertToAutoPtrFromString(parsingInstruction);
	string p = apPars;
	string output = parseGDB(r, p);
	String ^systemstring = gcnew String(output.c_str());
	return systemstring;
}


/// <summary> 
/// Function used to call the parser that will parse a supposed GDB response according to the supposed parsing instruction, storing 
/// the parsed response in the respective position in the Output Buffer. Must be used together with the functions that create and handle
/// the Output Buffer. DON'T CALL THIS METHOD PASSING A POSITION OF OUTPUT BUFFER THAT IS ALREADY FILLED! parseGDB(string, string, int) is
/// used in an environment that has a different thread consuming items from Output Buffer. In this initial unit tests environment only
/// the current thread is running, which means that, if you try to add data to a non-empty position, this method will be busy-waiting till 
/// that entry is empty, causing, in this scenario, a DEADLOCK.
/// </summary>
/// <param name="response"> GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the respective GDB response. </param>
/// <param name="seqStamp"> Position in the Output Buffer in which the parsed response will be stored. </param>
void GDBParserUnitTests::parse_GDB(String^ response, String^ parsingInstruction, int position)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(response);
	string r = apResp;
	CAutoPtr <char> apPars = convertToAutoPtrFromString(parsingInstruction);
	string p = apPars;
	parseGDB(r, p, position);
}


/// <summary> 
/// Reponsible for parsing a supposed GDB response. Depending on the parsing instruction, the parser can get the first occurrence  
/// from the supposed GDB response or get all of them that satisfies this parsing instruction. 
/// </summary>
/// <param name="response"> Supposed GDB response. </param>
/// <param name="parsingInstruction"> Instruction used to parse the supposed GDB response. </param>
/// <param name="respBegin"> Current character to be read in the supposed GDB response. </param>
/// <param name="repeat"> If the parsing instruction specifies that it has to get all occurences from the supposed GDB response, this 
/// value must be true. </param>
/// <param name="variables"> A variable stores a string parsed from the supposed GDB response and can be used as many times as needed to 
///	created the parsed response. Up to 10 variables can be created for a given parsing instruction. </param>
/// <param name="separator"> If the parsing instruction allows the parser to get all occurrences from the supposed  GDB response, this 
/// separator will be used at the end of each occurence. The default one is '#' but it can be specified in the parsing instruction. </param>
/// <returns> Returns the parsed GDB response. </returns>
String^ GDBParserUnitTests::parse_GDB(String^ response, String^ parsingInstruction, int respBegin, bool repeat, array<String^>^ variables, String^ separator)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(response);
	string r = apResp;
	CAutoPtr <char> apPars = convertToAutoPtrFromString(parsingInstruction);
	string p = apPars;
	CAutoPtr <char> apSep = convertToAutoPtrFromString(separator);
	string s = apSep;

	char v[10][128];
	for (int i = 0; i< 10; i++)
	{
		CAutoPtr <char> apAux = convertToAutoPtrFromString(variables[i]);
		string aux = apAux;

		if (aux.length() > 128)
			aux[127] = '\0';
		strcpy(v[i], aux.c_str());
	}

	string output = parseGDB(&r, p, respBegin, repeat, v, s);
	String ^systemstring = gcnew String(output.c_str());
	return systemstring;
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
int GDBParserUnitTests::find_Closing(char opening, char closing, String^ parsingInstruction, int ini)
{
	CAutoPtr <char> apPars = convertToAutoPtrFromString(parsingInstruction);
	string p = apPars;
	return findClosing(opening, closing, p, ini);
}


/// <summary> 
/// Get the next position of a given character (token) in a string message (txt), starting from a given position (pos). 
/// </summary>
/// <param name="token"> Character to search for. </param>
/// <param name="txt"> String message in which it will search for the given character. </param>
/// <param name="pos"> Starting position in the string message. </param>
/// <returns> An integer that corresponds to the next position of the character in the string. If the character is not found, returns
/// the length of the string. </returns>
int GDBParserUnitTests::get_Next_Char(char token, String^ txt, int pos)
{
	CAutoPtr <char> apTxt = convertToAutoPtrFromString(txt);
	string t = apTxt;
	return getNextChar(token, t, pos);
}


/// <summary> 
/// Get the position of a given string (txt) in the string "response", starting from a given position (begin). 
/// </summary>
/// <param name="response"> String response where the search will be performed. </param>
/// <param name="txt"> String to search for in the response. </param>
/// <param name="begin"> Starting position in the string response. </param>
/// <param name="times"> Search for that string "times" times. Ex: I want to find the third occurrence of word "qaqa" in the  
/// response. </param>
/// <param name="forward"> Direction: if true, search forwards; if not, search backwards </param>
/// <param name="instruction"> The kind of parsing instruction that called this method. '?', '@', or '~'. If searching forward and '?', the 
/// function will return the next position after the found string. If not, will return the first position of the found string. </param>
/// <returns> An integer that corresponds to the next position of the string txt in the response. -1 in case of an error. </returns>
int GDBParserUnitTests::search_Response(String^ response, String^ txt, int begin, int times, bool forward, char instruction)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(response);
	string r = apResp;
	CAutoPtr <char> apTxt = convertToAutoPtrFromString(txt);
	string t = apTxt;
	return searchResponse(r, t, begin, times, forward, instruction);
}


/// <summary> 
/// Substitute the existing variables in the string "txt" by their values, stored in the "variables" array. Each variable name 
/// has this format: $9$, where $ characters are used to identify the variable while the number corresponds to the variable ID, that also
/// corresponds to the array index. There is a special variable "$EOL$" that is substituted by "\r\n".
/// </summary>
/// <param name="txt"> String to search for variables. </param>
/// <param name="variables"> Array with the variable values. </param>
/// <returns> Returns the new modified string. </returns>
String^ GDBParserUnitTests::substitute_Variables(String^ txt, array<String^>^ variables)
{
	CAutoPtr <char> apTxt = convertToAutoPtrFromString(txt);
	string t = apTxt;
	char v[10][128];
	for (int i = 0; i< 10; i++)
	{
		CAutoPtr <char> apAux = convertToAutoPtrFromString(variables[i]);
		string aux = apAux;

		if (aux.length() > 128)
			aux[127] = '\0';
		strcpy(v[i], aux.c_str());
	}
	string output = substituteVariables(t, v);
	String ^systemstring = gcnew String(output.c_str());
	return systemstring;
}


/// <summary> 
/// This method stores a command that should be sent to GDB. Input buffer is a circular one. 
/// </summary>
/// <param name="GDBCommand"> Command that should be sent to GDB. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
bool GDBParserUnitTests::add_Into_Input_Buffer(String^ command)
{
	CAutoPtr <char> apCmd = convertToAutoPtrFromString(command);
	char GDBCommand[GDBCommandSize];
	strcpy(GDBCommand, apCmd);
	return addIntoInputBuffer(GDBCommand);
}


/// <summary> 
/// Gets the next command that would be sent to GDB. Input buffer is a circular one. 
/// </summary>
/// <returns> Returns the next command that would be sent to GDB. </returns>
String^ GDBParserUnitTests::remove_From_Input_Buffer()
{
	char command[GDBCommandSize] = "";
	removeFromInputBuffer(command);
	string basicstring(command);
	return(gcnew String(basicstring.c_str()));
}


/// <summary> 
/// Verify if Input buffer is empty. 
/// </summary>
/// <returns> Returns TRUE if empty; FALSE if not. </returns>
//bool GDBParserUnitTests::is_Input_Buffer_Empty()
//{
//	return isInputBufferEmpty();
//}


/// <summary> 
/// For every command that should be to GDB, the following parameters should be added into GDBBuffer. 
/// </summary>
/// <param name="seq_id"> Sequential ID of the command that would be sent to GDB. It is used to identify, when evaluating supposed GDB 
/// responses, which command generated that given response. This ID is also used to specify the location in which this entry will be 
/// stored in this buffer. </param>
/// <param name="instructionCode"> Each command that should be sent to GDB has an associated instruction code, that corresponds to the 
/// parsing instruction that will be used to parse the supposed GDB response. </param>
/// <param name="param"> Used to store the GDB command parameters, so they could be used during the parsing task. There are some GDB 
/// commands, like -break-delete for example, that results in a simple "^done" GDB response. The parser can identify which command  
/// caused that response but cannot know what was affected by it (considering the -break-delete command, the parser will know that some
/// breakpoint was successfully deleted but won't know which of them was deleted). Using "param" helps the parser letting it knows
/// which parameters were sent together with a given GDB command. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
bool GDBParserUnitTests::add_Into_GDB_Buffer(int seq_id, int instructionCode, String^ param)
{
	CAutoPtr <char> apParam = convertToAutoPtrFromString(param);
	string p = apParam;
	return addIntoGDBBuffer(seq_id, instructionCode, p);
}


/// <summary> 
/// This method is called to find the right parsing instruction for the supposed GDB response.  
/// </summary>
/// <param name="seq"> Sequential ID of the GDB response. </param>
/// <param name="param"> Returns the parameters sent together with the GDB command that generated this response, if there are some. </param>
/// <returns> Returns the instruction code associated to the sequential id (seq). </returns>
int GDBParserUnitTests::remove_From_GDB_Buffer(int seq, [Runtime::InteropServices::Out] String^ %param)
{
	string p = "";
	int code = removeFromGDBBuffer(seq, &p);
	param = gcnew String(p.c_str());
	return code;
}


/// <summary> 
/// Verify if GDB buffer is empty. 
/// </summary>
/// <returns> Returns TRUE if empty; FALSE if not. </returns>
bool GDBParserUnitTests::is_GDB_Buffer_Empty()
{
	return isGDBBufferEmpty();
}


/// <summary> 
/// Store the supposed parsed GDB response in the "seq_stamp" position of the Output buffer. 
/// </summary>
/// <param name="seq_stamp"> Position in the Output buffer to store the supposed parsed GDB response. </param>
/// <param name="parsedMessage"> Supposed parsed GDB response. </param>
/// <returns> Returns TRUE - successfully added; or FALSE - failed to add. </returns>
bool GDBParserUnitTests::add_Into_Output_Buffer(int seq_stamp, String^ message)
{
	CAutoPtr <char> apResp = convertToAutoPtrFromString(message);
	char parsedMessage[GDBCommandSize];
	strcpy(parsedMessage, apResp);
	return addIntoOutputBuffer(seq_stamp, parsedMessage);
}


/// <summary> 
/// This method returns the next supposed parsed GDB response (output) for a supposed asynchronous GDB RESPONSE or a supposed non 
/// synchronous GDB COMMAND. This managed part of Output buffer is circular. 
/// </summary>
/// <param name="output"> Returns the supposed parsed GDB response. </param>
String^ GDBParserUnitTests::remove_From_Output_Buffer()
{
	char response[GDBCommandSize] = "";
	removeFromOutputBuffer(response);
	string basicstring(response);
	return(gcnew String(basicstring.c_str()));
}


/// <summary> 
/// This method returns the supposed parsed GDB response (output) for a supposed synchronous GDB COMMAND (ID). Each entry in this part 
/// of Output buffer was previously reserved for each GDB command. 
/// </summary>
/// <param name="output"> Returns the supposed parsed GDB response. </param>
/// <param name="ID"> ID of the supposed GDB command. </param>
String^ GDBParserUnitTests::remove_Sync_From_Output_Buffer(int ID)
{
	char response[GDBCommandSize] = "";
	removeSyncFromOutputBuffer(response, ID);
	string basicstring(response);
	return(gcnew String(basicstring.c_str()));
}


/// <summary> 
/// Initialize / clean all the existing buffers: inputBuffer, GDBBuffer, outputBuffer. 
/// </summary>
void GDBParserUnitTests::clean_Buffers()
{
	cleanBuffers();
}
