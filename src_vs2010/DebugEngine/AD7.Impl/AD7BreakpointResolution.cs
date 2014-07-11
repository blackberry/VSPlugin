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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;

// TODO: Change to take filename and line number instead of an address?

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class represents the information that describes a bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb145894.aspx)
    /// </summary>
    public class AD7BreakpointResolution : IDebugBreakpointResolution2
    {
        /// <summary>
        ///  AD7 Engine.
        /// </summary>
        private readonly AD7Engine _engine;

        /// <summary>
        /// GDB Address
        /// </summary>
        private readonly uint _address;

        /// <summary>
        /// The document context to the debugger. A document context represents a location within a source file.
        /// </summary>
        private readonly AD7DocumentContext _documentContext;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> AD7 Engine. </param>
        /// <param name="address"> GDB Address. </param>
        /// <param name="documentContext"> The document context to the debugger. A document context represents a location within a 
        /// source file. </param>
        public AD7BreakpointResolution(AD7Engine engine, uint address, AD7DocumentContext documentContext)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            _engine = engine;
            _address = address;
            _documentContext = documentContext;
        }

        #region IDebugBreakpointResolution2 Members

        /// <summary>
        /// Gets the type of the breakpoint represented by this resolution. (http://msdn.microsoft.com/en-us/library/bb145576.aspx)
        /// </summary>
        /// <param name="pBPType"> The type of this breakpoint. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            // The VSNDK debug engine only supports code breakpoints.
            pBPType[0] = enum_BP_TYPE.BPT_CODE;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the breakpoint resolution information that describes this breakpoint. 
        /// (http://msdn.microsoft.com/en-us/library/bb146743.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags that determine which fields of the pBPResolutionInfo parameter are to be filled out. </param>
        /// <param name="pBPResolutionInfo"> The BP_RESOLUTION_INFO structure to be filled in with information about this breakpoint. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugBreakpointResolution2.GetResolutionInfo(enum_BPRESI_FIELDS dwFields, BP_RESOLUTION_INFO[] pBPResolutionInfo)
        {
            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION) != 0) 
            {
                // The sample engine only supports code breakpoints.
                BP_RESOLUTION_LOCATION location = new BP_RESOLUTION_LOCATION();
                location.bpType = (uint)enum_BP_TYPE.BPT_CODE;

                // The debugger will not QI the IDebugCodeContex2 interface returned here. We must pass the pointer
                // to IDebugCodeContex2 and not IUnknown.
                AD7MemoryAddress codeContext = new AD7MemoryAddress(_engine, _address);
                codeContext.SetDocumentContext(_documentContext);
                location.unionmember1 = Marshal.GetComInterfaceForObject(codeContext, typeof(IDebugCodeContext2));
                pBPResolutionInfo[0].bpResLocation = location;
                pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION;

            }

            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_PROGRAM) != 0) 
            {
                pBPResolutionInfo[0].pProgram = _engine;
                pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_PROGRAM;
            }

            return VSConstants.S_OK;
        }

        #endregion
    }

    /// <summary>
    /// Represents the resolution of a breakpoint error. (http://msdn.microsoft.com/en-us/library/bb161341.aspx)
    /// </summary>
    class AD7ErrorBreakpointResolution : IDebugErrorBreakpointResolution2
    {
        #region IDebugErrorBreakpointResolution2 Members

        /// <summary>
        /// Gets the breakpoint type. Not implemented. (http://msdn.microsoft.com/en-us/library/bb145065.aspx)
        /// </summary>
        /// <param name="pBPType"> The type of this breakpoint. </param>
        /// <returns> Not implemented. </returns>
        int IDebugErrorBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Gets the breakpoint error resolution information. Not implemented. (http://msdn.microsoft.com/en-us/library/bb161960.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags that determine which fields of pErrorResolutionInfo are to be filled out. </param>
        /// <param name="pErrorResolutionInfo"> The BP_ERROR_RESOLUTION_INFO structure that is filled in with the description of the 
        /// breakpoint resolution. </param>
        /// <returns> Not implemented. </returns>
        int IDebugErrorBreakpointResolution2.GetResolutionInfo(enum_BPERESI_FIELDS dwFields, BP_ERROR_RESOLUTION_INFO[] pErrorResolutionInfo)
        {
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_BPRESLOCATION) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_PROGRAM) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_THREAD) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_MESSAGE) != 0) { }
            if (((uint)dwFields & (uint)enum_BPERESI_FIELDS.BPERESI_TYPE) != 0) { }

            return EngineUtils.NotImplemented();
        }

        #endregion
    }
}
