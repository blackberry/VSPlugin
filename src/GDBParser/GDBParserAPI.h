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

#include <unordered_map>

#define NumberOfInstructions 48
#define GDBCommandSize 256

using namespace std;

BEGIN_NAMESPACE

public ref class GDBParser abstract sealed
{
public:	
	static String^ GetPIDsThroughGDB(String^, String^, bool, String^, String^);
	static bool LaunchProcess(String^, String^, String^, bool, String^, String^, String^);	
    static void BlackBerryConnect(String^, String^, String^, String^);

	static String^ parseCommand(String^, int);
	static String^ removeGDBResponse(); 
	static bool addGDBCommand(String^);  
	static bool is_Input_Buffer_Empty();
    static void exitGDB();

private:
	static void setNDKVars(bool);

	static DWORD s_mainThreadId;
	static String^ m_pcGDBCmd;
	static const int NUM_LIB_PATHS = 2;
	static array<String^>^ m_libPaths = gcnew array<String^>(NUM_LIB_PATHS);
    static bool s_running = true;
    static HANDLE m_BBConnectProcess;
};

END_NAMESPACE


BEGIN_NAMESPACE

public ref class GDBParserUnitTests abstract sealed
{
public:	
	static int get_Instruction_Code(String^, [Runtime::InteropServices::Out] String^ %);
	static int get_Seq_ID(String^);
	static bool inserting_Command_Codes(array<String^, 2>^, array<String^>^);

	static String^ parse_GDB(String^, String^);
	static void parse_GDB(String^, String^, int);
	static String^ parse_GDB(String^, String^, int, bool, array<String^>^, String^);

	static int find_Closing(char, char, String^, int);
	static int get_Next_Char(char, String^, int);
	static int search_Response(String^, String^, int, int, bool, char);
	static String^ substitute_Variables(String^, array<String^>^);

	static bool add_Into_Input_Buffer(String^);
	static String^ remove_From_Input_Buffer();
	static bool add_Into_GDB_Buffer(int, int, String^);
	static int remove_From_GDB_Buffer(int, [Runtime::InteropServices::Out] String^ %);
	static bool is_GDB_Buffer_Empty();
	static bool add_Into_Output_Buffer(int, String^);
	static String^ remove_From_Output_Buffer();
	static String^ remove_Sync_From_Output_Buffer(int);
	static void clean_Buffers();
};
END_NAMESPACE