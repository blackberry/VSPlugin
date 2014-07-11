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

using System;
using BlackBerry.NativeCore.Diagnostics;
using Microsoft.VisualStudio;
using System.Diagnostics;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// Some utilities used in VSNDK debug engine projects.
    /// </summary>
    public static class EngineUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hr"> An integer value. </param>
        public static void RequireOk(int hr)
        {
            if (hr != VSConstants.S_OK)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"> Exception. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        public static int UnexpectedException(Exception ex)
        {
            TraceLog.WriteException(ex);

            Debug.Fail("Unexpected exception: " + ex);
            return VSConstants.E_FAIL;
        }

        public static int NotImplemented()
        {
            TraceLog.WriteLine("Hit non implemented method!");
            if (Debugger.IsAttached)
                Debugger.Break();

            return VSConstants.E_NOTIMPL;
        }
    }
}
