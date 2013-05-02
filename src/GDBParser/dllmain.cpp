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

// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "resource.h"

#pragma managed(off)


/// <summary> 
/// Use the ATL Registrar to register the engine. 
/// </summary>
class CGDBParserModule : public CAtlDllModuleT< CGDBParserModule >
{

};

CGDBParserModule _GDBParserModule;
HMODULE _hModThis;


/// <summary> 
/// Adds entries to the system registry. 
/// </summary>
/// <returns> HRESULT </returns>
STDAPI DllRegisterServer(void)
{
    // Get this binaries full-path
    WCHAR wszThisFile[MAX_PATH + 1];
    GetModuleFileName(_hModThis, wszThisFile, MAX_PATH + 1);

	// Cut off the FileName. GDBPARSERPATH should point to GDBParserAPI
	WCHAR wszPath[MAX_PATH + 1];
	WCHAR* wszFileName;
	GetFullPathName(wszThisFile, MAX_PATH + 1, wszPath, &wszFileName);
	*wszFileName = L'\0';

    // Register the sample engine in the Visual Studio registry hive. See GDBParser.rgs for what is added.
     _ATL_REGMAP_ENTRY rgMap[] =
    {
        {L"GDBPARSERPATH",                   wszPath},
        {NULL, NULL}
    };

    HRESULT hr = _GDBParserModule.UpdateRegistryFromResourceS(IDR_GDBPARSER, true, rgMap);
	return hr;
}


/// <summary> 
/// Removes entries from the system registry. 
/// </summary>
/// <returns> HRESULT </returns>
STDAPI DllUnregisterServer(void)
{
    // Get this binaries full-path
    WCHAR wszThisFile[MAX_PATH + 1];
    GetModuleFileName(_hModThis, wszThisFile, MAX_PATH + 1);

	// Cut off the FileName. GDBPARSERPATH should point to GDBParserAPI
	WCHAR wszPath[MAX_PATH + 1];
	WCHAR* wszFileName;
	GetFullPathName(wszThisFile, MAX_PATH + 1, wszPath, &wszFileName);
	*wszFileName = L'\0';

    // Register the sample engine in the Visual Studio registry hive. See GDBParser.rgs for what is added.
     _ATL_REGMAP_ENTRY rgMap[] =
    {
        {L"GDBPARSERPATH",                   wszPath},
        {NULL, NULL}
    };

    HRESULT hr = _GDBParserModule.UpdateRegistryFromResourceS(IDR_GDBPARSER, false, rgMap);
	return hr;
}


/// <summary> </summary>
/// <param name="hModule"> </param>
/// <param name="ul_reason_for_call"> </param>
/// <param name="lpReserved"> </param>
/// <returns> TRUE. </returns>
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
    _hModThis = hModule;
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

