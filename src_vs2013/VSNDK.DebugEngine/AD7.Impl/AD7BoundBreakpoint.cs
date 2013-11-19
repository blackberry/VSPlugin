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
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace VSNDK.DebugEngine
{

    /// <summary> 
    /// This class represents a breakpoint that has been bound to a location in the debuggee. It is a child of the pending 
    /// breakpoint that creates it. Unless the pending breakpoint only has one bound breakpoint, each bound breakpoint is displayed as 
    /// a child of the pending breakpoint in the breakpoints window. Otherwise, only one is displayed. 
    /// (http://msdn.microsoft.com/en-us/library/bb161979.aspx)
    /// </summary>
    public class AD7BoundBreakpoint : IDebugBoundBreakpoint2
    {
        private AD7PendingBreakpoint m_pendingBreakpoint;
        private AD7BreakpointResolution m_breakpointResolution;
        private AD7Engine m_engine;        

        private bool m_enabled;
        private bool m_deleted;
        public uint m_hitCount;

        public uint m_bpLocationType;
        public string m_filename = "";
        public string m_fullPath = "";
        public uint m_line = 0;
        public string m_func = "";

        /// <summary> 
        /// This breakpoint's index in the list of active bound breakpoints. 
        /// </summary>
        protected int m_remoteID = -1;
        public int RemoteID
        {
            get { return m_remoteID; }
        }

        public BP_PASSCOUNT m_bpPassCount;
        public BP_CONDITION m_bpCondition;

        /// <summary>
        /// TRUE if the program has to stop when the hit count is equal to a given value.
        /// </summary>
        public bool m_isHitCountEqual = false; 

        /// <summary>
        /// Different than 0 if the program has to stop whenever the hit count is multiple of a given value
        /// </summary>
        public uint m_hitCountMultiple = 0; 
        public bool m_breakWhenCondChanged = false;
        public string m_previousCondEvaluation = "";
        
        /// <summary>
        /// Indicates if a given breakpoint is being manipulated in one of these 2 methods: SetPassCount and BreakpointHit.
        /// </summary>
        public bool m_blockedPassCount = false; 

        /// <summary>
        /// Indicates if a given breakpoint is being manipulated in one of these 2 methods: SetCondition and BreakpointHit.
        /// </summary>
        public bool m_blockedConditional = false; 

        /// <summary> 
        /// GDB member variables. 
        /// </summary>
        protected uint m_GDB_ID = 0;        
        protected string m_GDB_filename = "";
        protected uint m_GDB_linePos = 0;
        protected string m_GDB_Address = "";


        /// <summary> 
        /// GDB_ID Property. 
        /// </summary>
        public uint GDB_ID
        {
            get { return m_GDB_ID; }
            set { m_GDB_ID = value; }
        }


        /// <summary> 
        /// GDB_FileName Property. 
        /// </summary>
        public string GDB_FileName
        {
            get { return m_GDB_filename; }
            set { m_GDB_filename = value; }
        }

        
        /// <summary> 
        /// GDB_LinePos Property. 
        /// </summary>
        public uint GDB_LinePos
        {
            get { return m_GDB_linePos; }
            set { m_GDB_linePos = value; }
        }

        
        /// <summary> 
        /// GDB_Address Property. 
        /// </summary>
        public string GDB_Address
        {
            get { return m_GDB_Address; }
            set { m_GDB_Address = value; }
        }


        /// <summary> 
        /// GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the short path name for a given long one. 
        /// </summary>
        /// <param name="path">Long path name. </param>
        /// <param name="shortPath">Returns this short path name. </param>
        /// <param name="shortPathLength"> Lenght of this short path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength
                 );


        /// <summary> 
        /// AD7BoundBreakpoint constructor for file/line breaks. 
        /// </summary>
        /// <param name="engine"> AD7 Engine. </param>
        /// <param name="bpReqInfo"> Contains the information required to implement a breakpoint. </param>
        /// <param name="pendingBreakpoint"> Associated pending breakpoint. </param>
        public AD7BoundBreakpoint(AD7Engine engine, BP_REQUEST_INFO bpReqInfo, AD7PendingBreakpoint pendingBreakpoint)
        {
            if (bpReqInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                string documentName;

                // Get Decument Position and File Name
                IDebugDocumentPosition2 docPosition = (IDebugDocumentPosition2)(Marshal.GetObjectForIUnknown(bpReqInfo.bpLocation.unionmember2));
                docPosition.GetFileName(out documentName);

                // Need to shorten the path we send to GDB.
                StringBuilder shortPath = new StringBuilder(1024);
                GetShortPathName(documentName, shortPath, shortPath.Capacity);

                // Get the location in the document that the breakpoint is in.
                TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
                TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
                docPosition.GetRange(startPosition, endPosition);

                m_engine = engine;
                m_bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE;
                m_filename = shortPath.ToString();
                m_line = startPosition[0].dwLine + 1;

                m_pendingBreakpoint = pendingBreakpoint;
                m_enabled = true;
                m_deleted = false;
                m_hitCount = 0;
                m_remoteID = m_engine.BPMgr.RemoteAdd(this);
            }
            else if (bpReqInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
            {
                string func;

                IDebugFunctionPosition2 funcPosition = (IDebugFunctionPosition2)(Marshal.GetObjectForIUnknown(bpReqInfo.bpLocation.unionmember2));
                funcPosition.GetFunctionName(out func);

                m_engine = engine;
                m_func = func;
                m_enabled = true;
                m_deleted = false;
                m_hitCount = 0;
                m_bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET;
                m_pendingBreakpoint = pendingBreakpoint;
                m_remoteID = m_engine.BPMgr.RemoteAdd(this);
            }

//            if ((m_remoteID == 0) && (VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning == false))
            if (m_remoteID == 0)
            {
                return;
            }

            // Set the hit count and condition
            if (bpReqInfo.bpPassCount.stylePassCount != enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE)
                SetPassCount(bpReqInfo.bpPassCount);
            if (bpReqInfo.bpCondition.styleCondition != enum_BP_COND_STYLE.BP_COND_NONE)
                SetCondition(bpReqInfo.bpCondition);

            // Get the Line Position sent back from GDB
            TEXT_POSITION tpos = new TEXT_POSITION();
            tpos.dwLine = m_GDB_linePos - 1;

            uint xAddress = UInt32.Parse(m_GDB_Address.Substring(2), System.Globalization.NumberStyles.HexNumber);

            AD7MemoryAddress codeContext = new AD7MemoryAddress(m_engine, xAddress);
            AD7DocumentContext documentContext = new AD7DocumentContext(m_GDB_filename, tpos, tpos, codeContext);

            m_breakpointResolution = new AD7BreakpointResolution(m_engine, xAddress, documentContext);

            m_engine.Callback.OnBreakpointBound(this, 0);
        }


        /// <summary>
        ///  Sets the count and conditions upon which a breakpoint is fired. 
        ///  (http://msdn.microsoft.com/en-us/library/bb161364.aspx)
        /// </summary>
        /// <param name="bpPassCount"> Describes the count and conditions upon which a conditional breakpoint is fired. </param>
        /// <returns> VSConstants.S_OK if successful, VSConstants.S_FALSE if not. </returns>
        public int SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            bool isRunning = false;
            int result = VSConstants.S_FALSE;
            while (!m_engine.eDispatcher.lockedBreakpoint(this, true, false))
            {
                Thread.Sleep(0);
            }
            while (!m_engine.eDispatcher.enterCriticalRegion())
            {
                Thread.Sleep(0);
            }
            if ((m_engine.m_state == AD7Engine.DE_STATE.RUN_MODE) && (EventDispatcher.m_GDBRunMode == true))
            {
                isRunning = true;
                m_engine.eDispatcher.prepareToModifyBreakpoint();
            }
            m_bpPassCount = bpPassCount;
            if (bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER)
            {
                m_isHitCountEqual = false;
                m_hitCountMultiple = 0;
                if (!m_breakWhenCondChanged)
                {
                    if ((int)((bpPassCount.dwPassCount - m_hitCount)) >= 0)
                    {
                        if (m_engine.eDispatcher.ignoreHitCount(GDB_ID, (int)(bpPassCount.dwPassCount - m_hitCount)))
                            result = VSConstants.S_OK;
                    }
                    else
                    {
                        if (m_engine.eDispatcher.ignoreHitCount(GDB_ID, 1))
                            result = VSConstants.S_OK;
                    }

                }
                else
                    result = VSConstants.S_OK;
            }
            else if (bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL)
            {
                m_hitCountMultiple = 0;
                m_isHitCountEqual = true;
                if (!m_breakWhenCondChanged)
                {
                    if (m_engine.eDispatcher.ignoreHitCount(GDB_ID, (int)(bpPassCount.dwPassCount - m_hitCount)))
                        result = VSConstants.S_OK;
                }
                else
                    result = VSConstants.S_OK;
            }
            else if (bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD)
            {
                m_isHitCountEqual = false;
                m_hitCountMultiple = bpPassCount.dwPassCount;
                if (!m_breakWhenCondChanged)
                {
                    if (m_engine.eDispatcher.ignoreHitCount(GDB_ID, (int)(m_hitCountMultiple - (m_hitCount % m_hitCountMultiple))))
                        result = VSConstants.S_OK;
                }
                else
                    result = VSConstants.S_OK;
            }
            else if (bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE)
            {
                m_isHitCountEqual = false;
                m_hitCountMultiple = 0;
                if (!m_breakWhenCondChanged)
                {
                    if (m_engine.eDispatcher.ignoreHitCount(GDB_ID, 1)) // ignoreHitCount decrement by 1 automatically, so sending 1 means to stop ignoring (or ignore 0)
                        result = VSConstants.S_OK;
                }
                else
                    result = VSConstants.S_OK;
            }

            if (isRunning)
            {
                isRunning = false;
                m_engine.eDispatcher.resumeFromInterrupt();
            }

            m_engine.eDispatcher.leaveCriticalRegion();
            m_engine.eDispatcher.unlockBreakpoint(this, true, false);
            return result;
        }


        /// <summary>
        /// Sets the conditions under which a conditional breakpoint fires. (http://msdn.microsoft.com/en-us/library/bb146215.aspx)
        /// </summary>
        /// <param name="bpCondition"> Describes the conditions under which a breakpoint fires. </param>
        /// <returns> VSConstants.S_OK if successful, VSConstants.S_FALSE if not. </returns>
        public int SetCondition(BP_CONDITION bpCondition)
        {
            bool updatingCondBreak = this.m_engine.m_updatingConditionalBreakpoint.WaitOne(0);
            bool isRunning = false;
            bool verifyCondition = false;
            int result = VSConstants.S_FALSE;
            while (!m_engine.eDispatcher.lockedBreakpoint(this, false, true))
            {
                Thread.Sleep(0);
            }

            if (m_hitCount != 0)
            {
                m_engine.eDispatcher.resetHitCount(this, false);
            }

            while (!m_engine.eDispatcher.enterCriticalRegion())
            {
                Thread.Sleep(0);
            }

            if ((m_engine.m_state == AD7Engine.DE_STATE.RUN_MODE) && (EventDispatcher.m_GDBRunMode == true))
            {
                isRunning = true;
                m_engine.eDispatcher.prepareToModifyBreakpoint();
                m_engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;
            }

            m_bpCondition = bpCondition;

            if (bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_WHEN_TRUE)
            {
                if (m_breakWhenCondChanged)
                {
                    m_breakWhenCondChanged = false;
                    verifyCondition = true;
                }
                else
                    m_breakWhenCondChanged = false;

                m_previousCondEvaluation = "";
                if (m_engine.eDispatcher.setBreakpointCondition(GDB_ID, bpCondition.bstrCondition))
                    result = VSConstants.S_OK;
            }
            else if (bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_WHEN_CHANGED)
            {
                m_breakWhenCondChanged = true;
                m_previousCondEvaluation = bpCondition.bstrCondition; // just to initialize this variable
                m_engine.eDispatcher.ignoreHitCount(GDB_ID, 1); // have to break always to evaluate this option because GDB doesn't support it.
                m_engine.eDispatcher.setBreakpointCondition(GDB_ID, "");

                result = VSConstants.S_OK;
            }
            else if (bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_NONE)
            {
                if (m_breakWhenCondChanged)
                {
                    m_breakWhenCondChanged = false;
                    verifyCondition = true;
                }
                else
                    m_breakWhenCondChanged = false;

                m_previousCondEvaluation = "";
                if (m_engine.eDispatcher.setBreakpointCondition(GDB_ID, ""))
                    result = VSConstants.S_OK;
            }

            m_engine.eDispatcher.leaveCriticalRegion();
            m_engine.eDispatcher.unlockBreakpoint(this, false, true);

            if (verifyCondition)
            {
                SetPassCount(m_bpPassCount);
                verifyCondition = false;
            }

            if (isRunning)
            {
                isRunning = false;
                m_engine.m_state = AD7Engine.DE_STATE.RUN_MODE;
                m_engine.eDispatcher.resumeFromInterrupt();
            }

            this.m_engine.m_updatingConditionalBreakpoint.Set();

            return result;
        }

        #region IDebugBoundBreakpoint2 Members


        /// <summary>
        /// Called when the breakpoint is being deleted by the user. (http://msdn.microsoft.com/en-us/library/bb146595.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugBoundBreakpoint2.Delete()
        {
            if (!m_deleted)
            {
                m_enabled = false;
                m_deleted = true;
                m_pendingBreakpoint.OnBoundBreakpointDeleted(this);                
                m_engine.BPMgr.RemoteDelete(this);
                m_remoteID = -1;
            }
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Called by the debugger UI when the user is enabling or disabling a breakpoint. 
        /// (http://msdn.microsoft.com/en-us/library/bb145150.aspx)
        /// </summary>
        /// <param name="fEnable"> Equal to 0 if disabling; different than 0 if enabling. </param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugBoundBreakpoint2.Enable(int fEnable)
        {
            bool xEnabled = fEnable != 0;
            if (m_enabled != xEnabled)
            {
                if (xEnabled)
                {
                    m_engine.BPMgr.RemoteEnable(this);
                }
                else
                {
                    m_engine.BPMgr.RemoteDisable(this);
                }
                m_enabled = xEnabled;
            }
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Return the breakpoint resolution which describes how the breakpoint bound in the debuggee. 
        /// (http://msdn.microsoft.com/en-us/library/bb145891.aspx)
        /// </summary>
        /// <param name="ppBPResolution"> Contains the information that describes a bound breakpoint. </param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution)
        {
            ppBPResolution = m_breakpointResolution;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Return the pending breakpoint for this bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb145337.aspx)
        /// </summary>
        /// <param name="ppPendingBreakpoint"> Contains a breakpoint that is ready to bind to a code location.</param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = m_pendingBreakpoint;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the state of this bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb161276.aspx)
        /// </summary>
        /// <param name="pState"> Describes the state of the breakpoint. </param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
        {
            pState[0] = 0;

            if (m_deleted)
            {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            }
            else if (m_enabled)
            {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            }
            else if (!m_enabled)
            {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the current hit count for this bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb145340.aspx)
        /// </summary>
        /// <param name="pdwHitCount">Returns the hit count. </param>
        /// <returns> AD7_HRESULT.E_BP_DELETED if the breakpoint was deleted; or VSConstants.S_OK if not. </returns>
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            if (m_deleted)
            {
                pdwHitCount = 0;
                return AD7_HRESULT.E_BP_DELETED;
            }
            else
            {
                pdwHitCount = m_hitCount;
                return VSConstants.S_OK;
            }
        }


        /// <summary>
        /// Sets or changes the condition associated with this bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb146215.aspx)
        /// </summary>
        /// <param name="bpCondition"> Describes the condition. </param>
        /// <returns> VSConstants.S_OK if successful, VSConstants.S_FALSE if not. </returns>
        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            return SetCondition(bpCondition);
        }


        /// <summary>
        /// Sets the hit count for the bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb146736.aspx)
        /// </summary>
        /// <param name="dwHitCount"> The hit count to set. </param>
        /// <returns> AD7_HRESULT.E_BP_DELETED if the breakpoint was deleted; or VSConstants.S_OK if not. </returns>
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            if (m_deleted)
            {                
                return AD7_HRESULT.E_BP_DELETED;
            }
            else
            {
                if ((dwHitCount == 0) && (m_hitCount != 0))
                {
                    m_hitCount = dwHitCount;
                    m_engine.eDispatcher.resetHitCount(this, true);
                }
                else
                    m_hitCount = dwHitCount;
                return VSConstants.S_OK;
            }
        }


        /// <summary>
        /// Sets or changes the pass count associated with this bound breakpoint. (http://msdn.microsoft.com/en-us/library/bb161364.aspx)
        /// </summary>
        /// <param name="bpPassCount"> Specifies the pass count. </param>
        /// <returns> VSConstants.S_OK if successful, VSConstants.S_FALSE if not. </returns>
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            return SetPassCount(bpPassCount);
        }

        #endregion
    }
}
