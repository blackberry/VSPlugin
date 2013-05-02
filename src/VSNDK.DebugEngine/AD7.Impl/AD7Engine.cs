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
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading;
using VSNDK.Parser;
using VSNDK.AddIn;

using NameValueCollection = System.Collections.Specialized.NameValueCollection;
using NameValueCollectionHelper = VSNDK.AddIn.NameValueCollectionHelper;
using System.Collections;

namespace VSNDK.DebugEngine
{
    // AD7Engine is the primary entrypoint object for the sample engine. 
    //
    // It implements:
    //
    // IDebugEngine2: This interface represents a debug engine (DE). It is used to manage various aspects of a debugging session, 
    // from creating breakpoints to setting and clearing exceptions.
    //
    // IDebugEngineLaunch2: Used by a debug engine (DE) to launch and terminate programs.
    //
    // IDebugProgram3: This interface represents a program that is running in a process. Since this engine only debugs one process at a time and each 
    // process only contains one program, it is implemented on the engine.
    //
    // IDebugEngineProgram2: This interface provides simultanious debugging of multiple threads in a debuggee.
    
    [ComVisible(true)]
    [Guid("904AA6E0-942C-4D11-9094-7BAAEB3EE4B9")]
    public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3, IDebugEngineProgram2, IDebugSymbolSettings100
    {
        // used to send events to the debugger. Some examples of these events are thread create, exception thrown, module load.
        EngineCallback m_engineCallback = null;
        internal EngineCallback Callback
        {
            get { return m_engineCallback; }
        }

        // The sample debug engine is split into two parts: a managed front-end and a mixed-mode back end. DebuggedProcess is the primary
        // object in the back-end. AD7Engine holds a reference to it.
        // DebuggedProcess m_debuggedProcess;

        // This object facilitates calling from this thread into the worker thread of the engine. This is necessary because the Win32 debugging
        // api requires thread affinity to several operations.
        // WorkerThread m_pollThread;

        // This object manages breakpoints in the sample engine.
        protected BreakpointManager m_breakpointManager = null;
        public BreakpointManager BPMgr
        {
            get { return m_breakpointManager; }
        }

        // This object manages debug events in the engine.
        protected EventDispatcher m_eventDispatcher = null;
        public EventDispatcher eDispatcher
        {
            get { return m_eventDispatcher; }
        }
        public bool m_isStepping { get; set; }

        // A unique identifier for the program being debugged.
        Guid m_programGUID;

        internal AD7Module m_module = null;
        public AD7Process m_process = null;
        public AD7ProgramNode m_progNode = null;
        internal IDebugProgram2 m_program = null;
        public bool _updateThreads = false;
        private AD7Thread[] m_threads = null;
        public AD7Thread[] thread
        {
            get { return m_threads; }
        }
//        public string previouslyEvaluatedThreadID = "";

        public int _currentThreadIndex = -1;
        // Allows IDE to show current position in a source file
        public AD7DocumentContext m_docContext = null;

        public ManualResetEvent m_running = new ManualResetEvent(true); // used to avoid race condition when there are conditional breakpoints and the user hit break all.

        public ManualResetEvent m_updatingConditionalBreakpoint = new ManualResetEvent(true); // used to avoid race condition when there are conditional breakpoints and a breakpoint is hit.

        /// <summary>
        /// This is the engine GUID of the sample engine. It needs to be changed here and in the registration
        /// when creating a new engine.
        /// </summary>
        public const string Id = "{E5A37609-2F43-4830-AA85-D94CFA035DD2}";

        // Keeps track of debug engine states
        public enum DE_STATE
        {                
            DESIGN_MODE = 0,
            RUN_MODE,
            BREAK_MODE,
            STEP_MODE,
            DONE
        }
        public DE_STATE m_state = 0;

        public AD7Engine()
        {
            m_breakpointManager = new BreakpointManager(this);            
            m_state = DE_STATE.DESIGN_MODE;
            //GDBParser.Initialize();
        }

        ~AD7Engine()
        {
            // Transition DE state
            m_state = AD7Engine.DE_STATE.DONE;
        }

        public string GetAddressDescription(uint ip)
        {
            return "";
        }

        #region IDebugEngine2 Members

        // Attach the debug engine to a program. 
        int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint aCeltPrograms, IDebugEventCallback2 ad7Callback, enum_ATTACH_REASON dwReason)
        {
            // Attach the debug engine to a program. 
            //
            // Attach can either be called to attach to a new process, or to complete an attach
            // to a launched process.
            // So could we simplify and move code from LaunchSuspended to here and maybe even 
            // eliminate the debughost? Although I supposed DebugHost has some other uses as well.

            if (aCeltPrograms != 1)
            {
                System.Diagnostics.Debug.Fail("Cosmos Debugger only supports one debug target at a time.");
                throw new ArgumentException();
            }

            try
            {
                EngineUtils.RequireOk(rgpPrograms[0].GetProgramId(out m_programGUID));

                m_program = rgpPrograms[0];
                AD7EngineCreateEvent.Send(this);
                AD7ProgramCreateEvent.Send(this);
                AD7ModuleLoadEvent.Send(this, m_module, true);

                // Dummy main thread
                // We dont support threads yet, but the debugger expects threads. 
                // So we create a dummy object to represente our only "thread".
//                m_thread = m_process._threads;
//                m_thread = new AD7Thread(this, m_process);
                AD7LoadCompleteEvent.Send(this, currentThread());

                // If the reason for attaching is ATTACH_REASON_LAUNCH, the DE needs to send the IDebugEntryPointEvent2 event.
                // See http://msdn.microsoft.com/en-us/library/bb145136%28v=vs.100%29.aspx
                if (dwReason == enum_ATTACH_REASON.ATTACH_REASON_LAUNCH)
                {
                    AD7EntryPointEvent ad7Event = new AD7EntryPointEvent();
                    Guid riidEvent = new Guid(AD7EntryPointEvent.IID);
                    uint attributes = (uint)enum_EVENTATTRIBUTES.EVENT_STOPPING | (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;
                    int rc = ad7Callback.Event(this, null, m_program, currentThread(), ad7Event, ref riidEvent, attributes);
                    Debug.Assert(rc == VSConstants.S_OK);
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
            return VSConstants.S_OK;
        }

        // Requests that all programs being debugged by this DE stop execution the next time one of their threads attempts to run.
        // This is normally called in response to the user clicking on the pause button in the debugger.
        // When the break is complete, an AsyncBreakComplete event will be sent back to the debugger.
        int IDebugEngine2.CauseBreak()
        {
            // If not already broken, send the interrupt
            if (m_state != DE_STATE.DESIGN_MODE && m_state != DE_STATE.BREAK_MODE && m_state != DE_STATE.STEP_MODE)
            {
                bool signalReceived;
                bool hitBreakAll;
                if (EventDispatcher.m_GDBRunMode == true)
                {
                    HandleProcessExecution.m_mre.Reset();
                    do
                    {
                        hitBreakAll = m_running.WaitOne();
                        GDBParser.addGDBCommand(@"-exec-interrupt");

                        // Ensure the process is interrupted before returning                
                        signalReceived = HandleProcessExecution.m_mre.WaitOne(1000);
                    } while ((!signalReceived) && (VSNDKAddIn.isDebugEngineRunning));

                    m_running.Set();

                    if (VSNDKAddIn.isDebugEngineRunning)
                        HandleProcessExecution.m_mre.Reset();
//                    else
//                        return VSConstants.S_FALSE;
                }
//                Debug.Assert(m_state == AD7Engine.DE_STATE.BREAK_MODE);      
            }

            return VSConstants.S_OK;
        }

        // Called by the SDM to indicate that a synchronous debug event, previously sent by the DE to the SDM,
        // was received and processed. The only event the sample engine sends in this fashion is Program Destroy.
        // It responds to that event by shutting down the engine.
        int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 eventObject)
        {
            try
            {
                if (eventObject is AD7ProgramDestroyEvent)
                {
                    resetStackFrames();
                    m_process.Detach();
                    m_process = null;

                    m_program.Detach();
                    m_program = null;

                    m_engineCallback = null;
                    m_breakpointManager = null;
                    m_docContext = null;
                    m_eventDispatcher = null;
                    m_module = null;
                    m_progNode = null;
                    m_programGUID = Guid.Empty;

                    m_threads = null;
                    GC.Collect();
                }
                else
                {
                    Debug.Fail("Unknown synchronous event");
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
            
            if (m_eventDispatcher != null)
                m_eventDispatcher.continueExecution();

            return VSConstants.S_OK;
        }

        // Creates a pending breakpoint in the engine. A pending breakpoint is contains all the information needed to bind a breakpoint to 
        // a location in the debuggee.
        int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            Debug.Assert(m_breakpointManager != null);
            ppPendingBP = null;

            try
            {
                m_breakpointManager.CreatePendingBreakpoint(pBPRequest, out ppPendingBP);
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return VSConstants.S_OK;
        }

        // Informs a DE that the program specified has been atypically terminated and that the DE should 
        // clean up all references to the program and send a program destroy event.
        int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
        {
            // Tell the SDM that the engine knows that the program is exiting, and that the
            // engine will send a program destroy. We do this because the Win32 debug api will always
            // tell us that the process exited, and otherwise we have a race condition.

            return (AD7_HRESULT.E_PROGRAM_DESTROY_PENDING);
        }

        // Gets the GUID of the DE.
        int IDebugEngine2.GetEngineId(out Guid guidEngine)
        {
            guidEngine = new Guid(Id);
            return VSConstants.S_OK;
        }

        // Removes the list of exceptions the IDE has set for a particular run-time architecture or language.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.
        int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
        {
            return VSConstants.S_OK;
        }

        // Removes the specified exception so it is no longer handled by the debug engine.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.       
        int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
        {
            // The sample engine will always stop on all exceptions.

            return VSConstants.S_OK;
        }

        // Specifies how the DE should handle a given exception.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.
        int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
        {           
            return VSConstants.S_OK;
        }

        // Sets the locale of the DE.
        // This method is called by the session debug manager (SDM) to propagate the locale settings of the IDE so that
        // strings returned by the DE are properly localized. The sample engine is not localized so this is not implemented.
        int IDebugEngine2.SetLocale(ushort wLangID)
        {
            return VSConstants.S_OK;
        }

        // A metric is a registry value used to change a debug engine's behavior or to advertise supported functionality. 
        // This method can forward the call to the appropriate form of the Debugging SDK Helpers function, SetMetric.
        int IDebugEngine2.SetMetric(string pszMetric, object varValue)
        {
            // The sample engine does not need to understand any metric settings.
            return VSConstants.S_OK;
        }

        // Sets the registry root currently in use by the DE. Different installations of Visual Studio can change where their registry information is stored
        // This allows the debugger to tell the engine where that location is.
        int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
        {
            // The sample engine does not read settings from the registry.
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugEngineLaunch2 Members

        // Determines if a process can be terminated.
        int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 process)
        {                        
            return VSConstants.S_OK;
        }

        // Launches a process by means of the debug engine.
        // Normally, Visual Studio launches a program using the IDebugPortEx2::LaunchSuspended method and then attaches the debugger 
        // to the suspended program. However, there are circumstances in which the debug engine may need to launch a program 
        // (for example, if the debug engine is part of an interpreter and the program being debugged is an interpreted language), 
        // in which case Visual Studio uses the IDebugEngineLaunch2::LaunchSuspended method
        // The IDebugEngineLaunch2::ResumeProcess method is called to start the process after the process has been successfully launched in a suspended state.
        int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 port, string exe, string args, string dir, string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput, uint hStdOutput, uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
        {            
            Debug.Assert(m_programGUID == Guid.Empty);

            process = null;

            try
            {                
                VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning = true;
                m_engineCallback = new EngineCallback(this, ad7Callback);

                // Read arguments back from the args string
                var nvc = new NameValueCollection();
                NameValueCollectionHelper.LoadFromString(nvc, args);

                string pid = nvc.GetValues("pid")[0];
                string exePath = exe;
                string targetIP = nvc.GetValues("targetIP")[0];
                bool isSimulator = Convert.ToBoolean(nvc.GetValues("isSimulator")[0]);
                string toolsPath = nvc.GetValues("ToolsPath")[0];
                string publicKeyPath = nvc.GetValues("PublicKeyPath")[0];

                string password = null;
                string[] passwordArray = nvc.GetValues("Password");
                if (passwordArray != null)
                    password = passwordArray[0];

                if (GDBParser.LaunchProcess(pid, exePath, targetIP, isSimulator, toolsPath, publicKeyPath, password))
                {
                    process = m_process = new AD7Process(m_engineCallback, this, port);
                    m_eventDispatcher = new EventDispatcher(this, m_process);
                    m_programGUID = m_process._processID;
//                    m_programGUID = Guid.NewGuid();
                    m_module = new AD7Module();
                    m_progNode = new AD7ProgramNode(m_process.PhysID, pid, exePath, new Guid(AD7Engine.Id));
                    AddThreadsToProgram();

                    AD7EngineCreateEvent.Send(this);

                    return VSConstants.S_OK;
                }
                else
                {
                    GDBParser.exitGDB();
                    VSNDK.AddIn.VSNDKAddIn.isDebugEngineRunning = false;
                    return VSConstants.E_FAIL;
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Resume a process launched by IDebugEngineLaunch2.LaunchSuspended
        int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 process)
        {
            Debug.Assert(m_engineCallback != null);

            try
            {
                var xProcess = process as AD7Process;
                if (xProcess == null)
                {
                    return VSConstants.E_INVALIDARG;
                }

                // Send a program node to the SDM. This will cause the SDM to turn around and call IDebugEngine2.Attach
                // which will complete the hookup with AD7
                IDebugPort2 port = null;
                EngineUtils.RequireOk(process.GetPort(out port));
                
                IDebugDefaultPort2 defaultPort = (IDebugDefaultPort2)port;
                
                IDebugPortNotify2 portNotify = null;
                EngineUtils.RequireOk(defaultPort.GetPortNotify(out portNotify));

                //EngineUtils.RequireOk(portNotify.AddProgramNode(new AD7ProgramNode(m_debuggedProcess.Id))); //Copy AD7ProgramNode from Cosmos
                EngineUtils.RequireOk(portNotify.AddProgramNode(m_progNode)); //Copy AD7ProgramNode from Cosmos

                Callback.OnModuleLoad(m_module);
                //Callback.OnSymbolSearch(m_module, xProcess.mISO.Replace("iso", "pdb"), enum_MODULE_INFO_FLAGS.MIF_SYMBOLS_LOADED); // from Cosmos

                // Important! 
                //
                // This call triggers setting of breakpoints that exist before run.
                // So it must be called before we resume the process.
                // If not called VS will call it after our 3 startup events, but thats too late.
                // This line was commented out in earlier Cosmos builds and caused problems with
                // breakpoints and timing.
                Callback.OnThreadStart(currentThread());

                m_process.ResumeFromLaunch();                
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
            return VSConstants.S_OK;
        }

        // This function is used to terminate a process that the SampleEngine launched
        // The debugger will call IDebugEngineLaunch2::CanTerminateProcess before calling this method.
        int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 process)
        {
            CauseBreak();
            if (eDispatcher != null)
            {
                string killed = eDispatcher.killProcess();
                eDispatcher.endDebugSession(0);
            }

            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugProgram2 Members

        // Determines if a debug engine (DE) can detach from the program.
        public int CanDetach()
        {
            // The sample engine always supports detach
            return VSConstants.S_OK;
        }

        // The debugger calls CauseBreak when the user clicks on the pause button in VS. The debugger should respond by entering
        // breakmode. 
        public int CauseBreak()
        {
            return ((IDebugEngine2)this).CauseBreak();
        }

        // Continue is called from the SDM when it wants execution to continue in the debugee
        // but have stepping state remain. An example is when a tracepoint is executed, 
        // and the debugger does not want to actually enter break mode.
        public int Continue(IDebugThread2 pThread)
        {
            m_eventDispatcher.continueExecution();

            return VSConstants.S_OK;            
        }

        // Detach is called when debugging is stopped and the process was attached to (as opposed to launched)
        // or when one of the Detach commands are executed in the UI.
        public int Detach()
        {            
            CauseBreak();
            if (m_breakpointManager != null)
                m_breakpointManager.ClearBoundBreakpoints(); // TODO: Check if active bound BP list needs to be updated too?
            
            // Since we're closing the debugger for now instead of supporting attach / detach functionality,
            // this command is unneccessary.  It also will cause a hang if CauseBreak() has not been processed by the
            // time a synchronous command is issued.
            // TODO: Fix this when implementing attach/detach features, and remove the call to endDebugSession().
            //GDBParser.parseCommand("-target-detach"); // No output expected for this command

            if (eDispatcher != null)
                eDispatcher.endDebugSession(0);
            
            return VSConstants.S_OK;            
        }

        // Enumerates the code contexts for a given position in a source file.
        public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // EnumCodePaths is used for the step-into specific feature -- right click on the current statment and decide which
        // function to step into. This is not something that the SampleEngine supports.
        public int EnumCodePaths(string hint, IDebugCodeContext2 start, IDebugStackFrame2 frame, int fSource, out IEnumCodePaths2 pathEnum, out IDebugCodeContext2 safetyContext)
        {
            pathEnum = null;
            safetyContext = null;
            return VSConstants.E_NOTIMPL;
        }

        // EnumModules is called by the debugger when it needs to enumerate the modules in the program.
        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            // Setting ppEnum to null because we are not adding/working with this feature now. It was causing an error
            // when opening Threads Window and ppEnum = new AD7ModuleEnum(new[] { m_module }).
            ppEnum = null;
//            ppEnum = new AD7ModuleEnum(new[] { m_module });
            return VSConstants.S_OK;            
        }

        // EnumThreads is called by the debugger when it needs to enumerate the threads in the program.
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            AD7Thread[] listThreads = null;
            int currentThread = GetListOfThreads(out listThreads);

            // the following code seems to be weird but I had to update each field of this.m_process._threads because, if use
            // "this.m_process._threads = listThreads;" without having a new thread, VS start to duplicate the existing threads 
            // and, as a consequence, the call stack entries too.

            if ((currentThread == -1) || (listThreads == null))
            {
                ppEnum = null;
                return VSConstants.S_FALSE;
            }

            if (listThreads.Length != this.m_threads.Length)
            {
                foreach (AD7Thread t in this.m_threads)
                {
                    AD7ThreadDestroyEvent.Send(this, 0, t);
                }
                this.m_threads = null;
                this.m_threads = new AD7Thread[listThreads.Length]; 
                this.m_threads = listThreads;
                this._currentThreadIndex = currentThread;
                foreach (AD7Thread t in this.m_threads)
                {
                    m_engineCallback.OnThreadStart(t);
                }
                ppEnum = new AD7ThreadEnum(this.m_threads);
            }
            else 
            {
                if (this._currentThreadIndex != currentThread)
                {
                    if ((this._currentThreadIndex != -1) && (currentThread != -1))
                        this.m_threads[this._currentThreadIndex]._current = !this.m_threads[this._currentThreadIndex]._current;
                    if (currentThread != -1)
                        this.m_threads[currentThread]._current = !this.m_threads[currentThread]._current;
                    this._currentThreadIndex = currentThread;
                }

//                this.m_threads = listThreads;

                for (int i = 0; i < listThreads.Length; i++)
                {
                    if (this.m_threads[i]._engine != listThreads[i]._engine)
                    {
                        this.m_threads[i]._engine = listThreads[i]._engine;
                    }
                    if (this.m_threads[i]._threadDisplayName != listThreads[i]._threadDisplayName)
                    {
                        this.m_threads[i]._threadDisplayName = listThreads[i]._threadDisplayName;
                    }
                    if (this.m_threads[i]._id != listThreads[i]._id)
                    {
                        this.m_threads[i]._id = listThreads[i]._id;
                    }
                    if (this.m_threads[i]._state != listThreads[i]._state)
                    {
                        this.m_threads[i]._state = listThreads[i]._state;
                    }
                    if (this.m_threads[i]._targetID != listThreads[i]._targetID)
                    {
                        this.m_threads[i]._targetID = listThreads[i]._targetID;
                    }
                    if (this.m_threads[i]._priority != listThreads[i]._priority)
                    {
                        this.m_threads[i]._priority = listThreads[i]._priority;
                    }
                    if (this.m_threads[i]._line != listThreads[i]._line)
                    {
                        this.m_threads[i]._line = listThreads[i]._line;
                    }
                    if (this.m_threads[i]._filename != listThreads[i]._filename)
                    {
                        if (listThreads[i]._filename == "")
                            this.m_threads[i]._filename = "";
                        else
                            this.m_threads[i]._filename = listThreads[i]._filename;
                    }
                } 

                ppEnum = new AD7ThreadEnum(this.m_threads);
            }
            return VSConstants.S_OK;

        }

        // The properties returned by this method are specific to the program. If the program needs to return more than one property, 
        // then the IDebugProperty2 object returned by this method is a container of additional properties and calling the 
        // IDebugProperty2::EnumChildren method returns a list of all properties.
        // A program may expose any number and type of additional properties that can be described through the IDebugProperty2 interface. 
        // An IDE might display the additional program properties through a generic property browser user interface.
        // The sample engine does not support this
        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger calls this when it needs to obtain the IDebugDisassemblyStream2 for a particular code-context.
        // The sample engine does not support dissassembly so it returns E_NOTIMPL
        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 codeContext, out IDebugDisassemblyStream2 disassemblyStream)
        {
            disassemblyStream = null;
            return VSConstants.E_NOTIMPL;
        }

        // This method gets the Edit and Continue (ENC) update for this program. A custom debug engine always returns E_NOTIMPL
        public int GetENCUpdate(out object update)
        {
            // The sample engine does not participate in managed edit & continue.
            update = null;
            return VSConstants.S_OK;            
        }

        // Gets the name and identifier of the debug engine (DE) running this program.
        public int GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            /*engineName = ResourceStrings.EngineName;
            engineGuid = new Guid(AD7Engine.Id);
            return VSConstants.S_OK;*/

            engineName = "";
            engineGuid = new Guid();

            return VSConstants.E_NOTIMPL;
        }

        // The memory bytes as represented by the IDebugMemoryBytes2 object is for the program's image in memory and not any memory 
        // that was allocated when the program was executed.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Gets the name of the program.
        // The name returned by this method is always a friendly, user-displayable name that describes the program.
        public int GetName(out string programName)
        {
            // The Sample engine uses default transport and doesn't need to customize the name of the program,
            // so return NULL.
            if (this.m_progNode != null)
                programName = this.m_progNode.m_programName;
            else
                programName = null;
            return VSConstants.S_OK;
        }

        // Gets a GUID for this program. A debug engine (DE) must return the program identifier originally passed to the IDebugProgramNodeAttach2::OnAttach
        // or IDebugEngine2::Attach methods. This allows identification of the program across debugger components.
        public int GetProgramId(out Guid guidProgramId)
        {
            // Debug.Assert(m_ad7ProgramId != Guid.Empty);

            if (m_programGUID != null)
                guidProgramId = m_programGUID;
            else
                guidProgramId = m_process._processID;

            return VSConstants.S_OK;
        }

        // This method is deprecated. Use the IDebugProcess3::Step method instead.
        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            // Don't allow stepping through unknown code because it can lead to future problems with stepping and break-all
//            resetStackFrames();
            if (VSNDK.DebugEngine.EventDispatcher.m_unknownCode)
            {
                m_state = AD7Engine.DE_STATE.STEP_MODE;
                m_eventDispatcher.continueExecution();                

                return VSConstants.S_OK;
            }

            if (sk == enum_STEPKIND.STEP_INTO)
            { 
                // F11
                GDBParser.addGDBCommand("-exec-step --thread " + this.m_threads[this._currentThreadIndex]._id);
                // ??? Create a method to inspect all of the current variables 
            }
            else if (sk == enum_STEPKIND.STEP_OVER)
            { 
                // F10
                GDBParser.addGDBCommand("-exec-next --thread " + this.m_threads[this._currentThreadIndex]._id);
                // ??? Create a method to inspect all of the current variables 
            }
            else if (sk == enum_STEPKIND.STEP_OUT)
            { 
                // Shift-F11                
                if (eDispatcher.getStackDepth(this.m_threads[this._currentThreadIndex]._id) > 1)
                {
                    GDBParser.addGDBCommand("-exec-finish --thread " + this.m_threads[this._currentThreadIndex]._id + " --frame 0");
                }
                else
                {
                    // If this the only frame left, do a step-over
                    GDBParser.addGDBCommand("-exec-next --thread " + this.m_threads[this._currentThreadIndex]._id);
                }
                // ??? Create a method to inspect all of the current variables 
            }
            else if (sk == enum_STEPKIND.STEP_BACKWARDS)
            {
                return VSConstants.E_NOTIMPL;
            }
            else
            {
                m_engineCallback.OnStepComplete(); // Have to call this otherwise VS gets "stuck"
            }

            // Transition DE state                
//            Debug.Assert(m_state == AD7Engine.DE_STATE.BREAK_MODE);
            m_state = AD7Engine.DE_STATE.STEP_MODE;

            return VSConstants.S_OK;
        }

        // Terminates the program.
        public int Terminate()
        {
            // Because the sample engine is a native debugger, it implements IDebugEngineLaunch2, and will terminate
            // the process in IDebugEngineLaunch2.TerminateProcess
            return VSConstants.S_OK;
        }

        // Writes a dump to a file.
        public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            // The sample debugger does not support creating or reading mini-dumps.
            return VSConstants.E_NOTIMPL;
        }

        #endregion

        #region IDebugProgram3 Members

        // ExecuteOnThread is called when the SDM wants execution to continue and have 
        // stepping state cleared.
        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            m_eventDispatcher.continueExecution();

            return VSConstants.S_OK;            
        }

        #endregion

        #region IDebugEngineProgram2 Members

        // Stops all threads running in this program.
        // This method is called when this program is being debugged in a multi-program environment. When a stopping event from some other program 
        // is received, this method is called on this program. The implementation of this method should be asynchronous; 
        // that is, not all threads should be required to be stopped before this method returns. The implementation of this method may be 
        // as simple as calling the IDebugProgram2::CauseBreak method on this program.
        //
        // The sample engine only supports debugging native applications and therefore only has one program per-process
        public int Stop()
        {
            return VSConstants.E_NOTIMPL;
        }

        // WatchForExpressionEvaluationOnThread is used to cooperate between two different engines debugging 
        // the same process. The sample engine doesn't cooperate with other engines, so it has nothing
        // to do here.
        public int WatchForExpressionEvaluationOnThread(IDebugProgram2 pOriginatingProgram, uint dwTid, uint dwEvalFlags, IDebugEventCallback2 pExprCallback, int fWatch)
        {
            return VSConstants.S_OK;
        }

        // WatchForThreadStep is used to cooperate between two different engines debugging the same process.
        // The sample engine doesn't cooperate with other engines, so it has nothing to do here.
        public int WatchForThreadStep(IDebugProgram2 pOriginatingProgram, uint dwTid, int fWatch, uint dwFrame)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugSymbolSettings100 members
        public int SetSymbolLoadState(int bIsManual, int bLoadAdjacent, string strIncludeList, string strExcludeList)
        {
            // The SDM will call this method on the debug engine when it is created, to notify it of the user's
            // symbol settings in Tools->Options->Debugging->Symbols.
            //
            // Params:
            // bIsManual: true if 'Automatically load symbols: Only for specified modules' is checked
            // bLoadAdjacent: true if 'Specify modules'->'Always load symbols next to the modules' is checked
            // strIncludeList: semicolon-delimited list of modules when automatically loading 'Only specified modules'
            // strExcludeList: semicolon-delimited list of modules when automatically loading 'All modules, unless excluded'

            return VSConstants.S_OK;
        }
        #endregion

        #region Thread methods

        public bool evaluatedTheseFlags(string threadId, enum_FRAMEINFO_FLAGS flags)
        {
            if (this.thread != null)
            {
                int currentFlags = Convert.ToInt32(flags);
                int i = 0;
                for (; i < this.thread.Length; i++)
                {
                    if (this.thread[i]._id == threadId)
                        break;
                }
                if (i == this.thread.Length)
                {
                    return false;
                }

                if (this.thread[i]._alreadyEvaluated == currentFlags)
                {
                    return true;
                }
                else
                {
                    this.thread[i]._alreadyEvaluated = currentFlags;
                }
            }
            return false;
        }

        public void cleanEvaluatedThreads()
        {
            if (this.thread != null)
            {
                for (int i = 0; i < this.thread.Length; i++)
                {
                    this.thread[i]._alreadyEvaluated = 0;
                }
            }
        }

        public void resetStackFrames()
        {
            foreach (AD7Thread t in m_threads)
            {
                if (t.__stackFrames != null)
                {
                    t.__stackFrames.Clear();
                    t.__stackFrames = null;
                }
            }
        }

        public void UpdateListOfThreads()
        {
            AD7Thread[] listThreads = null;
            int currentThread = GetListOfThreads(out listThreads);

            if ((currentThread == -1) || (listThreads == null))
                return;

            foreach (AD7Thread t in this.m_threads)
            {
                AD7ThreadDestroyEvent.Send(this, 0, t);
            }
            this.m_threads = null;
            this.m_threads = new AD7Thread[listThreads.Length];
            this.m_threads = listThreads;
            this._currentThreadIndex = currentThread;
            foreach (AD7Thread t in this.m_threads)
            {
                m_engineCallback.OnThreadStart(t);
            }

            this._updateThreads = false;
        }

        // Get the list of threads being debugged, also returning the index of the current thread (or -1 in case of error)
        public int GetListOfThreads(out AD7Thread[] threadObjects)
        {
            // Ask for general threads information.
            string threadResponse = GDBParser.parseCommand(@"-thread-info", 22);
            string[] threadStrings = threadResponse.Split('#');
            cleanEvaluatedThreads();

            // Query the threads depth without inquiring GDB.
            int numThreads = threadStrings.Length - 1;
            string currentThread = threadStrings[threadStrings.Length - 1];
            if (numThreads < 1)
            {
                threadObjects = null;
                return -1;
            }

            try
            {
                List<string[]> threadsList = new List<string[]>();

                int currentThreadIndex = -1;

                for (int i = 0; i < numThreads; i++)
                {
                    string[] threadInfo = threadStrings[i].Split(';');
                    threadsList.Add(threadInfo);
                }

//                if (threadsList.Count > 1) // there is always one thread, the GDB debugger.
//                {
                    threadObjects = new AD7Thread[threadsList.Count];

                    for (int i = 0; i < threadsList.Count; i++)
                    {
                        bool current = false;
                        if (currentThread == threadsList[i][0])
                        {
                            currentThreadIndex = i;
                            current = true;
                        }

                        if (threadsList[i].Length > 5)
                            threadObjects[i] = new AD7Thread(this, current, threadsList[i][0], threadsList[i][2], threadsList[i][1], "Normal", threadsList[i][6], threadsList[i][4], threadsList[i][5]);
                        else
                            threadObjects[i] = new AD7Thread(this, current, threadsList[i][0], threadsList[i][2], threadsList[i][1], "Normal", threadsList[i][4], "", "");
                    }
//                }
//                else
//                {
//                    threadObjects = new AD7Thread[1];
//                    threadObjects[0] = new AD7Thread(this, false, "", "Thread 1", "", "", "", "", "0");
//                }
                return currentThreadIndex;
            }
            catch (ComponentException e)
            {
                threadObjects = null;
                return -1;
            }
            catch (Exception e)
            {
                threadObjects = null;
                return -1;
            }
        }

        AD7Thread GetCurrentThread()
        {
            AD7Thread[] listOfThreads = null;
            int threadIndex = GetListOfThreads(out listOfThreads);
            if (threadIndex != -1)
                return (listOfThreads[threadIndex]);
            else if ((listOfThreads != null) && (listOfThreads.Length > 0))
                return (listOfThreads[0]);
            return null;
        }

        public AD7Thread currentThread()
        {
            if (_currentThreadIndex != -1)
                return m_threads[_currentThreadIndex];
            else if ((m_threads != null) && (m_threads.Length > 0))
                return (m_threads[0]);
            return null;
        }

        public AD7Thread selectThread(string id)
        {
            for (int i = 0; i < this.m_threads.Length; i++)
            {
                if (this.m_threads[i]._id == id)
                    return (m_threads[i]);
            }
            if ((this.m_threads.Length == 1) && (id == "1") && (this.m_threads[0]._id == ""))
                return (m_threads[0]);
            return null;
        }

        public void setAsCurrentThread(string id)
        {
            for (int i = 0; i < this.m_threads.Length; i++)
            {
                if (this.m_threads[i]._id == id)
                {
                    this._currentThreadIndex = i;
                    break;
                }
            }
        }

        public int AddThreadsToProgram()
        {
//            if ((_currentThreadIndex == -1) && (m_threads != null))
//                AD7ThreadDestroyEvent.Send(this, 0, m_threads[0]);
            m_threads = null;
            _currentThreadIndex = GetListOfThreads(out m_threads);
            if (_currentThreadIndex != -1)
            {
                foreach (AD7Thread t in m_threads)
                {
                    m_engineCallback.OnThreadStart(t);
                }
            }
            else
                if ((m_threads != null) && (m_threads.Length > 0))
                    m_engineCallback.OnThreadStart(m_threads[0]);
                else
                    m_engineCallback.OnThreadStart(null);
            return VSConstants.S_OK;
        }

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 programs)
        {
            Debug.Fail("This function is not called by the debugger");

            programs = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Attach(IDebugEventCallback2 pCallback)
        {
            Debug.Fail("This function is not called by the debugger");

            return VSConstants.E_NOTIMPL;
        }

        public int GetProcess(out IDebugProcess2 process)
        {
            Debug.Fail("This function is not called by the debugger");

            process = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Execute()
        {
            m_eventDispatcher.continueExecution();
            return VSConstants.S_OK;
        }

        #endregion
    }
}

