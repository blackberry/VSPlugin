#pragma once

#include "stdafx.h"


#define LOG_GDB_RAW_IO 1


void LogInitialize();
void LogPrint (TCHAR *message);

void ErrorExit(LPTSTR);
void DisplayError(LPCTSTR);
