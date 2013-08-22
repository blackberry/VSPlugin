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

BEGIN_NAMESPACE

public ref class Constants
{
public:
	#undef S_OK
	static const int S_OK = 0;

	#undef S_FALSE
	static const int S_FALSE = 1;

	#define __E_NOTIMPL E_NOTIMPL
	#undef E_NOTIMPL
	static const int E_NOTIMPL = __E_NOTIMPL;

	#define __E_FAIL E_FAIL
	#undef E_FAIL
	static const int E_FAIL = __E_FAIL;

	static const int E_WIN32_INVALID_NAME = HRESULT_FROM_WIN32(ERROR_INVALID_NAME);

	static const int E_WIN32_ALREADY_INITIALIZED = HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);

	#define __RPC_E_SERVERFAULT RPC_E_SERVERFAULT
	#undef RPC_E_SERVERFAULT
	static const int RPC_E_SERVERFAULT = __RPC_E_SERVERFAULT;
};

END_NAMESPACE