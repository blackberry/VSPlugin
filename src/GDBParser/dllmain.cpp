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
class CGDBParserModule : public CAtlDllModuleT<CGDBParserModule>
{
private:
    HMODULE     _hModule;

#if _ATL_VER >= 0x0C00

public:
    // Added missing method, as it was removed between from VS2013 ATL library version.
    // The unsuffixed name has the same meaning as the old one with 'S' from the VS2010.
    inline HRESULT WINAPI UpdateRegistryFromResourceS(
        _In_ UINT nResID,
        _In_ BOOL bRegister,
        _In_opt_ struct _ATL_REGMAP_ENTRY* pMapEntries /*= NULL*/) throw()
    {
        return UpdateRegistryFromResource(nResID, bRegister, pMapEntries);
    }

#endif

public:
    // Stores the handle to current library instance.
    void SetModuleHandle(HMODULE hModule)
    {
        _hModule = hModule;
    }

    // Inserts into the specified buffer the path to the current library.
    BOOL GetDirectory(LPTSTR buffer, size_t size)
    {
        TCHAR wszLibNameBuffer[MAX_PATH];
        TCHAR *lpszFileName = NULL;

        if (buffer == NULL)
            return FALSE;

        // get the binary's full-path:
        GetModuleFileName(_hModule, wszLibNameBuffer, MAX_PATH);
        if (GetLastError() != ERROR_SUCCESS)
            return FALSE;

        // remove the file-name:
        if (!GetFullPathName(wszLibNameBuffer, size, buffer, &lpszFileName))
            return FALSE;

        if (lpszFileName == NULL)
            return FALSE;
        *lpszFileName = _T('\0');
        return TRUE;
    }
};

CGDBParserModule _GDBParserModule;


/// <summary> 
/// Adds entries to the system registry. 
/// </summary>
/// <returns> HRESULT </returns>
STDAPI DllRegisterServer(void)
{
    WCHAR wszPath[MAX_PATH + 1];

    // Get this binary's path
    if (!_GDBParserModule.GetDirectory(wszPath, MAX_PATH + 1))
        return E_FAIL;

    // GDBPARSERPATH should point to GDBParserAPI
    // Register the sample engine in the Visual Studio registry hive. See GDBParser.rgs for what is added.
     _ATL_REGMAP_ENTRY rgMap[] =
    {
        {L"GDBPARSERPATH", wszPath},
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
    WCHAR wszPath[MAX_PATH + 1];

    // Get this binary's path
    if (!_GDBParserModule.GetDirectory(wszPath, MAX_PATH + 1))
        return E_FAIL;

    // GDBPARSERPATH should point to GDBParserAPI
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
BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    _GDBParserModule.SetModuleHandle(hModule);

    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            ::DisableThreadLibraryCalls(hModule);
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}

