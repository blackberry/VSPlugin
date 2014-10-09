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
using BlackBerry.NativeCore;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class represents a thread running in a program and it implements:
    /// 
    /// IDebugThread2: (http://msdn.microsoft.com/en-ca/library/bb145332.aspx) 
    /// 
    /// IDebugThread100: (http://msdn.microsoft.com/en-us/library/ff471152.aspx)
    /// </summary>
    public sealed class AD7Thread : IDebugThread2, IDebugThread100
    {
        #region Internal Types

        /// <summary>
        /// Thread's category, from http://social.msdn.microsoft.com/Forums/en-US/vsx/thread/807e26cc-4f3f-4e90-a9a8-b550d484b8c1
        /// </summary>
        enum enum_THREADCATEGORY
        {
            THREADCATEGORY_Worker = 0,
            THREADCATEGORY_UI = (THREADCATEGORY_Worker + 1),
            THREADCATEGORY_Main = (THREADCATEGORY_UI + 1),
            THREADCATEGORY_RPC = (THREADCATEGORY_Main + 1),
            THREADCATEGORY_Unknown = (THREADCATEGORY_RPC + 1)
        }

        #endregion

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        public AD7Engine _engine;

        /// <summary>
        /// Thread's name.
        /// </summary>
        public string _threadDisplayName;

        /// <summary>
        /// The suspend count determines how many times the IDebugThread2::Suspend method has been called so far.
        /// </summary>
        public uint _suspendCount;

        /// <summary>
        /// Full long path file name.
        /// </summary>
        public string _filename;

        /// <summary>
        /// Line number.
        /// </summary>
        public uint _line;

        /// <summary>
        /// Contains flags that specifies the information that were already verified about this thread's stack frame object. 
        /// Used to avoid reevaluating stack frames that were already evaluated.
        /// </summary>
        public int _alreadyEvaluated;

        /// <summary>
        /// Contains the stack frames for this thread
        /// </summary>
        public ArrayList __stackFrames;

        /// <summary>
        /// A FRAMEINFO structure that is filled in with the description of the previous stack frame that works as a cache, to avoid
        /// reevaluate the stack frame again.
        /// </summary>
        public FRAMEINFO[] previousFrameInfoArray = new FRAMEINFO[0];

        /// <summary>
        /// Thread's ID.
        /// </summary>
        public string _id;

        /// <summary>
        /// Thread's state.
        /// </summary>
        public string _state;

        /// <summary>
        /// Process' ID + Thread's ID.
        /// </summary>
        public string _targetID;

        /// <summary>
        /// Thread's priority.
        /// </summary>
        public string _priority;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="engine"> The AD7Engine object that represents the DE. </param>
        /// <param name="id"> Thread's ID. </param>
        /// <param name="targetID"> Process' ID + Thread's ID. </param>
        /// <param name="state"> Thread's state. </param>
        /// <param name="priority"> Thread's priority. </param>
        /// <param name="name"> Thread's name. </param>
        /// <param name="filename"> Full short path file name. </param>
        /// <param name="line"> Line number. </param>
        public AD7Thread(AD7Engine engine, string id, string targetID, string state, string priority, string name, string filename, string line)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            _engine = engine;
            _suspendCount = 0;
            if (id == "1")
                _threadDisplayName = "Main Thread";
            else
                _threadDisplayName = (name != "") ? name : "<No Name>";

            _filename = NativeMethods.GetLongPathName(filename);
            uint.TryParse(line, out _line); // stopping on that exception was just horrible...
            _id = id;
            _state = state;
            _targetID = targetID;
            _priority = priority;
        }

        /// <summary>
        /// Called by EventDipatcher to set the current location during break mode.
        /// </summary>
        /// <param name="filename">  Full short path file name. </param>
        /// <param name="line"> Line number. </param>
        public void SetCurrentLocation(string filename, uint line)
        {
            _filename = NativeMethods.GetLongPathName(filename);
            _line = line;
        }

        /// <summary>
        /// Gets the function name.
        /// </summary>
        /// <returns> If successful, returns the function name; otherwise, returns "". </returns>
        public string GetFunctionName()
        {
            string func = "";

            if ((_id != "") && (_id != _engine.CurrentThread()._id))
                _engine.EventDispatcher.SelectThread(_id);

            string stackResponse = _engine.EventDispatcher.GetStackFrames().Replace("#;;;;", "");

            if (stackResponse != "")
            {
                string[] frameStrings = stackResponse.Split('#');

                // Query the stack depth without inquiring GDB.
                int numStackFrames = frameStrings.Length;

                if (numStackFrames > 30) // limiting the amount of stackFrames to avoid VS crashing.
                    numStackFrames = 30;

                for (int i = 0; i < numStackFrames; i++)
                {
                    string[] frameInfo = frameStrings[i].Split(';');
                    if (frameInfo.Length >= 2)
                    {
                        if ((frameInfo[2] != "") && (frameInfo[2] != "??") && (!frameInfo[2].Contains("object.")))
                        {
                            func = frameInfo[2];
                            break;
                        }
                    }
                }

                if ((_id != "") && (_id != _engine.CurrentThread()._id))
                    _engine.EventDispatcher.SelectThread(_engine.CurrentThread()._id);
            }

            return func;
        }

        /// <summary>
        /// Determines whether the next statement can be set to the given stack frame and code context. Not implemented.
        /// (http://msdn.microsoft.com/en-ca/library/bb146582.aspx)
        /// </summary>
        /// <param name="stackFrame"> Reserved for future use; set to a null value. If this is a null value, use the current 
        /// stack frame. </param>
        /// <param name="codeContext"> An IDebugCodeContext2 object that describes the code location about to be executed and 
        /// its context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Retrieves a list of the stack frames for this thread. (http://msdn.microsoft.com/en-ca/library/bb145138.aspx)
        /// </summary>
        /// <param name="flags"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the FRAMEINFO structures are to be filled out. </param>
        /// <param name="radix"> Radix used in formatting numerical information in the enumerator. </param>
        /// <param name="ppEnum"> Returns an IEnumDebugFrameInfo2 object that contains a list of FRAMEINFO structures describing the stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS flags, uint radix, out IEnumDebugFrameInfo2 ppEnum)
        {
            if (_id == "")
            {
                ppEnum = null;
                return VSConstants.S_FALSE;
            }

            if (_engine.EvaluatedTheseFlags(_id, flags))
            {
                ppEnum = new AD7FrameInfoEnum(previousFrameInfoArray);
                return VSConstants.S_OK;
            }

            // Ask for general stack information.
            if ((_id != "") && (_id != _engine.CurrentThread()._id))
                _engine.EventDispatcher.SelectThread(_id);

            string stackResponse = _engine.EventDispatcher.GetStackFrames().Replace("#;;;;", "");
            if (stackResponse == "")
            {
                ppEnum = null;
                return VSConstants.S_FALSE;
            }
            string[] frameStrings = stackResponse.Split('#');

            // Query the stack depth without inquiring GDB.
            int numStackFrames = frameStrings.Length;

            if (numStackFrames > 30) // limiting the amount of stackFrames to avoid VS crashing.
                numStackFrames = 30;

            ppEnum = null;
            try
            {
                bool created = false;
                FRAMEINFO[] frameInfoArray = new FRAMEINFO[numStackFrames];
                for (int i = 0; i < numStackFrames; i++)
                {
                    string[] frameInfo = frameStrings[i].Split(';');
                    if (frameInfo.Length >= 3)
                    {
                        frameInfo[3] = NativeMethods.GetLongPathName(frameInfo[3]);
                        AD7StackFrame frame = AD7StackFrame.Create(_engine, this, frameInfo, ref created);
                        frame.SetFrameInfo(flags, out frameInfoArray[i]);
                    }
                }
                if ((previousFrameInfoArray.Length != frameInfoArray.Length) || created)
                {
                    previousFrameInfoArray = frameInfoArray;
                    ppEnum = new AD7FrameInfoEnum(frameInfoArray);
                }
                else
                {
                    bool isEqual = true;
                    for (int i = 0; i < frameInfoArray.Length; i++)
                    {
                        if (frameInfoArray[i].m_bstrFuncName != previousFrameInfoArray[i].m_bstrFuncName)
                        {
                            isEqual = false;
                            break;
                        }
                        if (frameInfoArray[i].m_dwValidFields != previousFrameInfoArray[i].m_dwValidFields)
                        {
                            isEqual = false;
                            break;
                        }
                        if (frameInfoArray[i].m_bstrLanguage != previousFrameInfoArray[i].m_bstrLanguage)
                        {
                            isEqual = false;
                            break;
                        }
                    }
                    if (!isEqual)
                    {
                        previousFrameInfoArray = frameInfoArray;
                        ppEnum = new AD7FrameInfoEnum(frameInfoArray);
                    }
                    else
                    {
                        ppEnum = new AD7FrameInfoEnum(previousFrameInfoArray);
                    }
                }

                if ((_id != "") && (_id != _engine.CurrentThread()._id))
                    _engine.EventDispatcher.SelectThread(_engine.CurrentThread()._id);

                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                if ((_id != "") && (_id != _engine.CurrentThread()._id))
                    _engine.EventDispatcher.SelectThread(_engine.CurrentThread()._id);
                return EngineUtils.UnexpectedException(ex);
            }
        }

        /// <summary>
        /// Gets the logical thread associated with this physical thread. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb161676.aspx)
        /// </summary>
        /// <param name="stackFrame"> An IDebugStackFrame2 object that represents the stack frame. </param>
        /// <param name="ppLogicalThread"> Returns an IDebugLogicalThread2 interface that represents the associated logical 
        /// thread. A debug engine implementation should set this to a null value. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 stackFrame, out IDebugLogicalThread2 ppLogicalThread)
        {
            ppLogicalThread = null;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the name of a thread. (http://msdn.microsoft.com/en-ca/library/bb162273.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetName(out string pbstrName)
        {
            pbstrName = _threadDisplayName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the program in which a thread is running. (http://msdn.microsoft.com/en-ca/library/bb147002.aspx)
        /// </summary>
        /// <param name="ppProgram"> Returns an IDebugProgram2 object that represents the program this thread is running in. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetProgram(out IDebugProgram2 ppProgram)
        {
            ppProgram = _engine.Program;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the system thread identifier. (http://msdn.microsoft.com/en-ca/library/bb161964.aspx)
        /// </summary>
        /// <param name="pThreadId"> Returns the system thread identifier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetThreadId(out uint pThreadId)
        {
            try
            {
                pThreadId = Convert.ToUInt32(_id);
            }
            catch
            {
                pThreadId = 0;
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the properties that describe this thread. (http://msdn.microsoft.com/en-ca/library/bb145602.aspx)
        /// </summary>
        /// <param name="fields"> A combination of flags from the THREADPROPERTY_FIELDS enumeration that determines which fields of 
        /// ptp are to be filled in. </param>
        /// <param name="pThreadProperties"> A THREADPROPERTIES structure that that is filled in with the properties of the thread. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS fields, THREADPROPERTIES[] pThreadProperties)
        {
            try
            {
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
                {
                    try
                    {
                        pThreadProperties[0].dwThreadId = Convert.ToUInt32(_id);
                    }
                    catch
                    {
                        pThreadProperties[0].dwThreadId = 0;
                    }
                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
                }
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT) != 0)
                {
                    // VSNDK debug engine doesn't support suspending threads
                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
                }
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
                {
                    if (_state == "running")
                        pThreadProperties[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    else
                        pThreadProperties[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_STOPPED;
                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
                }
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_PRIORITY) != 0)
                {
                    pThreadProperties[0].bstrPriority = "Normal";
                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_PRIORITY;
                }
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
                {
                    pThreadProperties[0].bstrName = _threadDisplayName;
                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
                }
                if ((fields & enum_THREADPROPERTY_FIELDS.TPF_LOCATION) != 0)
                {
                    pThreadProperties[0].bstrLocation = "";
                    if (__stackFrames != null)
                    {
                        foreach (AD7StackFrame frame in __stackFrames)
                        {
                            if (frame._functionName != "")
                            {
                                pThreadProperties[0].bstrLocation = frame._functionName;
                                break;
                            }
                        }
                    }
                    if (pThreadProperties[0].bstrLocation == "")
                        pThreadProperties[0].bstrLocation = "[External Code]";

                    pThreadProperties[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_LOCATION;
                }

                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
            }
        }

        /// <summary>
        /// Resumes execution of a thread. (http://msdn.microsoft.com/en-ca/library/bb145813.aspx)
        /// </summary>
        /// <param name="pSuspendCount"> Returns the suspend count after the resume operation. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.Resume(out uint pSuspendCount)
        {
            _suspendCount--;
            pSuspendCount = _suspendCount;
            if (_suspendCount == 0)
            {
                // Send GDB command to resume execution of thread
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Sets the next statement to the given stack frame and code context. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb160944.aspx)
        /// </summary>
        /// <param name="stackFrame"> Reserved for future use; set to a null value. </param>
        /// <param name="codeContext"> An IDebugCodeContext2 object that describes the code location about to be executed and 
        /// its context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.SetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Sets the name of the thread. (http://msdn.microsoft.com/en-ca/library/bb162322.aspx)
        /// </summary>
        /// <param name="name"> The name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.SetThreadName(string name)
        {
            _threadDisplayName = name;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Suspends a thread. (http://msdn.microsoft.com/en-us/library/bb145297.aspx)
        /// </summary>
        /// <param name="pSuspendCount"> Returns the suspend count after the suspend operation. </param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugThread2.Suspend(out uint pSuspendCount)
        {
            _suspendCount++;
            pSuspendCount = _suspendCount;
            return VSConstants.S_OK;
        }

        #region IDebugThread100 Members

        /// <summary>
        /// Sets the name of the thread.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.setthreaddisplayname.aspx)
        /// </summary>
        /// <param name="name"> The name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread100.SetThreadDisplayName(string name)
        {
            _threadDisplayName = name;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the name of a thread.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.getthreaddisplayname.aspx)
        /// </summary>
        /// <param name="name"> Returns the name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread100.GetThreadDisplayName(out string name)
        {
            name = _threadDisplayName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns whether this thread can be used to do function/property evaluation. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.candofunceval.aspx)
        /// </summary>
        /// <returns> VSConstants.S_FALSE. </returns>
        int IDebugThread100.CanDoFuncEval()
        {
            return VSConstants.S_FALSE;
        }

        /// <summary>
        /// Set flags. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.setflags.aspx)
        /// </summary>
        /// <param name="flags"> Flags. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugThread100.SetFlags(uint flags)
        {
            // Not necessary to implement in the debug engine. Instead it is implemented in the SDM.
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Get flags. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.getflags.aspx)
        /// </summary>
        /// <param name="flags"> Flags. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugThread100.GetFlags(out uint flags)
        {
            // Not necessary to implement in the debug engine. Instead it is implemented in the SDM.
            flags = 0;
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// Gets the properties that describe this thread.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.getthreadproperties100.aspx)
        /// </summary>
        /// <param name="fields"> A combination of flags from the THREADPROPERTIES100 enumeration that determines which fields of 
        /// ptp are to be filled in. </param>
        /// <param name="pThreadproperties"> A THREADPROPERTIES100 structure that that is filled in with the properties of the thread. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread100.GetThreadProperties100(uint fields, THREADPROPERTIES100[] pThreadproperties)
        {
            try
            {
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_ID) != 0)
                {
                    try
                    {
                        pThreadproperties[0].dwThreadId = Convert.ToUInt32(_id);
                    }
                    catch
                    {
                        pThreadproperties[0].dwThreadId = 0;
                    }
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_ID;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_SUSPENDCOUNT) != 0)
                {
                    // VSNDK debug engine doesn't support suspending threads
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_SUSPENDCOUNT;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_STATE) != 0)
                {
                    if (_state == "running")
                        pThreadproperties[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    else
                        pThreadproperties[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_STOPPED;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_STATE;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY) != 0)
                {
                    pThreadproperties[0].bstrPriority = "Normal";
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_NAME) != 0)
                {
                    pThreadproperties[0].bstrName = _threadDisplayName;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_NAME;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME) != 0)
                {
                    // Thread display name is being requested
                    pThreadproperties[0].bstrDisplayName = _threadDisplayName;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME;

                    // Give this display name a higher priority than the default (0)
                    // so that it will actually be displayed
                    pThreadproperties[0].DisplayNamePriority = 10;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME_PRIORITY;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_LOCATION) != 0)
                {
                    pThreadproperties[0].bstrLocation = "";
                    if (__stackFrames != null)
                    {
                        foreach (AD7StackFrame frame in __stackFrames)
                        {
                            if ((frame._functionName != "") && (frame._functionName != "??"))
                            {
                                pThreadproperties[0].bstrLocation = frame._functionName;
                                break;
                            }
                        }
                    }
                    else
                        pThreadproperties[0].bstrLocation = GetFunctionName();

                    if (pThreadproperties[0].bstrLocation == "")
                        pThreadproperties[0].bstrLocation = "[External Code]";

                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_LOCATION;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY) != 0)
                {
                    if (_id == "1")
                        pThreadproperties[0].dwThreadCategory = (uint)enum_THREADCATEGORY.THREADCATEGORY_Main;
                    else
                        pThreadproperties[0].dwThreadCategory = (uint)enum_THREADCATEGORY.THREADCATEGORY_Worker;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY;
                }
                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY) != 0)
                {
                    // Thread cpu affinity is being requested
                    pThreadproperties[0].AffinityMask = 0;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY;
                }

                if ((fields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID) != 0)
                {
                    // Thread display name is being requested
                    pThreadproperties[0].priorityId = 0;
                    pThreadproperties[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID;
                }
                return VSConstants.S_OK;
            }
            catch (Exception ex)
            {
                return EngineUtils.UnexpectedException(ex);
            }
        }

        #endregion
    }
}
