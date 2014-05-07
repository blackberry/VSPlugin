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

public ref class ComponentException : public Exception
{
public:
    ComponentException(HRESULT hr)
        : Exception()
    {
        Exception::HResult = hr;
    }

    property HRESULT HResult
    {
        HRESULT get()
        {
            return Exception::HResult;
        }
    }
};

END_NAMESPACE
