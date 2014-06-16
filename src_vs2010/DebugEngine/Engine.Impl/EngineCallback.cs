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
    /// <summary>
    /// Used to send events to the debugger. Some examples of these events are thread create, exception thrown, module load.
    /// </summary>
    public class EngineCallback
    {

        /// <summary>
        ///  The IDebugEventCallback2 object that receives debugger events. 
        /// </summary>
        readonly IDebugEventCallback2 m_ad7Callback;

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        readonly AD7Engine m_engine;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="ad7Callback"> The IDebugEventCallback2 object that receives debugger events. </param>
        public EngineCallback(AD7Engine engine, IDebugEventCallback2 ad7Callback)
        {
            m_ad7Callback = ad7Callback;
            m_engine = engine;
        }


        /// <summary>
        /// Send events to the debugger.
        /// </summary>
        /// <param name="eventObject"> Event object to be sent to the debugger. </param>
        /// <param name="iidEvent"> ID of the event. </param>
        /// <param name="program"> A program that is running in a process. </param>
        /// <param name="thread"> A thread running in a program. </param>
        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes; 
            Guid riidEvent = new Guid(iidEvent);

            EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));

            if ((thread == null) && (m_engine != null) && (m_engine.thread != null) && (program != null) && (eventObject != null) && (riidEvent != null) && (attributes != 0))
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


        /// <summary>
        /// Call the method that will send the event to the debugger with all the arguments.
        /// </summary>
        /// <param name="eventObject"> Event object to be sent to the debugger. </param>
        /// <param name="iidEvent"> ID of the event. </param>
        /// <param name="thread"> A thread running in a program. </param>
        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugThread2 thread)
        {
            Send(eventObject, iidEvent, m_engine, thread);
        }


        /// <summary>
        /// IDebugErrorEvent2 is used to report error messages to the user when something goes wrong in the debug engine.
        /// The VSNDK debug engine doesn't take advantage of this. Not implemented.
        /// </summary>
        /// <param name="hrErr"></param>
        public void OnError(int hrErr)
        {
        }


        /// <summary>
        /// The VSNDK debug engine does not support binding breakpoints as modules load since the primary exe is the only module
        /// symbols are loaded for. A production debugger will need to bind breakpoints when a new module is loaded.
        /// </summary>
        /// <param name="debuggedModule"> A module loaded in the debugged process. </param>
        public void OnModuleLoad(AD7Module debuggedModule)
        {
            // AD7Module ad7Module = new AD7Module(debuggedModule);
            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(debuggedModule, true /* this is a module load */);

            // debuggedModule.Client = ad7Module;

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }


        /// <summary>
        /// Send to the session debug manager (SDM) an event to output a string.
        /// Not used.
        /// </summary>
        /// <param name="outputString"> The output string. </param>
        public void OnOutputString(string outputString)
        {
            AD7OutputDebugStringEvent eventObject = new AD7OutputDebugStringEvent(outputString);

            Send(eventObject, AD7OutputDebugStringEvent.IID, null);
        }


        /// <summary>
        /// Send an event to SDM with the breakpoint that was hit.
        /// </summary>
        /// <param name="thread"> The thread running in a program. </param>
        /// <param name="clients"> List of bound breakpoints. At this moment, this list has only one element. </param>
        public void OnBreakpoint(AD7Thread thread, IList<IDebugBoundBreakpoint2> clients)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[clients.Count];

            int i = 0;
            foreach (object objCurrentBreakpoint in clients)
            {
                boundBreakpoints[i] = (IDebugBoundBreakpoint2)objCurrentBreakpoint;
                i++;
            }

            AD7BoundBreakpointsEnum boundBreakpointsEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            AD7BreakpointEvent eventObject = new AD7BreakpointEvent(boundBreakpointsEnum);            

            Send(eventObject, AD7BreakpointEvent.IID, thread);
        }


        /// <summary>
        // Exception events are sent when an exception occurs in the debugged that the debugger was not expecting.
        // The VSNDK debug engine does not support these. Not implemented.
        /// </summary>
        public void OnException()
        {
            throw new Exception("The method or operation is not implemented.");
        }


        /// <summary>
        /// Step complete is sent when a step has finished. Not implemented.
        /// </summary>
        public void OnStepComplete()
        {
            throw new Exception("The method or operation is not implemented.");
//            AD7StepCompletedEvent.Send(m_engine);
            // TODO: implement this method...
        }


        /// <summary>
        /// This will get called when the engine receives the breakpoint event that is created when the user
        /// hits the pause button in VS.
        /// </summary>
        /// <param name="thread"> The thread running in a program. </param>
        public void OnAsyncBreakComplete(AD7Thread thread)
        {
            AD7AsyncBreakCompleteEvent eventObject = new AD7AsyncBreakCompleteEvent();
            Send(eventObject, AD7AsyncBreakCompleteEvent.IID, thread);
        }


        /// <summary>
        /// Engines notify the debugger about the results of a symbol search by sending an instance of IDebugSymbolSearchEvent2.
        /// Not used.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="status"></param>
        /// <param name="dwStatusFlags"></param>
        public void OnSymbolSearch(AD7Module module, string status, uint dwStatusFlags)
        {
            string statusString = (dwStatusFlags == 1 ? "Symbols Loaded - " : "No symbols loaded") + status;

            AD7SymbolSearchEvent eventObject = new AD7SymbolSearchEvent(module, statusString, dwStatusFlags);
            Send(eventObject, AD7SymbolSearchEvent.IID, null);
        }


        /// <summary>
        /// Engines notify the debugger that a breakpoint has bound through the breakpoint bound event.
        /// </summary>
        /// <param name="objBoundBreakpoint"> The bounded breakpoint. </param>
        /// <param name="address"> 0. </param>
        public void OnBreakpointBound(object objBoundBreakpoint, uint address)
        {
            AD7BoundBreakpoint boundBreakpoint = (AD7BoundBreakpoint)objBoundBreakpoint;
            IDebugPendingBreakpoint2 pendingBreakpoint;
            ((IDebugBoundBreakpoint2)boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);

            AD7BreakpointBoundEvent eventObject = new AD7BreakpointBoundEvent((AD7PendingBreakpoint)pendingBreakpoint, boundBreakpoint);
            Send(eventObject, AD7BreakpointBoundEvent.IID, null);
        }


        /// <summary>
        /// Send an event to notify the SDM that this thread was created.
        /// </summary>
        /// <param name="debuggedThread"> The new thread running in a program. </param>
        public void OnThreadStart(AD7Thread debuggedThread)
        {
            AD7ThreadCreateEvent eventObject = new AD7ThreadCreateEvent();
            Send(eventObject, AD7ThreadCreateEvent.IID, debuggedThread);
        }
    }
}
