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

BEGIN_NAMESPACE

//#define SHOW_BB_CONNECT_WINDOW

public ref class GDBParser abstract sealed
{
public:	
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
