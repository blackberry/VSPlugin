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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Text;

namespace BlackBerry.DebugEngine
{

    /// <summary>
    /// This class represents a peiding breakpoint that is ready to bind to a code location. A pending breakpoint is an abstract 
    /// representation of a breakpoint before it is bound. When a user creates a new breakpoint, the pending breakpoint is created 
    /// and is later bound. The bound breakpoints become children of the pending breakpoint.
    /// 
    /// It implements IDebugPendingBreakpoint2: (http://msdn.microsoft.com/en-ca/library/bb161807.aspx)
    /// </summary>
    public class AD7PendingBreakpoint : IDebugPendingBreakpoint2
    {       
        /// <summary>
        /// The breakpoint request that resulted in this pending breakpoint being created.
        /// </summary>
        private IDebugBreakpointRequest2 m_pBPRequest;
        private BP_REQUEST_INFO m_bpRequestInfo; 
        private AD7Engine m_engine;
        private BreakpointManager m_bpManager;

        private List<AD7BoundBreakpoint> m_boundBreakpoints;
        public List<AD7BoundBreakpoint> boundBPs
        {
            get
            {
                return m_boundBreakpoints;
            }
        }

        private bool m_enabled;
        private bool m_deleted;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pBPRequest"> The breakpoint request used to create this pending breakpoint. </param>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="bpManager"> The breakpoint manager. </param>
        public AD7PendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, AD7Engine engine, BreakpointManager bpManager)
        {
            m_pBPRequest = pBPRequest;
            BP_REQUEST_INFO[] requestInfo = new BP_REQUEST_INFO[1];
            m_pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION, requestInfo);
            m_bpRequestInfo = requestInfo[0];
            m_pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_CONDITION, requestInfo);
            if (requestInfo[0].dwFields != 0)
            {
                m_bpRequestInfo.bpCondition = requestInfo[0].bpCondition;
                m_bpRequestInfo.dwFields |= requestInfo[0].dwFields; 
            }
            m_pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_PASSCOUNT, requestInfo);
            if (requestInfo[0].dwFields != 0)
            {
                m_bpRequestInfo.bpPassCount = requestInfo[0].bpPassCount;
                m_bpRequestInfo.dwFields |= requestInfo[0].dwFields;
            }

            m_engine = engine;
            m_bpManager = bpManager;
            m_boundBreakpoints = new System.Collections.Generic.List<AD7BoundBreakpoint>();
            
            m_enabled = true;
            m_deleted = false;
        }


        /// <summary>
        /// Determines whether this pending breakpoint can bind to a code location.
        /// </summary>
        /// <returns> If successful, returns TRUE; otherwise, returns FALSE. </returns>
        private bool CanBind()
        {
            // The engine only supports these types of breakpoints: 
            // - File and line number.
            // - Function name and offset.
            if (this.m_deleted || m_engine._process == null)
            {
                return false;
            }
            else if (m_bpRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE
                || m_bpRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Remove all of the bound breakpoints for this pending breakpoint
        /// </summary>
        public void ClearBoundBreakpoints()
        {
            lock (m_boundBreakpoints)
            {
                for (int i = m_boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    ((IDebugBoundBreakpoint2)m_boundBreakpoints[i]).Delete();
                }
            }
        }


        /// <summary>
        /// Called by bound breakpoints when they are being deleted.
        /// </summary>
        /// <param name="boundBreakpoint"> A bound breakpoint. </param>
        public void OnBoundBreakpointDeleted(AD7BoundBreakpoint boundBreakpoint)
        {
            lock (m_boundBreakpoints)
            {
                m_boundBreakpoints.Remove(boundBreakpoint);
            }
        }

        #region IDebugPendingBreakpoint2 Members


        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the short path name for a given long one. </summary>
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
        /// Bind this breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145901.aspx)
        /// </summary>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugPendingBreakpoint2.Bind()
        {
            try
            {
                if (CanBind())
                {
                    AD7BoundBreakpoint xBBP = null;

                    // Visual Studio returns a start position that is one less than it actually is
                    xBBP = new AD7BoundBreakpoint(m_engine, m_bpRequestInfo, this);

                    if (DebugEngineStatus.IsRunning == false)
                    {
                        return VSConstants.S_FALSE;
                    }

                    if ((xBBP == null) || (xBBP.GDB_ID == 0))
                    {
                        return VSConstants.S_FALSE;
                    }

                    // Set the enabled state of the bound breakpoint based on the pending breakpoint's enabled state
                    ((IDebugBoundBreakpoint2)xBBP).Enable(Convert.ToInt32(m_enabled));

                    m_boundBreakpoints.Add(xBBP);                    

                    return VSConstants.S_OK;
                }
                else
                {
                    // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid 
                    // expression, etc... The VSNDK debug engine does not support this, but a real world engine will want to send an 
                    // instance of IDebugBreakpointErrorEvent2 to the UI and return a valid instance of IDebugErrorBreakpoint2 from 
                    // IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then display information about why the 
                    // breakpoint did not bind to the user.
                    return VSConstants.S_FALSE;
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Determines whether this pending breakpoint can bind to a code location. (http://msdn.microsoft.com/en-ca/library/bb146753.aspx)
        /// </summary>
        /// <param name="ppErrorEnum"> Returns an IEnumDebugErrorBreakpoints2 object that contains a list of IDebugErrorBreakpoint2 
        /// objects if there could be errors. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns S_FALSE </returns>
        int IDebugPendingBreakpoint2.CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
        {
            ppErrorEnum = null;

            if (!CanBind())
            {
                // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid 
                // expression, etc... The VSNDK debug engine does not support this, but a real world engine will want to send an 
                // instance of IDebugBreakpointErrorEvent2 to the UI and return a valid instance of IDebugErrorBreakpoint2 from 
                // IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then display information about why the 
                // breakpoint did not bind to the user.
                ppErrorEnum = null;
                return VSConstants.S_FALSE;
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Deletes this pending breakpoint and all breakpoints bound from it. (http://msdn.microsoft.com/en-ca/library/bb145918.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.Delete()
        {
            lock (m_boundBreakpoints)
            {
                for (int i = m_boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    ((IDebugBoundBreakpoint2)m_boundBreakpoints[i]).Delete();
                }
            }

            m_enabled = false;
            m_deleted = true;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Toggles the enabled state of this pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145046.aspx)
        /// </summary>
        /// <param name="fEnable"> Set to nonzero (TRUE) to enable a pending breakpoint, or to zero (FALSE) to disable. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.Enable(int fEnable)
        {
            lock (m_boundBreakpoints)
            {
                m_enabled = fEnable == 0 ? false : true;

                foreach (AD7BoundBreakpoint bp in m_boundBreakpoints)
                {                    
                    ((IDebugBoundBreakpoint2)bp).Enable(fEnable);                    
                }
            }
            
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Enumerates all breakpoints bound from this pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145139.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugBoundBreakpoints2 object that enumerates the bound breakpoints. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            lock (m_boundBreakpoints)
            {
                IDebugBoundBreakpoint2[] boundBreakpoints = m_boundBreakpoints.ToArray();
                ppEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            }
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Enumerates all error breakpoints that resulted from this pending breakpoint. 
        /// (http://msdn.microsoft.com/en-ca/library/bb145598.aspx)
        /// </summary>
        /// <param name="bpErrorType"> A combination of values from the enum_BP_ERROR_TYPE enumeration that selects the type of errors 
        /// to enumerate. </param>
        /// <param name="ppEnum"> Returns an IEnumDebugErrorBreakpoints2 object that contains a list of IDebugErrorBreakpoint2 objects. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugPendingBreakpoint2.EnumErrorBreakpoints(enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
        {
            // Called when a pending breakpoint could not be bound. This may occur for many reasons such as an invalid location, an 
            // invalid expression, etc... The VSNDK debug engine does not support this, but a real world engine will want to send an 
            // instance of IDebugBreakpointErrorEvent2 to the UI and return a valid enumeration of IDebugErrorBreakpoint2 from 
            // IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then display information about why the breakpoint 
            // did not bind to the user.
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets the breakpoint request that was used to create this pending breakpoint. 
        /// (http://msdn.microsoft.com/en-ca/library/bb161770.aspx)
        /// </summary>
        /// <param name="ppBPRequest"> Returns an IDebugBreakpointRequest2 object representing the breakpoint request that was used to 
        /// create this pending breakpoint. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
        {
            ppBPRequest = this.m_pBPRequest;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the state of this pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb162178.aspx)
        /// </summary>
        /// <param name="pState"> A PENDING_BP_STATE_INFO structure that is filled in with a description of this pending breakpoint. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (m_deleted)
            {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DELETED;
            }
            else if (m_enabled)
            {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_ENABLED;
            }
            else if (!m_enabled)
            {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Sets or changes the condition associated with the pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb144977.aspx).
        /// Not implemented here, but a conditional breakpoint can be set using IDebugBoundBreakpoint2::SetCondition().
        /// </summary>
        /// <param name="bpCondition"> A BP_CONDITION structure that specifies the condition to set. </param>
        /// <returns> Not implemented. </returns>
        int IDebugPendingBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Sets or changes the pass count associated with the pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145038.aspx).
        /// Not implemented here, but a pass count associated to a breakpoint can be set/changed using 
        /// IDebugBoundBreakpoint2::SetPassCount().
        /// </summary>
        /// <param name="bpPassCount"> A BP_PASSCOUNT structure that contains the pass count. </param>
        /// <returns> Not implemented. </returns>
        int IDebugPendingBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Toggles the virtualized state of this pending breakpoint. When a pending breakpoint is virtualized, the debug engine will 
        /// attempt to bind it every time new code loads into the program. (http://msdn.microsoft.com/en-ca/library/bb146187.aspx)
        /// The VSNDK debug engine does not support this. Not implemented.
        /// </summary>
        /// <param name="fVirtualize"> Set to nonzero (TRUE) to virtualize the pending breakpoint, or to zero (FALSE) to turn off 
        /// virtualization. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.Virtualize(int fVirtualize)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
