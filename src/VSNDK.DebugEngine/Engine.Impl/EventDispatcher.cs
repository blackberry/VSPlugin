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
using Microsoft.VisualStudio.Debugger.Interop;
using VSNDK.Parser;
using System.Threading;
using System.Windows.Forms;

namespace VSNDK.DebugEngine
{    
    /*     
     * This class manages debug events for the debug engine
     * 
     * Process GDB's output by classifying it by type (e.g. breakpoint event) and providing relevant data
     * Send GDB commands as appropriate for the received event (e.g. call -exec-continue to resume execution after a breakpoint is inserted)
     * Call debug engine methods to notify the SDM of an event (e.g. if a breakpoint is hit, call EngineCallback.OnBreakpoint())
     * 
     */
    public class EventDispatcher
    {
        private AD7Engine m_engine = null;
        private AD7Process m_process = null;        
        private Thread m_processingThread = null;
        GDBOutput m_gdbOutput;
//        public TextWriterTraceListener myWriter;
        public static bool m_unknownCode = false;
        public static bool m_updatedHitCount = false;
        private Object m_lockBreakpoint = new Object();
        private Object m_releaseBreakpoint = new Object();
        private Object m_criticalRegion = new Object(); // the critical regions are the ones where the GDB and/or VS can be turned on and off programatically
        private Object m_leaveCriticalRegion = new Object();
        public static bool m_GDBRunMode = true;
        public bool inCriticalRegion = false; // this variable is manipulated only in methods enterCriticalRegion and releaseCriticalRegion
        public int countSIGINT = 0;

        public AD7Engine engine
        {
            get { return m_engine; }
        }

        /**
         * Ends the debug session by closing GDB, sending the appropriate events to the SDM,
         * and breaking out of all buffer- and event-listening loops.
         */
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
            VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning = false;
        }

        public HandleBreakpoints hBreakpoints
        {
            get { return m_gdbOutput.hBreakpoints; }
        }

        public EventDispatcher(AD7Engine engine, AD7Process process)
        {
            m_engine = engine;
            m_process = process;            

            m_gdbOutput = new GDBOutput(this);
            m_processingThread = new Thread(m_gdbOutput.processingGDBOutput);
            m_processingThread.Start();

//            myWriter = new TextWriterTraceListener(System.Console.Out);
//            Debug.Listeners.Add(myWriter);
        }

        public class GDBOutput
        {
            private EventDispatcher m_eventDispatcher = null;
            private HandleBreakpoints m_hBreakpoints = null;            
            private HandleProcessExecution m_hProcExe = null;
            private HandleOutputs m_hOutputs = null;
            public bool _running = true;

            public GDBOutput(EventDispatcher ed)
            {
                m_eventDispatcher = ed;                
                _running = true;
            }

            public HandleBreakpoints hBreakpoints
            {
                get { return m_hBreakpoints; }
            }

            public void processingGDBOutput()
            {
//                string verify = "";
                while (_running)
                {
                    string response = "";
                    while ((response = GDBParser.removeGDBResponse()) == ""  && _running)
                    {
                    };

                    response = response.Replace("\r\n", "@"); // creating a char delimiter that will be used to split the response in more than one event

                    string[] events = response.Split('@');
                    foreach (string ev in events)
                    {
//                        verify += ev;
                        if (ev.Length > 1)  // only to avoid empty events, in case there are two delimiters together.
                        {
                            if (m_eventDispatcher.countSIGINT > 0)
                                if ((ev.Substring(0, 2) != "50") && (ev.Substring(0, 2) != "80"))
                                    m_eventDispatcher.countSIGINT = 0;
                            switch (ev[0])
                            {
                                case '0':  // events related to starting GDB
                                    break;
                                case '1':  // not used.
                                    break;
                                case '2':  // events related to breakpoints (including breakpoint hits)
                                    m_hBreakpoints = new HandleBreakpoints(m_eventDispatcher);
                                    m_hBreakpoints.handle(ev);
                                    break;
                                case '3':  // not used.
                                    break;
                                case '4':  // events related to execution control (processes, threads, programs) 1
                                    m_hProcExe = new HandleProcessExecution(m_eventDispatcher);
                                    m_hProcExe.handle(ev);
                                    break;
                                case '5':  // events related to execution control (processes, threads, programs and GDB Bugs) 2
                                    m_hProcExe = new HandleProcessExecution(m_eventDispatcher);
                                    m_hProcExe.handle(ev);
                                    break;
                                case '6':  // events related to evaluating expressions
                                    break;
                                case '7':  // events related to stack frames.
                                    break;
                                case '8':  // events related to output
                                    m_hOutputs = new HandleOutputs(m_eventDispatcher);
                                    m_hOutputs.handle(ev);
                                    break;
                                case '9':  // not used.
                                    break;
                                default:   // event not parsed correctly, or not implemented completely.
                                    break;
                            }
                        }
                    }
                }          
            }
        }

        // Interrupt the debugged process if necessary before changing a breakpoint
        public void prepareToModifyBreakpoint()
        {
            if (m_engine.m_state != AD7Engine.DE_STATE.DESIGN_MODE 
             && m_engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
            {
                HandleProcessExecution.m_needsResumeAfterInterrupt = true;
                m_engine.CauseBreak();
//                Debug.Assert(m_engine.m_state == AD7Engine.DE_STATE.BREAK_MODE);
            }            
        }

        // If the process was running when the breakpoint was changed, resume the process
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
        /// <param name="command">Initial command to set the breakpoint in GDB</param>
        /// <param name="GDB_ID">Breakpoint ID in GDB</param>
        /// <param name="GDB_line">Breakpoint Line Number</param>
        /// <param name="GDB_filename">Breakpoint File Name</param>
        /// <param name="GDB_address">Breakpoint Address</param>
        /// <returns>true if successful.</returns>
        private bool setBreakpointImpl(string command1, string command2, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string response;
            string bpointAddress;
            string bpointStopPoint;

            GDB_ID = 0;
            GDB_filename = "";
            GDB_address = "";
            GDB_line = 0;

            if (VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning == true)
            {
                prepareToModifyBreakpoint();

                response = GDBParser.parseCommand(command1, 6);

                if ((response.Contains("<PENDING>")) && (command2 != ""))
                    response = GDBParser.parseCommand(command2, 6);

                if ((response.Length < 2) && (VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning == false))
                {
                    return false;
                }


                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.handle(response);
                GDB_ID = (uint)hBreakpoints.number;
                GDB_filename = hBreakpoints.FileName;
                GDB_address = hBreakpoints.Address;

                if ((GDB_address != "<PENDING>") && (GDB_address != ""))
                {                //** Run Code to verify breakpoint stop point.
                    bpointAddress = GDBParser.parseCommand("info b " + GDB_ID.ToString(), 18);
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
        /// Set a breakpoint given a filename and line number
        /// </summary>
        /// <param name="filename">Full path and filename for the code source.</param>
        /// <param name="line">The line number for the breakpoint</param>
        /// <param name="GDB_ID">GDB ID</param>
        /// <returns>True if successfully set.</returns>
        public bool setBreakpoint(string filename, string fullPath, uint line, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string cmd1 = @"-break-insert --thread-group i1 -f " + fullPath + ":" + line;
            string cmd2 = @"-break-insert --thread-group i1 -f " + filename + ":" + line;
            return setBreakpointImpl(cmd1, cmd2, out GDB_ID, out GDB_line, out GDB_filename, out GDB_address);
        }
        
        // Set a breakpoint given a function name
        public bool setBreakpoint(string func, out uint GDB_ID, out uint GDB_line, out string GDB_filename, out string GDB_address)
        {
            string cmd = @"-break-insert " + func;
            return setBreakpointImpl(cmd, "", out GDB_ID, out GDB_line, out GDB_filename, out GDB_address);
        }

        // Ignore "ignore" hit counts
        public bool ignoreHitCount(uint GDB_ID, int ignore)
        {
            ignore -= 1;

            if (ignore < 0)
            {
                ignore = int.MaxValue; // had to ignore the biggest number of times to keep the breakpoint enabled and to avoid stopping on it.
            }

            string cmd = @"-break-after " + GDB_ID + " " + ignore;

            string response = GDBParser.parseCommand(cmd, 18);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);

            return true;
        }

        // Reset current hit count
        public bool resetHitCount(AD7BoundBreakpoint bbp, bool resetCondition)
        {
            // Declare local Variables
            uint GDB_LinePos = 0;
            string GDB_Filename = "";
            string GDB_address = "";

            uint GDB_ID = bbp.GDB_ID;
            deleteBreakpoint(GDB_ID);
            
            bool ret = false;
            if (bbp.m_bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                ret = setBreakpoint(bbp.m_filename, bbp.m_fullPath, bbp.m_line, out GDB_ID, out GDB_LinePos, out GDB_Filename, out GDB_address);
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

/*        // Break only when hit count is equal to "count"
        public bool setBreakWhenHitCountEqual(ref uint GDB_ID, uint counts, string filename, uint line)
        {
            // remove breakpoint GDB_ID and insert a temporary new one.
            deleteBreakpoint(GDB_ID);
            string cmd = @"-break-insert --thread-group i1 -t -f " + filename + ":" + line;

            string response = GDBParser.parseCommand(cmd, 20);

            if (response == "")
                return false;

            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);
            GDB_ID = (uint)hBreakpoints.number;

            ignoreHitCount(GDB_ID, counts);

            return true;
        }*/
        
        // Set breakpoint condition
        public bool setBreakpointCondition(uint GDB_ID, string condition)
        {
            string cmd;
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

/*        // Set watchpoint condition, i.e., stops when condition changes
        public bool setWatchpointCondition(ref uint GDB_ID, string condition)
        {
            if (condition != "")
            {
                // remove breakpoint GDB_ID and insert this new one... See the steps for creating a breakpoint.
                deleteBreakpoint(GDB_ID);
                string cmd = @"-break-watch " + condition;

                string response = GDBParser.parseCommand(cmd, 21);

                if (response == "")
                    return false;

                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.handle(response);
            }
            return true;
        }
        */
        public bool deleteBreakpoint(uint GDB_ID)
        {
            if (m_gdbOutput._running)
            {
                prepareToModifyBreakpoint();

                string response = GDBParser.parseCommand(@"-break-delete " + GDB_ID, 7);
                if (response == null || response == "")
                {
                    resumeFromInterrupt();
                    return false;
                }
            
                HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
                hBreakpoints.handle(response);
                uint retID = (uint)hBreakpoints.number;
                if (GDB_ID != retID)
                {
                    resumeFromInterrupt();
                    return false;
                }

                resumeFromInterrupt();
            }

            return true;
        }

        // Enable or disable a breakpoint
        public bool enableBreakpoint(uint GDB_ID, bool fEnable)
        {
            prepareToModifyBreakpoint();

            string inputCommand;
            string sEnable = "enable";

            if (!fEnable)
            {
                sEnable = "disable";
            }

            inputCommand = @"-break-" + sEnable + " " + GDB_ID;
            string response = GDBParser.parseCommand(inputCommand, 8);
 
            HandleBreakpoints hBreakpoints = new HandleBreakpoints(this);
            hBreakpoints.handle(response);
            uint retID = (uint)hBreakpoints.number;            
            if (GDB_ID != retID)
            {
                resumeFromInterrupt();
                return false;
            }

            resumeFromInterrupt();
            return true;
        }

        public void updateHitCount(uint ID, uint hitCount)
        {
            var bbp = m_engine.BPMgr.getBoundBreakpointForGDBID(ID);
            if (bbp != null)
            {
                if (!bbp.m_breakWhenCondChanged)
                    ((IDebugBoundBreakpoint2)bbp).SetHitCount(hitCount);
            }
        }

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

        public void releaseBreakpoint(AD7BoundBreakpoint bbp, bool hit, bool cond)
        {
            lock (m_releaseBreakpoint)
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

        public void releaseCriticalRegion()
        {
            lock (m_leaveCriticalRegion)
            {
                inCriticalRegion = false;
            }
        }
       
        // ID is the breakpoint ID from GDB
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

                /*if (mStepping)
                {
                    mCallback.OnStepComplete();
                    mStepping = false;
                }
                else
                {
                    mCallback.OnBreakpoint(mThread, new List<IDebugBoundBreakpoint2>());
                }*/
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

                            //** Send Exec Continue GDB command.
                            //                        engine.resetStackFrames();
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
                        //                        Debug.Assert(m_engine.m_state == AD7Engine.DE_STATE.RUN_MODE);
                        EventDispatcher.m_GDBRunMode = false;
                        m_engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                        // Found a bound breakpoint
                        m_engine.Callback.OnBreakpoint(m_engine.selectThread(threadID), xBoundBreakpoints.AsReadOnly());

                        if (bbp.m_isHitCountEqual)
                        {
                            ignoreHitCount(ID, int.MaxValue); // had to ignore the biggest number of times to keep the breakpoint enabled and to avoid stopping on it.
                        }
                        else if (bbp.m_hitCountMultiple != 0)
                        {
                            ignoreHitCount(ID, (int)(bbp.m_hitCountMultiple - (bbp.m_hitCount % bbp.m_hitCountMultiple)));
                        }
                    }
                    releaseCriticalRegion();
                    releaseBreakpoint(bbp, true, true);
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

                        //** Send Exec Continue GDB command.
                        //                    engine.resetStackFrames();
                        GDBParser.addGDBCommand(@"-exec-continue --thread-group i1");
                        EventDispatcher.m_GDBRunMode = true;
                        m_engine.m_running.Set();
                    }

                    releaseCriticalRegion();
                }
            }
        }

        // Returns the document context needed for showing the location of the current instruction pointer
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

        public int getStackDepth(string threadID)
        {
            return Convert.ToInt32(GDBParser.parseCommand(@"-stack-info-depth --thread " + threadID + " --frame 0", 9));
        }

        public string getStackFrames()
        {
            return GDBParser.parseCommand(@"-stack-list-frames", 10);
        }

        public string getVariablesForFrame(uint frameIndex, string threadID)
        {            
            return GDBParser.parseCommand(@"-stack-list-variables --thread " + threadID + " --frame " + frameIndex + " --simple-values", 11);
        }

        public string selectThread(string id)
        {
            return GDBParser.parseCommand(@"-thread-select " + id, 12);
        }

//        public string getThreadInfo()  //???? Who calls this???
//        {
//            return GDBParser.parseCommand(@"-thread-info", 12);
//        }

        public string createVar(string name, ref bool hasVsNdK_)
        {
            string response = GDBParser.parseCommand("-var-create " + name + " \"*\" " + name, 13);
            if (response == "ERROR")
            {
                response = GDBParser.parseCommand("-var-create VsNdK_" + name + " \"*\" " + name, 13);
                if (response != "ERROR")
                    hasVsNdK_ = true;
            }
            return response;
        }

        public string deleteVar(string name, bool hasVsNdK_)
        {
            string response;
            if (!hasVsNdK_)
                response = GDBParser.parseCommand("-var-delete " + name, 14);
            else
                response = GDBParser.parseCommand("-var-delete VsNdK_" + name, 14);
            return response;
        }

        public string listChildren(string name)
        {
            return GDBParser.parseCommand("-var-list-children --all-values " + name + " 0 50", 15); 
        }

        public string killProcess()
        {
            return GDBParser.parseCommand("kill", 16);
        }

        /// <summary>
        /// Called after the debug engine has set the initial breakpoints
        /// </summary>
        public void continueExecution()
        {
            //** Transition DE state
            //            Debug.Assert(m_engine.m_state == AD7Engine.DE_STATE.DESIGN_MODE || 
            //                         m_engine.m_state == AD7Engine.DE_STATE.BREAK_MODE || 
            //                         m_engine.m_state == AD7Engine.DE_STATE.STEP_MODE);
            bool hitBreakAll = m_engine.m_running.WaitOne(0);
            if (hitBreakAll)
            {
                m_engine.m_state = AD7Engine.DE_STATE.RUN_MODE;

                //** Send Exec Continue GDB command.
                //            engine.resetStackFrames();
                GDBParser.addGDBCommand(@"-exec-continue --thread-group i1");
                EventDispatcher.m_GDBRunMode = true;
                m_engine.m_running.Set();
            }
        }
    }

    public class HandleBreakpoints
    {
        private int m_number = -1;
        private bool m_enable = false;
        private string m_addr = "";
        private string m_func = "";
        private string m_filename = "";
        private int m_line = -1;
        private int m_hits = -1;
        private int m_ignoreHits = -1;
        private string m_condition = "";
        private string m_threadID = "";
        private EventDispatcher m_eventDispatcher = null;
        private AD7DocumentContext m_docContext;
        
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



        public HandleBreakpoints(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }

        public void handle(string ev)
        {
            int ini = 0;
            int end = 0;
            switch (ev[1])
            {
                case '0':  // breakpoint inserted (synchronous). Example: 20,1,y,0x0804d843,main,C:/Users/guarnold.RIMNET/vsplugin-ndk/samples/Square/Square/main.c,319,0       
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

                    break;
                case '1':  // breakpoint modified (asynchronous). Example: 21,1,y,0x0804d843,main,C:/Users/guarnold.RIMNET/vsplugin-ndk/samples/Square/Square/main.c,318,1
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

                    // Update hit count on affected bound breakpoint
                    EventDispatcher.m_updatedHitCount = true;
                    m_eventDispatcher.updateHitCount((uint)m_number, (uint)m_hits);

                    break;
                case '2':  // breakpoint deleted asynchronously (a temporary breakpoint). Exemple: 22,2\r\n
                    m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                    // Call the method/event that will delete the breakpoint in SDM here.

                    break;
                case '3':  // breakpoint enabled. Example: 23 (enabled all) or 23,1 (enabled only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints, or call the below method inside if/else instruction to remove this m_number = 0;.

                    // Call the method/event that will disable one breakpoint, or all of them, in SDM here.                                        

                    break;
                case '4':  // breakpoint disabled. Example: 24 (disabled all) or 24,1 (disabled only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints, or call the below method inside if/else instruction to remove this m_number = 0;.

                    // Call the method/event that will enable one breakpoint, or all of them, in SDM here.

                    break;
                case '5':  // breakpoint deleted. Example: 25 (deleted all) or 25,1 (deleted only breakpoint 1)
                    if (ev.Length > 2)
                        m_number = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));
                    else
                        m_number = 0;  // 0 means ALL breakpoints, or call the below method inside if/else instruction to remove this m_number = 0;.

                    // Call the method/event that will delete one breakpoint, or all of them, in SDM here.                    

                    break;
                case '6':  // break after "n" hits (or ignore n hits). Example: 26;1;100
                    ini = 3;
                    end = ev.IndexOf(';', 3);
                    m_number = Convert.ToInt32(ev.Substring(3, (end - 3)));

                    ini = end + 1;
                    m_ignoreHits = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                // Call the method/event that will add an "ignore n hits" for a given breakpoint in SDM here.

                    break;
                case '7':  // breakpoint hit. Example: 27,1,C:/Users/guarnold.RIMNET/vsplugin-ndk/samples/Square/Square/main.c,319;1\r\n
//                    EventDispatcher.m_GDBRunMode = false;  change to false only if it is going to enter in Break_Mode. See breakpointHit method that is called by the end of this case.
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
                        m_eventDispatcher.engine.selectThread(m_threadID).setCurrentLocation(m_filename, (uint)m_line);
                        m_eventDispatcher.engine.setAsCurrentThread(m_threadID);

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
                case '8':  // breakpoint condition set. Example: 28;1;expression
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
                    // Call the method/event that will set the condition for a given breakpoint in SDM here.
                    break;
                case '9':  // Error in testing breakpoint condition
                    break;
                default:   // not used.
                    break;
            }
        }
    }

    public class HandleProcessExecution
    {
        private int m_threadId = -1; // when threadId is 0, it means all threads.
        private int m_processId = -1;
        private int m_exitCode = -1;
        private string m_signalName = "";
        private string m_signalMeaning = "";
        private string m_file = "";
        private int m_line = -1;
        private int m_address = -1;
        private string m_func = "";
        private string m_error = "";
        private EventDispatcher m_eventDispatcher = null;
        private AD7DocumentContext m_docContext = null;
        public static bool m_needsResumeAfterInterrupt = false;
        public static ManualResetEvent m_mre = new ManualResetEvent(false);
        
        public HandleProcessExecution(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }

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
                        case '0':  // Thread created. Example: 40,2,20537438
                            EventDispatcher.m_GDBRunMode = true;
                            ini = 3;
                            end = ev.IndexOf(";", 3);
                            m_threadId = Convert.ToInt32(ev.Substring(ini, (end - ini)));

                            ini = end + 1;
                            m_processId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                            m_eventDispatcher.engine._updateThreads = true;

                            // Call the method/event that will update SDM that a new thread was created.

                            break;
                        case '1':  // Process running. Example: 41,1     (when threadId is 0 means "all threads": example: 41,0)
                            EventDispatcher.m_GDBRunMode = true;
                            m_threadId = Convert.ToInt32(ev.Substring(3, (ev.Length - 3)));

                            // Call the method/event that will let SDM know that the debugged program is running.

                            /*// Check if it was in stepping mode and call onStepCompleted (this could happen if it was in unknown code and a step-out was issued)
                            if (m_eventDispatcher.engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
                            {
                                m_eventDispatcher.engine.m_state = AD7Engine.DE_STATE.RUN_MODE;
                                //onStepCompleted(m_eventDispatcher, m_file, (uint)m_line);
                            }*/

                            break;
                        case '2':  // program exited normally. Example: 42
                            // Call the method/event that will let SDM know that the debugged program has exited normally.
                            m_eventDispatcher.endDebugSession(0);

                            break;
                        case '3':  // program was exited with an exit code. Example: 43,1;1 (not sure if there is a threadID, but the last ";" exist)   
                            // ??? not tested yet
                            // Call the method/event that will let SDM know that the debugged program exited with an exit-code.
                            end = ev.IndexOf(";", 3);
                            uint exitCode = Convert.ToUInt32(ev.Substring(3, (end - 3)));
                            m_eventDispatcher.endDebugSession(exitCode);

                            break;
                        case '4':  // program interrupted. 
                            // Examples:
                            // 44,ADDR,FUNC,THREAD-ID         
                            // 44,ADDR,FUNC,FILENAME,LINE,THREAD-ID
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
//                                if ((EventDispatcher.m_unknownCode == false) && (m_file != ""))
                                    m_eventDispatcher.engine.selectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.setAsCurrentThread(m_threadId.ToString());
                            }
                            
                            
                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            onInterrupt(m_threadId);

                            // Signal that interrupt is processed 
                            m_mre.Set();

                            break;

                        case '5':  // end-stepping-range   . Example: 
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
                                    m_eventDispatcher.engine.selectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.setAsCurrentThread(m_threadId.ToString());
                            }


                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            HandleProcessExecution.onStepCompleted(m_eventDispatcher, m_file, (uint)m_line);

                            break;
                        case '6':  // function-finished  . Example:      // ??? not tested yet
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
                                    m_eventDispatcher.engine.selectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.setAsCurrentThread(m_threadId.ToString());
                            }


                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            HandleProcessExecution.onStepCompleted(m_eventDispatcher, m_file, (uint)m_line);

                            break;
                        case '7':  // -exec-interrupt or signal-meaning="Killed". There's nothing to do in this case.
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
                                    m_eventDispatcher.engine.selectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.setAsCurrentThread(m_threadId.ToString());
                            }


                            if (m_eventDispatcher.engine.m_state != AD7Engine.DE_STATE.BREAK_MODE)
                            {
                                // Call the method/event that will let SDM know that the debugged program was interrupted.
                                onInterrupt(m_threadId);
                            }
                                // Signal that interrupt is processed 
                            m_mre.Set();

                            break;
                        case '8':  // SIGKILL
                            m_eventDispatcher.endDebugSession(0);
                            break;
                        case '9':  // ERROR, ex: 49,Cannot find bounds of current function
                            m_eventDispatcher.engine.resetStackFrames();
                            this.m_eventDispatcher.engine.cleanEvaluatedThreads();


                            if (m_eventDispatcher.engine._updateThreads)
                            {
                                m_eventDispatcher.engine.UpdateListOfThreads();
                            }


//                            EventDispatcher.m_GDBRunMode = false;
                            if (ev.Length >= 3)
                            {
                                m_error = ev.Substring(3, (ev.Length - 3));
                                if (m_error == "Cannot find bounds of current function")
                                {
                                    // We don't have symbols for this function so further stepping won't be possible. Return from this function.
                                    VSNDK.DebugEngine.EventDispatcher.m_unknownCode = true;
                                    m_eventDispatcher.engine.Step(m_eventDispatcher.engine.currentThread(), enum_STEPKIND.STEP_OUT, enum_STEPUNIT.STEP_LINE);
                                }
                            }
                            break;
                        default:   // not used.
                            break;
                    }
                    break;
                case '5':
                    switch (ev[1])
                    {
                        case '0':  // Quit (expect signal SIGINT when the program is resumed)
                            m_eventDispatcher.countSIGINT += 1;
                            if (m_eventDispatcher.countSIGINT > 5)
                            {
                                m_eventDispatcher.endDebugSession(0);
                                MessageBox.Show("Lost communication with GDB. Please refer to documentation for more details.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                //                                MessageBox.Show("This is a known issue that can happen when interrupting GDB's execution by hitting the \"break all\" or toggling a breakpoint in run mode. \n\n GDB failure when trying to interrupt the debugged program: \"Quit (expect signal SIGINT when the program is resumed)\" \r\n  \nPlease verify if the app is closed in the device/simulator to start debugging it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            break;
                        case '1':  // Thread exited. Example: 51,2
                            ini = 3;
                            m_threadId = Convert.ToInt32(ev.Substring(ini, (ev.Length - ini)));

                            m_eventDispatcher.engine._updateThreads = true;

                            break;
                        case '2':  // GDB Bugs, like "... 2374: internal-error: frame_cleanup_after_sniffer ...". Example: 52
                            m_eventDispatcher.endDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen when interrupting GDB's execution by hitting the \"break all\" or toggling a breakpoint in run mode. \n\n GDB CRASHED. Details: \"../../gdb/frame.c:2374: internal-error: frame_cleanup_after_sniffer: Assertion `frame->prologue_cache == NULL' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '3':  // Lost communication with device/simulator: ^error,msg="Remote communication error: No error."
                            MessageBox.Show("Lost communication with the device/simulator.", "Communication lost", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            m_eventDispatcher.endDebugSession(0);

                            break;
                        case '4':  // program interrupted: Segmentation Fault. 
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
                                //                                if ((EventDispatcher.m_unknownCode == false) && (m_file != ""))
                                m_eventDispatcher.engine.selectThread(m_threadId.ToString()).setCurrentLocation(m_file, (uint)m_line);
                                m_eventDispatcher.engine.setAsCurrentThread(m_threadId.ToString());
                            }

                            // Call the method/event that will let SDM know that the debugged program was interrupted.
                            onInterrupt(m_threadId);

                            break;

                        case '5':  // exited-signaled. Ex: 55;SIGSEGV;Segmentation fault
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

                            break;
                        case '6':  // GDB Bugs, like "... 3550: internal-error: handle_inferior_event ...". Example: 56
                            m_eventDispatcher.endDebugSession(0);
                            MessageBox.Show("This is a known issue that can happen while debugging multithreaded programs. \n\n GDB CRASHED. Details: \"../../gdb/infrun.c:3550: internal-error: handle_inferior_event: Assertion ptid_equal (singlestep_ptid, ecs->ptid)' failed.\nA problem internal to GDB has been detected,\nfurther debugging may prove unreliable.\" \r\n \nPlease close the app in the device/simulator if you want to debug it again.", "GDB failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            break;
                        case '7':  // not used
                            break;
                        case '8':  // not used
                            break;
                        case '9':  // not used
                            break;
                        default:   // not used.
                            break;
                    }
                    break;
                default:   // not used.
                    break;
            }
        }

        // HandleBreakpoints calls this too, so it needs to be public and static
        public static void onStepCompleted(EventDispatcher eventDispatcher, string file, uint line)
        {
            if (eventDispatcher.engine.m_state == AD7Engine.DE_STATE.STEP_MODE)
            {
                eventDispatcher.engine.m_state = AD7Engine.DE_STATE.BREAK_MODE;

                // Visual Studio shows the line position one more than it actually is
                eventDispatcher.engine.m_docContext = eventDispatcher.getDocumentContext(file, line - 1);
                AD7StepCompletedEvent.Send(eventDispatcher.engine);
                
//                EvaluatedExp.reset();
//                VariableInfo.reset();
//                 eventDispatcher.engine.resetStackFrames();
            }
        }

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
                m_eventDispatcher.engine.Callback.OnAsyncBreakComplete(m_eventDispatcher.engine.selectThread(threadID.ToString()));
            }

//            VariableInfo.reset();
//            m_eventDispatcher.engine.resetStackFrames();            
        }

    }

    public class HandleOutputs
    {
        private string m_stdOut = "";
        private string m_console = "";
        private EventDispatcher m_eventDispatcher = null;

        public HandleOutputs(EventDispatcher ed)
        {
            m_eventDispatcher = ed;
        }

        public void handle(string ev)
        {
            int ini = 0;
            int end = 0;
            switch (ev[1])
            {
                case '0':  // Display the m_console message in the VS output window. Example: 80,\"\"[New pid 15380494 tid 2]\\n\"\"!80
                    ini = 4;
                    end = ev.IndexOf("\"!80", 4);
                    m_console = ev.Substring(ini, (end - ini));

//                    var eventObject0 = new AD7OutputDebugStringEvent(m_console+"\r\n");
//                    m_eventDispatcher.engine.Callback.Send(eventObject0, AD7OutputDebugStringEvent.IID, null);

                // Call the method/event that will output this message in the VS output window.

                    break;
                case '1':  // Display the m_stdOut message in the VS standard output window. Instruction should look like this: 81,\"\" ... "\"!81
                    ini = 4;
                    end = ev.IndexOf("\"!81", 4);
                    m_stdOut = ev.Substring(ini, (end - ini));

//                    var eventObject1 = new AD7OutputDebugStringEvent(m_stdOut + "\r\n");
//                    m_eventDispatcher.engine.Callback.Send(eventObject1, AD7OutputDebugStringEvent.IID, null);                

                // Call the method/event that will output this message in the VS standar output window.

                    break;
                default:   // not used.
                    break;
            }
        }
    }

}
