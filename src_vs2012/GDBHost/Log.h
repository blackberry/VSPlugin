#pragma once

#include "stdafx.h"


#define LOGS_ENABLED        0


#if LOGS_ENABLED
    void LogInitialize();
    void LogPrint(TCHAR *message);
#else
#   define LogInitialize()                  // do nothing
#   define LogPrint(message)                // do nothing
#endif

void PrintConsole(LPCTSTR format, ...);
void PrintError(LPCTSTR lpszFunctionName);
void ShowMessage(LPCTSTR lpszFunctionName, LPCTSTR arguments);
