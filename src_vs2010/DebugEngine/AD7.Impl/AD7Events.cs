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

// This file contains the various event objects that are sent to the debugger from the VSNDK debug engine via IDebugEventCallback2::Event.
// These are used in EngineCallback.cs.
// The events are how the engine tells the debugger about what is happening in the debuggee process. 
// There are four base classes the other events derive from: AD7AsynchronousEvent, AD7StoppingEvent, AD7SynchronousEvent and 
// AD7SynchronousStoppingEvent. Each of them implements the IDebugEvent2.GetAttributes method for the type of event they represent. 
// Most events sent by the debugger are asynchronous ones.

namespace BlackBerry.DebugEngine
{
    #region Event base classes


    /// <summary>
    /// Used to communicate both critical debug information and non-critical information.
    /// (http://msdn.microsoft.com/en-us/library/bb161977.aspx)
    /// </summary>
    class AD7AsynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;


        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// Used to communicate both critical debug information and non-critical information.
    /// (http://msdn.microsoft.com/en-us/library/bb161977.aspx)
    /// </summary>
    class AD7StoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNC_STOP;


        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// Used to communicate both critical debug information and non-critical information.
    /// (http://msdn.microsoft.com/en-us/library/bb161977.aspx)
    /// </summary>
    class AD7SynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;


        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// Used to communicate both critical debug information and non-critical information.
    /// (http://msdn.microsoft.com/en-us/library/bb161977.aspx)
    /// </summary>
    class AD7SynchronousStoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_STOPPING | (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;


        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }
    
    #endregion


    /// <summary>
    /// The debug engine (DE) sends this interface to the session debug manager (SDM) when an instance of the DE is created.
    /// (http://msdn.microsoft.com/en-us/library/bb145830.aspx)
    /// </summary>
    sealed class AD7EngineCreateEvent : AD7AsynchronousEvent, IDebugEngineCreateEvent2
    {
        public const string IID = "FE5B734C-759D-4E59-AB04-F103343BDD06";
        private IDebugEngine2 m_engine;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        AD7EngineCreateEvent(AD7Engine engine)
        {
            m_engine = engine;
        }


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public static void Send(AD7Engine engine)
        {
            AD7EngineCreateEvent eventObject = new AD7EngineCreateEvent(engine);
            engine.Callback.Send(eventObject, IID, null, null);
        }
        

        /// <summary>
        /// Retrieves the object that represents the newly created debug engine (DE). 
        /// (http://msdn.microsoft.com/en-us/library/bb145143.aspx)
        /// </summary>
        /// <param name="engine"> Returns an AD7Engine object that represents the newly created DE. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 engine)
        {
            engine = m_engine;
            
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// This interface is sent when a process is launched. (http://msdn.microsoft.com/en-ca/library/bb161755.aspx)
    /// </summary>
    class AD7ProcessCreateEvent : IDebugEvent2, IDebugProcessCreateEvent2
    {
        private Guid IID = new Guid("9020DEE3-362D-4FF2-8CA9-8F6791F0EC85");
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_IMMEDIATE;

        /// <summary>
        /// Gets the GUID of this event. 
        /// </summary>
        /// <returns> Returns the GUID of this event. </returns>
        public Guid getGuid()
        {
            return IID;
        }

        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// This interface is sent when a process is terminated, exits atypically, or is detached from.
    /// (http://msdn.microsoft.com/en-us/library/bb145152.aspx)
    /// </summary>
    class AD7ProcessDestroyEvent : IDebugEvent2, IDebugProcessDestroyEvent2
    {
        private Guid IID = new Guid("29DAA0AC-C718-4F93-A11E-6D15681476C7");
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_IMMEDIATE;

        /// <summary>
        /// Gets the GUID of this event. 
        /// </summary>
        /// <returns> Returns the GUID of this event. </returns>
        public Guid getGuid()
        {
            return IID;
        }

        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="eventAttributes"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is attached to.
    /// (http://msdn.microsoft.com/en-ca/library/bb161345.aspx)
    /// </summary>
    sealed class AD7ProgramCreateEvent : AD7AsynchronousEvent, IDebugProgramCreateEvent2
    {
        public const string IID = "96CD11EE-ECD4-4E89-957E-B5D496FC4139";
        
        
        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        internal static void Send(AD7Engine engine)
        {
            AD7ProgramCreateEvent eventObject = new AD7ProgramCreateEvent();
            engine.Callback.Send(eventObject, IID, null);
        }
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when asynchronous expression evaluation 
    /// is complete. (http://msdn.microsoft.com/en-ca/library/bb161810.aspx)
    /// </summary>
    sealed class AD7ExpressionEvaluationCompleteEvent : AD7AsynchronousEvent, IDebugExpressionEvaluationCompleteEvent2 
    {
        public const string IID = "C0E13A85-238A-4800-8315-D947C960A843";
        private readonly IDebugExpression2 m_expression;
        private readonly IDebugProperty2 m_property;
 

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="expression"> The IDebugExpression2 object that represents the expression. </param>
        /// <param name="property"> The IDebugProperty2 object that represents the result of the expression evaluation. </param>
        public AD7ExpressionEvaluationCompleteEvent(IDebugExpression2 expression, IDebugProperty2 property) 
        {
            this.m_expression = expression;
            this.m_property = property;
        }
 

        /// <summary>
        /// Gets the original expression. (http://msdn.microsoft.com/en-ca/library/bb162323.aspx)
        /// </summary>
        /// <param name="ppExpr"> Returns an IDebugExpression2 object that represents the expression that was parsed. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetExpression(out IDebugExpression2 ppExpr) 
        {
            ppExpr = m_expression;
            return VSConstants.S_OK;
        }
 

        /// <summary>
        /// Gets the result of expression evaluation. (http://msdn.microsoft.com/en-ca/library/bb161962.aspx)
        /// </summary>
        /// <param name="ppResult"> Returns an IDebugProperty2 object that represents the result of the expression evaluation. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetResult(out IDebugProperty2 ppResult) 
        {
            ppResult = m_property;
            return VSConstants.S_OK;
        }
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a module is loaded or unloaded.
    /// (http://msdn.microsoft.com/en-ca/library/bb146706.aspx)
    /// </summary>
    sealed class AD7ModuleLoadEvent : AD7AsynchronousEvent, IDebugModuleLoadEvent2
    {
        public const string IID = "989DB083-0D7C-40D1-A9D9-921BF611A4B2";
        
        readonly AD7Module m_module;
        readonly bool m_fLoad;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="module"> The IDebugModule2 object that represents the module which is loading or unloading. </param>
        /// <param name="fLoad"> onzero (TRUE) if the module is loading and zero (FALSE) if the module is unloading. </param>
        public AD7ModuleLoadEvent(AD7Module module, bool fLoad)
        {
            m_module = module;
            m_fLoad = fLoad;
        }


        /// <summary>
        /// Gets the module that is being loaded or unloaded. (http://msdn.microsoft.com/en-ca/library/bb161763.aspx)
        /// </summary>
        /// <param name="module"> Returns an IDebugModule2 object that represents the module which is loading or unloading. </param>
        /// <param name="debugMessage"> Returns an optional message describing this event. If this parameter is a null value, no message 
        /// is requested. </param>
        /// <param name="fIsLoad"> Nonzero (TRUE) if the module is loading and zero (FALSE) if the module is unloading. If this 
        /// parameter is a null value, no status is requested. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugModuleLoadEvent2.GetModule(out IDebugModule2 module, ref string debugMessage, ref int fIsLoad)
        {
            module = m_module;

            if (m_fLoad)
            {
                fIsLoad = 1;
            }
            else
            {
                fIsLoad = 0;
            }

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="aModule"> The IDebugModule2 object that represents the module which is loading or unloading. </param>
        /// <param name="fLoad"> onzero (TRUE) if the module is loading and zero (FALSE) if the module is unloading. </param>
        internal static void Send(AD7Engine engine, AD7Module aModule, bool fLoad)
        {
            var eventObject = new AD7ModuleLoadEvent(aModule, fLoad);
            engine.Callback.Send(eventObject, IID, null);
        }
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program has run to completion
    /// or is otherwise destroyed. (http://msdn.microsoft.com/en-ca/library/bb161972.aspx)
    /// </summary>
    sealed class AD7ProgramDestroyEvent : AD7SynchronousEvent, IDebugProgramDestroyEvent2
    {
        public const string IID = "E147E9E3-6440-4073-A7B7-A65592C714B5";

        readonly uint m_exitCode;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exitCode"> The program's exit code. </param>
        public AD7ProgramDestroyEvent(uint exitCode)
        {
            m_exitCode = exitCode;
        }

        #region IDebugProgramDestroyEvent2 Members


        /// <summary>
        /// Gets the program's exit code. (http://msdn.microsoft.com/en-ca/library/bb146724.aspx)
        /// </summary>
        /// <param name="exitCode"> Returns the program's exit code. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = m_exitCode;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="exitCode"> The program's exit code. </param>
        internal static void Send(AD7Engine engine, uint exitCode)
        {
            var eventObject = new AD7ProgramDestroyEvent(exitCode);
            engine.Callback.Send(eventObject, IID, null);
        }

        #endregion
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program 
    /// being debugged. (http://msdn.microsoft.com/en-ca/library/bb161327.aspx)
    /// </summary>
    sealed class AD7ThreadCreateEvent : AD7AsynchronousEvent, IDebugThreadCreateEvent2
    {
        public const string IID = "2090CCFC-70C5-491D-A5E8-BAD2DD9EE3EA";
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread has run to completion.
    /// (http://msdn.microsoft.com/en-ca/library/bb162330.aspx)
    /// </summary>
    sealed class AD7ThreadDestroyEvent : AD7AsynchronousEvent, IDebugThreadDestroyEvent2
    {
        public const string IID = "2C3B7532-A36F-4A6E-9072-49BE649B8541";

        readonly uint m_exitCode;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exitCode"> The thread's exit code. </param>
        public AD7ThreadDestroyEvent(uint exitCode)
        {
            m_exitCode = exitCode;
        }

        #region IDebugThreadDestroyEvent2 Members


        /// <summary>
        /// Gets the exit code for a thread. (http://msdn.microsoft.com/en-ca/library/bb146996.aspx)
        /// </summary>
        /// <param name="exitCode"> Returns the thread's exit code. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThreadDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = m_exitCode;
            
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="exitCode"> The thread's exit code. </param>
        /// <param name="thread"> The AD7Thread object that represents the thread. </param>
        internal static void Send(AD7Engine engine, uint exitCode, AD7Thread thread)
        {
            var eventObject = new AD7ThreadDestroyEvent(exitCode);
            if (thread == null)
            {
                foreach (AD7Thread t in engine.thread)
                {
                    engine.Callback.Send(eventObject, IID, t);
                }
                engine._currentThreadIndex = -1;
            }
            else
            {
                engine.Callback.Send(eventObject, IID, thread);
            }
        }

        #endregion
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is loaded, but before 
    /// any code is executed. (http://msdn.microsoft.com/en-ca/library/bb145834.aspx)
    /// </summary>
    sealed class AD7LoadCompleteEvent : AD7StoppingEvent, IDebugLoadCompleteEvent2
    {
        public const string IID = "B1844850-1349-45D4-9F12-495212F5EB0B";

        
        /// <summary>
        /// Constructor.
        /// </summary>
        public AD7LoadCompleteEvent()
        {
        }


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="aEngine"> The AD7Engine object that represents the DE. </param>
        /// <param name="aThread"> The AD7Thread object that represents the thread. </param>
        internal static void Send(AD7Engine aEngine, AD7Thread aThread)
        {
            var xMessage = new AD7LoadCompleteEvent();
            aEngine.Callback.Send(xMessage, IID, aThread);
        }
    }


    /// <summary>
    /// This interface tells the session debug manager (SDM) that an asynchronous break has been successfully completed.
    /// (http://msdn.microsoft.com/en-ca/library/bb146180.aspx)
    /// </summary>
    sealed class AD7AsyncBreakCompleteEvent : AD7StoppingEvent, IDebugBreakEvent2
    {
        public const string IID = "c7405d1d-e24b-44e0-b707-d8a5a4e1641b";
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) to output a string.
    /// (http://msdn.microsoft.com/en-ca/library/bb146756.aspx)
    /// </summary>
    sealed class AD7OutputDebugStringEvent : AD7AsynchronousEvent, IDebugOutputStringEvent2  
    {
        public const string IID = "569c4bb1-7b82-46fc-ae28-4536ddad753e";

        private string m_str;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="str"> The displayable message. </param>
        public AD7OutputDebugStringEvent(string str)
        {
            m_str = str;
        }

        #region IDebugOutputStringEvent2 Members

        /// <summary>
        /// Gets the displayable message. (http://msdn.microsoft.com/en-ca/library/bb162293.aspx)
        /// </summary>
        /// <param name="pbstrString"> Returns the displayable message. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugOutputStringEvent2.GetString(out string pbstrString)
        {
            pbstrString = m_str;
            return VSConstants.S_OK;
        }

        #endregion
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to indicate that the debugging symbols for a module being debugged have 
    /// been loaded. (http://msdn.microsoft.com/en-ca/library/bb160924.aspx)
    /// </summary>
    sealed class AD7SymbolSearchEvent : AD7AsynchronousEvent, IDebugSymbolSearchEvent2
    {
        public const string IID = "638F7C54-C160-4c7b-B2D0-E0337BC61F8C";

        private AD7Module m_module;
        private string m_searchInfo;
        private uint m_symbolFlags;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="module"> The AD7Module object representing the module for which the symbols were loaded. </param>
        /// <param name="searchInfo"> The string containing any error messages from the module. </param>
        /// <param name="symbolFlags"> A combination of flags from the MODULE_INFO_FLAGS enumeration indicating whether any 
        /// symbols were loaded. </param>
        public AD7SymbolSearchEvent(AD7Module module, string searchInfo, uint symbolFlags)
        {
            m_module = module;
            m_searchInfo = searchInfo;
            m_symbolFlags = symbolFlags;
        }

        #region IDebugSymbolSearchEvent2 Members

        /// <summary>
        /// Called by an event handler to retrieve results about a symbol load process. 
        /// (http://msdn.microsoft.com/en-ca/library/bb161324.aspx)
        /// </summary>
        /// <param name="pModule"> An IDebugModule3 object representing the module for which the symbols were loaded. </param>
        /// <param name="pbstrDebugMessage"> Returns a string containing any error messages from the module. If there is no error, 
        /// then this string will just contain the module's name but it is never empty. </param>
        /// <param name="pdwModuleInfoFlags"> A combination of flags from the MODULE_INFO_FLAGS enumeration indicating whether any 
        /// symbols were loaded. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugSymbolSearchEvent2.GetSymbolSearchInfo(out IDebugModule3 pModule, ref string pbstrDebugMessage, enum_MODULE_INFO_FLAGS[] pdwModuleInfoFlags)
        {
            pModule = m_module;
            pbstrDebugMessage = m_searchInfo;
            pdwModuleInfoFlags[0] = (enum_MODULE_INFO_FLAGS)m_symbolFlags;

            return VSConstants.S_OK;
        }

        #endregion
    }
    

    /// <summary>
    /// This interface tells the session debug manager (SDM) that a pending breakpoint has been successfully bound to a loaded program.
    /// (http://msdn.microsoft.com/en-us/library/bb145356.aspx)
    /// </summary>
    sealed class AD7BreakpointBoundEvent : AD7AsynchronousEvent, IDebugBreakpointBoundEvent2
    {
        public const string IID = "1dddb704-cf99-4b8a-b746-dabb01dd13a0";

        private AD7PendingBreakpoint m_pendingBreakpoint;
        private AD7BoundBreakpoint m_boundBreakpoint;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pendingBreakpoint"> The AD7PendingBreakpoint object that represents the pending breakpoint being bound. </param>
        /// <param name="boundBreakpoint"> The AD7BoundBreakpoint object that represents the breakpoint being bound. </param>
        public AD7BreakpointBoundEvent(AD7PendingBreakpoint pendingBreakpoint, AD7BoundBreakpoint boundBreakpoint)
        {
            m_pendingBreakpoint = pendingBreakpoint;
            m_boundBreakpoint = boundBreakpoint;
        }

        #region IDebugBreakpointBoundEvent2 Members

        /// <summary>
        /// Creates an enumerator of breakpoints that were bound on this event. (http://msdn.microsoft.com/en-us/library/bb145322.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugBoundBreakpoints2 object that enumerates all the breakpoints bound from 
        /// this event. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[1];
            boundBreakpoints[0] = m_boundBreakpoint;
            ppEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the pending breakpoint that is being bound. (http://msdn.microsoft.com/en-us/library/bb146558.aspx)
        /// </summary>
        /// <param name="ppPendingBP"> Returns the IDebugPendingBreakpoint2 object that represents the pending breakpoint being 
        /// bound. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = m_pendingBreakpoint;
            return VSConstants.S_OK;
        }

        #endregion
    }


    /// <summary>
    /// The debug engine (DE) sends this interface to the session debug manager (SDM) when a program stops at a breakpoint.
    /// (http://msdn.microsoft.com/en-us/library/bb145927.aspx)
    /// </summary>
    sealed class AD7BreakpointEvent : AD7StoppingEvent, IDebugBreakpointEvent2
    {
        public const string IID = "501C1E21-C557-48B8-BA30-A1EAB0BC4A74";

        IEnumDebugBoundBreakpoints2 m_boundBreakpoints;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boundBreakpoints"> The IEnumDebugBoundBreakpoints2 object that enumerates all the breakpoints associated with 
        /// the current code location. </param>
        public AD7BreakpointEvent(IEnumDebugBoundBreakpoints2 boundBreakpoints)
        {
            m_boundBreakpoints = boundBreakpoints;
        }

        #region IDebugBreakpointEvent2 Members

        /// <summary>
        /// Creates an enumerator for all the breakpoints that fired at the current code location. 
        /// (http://msdn.microsoft.com/en-us/library/bb146247.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugBoundBreakpoints2 object that enumerates all the breakpoints associated with 
        /// the current code location. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugBreakpointEvent2.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = m_boundBreakpoints;
            return VSConstants.S_OK;
        }

        #endregion
    }


    /// <summary>
    /// This interface is sent by the debug engine (DE) to the session debug manager (SDM) when the program being debugged completes 
    /// a step into, a step over, or a step out of a line of source code or statement or instruction. 
    /// (http://msdn.microsoft.com/en-us/library/bb162189.aspx)
    /// </summary>
    sealed class AD7StepCompletedEvent : IDebugEvent2, IDebugStepCompleteEvent2
    {
        public const string IID = "0F7F24C1-74D9-4EA6-A3EA-7EDB2D81441D";

        
        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public static void Send(AD7Engine engine)
        {
            var xEvent = new AD7StepCompletedEvent();
            engine.Callback.Send(xEvent, IID, engine.CurrentThread());
        }

        #region IDebugEvent2 Members

        /// <summary>
        /// Gets the attributes for this debug event. (http://msdn.microsoft.com/en-us/library/bb145575.aspx)
        /// </summary>
        /// <param name="pdwAttrib"> A combination of flags from the enum_EVENTATTRIBUTES enumeration. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetAttributes(out uint pdwAttrib)
        {
            pdwAttrib = (uint)(enum_EVENTATTRIBUTES.EVENT_ASYNC_STOP);
            return VSConstants.S_OK;
        }

        #endregion
    }


    /// <summary>
    /// The debug engine (DE) sends this interface to the session debug manager (SDM) when the program is about to execute its 
    /// first instruction of user code. (http://msdn.microsoft.com/en-us/library/bb161265.aspx)
    /// </summary>
    sealed class AD7EntryPointEvent : AD7SynchronousStoppingEvent, IDebugEntryPointEvent2
    {
        public const string IID = "86D5A99E-C721-4625-A401-4D052DF38475";


        /// <summary>
        /// Sends the event.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        public static void Send(AD7Engine engine)
        {
            var xEvent = new AD7EntryPointEvent();
            engine.Callback.Send(xEvent, IID, engine.CurrentThread());
        }
    }
}
