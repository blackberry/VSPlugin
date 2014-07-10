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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class represents a pending breakpoint that is ready to bind to a code location. A pending breakpoint is an abstract
    /// representation of a breakpoint before it is bound. When a user creates a new breakpoint, the pending breakpoint is created
    /// and is later bound. The bound breakpoints become children of the pending breakpoint.
    /// 
    /// It implements IDebugPendingBreakpoint2: (http://msdn.microsoft.com/en-ca/library/bb161807.aspx)
    /// </summary>
    public sealed class AD7PendingBreakpoint : IDebugPendingBreakpoint2
    {
        private readonly AD7Engine _engine;
        /// <summary>
        /// The breakpoint request that resulted in this pending breakpoint being created.
        /// </summary>
        private readonly IDebugBreakpointRequest2 _breakpointRequest;
        private BP_REQUEST_INFO _breakpointRequestInfo;

        private readonly List<IDebugBoundBreakpoint2> _boundBreakpoints;
        private bool _enabled;
        private bool _deleted;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="breakpointRequest"> The breakpoint request used to create this pending breakpoint. </param>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public AD7PendingBreakpoint(AD7Engine engine, IDebugBreakpointRequest2 breakpointRequest)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");
            if (breakpointRequest == null)
                throw new ArgumentNullException("breakpointRequest");

            BP_REQUEST_INFO[] requestInfo = new BP_REQUEST_INFO[1];
            _breakpointRequest = breakpointRequest;
            _breakpointRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION, requestInfo); // PH: FIXME: check return code here and in following calls...
            _breakpointRequestInfo = requestInfo[0];
            _breakpointRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_CONDITION, requestInfo);
            if (requestInfo[0].dwFields != 0)
            {
                _breakpointRequestInfo.bpCondition = requestInfo[0].bpCondition;
                _breakpointRequestInfo.dwFields |= requestInfo[0].dwFields; 
            }
            _breakpointRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_PASSCOUNT, requestInfo);
            if (requestInfo[0].dwFields != 0)
            {
                _breakpointRequestInfo.bpPassCount = requestInfo[0].bpPassCount;
                _breakpointRequestInfo.dwFields |= requestInfo[0].dwFields;
            }

            _engine = engine;
            _boundBreakpoints = new List<IDebugBoundBreakpoint2>();

            _enabled = true;
            _deleted = false;
        }

        /// <summary>
        /// Determines whether this pending breakpoint can bind to a code location.
        /// </summary>
        /// <returns> If successful, returns TRUE; otherwise, returns FALSE. </returns>
        private bool CanBind()
        {
            if (_deleted || !_engine.HasProcess)
                return false;

            // The engine only supports these types of breakpoints:
            // - file and line number
            // - function name and offset
            if (_breakpointRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE
                || _breakpointRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
                return true;

            return false;
        }

        /// <summary>
        /// Remove all of the bound breakpoints for this pending breakpoint
        /// </summary>
        public void ClearBoundBreakpoints()
        {
            lock (_boundBreakpoints)
            {
                for (int i = _boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    _boundBreakpoints[i].Delete();
                }
            }
        }

        /// <summary>
        /// Called by bound breakpoints when they are being deleted.
        /// </summary>
        /// <param name="boundBreakpoint"> A bound breakpoint. </param>
        public void OnBoundBreakpointDeleted(AD7BoundBreakpoint boundBreakpoint)
        {
            lock (_boundBreakpoints)
            {
                _boundBreakpoints.Remove(boundBreakpoint);
            }
        }

        #region IDebugPendingBreakpoint2 Members

        /// <summary>
        /// Bind this breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145901.aspx)
        /// </summary>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugPendingBreakpoint2.Bind()
        {
            if (DebugEngineStatus.IsRunning == false)
            {
                return VSConstants.S_FALSE;
            }

            try
            {
                if (CanBind())
                {
                    // Visual Studio returns a start position that is one less than it actually is
                    var boundBreakpoint = new AD7BoundBreakpoint(_engine, _breakpointRequestInfo, this);

                    if (boundBreakpoint.GDB_ID == 0)
                    {
                        return VSConstants.S_FALSE;
                    }

                    // Set the enabled state of the bound breakpoint based on the pending breakpoint's enabled state
                    ((IDebugBoundBreakpoint2)boundBreakpoint).Enable(_enabled ? 1 : 0);
                    _boundBreakpoints.Add(boundBreakpoint);

                    return VSConstants.S_OK;
                }

                // PH: FIXME:
                // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid
                // expression, etc... The VSNDK debug engine does not support this, but a real world engine will want to send an
                // instance of IDebugBreakpointErrorEvent2 to the UI and return a valid instance of IDebugErrorBreakpoint2 from
                // IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then display information about why the
                // breakpoint did not bind to the user.
                return VSConstants.S_FALSE;
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
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
                // PH: FIXME:
                // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid 
                // expression, etc... The VSNDK debug engine does not support this, but a real world engine will want to send an 
                // instance of IDebugBreakpointErrorEvent2 to the UI and return a valid instance of IDebugErrorBreakpoint2 from 
                // IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then display information about why the 
                // breakpoint did not bind to the user.
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
            lock (_boundBreakpoints)
            {
                for (int i = _boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    _boundBreakpoints[i].Delete();
                }
            }

            _enabled = false;
            _deleted = true;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Toggles the enabled state of this pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb145046.aspx)
        /// </summary>
        /// <param name="fEnable"> Set to nonzero (TRUE) to enable a pending breakpoint, or to zero (FALSE) to disable. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.Enable(int fEnable)
        {
            lock (_boundBreakpoints)
            {
                _enabled = fEnable != 0;

                foreach (var breakpoint in _boundBreakpoints)
                {                    
                    breakpoint.Enable(fEnable);
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
            lock (_boundBreakpoints)
            {
                IDebugBoundBreakpoint2[] boundBreakpoints = _boundBreakpoints.ToArray();
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
            // PH: FIXME:
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
            ppBPRequest = _breakpointRequest;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the state of this pending breakpoint. (http://msdn.microsoft.com/en-ca/library/bb162178.aspx)
        /// </summary>
        /// <param name="pState"> A PENDING_BP_STATE_INFO structure that is filled in with a description of this pending breakpoint. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (_deleted)
            {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DELETED;
                return VSConstants.S_OK;
            }

            if (!_enabled)
            {
                pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_DISABLED;
                return VSConstants.S_OK;
            }

            pState[0].state = (enum_PENDING_BP_STATE)enum_BP_STATE.BPS_ENABLED;
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
            return VSConstants.E_NOTIMPL;
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
            return VSConstants.E_NOTIMPL;
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
