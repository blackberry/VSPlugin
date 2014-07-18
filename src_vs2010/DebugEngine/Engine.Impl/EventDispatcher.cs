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
using System.Diagnostics;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Debugger.Model;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Threading;

namespace BlackBerry.DebugEngine
{    
    /// <summary>
    /// This class manages debug events for the debug engine.
    /// 
    /// Process GDB's output by classifying it by type (e.g. breakpoint event) and providing relevant data.
    /// Send GDB commands as appropriate for the received event (e.g. call -exec-continue to resume execution after a breakpoint is 
    /// inserted).
    /// Call debug engine methods to notify the SDM of an event (e.g. if a breakpoint is hit, call EngineCallback.OnBreakpoint()).
    /// </summary>
    public sealed class EventDispatcher
    {
        /// <summary>
        /// Represents the object that process asynchronous GDB's output by classifying it by type (e.g. breakpoint event).
        /// </summary>
        private readonly GDBOutput _gdbOutput;

        /// <summary>
        /// Boolean variable that indicates if the current code is known or unknown, i.e., if there is a source code file associated.
        /// </summary>
        public static bool _unknownCode;

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "lockedBreakpoint" method.
        /// </summary>
        private readonly object _lockBreakpoint = new object();

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "unlockBreakpoint" method.
        /// </summary>
        private readonly object _unlockBreakpoint = new object();

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "enterCriticalRegion" method.
        /// </summary>
        private readonly object _criticalRegion = new object(); 

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "leaveCriticalRegion" method.
        /// </summary>
        private readonly object _leaveCriticalRegion = new object();
        
        /// <summary>
        /// Boolean variable that indicates the GDB state: TRUE -> run mode; FALSE -> break mode.
        /// </summary>
        public static bool _GDBRunMode = true;

        /// <summary>
        /// Variable that is manipulated only in methods enterCriticalRegion and leaveCriticalRegion        
        /// </summary>
        public bool _inCriticalRegion;

        /// <summary>
        /// There is a GDB bug that causes a message "Quit (expect signal SIGINT when the program is resumed)". If this message occurs
        /// 5 times, VSNDK will end the debug session. That's why this variable is needed, to count the amount of this kind of message
        /// that is received in a sequence.
        /// </summary>
        public int countSIGINT;

        #region Properties

        /// <summary>
        /// The public AD7Engine object that represents the DE.
        /// </summary>
        public AD7Engine Engine
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Ends the debug session by closing GDB, sending the appropriate events to the SDM, and breaking out of all 
        /// buffer- and event-listening loops.
        /// </summary>
        /// <param name="exitCode">The exit code. </param>
        public void EndDebugSession(uint exitCode)
        {
            // Exit the event dispatch loop.
            _gdbOutput.IsRunning = false;

            // Send events to the SDM.
            AD7ThreadDestroyEvent.Send(Engine, exitCode, null);
            AD7ProgramDestroyEvent.Send(Engine, exitCode);

            // Exit GDB.
            GdbWrapper.Exit();

            // Notify the AddIn that this debug session has ended.
            DebugEngineStatus.IsRunning = false;
        }

        /// <summary>
        /// Constructor. Starts the thread responsible for handling asynchronous GDB output.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public EventDispatcher(AD7Engine engine)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            Engine = engine;

            _gdbOutput = new GDBOutput(this);
        }

        /// <summary>
        /// Process asynchronous GDB's output by classifying it by type (e.g. breakpoint event).
        /// </summary>
        public sealed class GDBOutput
        {
            /// <summary>
            /// This object manages debug events in the engine.
            /// </summary>
            private readonly EventDispatcher _eventDispatcher;

            /// <summary>
            /// This object manages breakpoints events.
            /// </summary>
            private HandleBreakpoints _hBreakpoints;

            /// <summary>
            /// This object manages events related to execution control (processes, threads, programs). 
            /// </summary>
            private HandleProcessExecution _hProcExe;

            /// <summary>
            /// This object manages events related to output messages.
            /// </summary>
            private HandleOutputs _hOutputs;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="ed"> This object manages debug events in the engine. </param>
            public GDBOutput(EventDispatcher ed)
            {
                _eventDispatcher = ed;
                IsRunning = true;
                GdbWrapper.Received += GdbOnReceivedResponse;
            }

            private void GdbOnReceivedResponse(object sender, ResponseReceivedEventArgs e)
            {
                Debug.Assert(e != null && e.Response != null && e.Response.RawData != null, "Invalid response object received");
                ProcessingGDBOutput(e.Response.RawData);
            }

            #region Properties

            /// <summary>
            /// Corresponds to the event-dispatcher status. When false, exit the event dispatch loop.
            /// </summary>
            public bool IsRunning
            {
                get;
                set;
            }

            #endregion

            /// <summary>
            /// Thread responsible for handling asynchronous GDB output.
            /// </summary>
            private void ProcessingGDBOutput(string[] response)
            {
                string[] events = response; // PH: FIXME: .Split('@');
                foreach (string ev in events)
                {
                    if (ev.Length > 1) // only to avoid empty events, when there are two delimiters characters together.
                    {
                        if (_eventDispatcher.countSIGINT > 0)
                            if ((ev.Substring(0, 2) != "50") && (ev.Substring(0, 2) != "80"))
                                _eventDispatcher.countSIGINT = 0; // Reset the counter, if GDB has recovered from a GDB bug.
                        switch (ev[0])
                        {
                            case '0': // Events related to starting GDB.
                                break;
                            case '1': // Not used.
                                break;
                            case '2': // Events related to breakpoints (including breakpoint hits).
                                _hBreakpoints = new HandleBreakpoints(_eventDispatcher);
                                _hBreakpoints.Handle(ev);
                                break;
                            case '3': // Not used.
                                break;
                            case '4': // Events related to execution control (processes, threads, programs) 1.
                                _hProcExe = new HandleProcessExecution(_eventDispatcher);
                                _hProcExe.Handle(ev);
                                break;
                            case '5': // Events related to execution control (processes, threads, programs and GDB Bugs) 2.
                                _hProcExe = new HandleProcessExecution(_eventDispatcher);
                                _hProcExe.Handle(ev);
                                break;
                            case '6': // Events related to evaluating expressions. Not used.
                                break;
                            case '7': // Events related to stack frames. Not used.
                                break;
                            case '8': // Events related to output.
                                _hOutputs = new HandleOutputs(_eventDispatcher);
                                _hOutputs.Handle(ev);
                                break;
                            case '9': // Not used.
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Interrupt the debugged process if necessary before changing a breakpoint.
        /// </summary>
        public void PrepareToModifyBreakpoint()
        {
            if (Engine.m_state != AD7Engine.DE_STATE.DESIGN_MODE 
             && Engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
            {
                HandleProcessExecution.NeedsResumeAfterInterrupt = true;
                Engine.CauseBreak();
            }
        }

        /// <summary>
        /// If the process was running when the breakpoint was changed, resume the process.
        /// </summary>
        public void ResumeFromInterrupt()
        {
            if (HandleProcessExecution.NeedsResumeAfterInterrupt)
            {
                HandleProcessExecution.NeedsResumeAfterInterrupt = false;
                ContinueExecution();
            }
        }

        /// <summary>
        /// Code to set the breakpoint in GDB and then confirm and set in Visual Studio
        /// </summary>
        /// <param name="command"> Initial command to set the breakpoint in GDB, with the entire path when setting
        /// a breakpoint in a given line number. </param>
        /// <param name="command2"> Initial command to set the breakpoint in GDB, with only the file name when setting
        /// a breakpoint in a given line number, or "" when setting a breakpoint in a function. </param>
        /// <param name="GDB_ID"> Returns the breakpoint ID in GDB. </param>
        /// <param name="GDB_line"> Returns the breakpoint Line Number. </param>
        /// <param name="GDB_filename"> Returns the breakpoint File Name. </param>
        /// <param name="GDB_address"> Returns the Breakpoint Address. </param>
        /// <returns> If successful, returns true; otherwise, returns false. </returns>
        private bool SetBreakpointImpl(string command, string command2, out BreakpointInfo breakpointInfo)
        {
            string response;
            string bpointAddress;
            string bpointStopPoint;

            if (DebugEngineStatus.IsRunning)
            {
                PrepareToModifyBreakpoint();

                // Gets the parsed response for the GDB/MI command that inserts breakpoint in a given line or a given function.
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
                response = GdbWrapper.SendCommand(command, 6);

                if ((command2 != "") && ((response.Contains("<PENDING>"))))
                {
                    response = GdbWrapper.SendCommand(command2, 6);
                }

                if (((response.Length < 2) && (DebugEngineStatus.IsRunning == false)) || (response == "Function not found!"))
                {
                    breakpointInfo = null;
                    ResumeFromInterrupt();
                    return false;
                }

                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.Handle(response);

                if (hBreakpoints.Address != "<PENDING>" && hBreakpoints.Address != "")
                {
                    //** Run Code to verify breakpoint stop point.

                    // Gets the parsed response for the GDB command that print information about the specified breakpoint, in this 
                    // case, only its address. (http://sourceware.org/gdb/onlinedocs/gdb/Set-Breaks.html)
                    bpointAddress = GdbWrapper.SendCommand("info b " + hBreakpoints.Number, 18);

                    // Gets the parsed response for the GDB command that inquire what source line covers a particular address.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/Machine-Code.html)
                    bpointStopPoint = GdbWrapper.SendCommand("info line *" + bpointAddress, 18);

                    var line = (uint)Convert.ToInt64(bpointStopPoint.Trim());
                    uint address = 0;

                    if (hBreakpoints.Address != null && hBreakpoints.Address.Length > 2)
                    {
                        uint.TryParse(hBreakpoints.Address.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out address);
                    }
                    breakpointInfo = new BreakpointInfo((uint)hBreakpoints.Number, hBreakpoints.FileName, line, address);
                }
                else
                {
                    var line = (uint)hBreakpoints.LinePosition;
                    breakpointInfo = new BreakpointInfo((uint)hBreakpoints.Number, hBreakpoints.FileName, line, 0);
                }

                ResumeFromInterrupt();
                return true;
            }

            breakpointInfo = null;
            return false;
        }

        /// <summary>
        /// Set a breakpoint given a filename and line number.
        /// </summary>
        /// <param name="filename">Full path and filename for the code source.</param>
        /// <param name="line">The line number for the breakpoint.</param>
        /// <param name="breakpoint">Info of GDB breakpoint.</param>
        /// <returns> If successful, returns true; otherwise, returns false. </returns>
        public bool SetBreakpoint(string filename, uint line, out BreakpointInfo breakpoint)
        {
            string cmd = @"-break-insert --thread-group i1 -f " + filename + ":" + line;
            int i = filename.LastIndexOf('\\');
            if ((i != -1) && (i + 1 < filename.Length))
                filename = filename.Substring(i + 1);
            string cmd2 = @"-break-insert --thread-group i1 -f " + filename + ":" + line;
            return SetBreakpointImpl(cmd, cmd2, out breakpoint);
        }

        /// <summary>
        /// Set a breakpoint given a function name.
        /// </summary>
        /// <param name="functionName"> Function name. </param>
        /// <param name="breakpoint">Info of GDB breakpoint.</param>
        /// <returns> If successful, returns true; otherwise, returns false. </returns>
        public bool SetBreakpoint(string functionName, out BreakpointInfo breakpoint)
        {
            string cmd = @"-break-insert " + functionName;
            return SetBreakpointImpl(cmd, "", out breakpoint);
        }

        /// <summary>
        /// Ignore a given number of hit counts in GDB.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <param name="ignore"> Number of hit counts to ignore. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool IgnoreHitCount(uint GDB_ID, int ignore)
        {
            ignore -= 1; // The given number is decreased by one, because the VS command means to break when hit count is equal or greater
                         // than X hits. So, the equivalent GDB command is to break after (X - 1) hit counts.

            if (ignore < 0)
            {
                // Had to ignore the biggest number of times to keep the breakpoint enabled and to avoid stopping on it.
                ignore = int.MaxValue; 
            }

            // Gets the parsed response for the GDB/MI command that makes the breakpoint "GDB_ID" ignore "ignore" hit counts.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
            string response = GdbWrapper.SendCommand(@"-break-after " + GDB_ID + " " + ignore, 18);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.Handle(response);

            return true;
        }

        /// <summary>
        /// Reset current hit count in GDB. There is no way to reset hit counts in GDB. To implement this functionality, the specified GDB 
        /// breakpoint is deleted and a new one is created with the same conditions, substituting the GDB_ID of the VS breakpoint.
        /// </summary>
        /// <param name="bbp"> The VS breakpoint. </param>
        /// <param name="resetCondition"> Is false when this method is called by SetCondition, true if not. Used to avoid setting
        /// breakpoint conditions again in case it is called by SetCondition method. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool ResetHitCount(AD7BoundBreakpoint bbp, bool resetCondition)
        {
            // Declare local Variables
            BreakpointInfo info = null;

            // Deleting GDB breakpoint.
            DeleteBreakpoint(bbp.GdbInfo.ID);

            // Creating a new GDB breakpoint.
            bool ret = false;
            if (bbp.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                ret = SetBreakpoint(bbp.m_filename, bbp.m_line, out info);
            }
            else if (bbp.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
            {
                ret = SetBreakpoint(bbp.m_func, out info);
            }

            if (ret && info != null)
            {
                bbp.GdbInfo = info;
                bbp._hitCount = 0;
                bbp.SetPassCount(bbp.m_bpPassCount);
                if (resetCondition)
                    bbp.SetCondition(bbp.m_bpCondition);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Set breakpoint condition in GDB.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <param name="condition"> Condition to be set. When empty (""), means to remove any previous condition. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool SetBreakpointCondition(uint GDB_ID, string condition)
        {
            // Gets the parsed response for the GDB/MI command that sets a condition to the breakpoint "GDB_ID". If there is no 
            // condition, any previous one associated to this breakpoint will be removed.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
            string response = GdbWrapper.SendCommand(string.Concat(@"-break-condition ", GDB_ID, string.IsNullOrEmpty(condition) ? string.Empty : " ", condition ?? string.Empty), 19);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.Handle(response);
            return true;
        }

        /// <summary>
        /// Delete a breakpoint in GDB.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool DeleteBreakpoint(uint GDB_ID)
        {
            if (_gdbOutput.IsRunning)
            {
                PrepareToModifyBreakpoint();

                // Gets the parsed response for the GDB/MI command that deletes the breakpoint "GDB_ID".
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
                string response = GdbWrapper.SendCommand(@"-break-delete " + GDB_ID, 7);
                if (string.IsNullOrEmpty(response))
                {
                    ResumeFromInterrupt();
                    return false;
                }

                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.Handle(response);
                uint retID = (uint)hBreakpoints.Number;

                ResumeFromInterrupt();

                if (GDB_ID != retID)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Enable or disable a breakpoint.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <param name="enable"> If true, enable the breakpoint. If false, disable it. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool EnableBreakpoint(uint GDB_ID, bool enable)
        {
            PrepareToModifyBreakpoint();

            // Gets the parsed response for the GDB/MI command that enables or disables the breakpoint "GDB_ID".
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
            string response = GdbWrapper.SendCommand(@"-break-" + (enable ? "enable" : "disable") + " " + GDB_ID, 8);
 
            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.Handle(response);
            uint retID = (uint)hBreakpoints.Number;

            ResumeFromInterrupt();
            if (GDB_ID != retID)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update hit count.
        /// </summary>
        /// <param name="ID"> Breakpoint ID in GDB. </param>
        /// <param name="hitCount"> Hit count. </param>
        public void UpdateHitCount(uint ID, uint hitCount)
        {
            var bbp = Engine.BreakpointManager.GetBoundBreakpointForGDBID(ID);
            if (bbp != null)
            {
                if (!bbp._breakWhenCondChanged)
                {
                    ((IDebugBoundBreakpoint2) bbp).SetHitCount(hitCount);
                }
            }
        }

        /// <summary>
        /// Lock a breakpoint before updating its hit counts and/or condition. This is done to avoid a race condition that can happen when
        /// user modifies a breakpoint condition at run time and the same breakpoint is hit. When that happens, only one event will be
        /// handled at a time.
        /// </summary>
        /// <param name="bbp"> Breakpoint to be locked. </param>
        /// <param name="hit"> True if user is adding/modifying count and conditions upon which a breakpoint is fired. It is also true
        /// when event dispatcher is handling a breakpoint hit. </param>
        /// <param name="cond"> True if user is adding/modifying conditions under which a conditional breakpoint fires. It is also true
        /// when event dispatcher is handling a breakpoint hit. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool LockedBreakpoint(AD7BoundBreakpoint bbp, bool hit, bool cond)
        {
            lock (_lockBreakpoint)
            {
                if (hit && cond)
                {
                    if ((!bbp._blockedPassCount) && (!bbp._blockedConditional))
                    {
                        bbp._blockedPassCount = true;
                        bbp._blockedConditional = true;
                        return true;
                    }
                }
                else if (hit)
                {
                    if (!bbp._blockedPassCount)
                    {
                        bbp._blockedPassCount = true;
                        return true;
                    }
                }
                else if (cond)
                {
                    if (!bbp._blockedConditional)
                    {
                        bbp._blockedConditional = true;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Unlock a breakpoint after updating its hit counts and/or condition. This is done to avoid a race condition that can happen when
        /// user modifies a breakpoint condition at run time and the same breakpoint is hit. When that happens, only one event will be
        /// handled at a time.
        /// </summary>
        /// <param name="bbp"> Breakpoint to be locked. </param>
        /// <param name="hit"> True if user is adding/modifying count and conditions upon which a breakpoint is fired. It is also true
        /// when event dispatcher is handling a breakpoint hit. </param>
        /// <param name="cond"> True if user is adding/modifying conditions under which a conditional breakpoint fires. It is also true
        /// when event dispatcher is handling a breakpoint hit. </param>
        public void UnlockBreakpoint(AD7BoundBreakpoint bbp, bool hit, bool cond)
        {
            lock (_unlockBreakpoint)
            {
                if (hit && cond)
                {
                    bbp._blockedPassCount = false;
                    bbp._blockedConditional = false;
                }
                else if (hit)
                {
                    bbp._blockedPassCount = false;
                }
                else if (cond)
                {
                    bbp._blockedConditional = false;
                }
            }
        }

        /// <summary>
        /// Control access to a critical region, that manipulates some of the debug engine variables, like its state. This is done to 
        /// avoid a race condition that can happen when user modifies a breakpoint condition at run time and the same breakpoint is hit. 
        /// When that happens, only one event will be handled at a time.
        /// </summary>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool EnterCriticalRegion()
        {
            lock (_criticalRegion)
            {
                if (!_inCriticalRegion)
                {
                    _inCriticalRegion = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Leave the critical region, that manipulates some of the debug engine variables, like its state. This is done to 
        /// avoid a race condition that can happen when user modifies a breakpoint condition at run time and the same breakpoint is hit. 
        /// When that happens, only one event will be handled at a time.
        /// </summary>
        public void LeaveCriticalRegion()
        {
            lock (_leaveCriticalRegion)
            {
                _inCriticalRegion = false;
            }
        }

        /// <summary>
        /// Update VS when a breakpoint is hit in GDB.
        /// </summary>
        /// <param name="ID"> Breakpoint ID from GDB. </param>
        /// <param name="threadID"> Thread ID. </param>
        public void BreakpointHit(uint ID, string threadID)
        {
            var xBoundBreakpoints = new List<IDebugBoundBreakpoint2>();

            // Search the active bound BPs and find ones that match the ID.
            var bbp = Engine.BreakpointManager.GetBoundBreakpointForGDBID(ID);

            if (bbp != null)
                xBoundBreakpoints.Add(bbp);

            if ((bbp == null) || (xBoundBreakpoints.Count == 0))
            {
                // if no matching breakpoints are found then its one of the following:
                //   - Stepping operation
                //   - Code based break
                //   - Asm stepping
            }
            else
            {
                if (LockedBreakpoint(bbp, true, true))
                {
                    while (!EnterCriticalRegion())
                    {
                        Thread.Sleep(0);
                    }

                    bool breakExecution = true;

                    if (bbp._breakWhenCondChanged)
                    {
                        string result;
                        bool valid = VariableInfo.EvaluateExpression(bbp.m_bpCondition.bstrCondition, out result, null);
                        if ((valid) && (bbp._previousCondEvaluation != result)) // check if condition evaluation has changed
                        {
                            if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL) && (bbp._hitCount != bbp.m_bpPassCount.dwPassCount))
                            {
                                breakExecution = false;
                            }
                            else if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER) && (bbp._hitCount < bbp.m_bpPassCount.dwPassCount))
                            {
                                breakExecution = false;
                            }
                            else if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD) && ((bbp._hitCount % bbp.m_bpPassCount.dwPassCount) != 0))
                            {
                                breakExecution = false;
                            }
                            bbp._previousCondEvaluation = result;
                        }
                        else
                            breakExecution = false;
                    }
                    if (!breakExecution) // must continue the execution
                    {
                        bool hitBreakAll = Engine._running.WaitOne(0);
                        if (hitBreakAll)
                        {
                            Engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                            // Sends the GDB command that resumes the execution of the inferior program. 
                            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                            GdbWrapper.PostCommand(@"-exec-continue --thread-group i1");
                            _GDBRunMode = true;
                            Engine._running.Set();
                        }
                    }
                    else
                    {
                        if (bbp._breakWhenCondChanged)
                            bbp._hitCount += 1;

                        // Transition DE state
                        _GDBRunMode = false;
                        Engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                        // Found a bound breakpoint
                        Engine.Callback.OnBreakpoint(Engine.SelectThread(threadID), xBoundBreakpoints.AsReadOnly());

                        if (bbp._isHitCountEqual)
                        {
                            // Have to ignore the biggest number of times to keep the breakpoint enabled and to avoid stopping on it.
                            IgnoreHitCount(ID, int.MaxValue); 
                        }
                        else if (bbp._hitCountMultiple != 0)
                        {
                            IgnoreHitCount(ID, (int)(bbp._hitCountMultiple - (bbp._hitCount % bbp._hitCountMultiple)));
                        }
                    }
                    LeaveCriticalRegion();
                    UnlockBreakpoint(bbp, true, true);
                }
                else
                {
                    while (!EnterCriticalRegion())
                    {
                        Thread.Sleep(0);
                    }

                    bool hitBreakAll = Engine._running.WaitOne(0);
                    if (hitBreakAll)
                    {
                        Engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                        // Sends the GDB command that resumes the execution of the inferior program. 
                        // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                        GdbWrapper.PostCommand(@"-exec-continue --thread-group i1");
                        _GDBRunMode = true;
                        Engine._running.Set();
                    }

                    LeaveCriticalRegion();
                }
            }
        }

        /// <summary>
        /// Returns the document context needed for showing the location of the current instruction pointer.
        /// </summary>
        /// <param name="filename"> File name. </param>
        /// <param name="line"> Line number. </param>
        /// <returns> Returns the document context needed for showing the location of the current instruction pointer. </returns>
        public AD7DocumentContext GetDocumentContext(string filename, uint line)
        {
            // Get the location in the document that the breakpoint is in.
            TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
            startPosition[0].dwLine = line;
            startPosition[0].dwColumn = 0;
            TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
            endPosition[0].dwLine = line;
            endPosition[0].dwColumn = 0;

            uint address = 0;
            AD7MemoryAddress codeContext = new AD7MemoryAddress(Engine, address);

            return new AD7DocumentContext(filename, startPosition[0], endPosition[0], codeContext);
        }

        /// <summary>
        /// Return the depth of the stack.
        /// </summary>
        /// <param name="threadID"> Thread ID. </param>
        /// <returns> Returns the stack depth. </returns>
        public int GetStackDepth(string threadID)
        {
            // Returns the parsed response for the GDB/MI command that inquires about the depth of the stack. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return Convert.ToInt32(GdbWrapper.SendCommand(@"-stack-info-depth --thread " + threadID + " --frame 0", 9));
        }

        /// <summary>
        /// List the frames currently on the stack.
        /// </summary>
        /// <returns> Returns a string with the frames currently on the stack. </returns>
        public string GetStackFrames()
        {
            // Returns the parsed response for the GDB/MI command that list the frames currently on the stack. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return GdbWrapper.SendCommand(@"-stack-list-frames", 10);
        }

        /// <summary>
        /// List the names of local variables and function arguments for the selected frame.
        /// </summary>
        /// <param name="frameIndex"> Frame number. </param>
        /// <param name="threadID"> Thread ID. </param>
        /// <returns> Returns a string with the names of local variables and function arguments for the selected frame. </returns>
        public string GetVariablesForFrame(uint frameIndex, string threadID)
        {
            // Returns the parsed response for the GDB/MI command that list the names of local variables and function arguments for the 
            // selected frame. (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return GdbWrapper.SendCommand(@"-stack-list-variables --thread " + threadID + " --frame " + frameIndex + " --simple-values", 11);
        }

        /// <summary>
        /// Make "id" the current thread.
        /// </summary>
        /// <param name="id"> Thread ID. </param>
        public void SelectThread(string id)
        {
            // Waits for the parsed response for the GDB/MI command that make "id" the current thread.
            // (http://www.sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Thread-Commands.html)
            GdbWrapper.SendCommand(@"-thread-select " + id, 12);
        }

        /// <summary>
        /// Creates a variable object.
        /// </summary>
        /// <param name="name"> Name of the variable. </param>
        /// <param name="hasVsNdK"> Boolean value that indicates if the variable name has the prefix VsNdK_. </param>
        /// <returns> If successful, returns the variable's number of children; otherwise, returns string "ERROR". </returns>
        public string CreateVar(string name, out bool hasVsNdK)
        {
            hasVsNdK = false;

            // Gets the variable's number of children after sending the following GDB/MI command to create a variable object. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Variable-Objects.html)
            string response = GdbWrapper.SendCommand("-var-create " + name + " \"*\" " + name, 13);
            if (response == "ERROR")
            {
                // The -var-create GDB/MI command can return an error when the variable name starts with some characters, like '_'.
                // So, in case of an error, add the prefix VsNdK_ to the variable name and try to create it again. 
                response = GdbWrapper.SendCommand("-var-create VsNdK_" + name + " \"*\" " + name, 13);
                if (response != "ERROR")
                    hasVsNdK = true;
            }
            return response;
        }

        /// <summary>
        /// Deletes a previously created variable object and all of its children.
        /// </summary>
        /// <param name="name"> Name of the variable. </param>
        /// <param name="hasVsNdK"> Boolean value that indicates if the variable name has the prefix VsNdK_. </param>
        public void DeleteVar(string name, bool hasVsNdK)
        {
            // Waits for the parsed response for the GDB/MI command that deletes a previously created variable object and all of 
            // its children. (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Variable-Objects.html)
            if (!hasVsNdK)
                GdbWrapper.SendCommand("-var-delete " + name, 14);
            else
                GdbWrapper.SendCommand("-var-delete VsNdK_" + name, 14);
        }

        /// <summary>
        /// Return a list of the children of the specified variable object.
        /// </summary>
        /// <param name="name"> Variable name. </param>
        /// <returns> Return a string that contains the list of the children of the specified variable object. </returns>
        public string listChildren(string name)
        {
            // Returns the parsed response for the GDB/MI command that list the children of the specified variable object.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Variable-Objects.html)
            return GdbWrapper.SendCommand("-var-list-children --all-values " + name + " 0 50", 15); 
        }

        /// <summary>
        /// Kill the child process in which your program is running under gdb.
        /// </summary>
        public void KillProcess()
        {
            // Waits for the parsed response for the GDB command that kills the child process in which your program is running. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/Kill-Process.html#Kill-Process)
            GdbWrapper.SendCommand("kill", 16);
        }

        /// <summary>
        /// Called after the debug engine has set the initial breakpoints, or to resume a process that was interrupted.
        /// </summary>
        public void ContinueExecution()
        {
            //** Transition DE state
            bool hitBreakAll = Engine._running.WaitOne(0);
            if (hitBreakAll)
            {
                Engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                // Sends the GDB command that resumes the execution of the inferior program. 
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                GdbWrapper.PostCommand(@"-exec-continue --thread-group i1");
                _GDBRunMode = true;
                Engine._running.Set();
            }
        }
    }
}
