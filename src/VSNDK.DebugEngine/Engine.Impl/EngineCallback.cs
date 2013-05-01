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
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace VSNDK.DebugEngine
{
    public class EngineCallback // : ISampleEngineCallback
    {
        readonly IDebugEventCallback2 m_ad7Callback;
        readonly AD7Engine m_engine;

        public EngineCallback(AD7Engine engine, IDebugEventCallback2 ad7Callback)
        {
            m_ad7Callback = ad7Callback;
            m_engine = engine;
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes; 
            Guid riidEvent = new Guid(iidEvent);

            EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));

            if ((thread == null) && (m_engine != null) && (m_engine.thread != null) && (program != null) && (eventObject != null) && (riidEvent != null) && (attributes != null))
            {
                if (m_engine._currentThreadIndex != -1)
                {
                    EngineUtils.RequireOk(m_ad7Callback.Event(m_engine, null, program, m_engine.thread[m_engine._currentThreadIndex], eventObject, ref riidEvent, attributes));
                }
                else
                {
                    if (m_engine.thread != null)
                        EngineUtils.RequireOk(m_ad7Callback.Event(m_engine, null, program, m_engine.thread[0], eventObject, ref riidEvent, attributes));
                    else
                        EngineUtils.RequireOk(m_ad7Callback.Event(m_engine, null, program, null, eventObject, ref riidEvent, attributes));
                }
            }
            else
                EngineUtils.RequireOk(m_ad7Callback.Event(m_engine, null, program, thread, eventObject, ref riidEvent, attributes));
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugThread2 thread)
        {
            Send(eventObject, iidEvent, m_engine, thread);
        }

        #region ISampleEngineCallback Members

        public void OnError(int hrErr)
        {
            // Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            // IDebugErrorEvent2 is used to report error messages to the user when something goes wrong in the debug engine.
            // The sample engine doesn't take advantage of this.
        }

        //public void OnModuleLoad(DebuggedModule debuggedModule)
        public void OnModuleLoad(AD7Module debuggedModule)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a mod-load event
            // for the exe.
            // if (m_engine.DebuggedProcess != null)
            // { 
                // Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            // }

            // AD7Module ad7Module = new AD7Module(debuggedModule);
            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(debuggedModule, true /* this is a module load */);

            // debuggedModule.Client = ad7Module;

            // The sample engine does not support binding breakpoints as modules load since the primary exe is the only module
            // symbols are loaded for. A production debugger will need to bind breakpoints when a new module is loaded.

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }

        public void OnModuleUnload()
        {
            
        }

        public void OnOutputString(string outputString)
        {
            // Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            AD7OutputDebugStringEvent eventObject = new AD7OutputDebugStringEvent(outputString);

            Send(eventObject, AD7OutputDebugStringEvent.IID, null);
        }

        public void OnProcessExit(uint exitCode)
        {
            // Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);

            Send(eventObject, AD7ProgramDestroyEvent.IID, null);
        }

        public void OnThreadExit()
        {

        }
        
        /*public void OnThreadStart(DebuggedThread debuggedThread)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a thread start event
            // for the main thread of the application.
            //if (m_engine.DebuggedProcess != null)
            //{
            //    Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            //}

            // AD7Thread ad7Thread = new AD7Thread(m_engine, debuggedThread);
            // debuggedThread.Client = ad7Thread;
            
            AD7ThreadCreateEvent eventObject = new AD7ThreadCreateEvent();
            // Send(eventObject, AD7ThreadCreateEvent.IID, ad7Thread);
            Send(eventObject, AD7ThreadCreateEvent.IID, debuggedThread);
        }*/

        public void OnBreakpoint(AD7Thread thread, IList<IDebugBoundBreakpoint2> clients)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[clients.Count];

            int i = 0;
            foreach (object objCurrentBreakpoint in clients)
            {
                boundBreakpoints[i] = (IDebugBoundBreakpoint2)objCurrentBreakpoint;
                i++;
            }

            // An engine that supports more advanced breakpoint features such as hit counts, conditions and filters
            // should notify each bound breakpoint that it has been hit and evaluate conditions here.
            // The sample engine does not support these features.

            AD7BoundBreakpointsEnum boundBreakpointsEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            AD7BreakpointEvent eventObject = new AD7BreakpointEvent(boundBreakpointsEnum);            

            Send(eventObject, AD7BreakpointEvent.IID, thread);
        }

        public void OnException()
        {
            // Exception events are sent when an exception occurs in the debuggee that the debugger was not expecting.
            // The sample engine does not support these.
            throw new Exception("The method or operation is not implemented.");
        }

        public void OnStepComplete()
        {
            // Step complete is sent when a step has finished. The sample engine does not support stepping.
            throw new Exception("The method or operation is not implemented.");
//            AD7StepCompletedEvent.Send(m_engine);
            // ??? implement this method...
        }

        public void OnAsyncBreakComplete(AD7Thread thread)
        {
            // This will get called when the engine receives the breakpoint event that is created when the user
            // hits the pause button in vs.
            // Debug.Assert(GDBParser.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            
            AD7AsyncBreakCompleteEvent eventObject = new AD7AsyncBreakCompleteEvent();
            Send(eventObject, AD7AsyncBreakCompleteEvent.IID, thread);
        }

        public void OnLoadComplete(AD7Thread thread)
        {            
            AD7LoadCompleteEvent eventObject = new AD7LoadCompleteEvent();
            Send(eventObject, AD7LoadCompleteEvent.IID, thread);
        }

        public void OnProgramDestroy(uint exitCode)
        {                     
            AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);
            Send(eventObject, AD7ProgramDestroyEvent.IID, null);
        }

        // Engines notify the debugger about the results of a symbol serach by sending an instance
        // of IDebugSymbolSearchEvent2
        //public void OnSymbolSearch(DebuggedModule module, string status, uint dwStatusFlags)
        public void OnSymbolSearch(AD7Module module, string status, uint dwStatusFlags)
        {
            string statusString = (dwStatusFlags == 1 ? "Symbols Loaded - " : "No symbols loaded") + status;

            //AD7Module ad7Module = new AD7Module(module);
            //AD7SymbolSearchEvent eventObject = new AD7SymbolSearchEvent(ad7Module, statusString, dwStatusFlags);
            AD7SymbolSearchEvent eventObject = new AD7SymbolSearchEvent(module, statusString, dwStatusFlags);
            Send(eventObject, AD7SymbolSearchEvent.IID, null);
        }

        // Engines notify the debugger that a breakpoint has bound through the breakpoint bound event.
        public void OnBreakpointBound(object objBoundBreakpoint, uint address)
        {
            AD7BoundBreakpoint boundBreakpoint = (AD7BoundBreakpoint)objBoundBreakpoint;
            IDebugPendingBreakpoint2 pendingBreakpoint;
            ((IDebugBoundBreakpoint2)boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);

            AD7BreakpointBoundEvent eventObject = new AD7BreakpointBoundEvent((AD7PendingBreakpoint)pendingBreakpoint, boundBreakpoint);
            Send(eventObject, AD7BreakpointBoundEvent.IID, null);
        }

        public void OnThreadStart(AD7Thread debuggedThread)
        {
            AD7ThreadCreateEvent eventObject = new AD7ThreadCreateEvent();
            Send(eventObject, AD7ThreadCreateEvent.IID, debuggedThread);
        }

        #endregion
    }
}
