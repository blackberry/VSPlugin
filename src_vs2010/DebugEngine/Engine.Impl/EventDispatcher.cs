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
using BlackBerry.Package;
using Microsoft.VisualStudio.Debugger.Interop;
using VSNDK.Parser;
using System.Threading;
using System.Windows.Forms;

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
    public class EventDispatcher
    {
        /// <summary>
        /// The private AD7Engine object that represents the DE.
        /// </summary>
        private AD7Engine m_engine;
        
        /// <summary>
        /// The public AD7Engine object that represents the DE.
        /// </summary>
        public AD7Engine engine
        {
            get { return m_engine; }
        }
        
        /// <summary>
        /// Thread responsible for handling asynchronous output from GDB.
        /// </summary>
        private Thread m_processingThread;

        /// <summary>
        /// Represents the object that process asynchronous GDB's output by classifying it by type (e.g. breakpoint event).
        /// </summary>
        GDBOutput m_gdbOutput;

        /// <summary>
        /// Boolean variable that indicates if the current code is known or unknown, i.e., if there is a source code file associated.
        /// </summary>
        public static bool m_unknownCode = false;

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "lockedBreakpoint" method.
        /// </summary>
        private Object m_lockBreakpoint = new Object();

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "unlockBreakpoint" method.
        /// </summary>
        private Object m_unlockBreakpoint = new Object();

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "enterCriticalRegion" method.
        /// </summary>
        private Object m_criticalRegion = new Object(); 

        /// <summary>
        /// Object used to control the access to the critical section that exists in the "leaveCriticalRegion" method.
        /// </summary>
        private Object m_leaveCriticalRegion = new Object();
        
        /// <summary>
        /// Boolean variable that indicates the GDB state: TRUE -> run mode; FALSE -> break mode.
        /// </summary>
        public static bool m_GDBRunMode = true;

        /// <summary>
        /// Variable that is manipulated only in methods enterCriticalRegion and leaveCriticalRegion        
        /// </summary>
        public bool inCriticalRegion = false; 

        /// <summary>
        /// There is a GDB bug that causes a message "Quit (expect signal SIGINT when the program is resumed)". If this message occurs
        /// 5 times, VSNDK will end the debug session. That's why this variable is needed, to count the amount of this kind of message
        /// that is received in a sequence.
        /// </summary>
        public int countSIGINT = 0;
        

        /// <summary>
        /// Ends the debug session by closing GDB, sending the appropriate events to the SDM, and breaking out of all 
        /// buffer- and event-listening loops.
        /// </summary>
        /// <param name="exitCode">The exit code. </param>
        public void endDebugSession(uint exitCode)
        {
            // Exit the event dispatch loop.
            m_gdbOutput._running = false;

            // Send events to the SDM.
            AD7ThreadDestroyEvent.Send(engine, exitCode, null);
            AD7ProgramDestroyEvent.Send(engine, exitCode);

            // Exit GDB.
            GDBParser.exitGDB();

            // Notify the AddIn that this debug session has ended.
            ControlDebugEngine.isDebugEngineRunning = false;
        }


        /// <summary>
        /// Constructor. Starts the thread responsible for handling asynchronous GDB output.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public EventDispatcher(AD7Engine engine)
        {
            m_engine = engine;

            m_gdbOutput = new GDBOutput(this);
            m_processingThread = new Thread(m_gdbOutput.processingGDBOutput);
            m_processingThread.Start();
        }


        /// <summary>
        /// Process asynchronous GDB's output by classifying it by type (e.g. breakpoint event).
        /// </summary>
        public class GDBOutput
        {
            /// <summary>
            /// This object manages debug events in the engine.
            /// </summary>
            private EventDispatcher m_eventDispatcher;

            /// <summary>
            /// This object manages breakpoints events.
            /// </summary>
            private HandleBreakpoints m_hBreakpoints;

            /// <summary>
            /// This object manages events related to execution control (processes, threads, programs). 
            /// </summary>
            private HandleProcessExecution m_hProcExe;

            /// <summary>
            /// This object manages events related to output messages.
            /// </summary>
            private HandleOutputs m_hOutputs;

            /// <summary>
            /// Boolean variable that corresponds to the event dispatcher status. When false, exit the event dispatch loop.
            /// </summary>
            public bool _running = true;


            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="ed"> This object manages debug events in the engine. </param>
            public GDBOutput(EventDispatcher ed)
            {
                m_eventDispatcher = ed;                
                _running = true;
            }


            /// <summary>
            /// Thread responsible for handling asynchronous GDB output.
            /// </summary>
            public void processingGDBOutput()
            {
                while (_running)
                {
                    string response = "";
                    while ((response = GDBParser.removeGDBResponse()) == ""  && _running)
                    {
                    };

                    // Creating a char delimiter that will be used to split the response in more than one event
                    response = response.Replace("\r\n", "@"); 

                    string[] events = response.Split('@');
                    foreach (string ev in events)
                    {
                        if (ev.Length > 1)  // only to avoid empty events, when there are two delimiters characters together.
                        {
                            if (m_eventDispatcher.countSIGINT > 0)
                                if ((ev.Substring(0, 2) != "50") && (ev.Substring(0, 2) != "80"))
                                    m_eventDispatcher.countSIGINT = 0; // Reset the counter, if GDB has recovered from a GDB bug.
                            switch (ev[0])
                            {
                                case '0':  // Events related to starting GDB.
                                    break;
                                case '1':  // Not used.
                                    break;
                                case '2':  // Events related to breakpoints (including breakpoint hits).
                                    m_hBreakpoints = new HandleBreakpoints(m_eventDispatcher);
                                    m_hBreakpoints.handle(ev);
                                    break;
                                case '3':  // Not used.
                                    break;
                                case '4':  // Events related to execution control (processes, threads, programs) 1.
                                    m_hProcExe = new HandleProcessExecution(m_eventDispatcher);
                                    m_hProcExe.handle(ev);
                                    break;
                                case '5':  // Events related to execution control (processes, threads, programs and GDB Bugs) 2.
                                    m_hProcExe = new HandleProcessExecution(m_eventDispatcher);
                                    m_hProcExe.handle(ev);
                                    break;
                                case '6':  // Events related to evaluating expressions. Not used.
                                    break;
                                case '7':  // Events related to stack frames. Not used.
                                    break;
                                case '8':  // Events related to output.
                                    m_hOutputs = new HandleOutputs(m_eventDispatcher);
                                    m_hOutputs.handle(ev);
                                    break;
                                case '9':  // Not used.
                                    break;
                                default:   // Event that was not parsed correctly, or not handled completely.
                                    break;
                            }
                        }
                    }
                }          
            }
        }


        /// <summary>
        /// Interrupt the debugged process if necessary before changing a breakpoint.
        /// </summary>
        public void prepareToModifyBreakpoint()
        {
            if (m_engine.m_state != AD7Engine.DE_STATE.DESIGN_MODE 
             && m_engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
            {
                HandleProcessExecution.m_needsResumeAfterInterrupt = true;
                m_engine.CauseBreak();
            }            
        }


        /// <summary>
        /// If the process was running when the breakpoint was changed, resume the process.
        /// </summary>
        public void resumeFromInterrupt()
        {
            if (HandleProcessExecution.m_needsResumeAfterInterrupt)
            {
                HandleProcessExecution.m_needsResumeAfterInterrupt = false;
                continueExecution();
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
        private bool setBreakpointImpl(string command, string command2, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string response;
            string bpointAddress;
            string bpointStopPoint;

            GDB_ID = 0;
            GDB_filename = "";
            GDB_address = "";
            GDB_line = 0;

            if (ControlDebugEngine.isDebugEngineRunning)
            {
                prepareToModifyBreakpoint();

                // Gets the parsed response for the GDB/MI command that inserts breakpoint in a given line or a given function.
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
                response = GDBParser.parseCommand(command, 6);

                if ((command2 != "") && ((response.Contains("<PENDING>"))))
                {
                    response = GDBParser.parseCommand(command2, 6);
                }

                if (((response.Length < 2) && (ControlDebugEngine.isDebugEngineRunning == false)) || (response == "Function not found!"))
                {
                    resumeFromInterrupt();
                    return false;
                }

                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.handle(response);
                GDB_ID = (uint)hBreakpoints.number;
                GDB_filename = hBreakpoints.FileName;
                GDB_address = hBreakpoints.Address;

                if ((GDB_address != "<PENDING>") && (GDB_address != ""))
                {
                    //** Run Code to verify breakpoint stop point.

                    // Gets the parsed response for the GDB command that print information about the specified breakpoint, in this 
                    // case, only its address. (http://sourceware.org/gdb/onlinedocs/gdb/Set-Breaks.html)
                    bpointAddress = GDBParser.parseCommand("info b " + GDB_ID.ToString(), 18);

                    // Gets the parsed response for the GDB command that inquire what source line covers a particular address.
                    // (http://sourceware.org/gdb/onlinedocs/gdb/Machine-Code.html)
                    bpointStopPoint = GDBParser.parseCommand("info line *" + bpointAddress, 18);

                    GDB_line = (uint)Convert.ToInt64(bpointStopPoint.Trim());
                }
                else
                {
                    GDB_address = "0x0";
                    GDB_line = (uint)hBreakpoints.linePos;
                }

                resumeFromInterrupt();

                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Set a breakpoint given a filename and line number.
        /// </summary>
        /// <param name="filename"> Full path and filename for the code source. </param>
        /// <param name="line"> The line number for the breakpoint. </param>
        /// <param name="GDB_ID"> Returns the breakpoint ID in GDB. </param>
        /// <param name="GDB_line"> Returns the breakpoint Line Number. </param>
        /// <param name="GDB_filename"> Returns the breakpoint File Name. </param>
        /// <param name="GDB_address"> Returns the Breakpoint Address. </param>
        /// <returns> If successful, returns true; otherwise, returns false. </returns>
        public bool setBreakpoint(string filename, uint line, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string cmd = @"-break-insert --thread-group i1 -f " + filename + ":" + line;
            int i = filename.LastIndexOf('\\');
            if ((i != -1) && (i + 1 < filename.Length))
                filename = filename.Substring(i + 1);
            string cmd2 = @"-break-insert --thread-group i1 -f " + filename + ":" + line;
            return setBreakpointImpl(cmd, cmd2, out GDB_ID, out GDB_line, out GDB_filename, out GDB_address);
        }

        /// <summary>
        /// Set a breakpoint given a function name.
        /// </summary>
        /// <param name="func"> Function name. </param>
        /// <param name="GDB_ID"> Returns the breakpoint ID in GDB. </param>
        /// <param name="GDB_line"> Returns the breakpoint Line Number. </param>
        /// <param name="GDB_filename"> Returns the breakpoint File Name. </param>
        /// <param name="GDB_address"> Returns the Breakpoint Address. </param>
        /// <returns> If successful, returns true; otherwise, returns false. </returns>
        public bool setBreakpoint(string func, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string cmd = @"-break-insert " + func;
            return setBreakpointImpl(cmd, "", out GDB_ID, out GDB_line, out GDB_filename, out GDB_address);
        }

        /// <summary>
        /// Ignore a given number of hit counts in GDB.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <param name="ignore"> Number of hit counts to ignore. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool ignoreHitCount(uint GDB_ID, int ignore)
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
            string cmd = @"-break-after " + GDB_ID + " " + ignore;
            string response = GDBParser.parseCommand(cmd, 18);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);

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
        public bool resetHitCount(AD7BoundBreakpoint bbp, bool resetCondition)
        {
            // Declare local Variables
            uint GDB_LinePos = 0;
            string GDB_Filename = "";
            string GDB_address = "";

            uint GDB_ID = bbp.GDB_ID;

            // Deleting GDB breakpoint.
            deleteBreakpoint(GDB_ID);

            // Creating a new GDB breakpoint.
            bool ret = false;
            if (bbp.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                ret = setBreakpoint(bbp.m_filename, bbp.m_line, out GDB_ID, out GDB_LinePos, out GDB_Filename, out GDB_address);
            }
            else if (bbp.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET)
            {
                ret = setBreakpoint(bbp.m_func, out GDB_ID, out GDB_LinePos, out GDB_Filename, out GDB_address);
            }

            if (ret)
            {
                bbp.GDB_ID = GDB_ID;
                bbp.m_hitCount = 0;
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
        public bool setBreakpointCondition(uint GDB_ID, string condition)
        {
            string cmd;

            // Gets the parsed response for the GDB/MI command that sets a condition to the breakpoint "GDB_ID". If there is no 
            // condition, any previous one associated to this breakpoint will be removed.
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
            if (condition != "")
                cmd = @"-break-condition " + GDB_ID + " " + condition;
            else
                cmd = @"-break-condition " + GDB_ID;
            string response = GDBParser.parseCommand(cmd, 19);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);
            return true;
        }


        /// <summary>
        /// Delete a breakpoint in GDB.
        /// </summary>
        /// <param name="GDB_ID"> Breakpoint ID in GDB. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool deleteBreakpoint(uint GDB_ID)
        {
            if (m_gdbOutput._running)
            {
                prepareToModifyBreakpoint();

                // Gets the parsed response for the GDB/MI command that deletes the breakpoint "GDB_ID".
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
                string response = GDBParser.parseCommand(@"-break-delete " + GDB_ID, 7);
                if (response == null || response == "")
                {
                    resumeFromInterrupt();
                    return false;
                }
            
                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.handle(response);
                uint retID = (uint)hBreakpoints.number;

                resumeFromInterrupt();

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
        /// <param name="fEnable"> If true, enable the breakpoint. If false, disable it. </param>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool enableBreakpoint(uint GDB_ID, bool fEnable)
        {
            prepareToModifyBreakpoint();

            string inputCommand;
            string sEnable = "enable";

            if (!fEnable)
            {
                sEnable = "disable";
            }

            // Gets the parsed response for the GDB/MI command that enables or disables the breakpoint "GDB_ID".
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Breakpoint-Commands.html)
            inputCommand = @"-break-" + sEnable + " " + GDB_ID;
            string response = GDBParser.parseCommand(inputCommand, 8);
 
            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);
            uint retID = (uint)hBreakpoints.number;

            resumeFromInterrupt();
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
        public void updateHitCount(uint ID, uint hitCount)
        {
            var bbp = m_engine.BPMgr.getBoundBreakpointForGDBID(ID);
            if (bbp != null)
            {
                if (!bbp.m_breakWhenCondChanged)
                    ((IDebugBoundBreakpoint2)bbp).SetHitCount(hitCount);
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
        public bool lockedBreakpoint(AD7BoundBreakpoint bbp, bool hit, bool cond)
        {
            lock (m_lockBreakpoint)
            {
                if (hit && cond)
                {
                    if ((!bbp.m_blockedPassCount) && (!bbp.m_blockedConditional))
                    {
                        bbp.m_blockedPassCount = true;
                        bbp.m_blockedConditional = true;
                        return true;
                    }
                }
                else if (hit)
                {
                    if (!bbp.m_blockedPassCount)
                    {
                        bbp.m_blockedPassCount = true;
                        return true;
                    }
                }
                else if (cond)
                {
                    if (!bbp.m_blockedConditional)
                    {
                        bbp.m_blockedConditional = true;
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
        public void unlockBreakpoint(AD7BoundBreakpoint bbp, bool hit, bool cond)
        {
            lock (m_unlockBreakpoint)
            {
                if (hit && cond)
                {
                    bbp.m_blockedPassCount = false;
                    bbp.m_blockedConditional = false;
                }
                else if (hit)
                {
                    bbp.m_blockedPassCount = false;
                }
                else if (cond)
                {
                    bbp.m_blockedConditional = false;
                }
            }
        }


        /// <summary>
        /// Control access to a critical region, that manipulates some of the debug engine variables, like its state. This is done to 
        /// avoid a race condition that can happen when user modifies a breakpoint condition at run time and the same breakpoint is hit. 
        /// When that happens, only one event will be handled at a time.
        /// </summary>
        /// <returns>  If successful, returns true; otherwise, returns false. </returns>
        public bool enterCriticalRegion()
        {
            lock (m_criticalRegion)
            {
                if (!inCriticalRegion)
                {
                    inCriticalRegion = true;
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
        public void leaveCriticalRegion()
        {
            lock (m_leaveCriticalRegion)
            {
                inCriticalRegion = false;
            }
        }
       

        /// <summary>
        /// Update VS when a breakpoint is hit in GDB.
        /// </summary>
        /// <param name="ID"> Breakpoint ID from GDB. </param>
        /// <param name="threadID"> Thread ID. </param>
        public void breakpointHit(uint ID, string threadID)
        {
            var xBoundBreakpoints = new List<IDebugBoundBreakpoint2>();

            // Search the active bound BPs and find ones that match the ID.
            var bbp = m_engine.BPMgr.getBoundBreakpointForGDBID(ID);

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
                if (lockedBreakpoint(bbp, true, true))
                {
                    while (!enterCriticalRegion())
                    {
                        Thread.Sleep(0);
                    }

                    bool breakExecution = true;

                    if (bbp.m_breakWhenCondChanged)
                    {
                        string result = "";
                        bool valid = VariableInfo.evaluateExpression(bbp.m_bpCondition.bstrCondition, ref result, null);
                        if ((valid) && (bbp.m_previousCondEvaluation != result)) // check if condition evaluation has changed
                        {
                            if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL) && (bbp.m_hitCount != bbp.m_bpPassCount.dwPassCount))
                            {
                                breakExecution = false;
                            }
                            else if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER) && (bbp.m_hitCount < bbp.m_bpPassCount.dwPassCount))
                            {
                                breakExecution = false;
                            }
                            else if ((bbp.m_bpPassCount.stylePassCount == enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD) && ((bbp.m_hitCount % bbp.m_bpPassCount.dwPassCount) != 0))
                            {
                                breakExecution = false;
                            }
                            bbp.m_previousCondEvaluation = result;
                        }
                        else
                            breakExecution = false;
                    }
                    if (!breakExecution) // must continue the execution
                    {
                        bool hitBreakAll = m_engine.m_running.WaitOne(0);
                        if (hitBreakAll)
                        {
                            m_engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                            // Sends the GDB command that resumes the execution of the inferior program. 
                            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                            GDBParser.addGDBCommand(@"-exec-continue --thread-group i1");
                            EventDispatcher.m_GDBRunMode = true;
                            m_engine.m_running.Set();
                        }
                    }
                    else
                    {
                        if (bbp.m_breakWhenCondChanged)
                            bbp.m_hitCount += 1;

                        // Transition DE state
                        EventDispatcher.m_GDBRunMode = false;
                        m_engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                        // Found a bound breakpoint
                        m_engine.Callback.OnBreakpoint(m_engine.SelectThread(threadID), xBoundBreakpoints.AsReadOnly());

                        if (bbp.m_isHitCountEqual)
                        {
                            // Have to ignore the biggest number of times to keep the breakpoint enabled and to avoid stopping on it.
                            ignoreHitCount(ID, int.MaxValue); 
                        }
                        else if (bbp.m_hitCountMultiple != 0)
                        {
                            ignoreHitCount(ID, (int)(bbp.m_hitCountMultiple - (bbp.m_hitCount % bbp.m_hitCountMultiple)));
                        }
                    }
                    leaveCriticalRegion();
                    unlockBreakpoint(bbp, true, true);
                }
                else
                {
                    while (!enterCriticalRegion())
                    {
                        Thread.Sleep(0);
                    }

                    bool hitBreakAll = m_engine.m_running.WaitOne(0);
                    if (hitBreakAll)
                    {
                        m_engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                        // Sends the GDB command that resumes the execution of the inferior program. 
                        // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                        GDBParser.addGDBCommand(@"-exec-continue --thread-group i1");
                        EventDispatcher.m_GDBRunMode = true;
                        m_engine.m_running.Set();
                    }

                    leaveCriticalRegion();
                }
            }
        }


        /// <summary>
        /// Returns the document context needed for showing the location of the current instruction pointer.
        /// </summary>
        /// <param name="filename"> File name. </param>
        /// <param name="line"> Line number. </param>
        /// <returns> Returns the document context needed for showing the location of the current instruction pointer. </returns>
        public AD7DocumentContext getDocumentContext(string filename, uint line)
        {
            // Get the location in the document that the breakpoint is in.
            TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
            startPosition[0].dwLine = line;
            startPosition[0].dwColumn = 0;
            TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
            endPosition[0].dwLine = line;
            endPosition[0].dwColumn = 0;

            uint address = 0;
            AD7MemoryAddress codeContext = new AD7MemoryAddress(m_engine, address);

            return new AD7DocumentContext(filename, startPosition[0], endPosition[0], codeContext);
        }


        /// <summary>
        /// Return the depth of the stack.
        /// </summary>
        /// <param name="threadID"> Thread ID. </param>
        /// <returns> Returns the stack depth. </returns>
        public int getStackDepth(string threadID)
        {
            // Returns the parsed response for the GDB/MI command that inquires about the depth of the stack. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return Convert.ToInt32(GDBParser.parseCommand(@"-stack-info-depth --thread " + threadID + " --frame 0", 9));
        }


        /// <summary>
        /// List the frames currently on the stack.
        /// </summary>
        /// <returns> Returns a string with the frames currently on the stack. </returns>
        public string getStackFrames()
        {
            // Returns the parsed response for the GDB/MI command that list the frames currently on the stack. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return GDBParser.parseCommand(@"-stack-list-frames", 10);
        }


        /// <summary>
        /// List the names of local variables and function arguments for the selected frame.
        /// </summary>
        /// <param name="frameIndex"> Frame number. </param>
        /// <param name="threadID"> Thread ID. </param>
        /// <returns> Returns a string with the names of local variables and function arguments for the selected frame. </returns>
        public string getVariablesForFrame(uint frameIndex, string threadID)
        {
            // Returns the parsed response for the GDB/MI command that list the names of local variables and function arguments for the 
            // selected frame. (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Stack-Manipulation.html)
            return GDBParser.parseCommand(@"-stack-list-variables --thread " + threadID + " --frame " + frameIndex + " --simple-values", 11);
        }


        /// <summary>
        /// Make "id" the current thread.
        /// </summary>
        /// <param name="id"> Thread ID. </param>
        public void selectThread(string id)
        {
            // Waits for the parsed response for the GDB/MI command that make "id" the current thread.
            // (http://www.sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Thread-Commands.html)
            GDBParser.parseCommand(@"-thread-select " + id, 12);
        }

        
        /// <summary>
        /// Creates a variable object.
        /// </summary>
        /// <param name="name"> Name of the variable. </param>
        /// <param name="hasVsNdK_"> Boolean value that indicates if the variable name has the prefix VsNdK_. </param>
        /// <returns> If successful, returns the variable's number of children; otherwise, returns string "ERROR". </returns>
        public string createVar(string name, ref bool hasVsNdK_)
        {
            // Gets the variable's number of children after sending the following GDB/MI command to create a variable object. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Variable-Objects.html)
            string response = GDBParser.parseCommand("-var-create " + name + " \"*\" " + name, 13);
            if (response == "ERROR")
            {
                // The -var-create GDB/MI command can return an error when the variable name starts with some characters, like '_'.
                // So, in case of an error, add the prefix VsNdK_ to the variable name and try to create it again. 
                response = GDBParser.parseCommand("-var-create VsNdK_" + name + " \"*\" " + name, 13);
                if (response != "ERROR")
                    hasVsNdK_ = true;
            }
            return response;
        }


        /// <summary>
        /// Deletes a previously created variable object and all of its children.
        /// </summary>
        /// <param name="name"> Name of the variable. </param>
        /// <param name="hasVsNdK_"> Boolean value that indicates if the variable name has the prefix VsNdK_. </param>
        public void deleteVar(string name, bool hasVsNdK_)
        {
            // Waits for the parsed response for the GDB/MI command that deletes a previously created variable object and all of 
            // its children. (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Variable-Objects.html)
            if (!hasVsNdK_)
                GDBParser.parseCommand("-var-delete " + name, 14);
            else
                GDBParser.parseCommand("-var-delete VsNdK_" + name, 14);
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
            return GDBParser.parseCommand("-var-list-children --all-values " + name + " 0 50", 15); 
        }


        /// <summary>
        /// Kill the child process in which your program is running under gdb.
        /// </summary>
        public void killProcess()
        {
            // Waits for the parsed response for the GDB command that kills the child process in which your program is running. 
            // (http://sourceware.org/gdb/onlinedocs/gdb/Kill-Process.html#Kill-Process)
            GDBParser.parseCommand("kill", 16);
        }


        /// <summary>
        /// Called after the debug engine has set the initial breakpoints, or to resume a process that was interrupted.
        /// </summary>
        public void continueExecution()
        {
            //** Transition DE state
            bool hitBreakAll = m_engine.m_running.WaitOne(0);
            if (hitBreakAll)
            {
                m_engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                // Sends the GDB command that resumes the execution of the inferior program. 
                // (http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Program-Execution.html)
                GDBParser.addGDBCommand(@"-exec-continue --thread-group i1");
                EventDispatcher.m_GDBRunMode = true;
                m_engine.m_running.Set();
            }
        }
    }


    /// <summary>
    /// This class manages breakpoints events.
    /// </summary>
    public class HandleBreakpoints
    {
        /// <summary>
        /// GDB breakpoint ID.
        /// </summary>
        private int m_number = -1;

        /// <summary>
        /// Boolean variable that indicates if this breakpoint is enable (true) or disable (false).
        /// </summary>
        private bool m_enable;

        /// <summary>
        /// Breakpoint address.
        /// </summary>
        private string m_addr = "";

        /// <summary>
        /// Name of the function that contains this breakpoint.
        /// </summary>
        private string m_func = "";

        /// <summary>
        /// File name that contains this breakpoint.
        /// </summary>
        private string m_filename = "";

        /// <summary>
        /// Line number for this breakpoint.
        /// </summary>
        private int m_line = -1;

        /// <summary>
        /// Number of hits for this breakpoint.
        /// </summary>
        private int m_hits = -1;

        /// <summary>
        /// Number of hits to be ignored by this breakpoint.
        /// </summary>
        private int m_ignoreHits = -1;

        /// <summary>
        /// Condition associated to this breakpoint.
        /// </summary>
        private string m_condition = "";

        /// <summary>
        /// Thread ID that was interrupted when this breakpoint was hit.
        /// </summary>
        private string m_threadID = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private EventDispatcher m_eventDispatcher;

        /// <summary>
        /// GDB_ID Property
        /// </summary>
        public int number 
        {
            get { return m_number; }
        }

        /// <summary>
        /// GDB Line Position Property
        /// </summary>
        public int linePos
        {
            get { return m_line; }
        }

        /// <summary>
        /// GDB File name
        /// </summary>
        public string FileName
        {
            get { return m_filename; }
        }

        /// <summary>
        /// GDB Address
        /// </summary>
        public string Address
        {
            get { return m_addr; }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ed"> This object manages debug events in the engine. </param>
        public HandleBreakpoints(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }

        
        /// <summary>
        /// This method manages breakpoints events by classifying each of them by sub-type (e.g. breakpoint inserted, modified, etc.).
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void handle(string ev)
        {
            int ini = 0;
            int end = 0;
            switch (ev[1])
            {
                case '0':  
                // Breakpoint inserted (synchronous). 
                // Example: 20,1,y,0x0804d843,main,C:/Users/xxxxx/vsplugin-ndk/samples/Square/Square/main.c,319,0       
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    m_number = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    if (ev[end + 1] == 'y')
                        m_enable = true;
                    else
                        m_enable = false;

                    ini = end + 3;
                    end = ev.IndexOf(';', ini);
                    m_addr = ev.Substring(ini, (end - ini));
                    if (m_addr == "<PENDING>")
                    {
                        m_func = "??";
                        EventDispatcher.m_unknownCode = true;
                        m_filename = "";
                        m_line = 0;
                        m_hits = 0;
                        return;
                    }

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_func = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_filename = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    ini = end + 1;
                    m_hits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));                   

                    break;
                case '1':  
                // Breakpoint modified (asynchronous). 
                // Example: 21,1,y,0x0804d843,main,C:/Users/xxxxxx/vsplugin-ndk/samples/Square/Square/main.c,318,1
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    m_number = Convert.ToInt32(ev.Substring(ini, (end - ini))); ;

                    if (ev[end + 1] == 'y')
                        m_enable = true;
                    else
                        m_enable = false;

                    ini = end + 3;
                    end = ev.IndexOf(';', ini);
                    m_addr = ev.Substring(ini, (end - ini));

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_func = ev.Substring(ini, end - ini);

                    // Need to set the flag for unknown code if necessary.
                    if (m_func == "??")
                    {
                        EventDispatcher.m_unknownCode = true;
                    }
                    else
                    {
                        EventDispatcher.m_unknownCode = false;
                    }

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_filename = ev.Substring(ini, end - ini);

                    ini = end + 1;
                    end = ev.IndexOf(';', ini);
                    m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                    ini = end + 1;
                    m_hits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                    // Update hit count on affected bound breakpoint.
                    m_eventDispatcher.updateHitCount((uint)m_number, (uint)m_hits);

                    break;
                case '2':  
                // Breakpoint deleted asynchronously (a temporary breakpoint). Example: 22,2\r\n
                    m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                    break;
                case '3':  
                // Breakpoint enabled. Example: 23 (enabled all) or 23,1 (enabled only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints.

                    break;
                case '4':  
                // Breakpoint disabled. Example: 24 (disabled all) or 24,1 (disabled only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints.

                    break;
                case '5':  
                // Breakpoint deleted. Example: 25 (deleted all) or 25,1 (deleted only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints.

                    break;
                case '6':  
                // Break after "n" hits (or ignore n hits). Example: 26;1;100
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    m_number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                    ini = end + 1;
                    m_ignoreHits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                    break;
                case '7':  
                // Breakpoint hit. 
                // Example: 27,1,C:/Users/xxxxxx/vsplugin-ndk/samples/Square/Square/main.c,319;1\r\n
                    bool updatingCondBreak = this.m_eventDispatcher.engine.m_updatingConditionalBreakpoint.WaitOne(0);
                    if (updatingCondBreak)
                    {

                        m_eventDispatcher.engine.resetStackFrames();

                        ini = 3;
                        end = ev.IndexOf(';', 3);
                        m_number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                        ini = end + 1;
                        end = ev.IndexOf(';', ini);
                        m_filename = ev.Substring(ini, end - ini);

                        ini = end + 1;
                        end = ev.IndexOf(';', ini);
                        m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                        ini = end + 1;
                        m_threadID = ev.Substring(ini, (ev.Length - ini));

                        this.m_eventDispatcher.engine.cleanEvaluatedThreads();

                        // Call the method/event that will stop SDM because a breakpoint was hit here.
                        if (m_eventDispatcher.engine._updateThreads)
                        {
                            m_eventDispatcher.engine.UpdateListOfThreads();
                        }
                        m_eventDispatcher.engine.SelectThread(m_threadID).setCurrentLocation(m_filename, (uint)m_line);
                        m_eventDispatcher.engine.SetAsCurrentThread(m_threadID);

                        // A breakpoint can be hit during a step
                        if (m_eventDispatcher.engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
                        {
                            HandleProcessExecution.onStepCompleted(m_eventDispatcher, m_filename, (uint)m_line);
                        }
                        else
                        {
                            // Visual Studio shows the line position one more than it actually is
                            m_eventDispatcher.engine.m_docContext = m_eventDispatcher.getDocumentContext(m_filename, (uint)(m_line - 1));
                            m_eventDispatcher.breakpointHit((uint)m_number, m_threadID);
                        }
                        this.m_eventDispatcher.engine.m_updatingConditionalBreakpoint.Set();
                    }
                    break;
                case '8':  
                // Breakpoint condition set. Example: 28;1;expression
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    if (end != -1)
                    {
                        m_number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                        ini = end + 1;
                        m_condition = ev.Substring(ini, (ev.Length - ini));
                    }
                    else
                    {
                        m_number = Convert.ToInt32(ev.Substring(3));
                        m_condition = "";
                    }
                    break;
                case '9':  // Error in testing breakpoint condition
                    break;
                default:   // not used.
                    break;
            }
        }
    }


    /// <summary>
    /// This class manages events related to execution control (processes, threads, programs).
    /// </summary>
    public class HandleProcessExecution
    {
        /// <summary>
        /// Thread ID.
        /// </summary>
        private int m_threadId = -1; // when threadId is 0, it means all threads.

        /// <summary>
        /// Process ID.
        /// </summary>
        private int m_processId = -1;

        /// <summary>
        /// Name of the signal that caused an interruption.
        /// </summary>
        private string m_signalName = "";

        /// <summary>
        /// Meaning of the signal that caused an interruption.
        /// </summary>
        private string m_signalMeaning = "";

        /// <summary>
        /// File name.
        /// </summary>
        private string m_file = "";

        /// <summary>
        /// Line number.
        /// </summary>
        private int m_line = -1;

        /// <summary>
        /// Address.
        /// </summary>
        private int m_address = -1;

        /// <summary>
        /// Function name.
        /// </summary>
        private string m_func = "";

        /// <summary>
        /// Error caused by a GDB command that failed.
        /// </summary>
        private string m_error = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private EventDispatcher m_eventDispatcher = null;

        /// <summary>
        /// Boolean variable that indicates if GDB has to resume execution after handling what caused it to enter in break mode.
        /// </summary>
        public static bool m_needsResumeAfterInterrupt = false;

        /// <summary>
        /// Used as a communication signal between the Event Dispatcher and the debug engine method responsible for stopping GDB 
        /// execution (IDebugEngine2.CauseBreak()). So, VS is able to wait for GDB's interruption before entering in break mode.
        /// </summary>
        public static ManualResetEvent m_mre = new ManualResetEvent(false);
        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ed"> This object manages debug events in the engine. </param>
        public HandleProcessExecution(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }


        /// <summary>
        /// This method manages events related to execution control by classifying each of them by sub-type (e.g. thread created, program interrupted, etc.).
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void handle(string ev)
        {
            int ini = 0;
            int end = 0;
            int numCommas = 0;
            switch (ev[0])
            {
                case '4':
                    switch (ev[1])
                    {
                        case '0':  
                        // Thread created. Example: 40,2,20537438
                            EventDispatcher.m_GDBRunMode = true;
                            ini = 3;
                            end = ev.IndexOf(";", 3);
                            m_threadId = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                            ini = end + 1;
                            try
                            {
                                m_processId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                            }
                            catch
                            {
                                m_processId = 0;
                            }

                            m_eventDispatcher.engine._updateThreads = true;

                            break;
                        case '1':  
                        // Process running. Example: 41,1     (when threadId is 0 means "all threads": example: 41,0)
                            EventDispatcher.m_GDBRunMode = true;
                            m_threadId = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                            break;
                        case '2':  
                        // Program exited normally. Example: 42
                            m_eventDispatcher.endDebugSession(0);

                            break;
                        case '3':  
                        // Program was exited with an exit code. Example: 43,1;1 (not sure if there is a threadID, but the last ";" exist)   
                            // TODO: not tested yet
                            end = ev.IndexOf(";", 3);
                            uint exitCode = Convert.ToUInt32(ev.Substring(3, (end - 3)));
                            m_eventDispatcher.endDebugSession(exitCode);

                            break;
                        case '4':  
                        // Program interrupted. 
                            // Examples:
                            // 44,ADDR,FUNC,THREAD-ID         
                            // 44,ADDR,FUNC,FILENAME,LINE,THREAD-ID

                            m_eventDispatcher.engine.resetStackFrames();
                            EventDispatcher.m_GDBRunMode = false;
                            numCommas = 0;
                            foreach (char c in ev)
                            {
                                if (c == ';')
                                    numCommas++;
                            }

                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            m_address = Convert.ToInt32(ev.Substring(ini, (end - ini)), 16);

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            m_func = ev.Substring(ini, (end - ini));

                            if (m_func == "??")
                            {
                                EventDispatcher.m_unknownCode = true;
                            }
                            else
                            {
                                EventDispatcher.m_unknownCode = false;
                            }

                            switch (numCommas)
                            {
                                case 3:
                                    // Thread ID
                                    ini = end + 1;
                                    m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    EventDispatcher.m_unknownCode = true;
                                    break;
                                case 4:
                                    // Filename and line number
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    m_file = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    m_line = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                                case 5:
                                    //  Filename, line number and thread ID
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    m_file = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                    ini = end + 1;
                                    m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                                default:
                                    break;
                            }

                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();


                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }
                            if (m_threadId > 0)
                            {
                                m_eventDispatcher.engine.SelectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.SetAsCurrentThread(m_threadId.ToString());
                            }
                            
                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            onInterrupt(m_threadId);

                            // Signal that interrupt is processed 
                            m_mre.Set();

                            break;

                        case '5':  
                        // End-stepping-range.
                            m_eventDispatcher.engine.resetStackFrames();
                            EventDispatcher.m_GDBRunMode = false;
                            ini = 3;
                            end = ev.IndexOf(';', 3);
                            if (end == -1)
                                end = ev.Length;
                            string temp = ev.Substring(ini, (end - ini));

                            ini = end + 1;

                            if (ev.Length > ini)
                                end = ev.IndexOf(';', ini);
                            else
                                end = -1;

                            if (end == -1)
                            {
                                // Set sane default values for the missing file and line information 
                                m_file = "";
                                m_line = 1;
                                m_threadId = Convert.ToInt32(temp);
                                EventDispatcher.m_unknownCode = true;
                            }
                            else
                            {
                                m_file = temp;
                                m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                ini = end + 1;
                                m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher.m_unknownCode = false;
                            }

                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();


                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }
                            if (m_threadId > 0)
                            {
                                if ((EventDispatcher.m_unknownCode == false) && (m_file != ""))
                                    m_eventDispatcher.engine.SelectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.SetAsCurrentThread(m_threadId.ToString());
                            }

                            HandleProcessExecution.onStepCompleted(m_eventDispatcher, m_file, (uint)m_line);

                            break;
                        case '6':  
                        // Function-finished.
                            m_eventDispatcher.engine.resetStackFrames();
                            EventDispatcher.m_GDBRunMode = false;
                            ini = 3;
                            end = ev.IndexOf(';', 3);
                            if (end == -1)
                            {
                                // Set sane default values for the missing file and line information 
                                m_file = "";
                                m_line = 1;
                                m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher.m_unknownCode = true;
                            }
                            else
                            {
                                m_file = ev.Substring(ini, (end - ini));
                                ini = end + 1;
                                end = ev.IndexOf(';', ini);
                                m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                ini = end + 1;
                                m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                EventDispatcher.m_unknownCode = false;
                            }

                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();

                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }
                            if (m_threadId > 0)
                            {
                                if ((EventDispatcher.m_unknownCode == false) && (m_file != ""))
                                    m_eventDispatcher.engine.SelectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.SetAsCurrentThread(m_threadId.ToString());
                            }

                            HandleProcessExecution.onStepCompleted(m_eventDispatcher, m_file, (uint)m_line);

                            break;
                        case '7':  
                        // -exec-interrupt or signal-meaning="Killed". There's nothing to do in this case.
                            m_eventDispatcher.engine.resetStackFrames();
                            EventDispatcher.m_GDBRunMode = false;

                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();

                            m_threadId = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }
                            if (m_threadId > 0)
                            {
                                if ((EventDispatcher.m_unknownCode == false) && (m_file != ""))
                                    m_eventDispatcher.engine.SelectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.SetAsCurrentThread(m_threadId.ToString());
                            }

                            if (m_eventDispatcher.engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
                            {
                                onInterrupt(m_threadId);
                            }
                            // Signal that interrupt is processed 
                            m_mre.Set();

                            break;
                        case '8':  
                        // SIGKILL
                            m_eventDispatcher.endDebugSession(0);
                            break;
                        case '9':  
                        // ERROR, ex: 49,Cannot find bounds of current function
                            m_eventDispatcher.engine.resetStackFrames();
                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();

                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }

                            if (ev.Length >= 3)
                            {
                                m_error = ev.Substring(3, (ev.Length - 3));
                                if (m_error == "Cannot find bounds of current function")
                                {
                                    // We don't have symbols for this function so further stepping won't be possible. Return from this function.
                                    EventDispatcher.m_unknownCode = true;
                                    m_eventDispatcher.engine.Step(m_eventDispatcher.engine.CurrentThread(), enum_STEPKIND.STEP_OUT, enum_STEPUNIT.STEP_LINE);
                                }
                            }
                            break;
                        default:   // Not used.
                            break;
                    }
                    break;
                case '5':
                    switch (ev[1])
                    {
                        case '0':  
                        // Quit (expect signal SIGINT when the program is resumed)
                            m_eventDispatcher.countSIGINT += 1;
                            if (m_eventDispatcher.countSIGINT > 5)
                            {
                                m_eventDispatcher.endDebugSession(0);
                                MessageBox.Show("Lost communication with GDB. Please refer to documentation for more details.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        case '1':  
                        // Thread exited. Example: 51,2
                            ini = 3;
                            m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                            m_eventDispatcher.engine._updateThreads = true;

                            break;
                        case '2':  
                        // GDB Bugs, like "... 2374: internal-error: frame_cleanup_after_sniffer ...". Example: 52
                            m_eventDispatcher.endDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen when interrupting GDB's execution by hitting the \"break all\" or toggling a breakpoint in run mode. \n\n GDB CRASHED. Details: \"../../gdb/frame.c:2374: internal-error: frame_cleanup_after_sniffer: Assertion `frame->prologue_cache == NULL' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '3':  
                        // Lost communication with device/simulator: ^error,msg="Remote communication error: No error."
                            MessageBox.Show("Lost communication with the device/simulator.", "Communication lost", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            m_eventDispatcher.endDebugSession(0);

                            break;
                        case '4':  
                        // Program interrupted due to a segmentation fault. 
                            // Examples:
                            // 54,ADDR,FUNC,THREAD-ID         
                            // 54,ADDR,FUNC,FILENAME,LINE,THREAD-ID

                            m_eventDispatcher.engine.resetStackFrames();
                            EventDispatcher.m_GDBRunMode = false;
                            numCommas = 0;
                            foreach (char c in ev)
                            {
                                if (c == ';')
                                    numCommas++;
                            }

                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            m_address = Convert.ToInt32(ev.Substring(ini, (end - ini)), 16);

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            m_func = ev.Substring(ini, (end - ini));

                            if (m_func == "??")
                            {
                                EventDispatcher.m_unknownCode = true;
                            }
                            else
                            {
                                EventDispatcher.m_unknownCode = false;
                            }

                            switch (numCommas)
                            {
                                case 3:
                                    // Thread ID
                                    ini = end + 1;
                                    m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    EventDispatcher.m_unknownCode = true;
                                    break;
                                case 5:
                                    //  Filename, line number and thread ID
                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    m_file = ev.Substring(ini, (end - ini));

                                    ini = end + 1;
                                    end = ev.IndexOf(';', ini);
                                    m_line = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                                    ini = end + 1;
                                    m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                                    break;
                                default:
                                    break;
                            }

                            MessageBox.Show("Segmentation Fault: If you continue debugging could take the environment to an unstable state.", "Segmentation Fault", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();

                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }
                            if (m_threadId > 0)
                            {
                                m_eventDispatcher.engine.SelectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.SetAsCurrentThread(m_threadId.ToString());
                            }

                            onInterrupt(m_threadId);

                            break;

                        case '5':  
                        // Exited-signaled. Ex: 55;SIGSEGV;Segmentation fault
                        // or Aborted. Ex: 55;SIGABRT;Aborted
                            ini = 3;
                            end = ev.IndexOf(';', ini);
                            m_signalName = ev.Substring(ini, (end - ini));

                            ini = end + 1;
                            end = ev.IndexOf(';', ini);
                            m_signalMeaning = ev.Substring(ini, (end - ini));

                            ini = end + 1;
                            try
                            {
                                m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));
                            }
                            catch
                            {
                            }

                            if (m_signalMeaning == "Segmentation fault")
                            {
                                MessageBox.Show("Segmentation Fault: Closing debugger.", "Segmentation Fault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                m_eventDispatcher.endDebugSession(0);
                            }

                            if (m_signalMeaning == "Aborted")
                            {
                                MessageBox.Show("Program aborted: Closing debugger.", "Program Aborted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                m_eventDispatcher.endDebugSession(0);
                            }

                            break;
                        case '6':  
                        // GDB Bugs, like "... 3550: internal-error: handle_inferior_event ...". Example: 56
                            m_eventDispatcher.endDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen while debugging multithreaded programs. \n\n GDB CRASHED. Details: \"../../gdb/infrun.c:3550: internal-error: handle_inferior_event: Assertion ptid_equal (singlestep_ptid, ecs->ptid)' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '7':  // Not used
                            break;
                        case '8':  // Not used
                            break;
                        case '9':  // Not used
                            break;
                        default:   // Not used.
                            break;
                    }
                    break;
                default:   // Not used.
                    break;
            }
        }


        /// <summary>
        /// Update VS when a step action is completed in GDB.
        /// </summary>
        /// <param name="eventDispatcher"> This object manages debug events in the engine. </param>
        /// <param name="file"> File name. </param>
        /// <param name="line"> Line number. </param>
        public static void onStepCompleted(EventDispatcher eventDispatcher, string file, uint line)
        {
            if (eventDispatcher.engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
            {
                eventDispatcher.engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                // Visual Studio shows the line position one more than it actually is
                eventDispatcher.engine.m_docContext = eventDispatcher.getDocumentContext(file, line - 1);
                AD7StepCompletedEvent.Send(eventDispatcher.engine);
            }
        }


        /// <summary>
        /// Update VS when the debugging process is interrupted in GDB.
        /// </summary>
        /// <param name="threadID"> Thread ID. </param>
        private void onInterrupt(int threadID)
        {
            Debug.Assert(m_eventDispatcher.engine.m_state == AD7Engine.DE_STATE.RUN_MODE);
            m_eventDispatcher.engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

            if (m_file != "" && m_line > 0)
            {
                // Visual Studio shows the line position one more than it actually is
                m_eventDispatcher.engine.m_docContext = m_eventDispatcher.getDocumentContext(m_file, (uint)(m_line - 1));
            }

            // Only send OnAsyncBreakComplete if break-all was requested by the user
            if (!m_needsResumeAfterInterrupt)
            {
                m_eventDispatcher.engine.Callback.OnAsyncBreakComplete(m_eventDispatcher.engine.SelectThread(threadID.ToString()));
            }
        }
    }


    /// <summary>
    /// This class manages events related to output messages.
    /// </summary>
    public class HandleOutputs
    {
        /// <summary>
        /// GDB textual output from the running target to be presented in the VS standard output window.
        /// </summary>
        private string m_stdOut = "";

        /// <summary>
        /// Other GDB messages to be presented in the VS standard output window.
        /// </summary>
        private string m_console = "";

        /// <summary>
        /// This object manages debug events in the engine.
        /// </summary>
        private EventDispatcher m_eventDispatcher = null;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ed"> This object manages debug events in the engine. </param>
        public HandleOutputs(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }


        /// <summary>
        /// This method manages events related to output messages by classifying each of them by sub-type.
        /// </summary>
        /// <param name="ev"> String that contains the event description. </param>
        public void handle(string ev)
        {
            int ini = 0;
            int end = 0;
            switch (ev[1])
            {
                case '0':  // Display the m_console message in the VS output window. Example: 80,\"\"[New pid 15380494 tid 2]\\n\"\"!80
                    ini = 4;
                    end = ev.IndexOf("\"!80", 4);
                    if (end == -1)
                        end = ev.Length;
                    m_console = ev.Substring(ini, (end - ini));

                // TODO: Call the method/event that will output this message in the VS output window.

                    break;
                case '1':  // Display the m_stdOut message in the VS standard output window. Instruction should look like this: 81,\"\" ... "\"!81
                    ini = 4;
                    end = ev.IndexOf("\"!81", 4);
                    if (end == -1)
                        end = ev.Length;
                    m_stdOut = ev.Substring(ini, (end - ini));

                // TODO: Call the method/event that will output this message in the VS standar output window.

                    break;
                default:   // not used.
                    break;
            }
        }
    }
}
