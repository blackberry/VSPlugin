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
using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Interop;
using VSNDK.Parser;

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This class manages breakpoints for the engine. 
    /// </summary>
    public class BreakpointManager
    {

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        private AD7Engine m_engine;        

        /// <summary>
        /// List of pending breakpoints.
        /// </summary>
        private List<AD7PendingBreakpoint> m_pendingBreakpoints;

        /// <summary>
        /// List of active breakpoints.
        /// </summary>
        private List<AD7BoundBreakpoint> m_activeBPs;


        /// <summary>
        /// Breakpoint manager constructor.
        /// </summary>
        /// <param name="engine"> Associated Debug Engine. </param>
        public BreakpointManager(AD7Engine engine)
        {
            m_engine = engine;
            m_pendingBreakpoints = new System.Collections.Generic.List<AD7PendingBreakpoint>();
            m_activeBPs = new System.Collections.Generic.List<AD7BoundBreakpoint>();
        }


        /// <summary>
        /// A helper method used to construct a new pending breakpoint.
        /// </summary>
        /// <param name="pBPRequest"> An IDebugBreakpointRequest2 object that describes the pending breakpoint to create. </param>
        /// <param name="ppPendingBP"> Returns an IDebugPendingBreakpoint2 object that represents the pending breakpoint. </param>
        public void CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            AD7PendingBreakpoint pendingBreakpoint = new AD7PendingBreakpoint(pBPRequest, m_engine, this);
            ppPendingBP = (IDebugPendingBreakpoint2)pendingBreakpoint;
            m_pendingBreakpoints.Add(pendingBreakpoint);
        }


        /// <summary>
        /// Return the active bound breakpoint matching the given GDB ID.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <returns> If successful, returns the active bound breakpoint; otherwise, returns null. </returns>
        public AD7BoundBreakpoint getBoundBreakpointForGDBID(uint GDB_ID)
        {
            foreach (AD7BoundBreakpoint bbp in m_activeBPs)
            {
                if ((bbp != null) && (bbp.GDB_ID == GDB_ID))
                {
                    return bbp;                    
                }
            }
            return null;
        }


        /// <summary>
        /// Called from the engine's detach method to remove the debugger's breakpoint instructions.
        /// </summary>
        public void ClearBoundBreakpoints()
        {
            foreach (AD7PendingBreakpoint pendingBreakpoint in m_pendingBreakpoints)
            {
                pendingBreakpoint.ClearBoundBreakpoints();
            }
        }

        /// <summary>
        /// Creates an entry and remotely enables the breakpoint in the debug stub.
        /// </summary>
        /// <param name="aBBP"> The bound breakpoint to add. </param>
        /// <returns> Breakpoint ID Number. </returns>
        public int RemoteAdd(AD7BoundBreakpoint aBBP)
        {

            m_activeBPs.Add(aBBP);

            // Call GDB to set a breakpoint based on filename and line no. in aBBP                                                           
            uint GDB_ID = 0;
            uint GDB_LinePos = 0;
            string GDB_Filename = "";
            string GDB_address = "";
            bool ret = false;

            if (aBBP.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                ret = m_engine.eDispatcher.setBreakpoint(aBBP.m_filename, aBBP.m_line, out GDB_ID, out GDB_LinePos, out GDB_Filename, out GDB_address);
            }
            else if (aBBP.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
            {
                ret = m_engine.eDispatcher.setBreakpoint(aBBP.m_func, out GDB_ID, out GDB_LinePos, out GDB_Filename, out GDB_address);
            }

            if (ret)
            {
                aBBP.GDB_ID = GDB_ID;
                aBBP.GDB_FileName = GDB_Filename;
                aBBP.GDB_LinePos = GDB_LinePos;
                aBBP.GDB_Address = GDB_address;
                m_engine.Callback.OnBreakpointBound(aBBP, 0);
            }
            return (int)GDB_ID;
        }

        /// <summary>
        /// Enable bound breakpoint.
        /// </summary>
        /// <param name="aBBP"> The Bound breakpoint to enable. </param>
        public void RemoteEnable(AD7BoundBreakpoint aBBP)
        {            
            m_engine.eDispatcher.enableBreakpoint(aBBP.GDB_ID, true);
        }

        
        /// <summary>
        /// Disable bound breakpoint.
        /// </summary>
        /// <param name="aBBP"> The Bound breakpoint to disable. </param>
        public void RemoteDisable(AD7BoundBreakpoint aBBP)
        {            
            m_engine.eDispatcher.enableBreakpoint(aBBP.GDB_ID, false);
        }

        
        /// <summary>
        /// Remove the associated bound breakpoint.
        /// </summary>
        /// <param name="aBBP"> The breakpoint to remove. </param>
        public void RemoteDelete(AD7BoundBreakpoint aBBP)
        {
            m_activeBPs.Remove(aBBP);        
            m_engine.eDispatcher.deleteBreakpoint(aBBP.GDB_ID);
        }
    }
}
