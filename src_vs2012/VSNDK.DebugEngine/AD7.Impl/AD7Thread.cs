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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using VSNDK.Parser;
using System.Runtime.InteropServices;
using System.Collections;

namespace VSNDK.DebugEngine
{

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
    };


    /// <summary>
    /// This class represents a thread running in a program and it implements:
    /// 
    /// IDebugThread2: (http://msdn.microsoft.com/en-ca/library/bb145332.aspx) 
    /// 
    /// IDebugThread100: (http://msdn.microsoft.com/en-us/library/ff471152.aspx)
    /// </summary>
    public class AD7Thread : IDebugThread2, IDebugThread100
    {
        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        public AD7Engine _engine = null;

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
        public int _alreadyEvaluated = 0;

        /// <summary>
        /// Contains the stack frames for this thread
        /// </summary>
        public ArrayList __stackFrames = null;

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
        /// <param name="aEngine"> The AD7Engine object that represents the DE. </param>
        /// <param name="id"> Thread's ID. </param>
        /// <param name="targetID"> Process' ID + Thread's ID. </param>
        /// <param name="state"> Thread's state. </param>
        /// <param name="priority"> Thread's priority. </param>
        /// <param name="name"> Thread's name. </param>
        /// <param name="fullname"> Full short path file name. </param>
        /// <param name="line"> Line number. </param>
        public AD7Thread(AD7Engine aEngine, string id, string targetID, string state, string priority, string name, string fullname, string line)//, DebuggedThread debuggedThread)
        {
            _engine = aEngine;
            _suspendCount = 0;
            if (id == "1")
                _threadDisplayName = "Main Thread";
            else
                _threadDisplayName = (name != "") ? name : "<No Name>";

            if (fullname.Contains("~"))
            {
                // Need to lengthen the path used by Visual Studio.
                StringBuilder longPathName = new StringBuilder(1024);
                GetLongPathName(fullname, longPathName, longPathName.Capacity);
                _filename = longPathName.ToString();
            }
            else
                _filename = fullname;

            try
            {
                _line = Convert.ToUInt32(line);
            }
            catch
            {
                _line = 0;
            }

            _id = id;
            _state = state;
            _targetID = targetID;
            _priority = priority;
        }


        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the long path name for a given short one. </summary>
        /// <param name="path">Short path name. </param>
        /// <param name="longPath">Returns this long path name. </param>
        /// <param name="longPathLength"> Lenght of this long path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength
            );
        

        /// <summary>
        /// Called by EventDipatcher to set the current location during break mode.
        /// </summary>
        /// <param name="filename">  Full short path file name. </param>
        /// <param name="line"> Line number. </param>
        public void setCurrentLocation(string filename, uint line)
        {
            if (filename.Contains("~"))
            {
                // Need to lengthen the path used by Visual Studio.
                StringBuilder longPathName = new StringBuilder(1024);
                GetLongPathName(filename, longPathName, longPathName.Capacity);
                _filename = longPathName.ToString();
            }
            else
                _filename = filename;
            _line = line;
        }


        /// <summary>
        /// Gets the function name.
        /// </summary>
        /// <returns> If successful, returns the function name; otherwise, returns "". </returns>
        public string getFunctionName()
        {
            string func = "";

            if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                _engine.eDispatcher.selectThread(this._id);

            string stackResponse = _engine.eDispatcher.getStackFrames().Replace("#;;;;", "");

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

                if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                    _engine.eDispatcher.selectThread(this._engine.currentThread()._id);
            }

            return func;
        }


        /// <summary>
        /// Determines whether the next statement can be set to the given stack frame and code context. Not implemented.
        /// (http://msdn.microsoft.com/en-ca/library/bb146582.aspx)
        /// </summary>
        /// <param name="pStackFrame"> Reserved for future use; set to a null value. If this is a null value, use the current 
        /// stack frame. </param>
        /// <param name="pCodeContext"> An IDebugCodeContext2 object that describes the code location about to be executed and 
        /// its context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Retrieves a list of the stack frames for this thread. (http://msdn.microsoft.com/en-ca/library/bb145138.aspx)
        /// </summary>
        /// <param name="dwFieldSpec"> A combination of flags from the FRAMEINFO_FLAGS enumeration that specifies which fields of the 
        /// FRAMEINFO structures are to be filled out. </param>
        /// <param name="nRadix"> Radix used in formatting numerical information in the enumerator. </param>
        /// <param name="ppEnum"> Returns an IEnumDebugFrameInfo2 object that contains a list of FRAMEINFO structures describing the 
        /// stack frame. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 ppEnum)
        {
            if (this._id == "")
            {
                ppEnum = null;
                return Constants.S_FALSE;
            }

            if (this._engine.evaluatedTheseFlags(this._id, dwFieldSpec))
            {
                ppEnum = new AD7FrameInfoEnum(previousFrameInfoArray);
                return Constants.S_OK;
            }

            // Ask for general stack information.
            if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                _engine.eDispatcher.selectThread(this._id);

            string stackResponse = _engine.eDispatcher.getStackFrames().Replace("#;;;;", "");
            if (stackResponse == "")
            {
                ppEnum = null;
                return Constants.S_FALSE;
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
                        if (frameInfo[3].Contains("~"))
                        {
                            // Need to lengthen the path used by Visual Studio.
                            StringBuilder longPathName = new StringBuilder(1024);
                            GetLongPathName(frameInfo[3], longPathName, longPathName.Capacity);
                            frameInfo[3] = longPathName.ToString();
                        }
                        AD7StackFrame frame = AD7StackFrame.create(_engine, this, frameInfo, ref created);
                        frame.SetFrameInfo(dwFieldSpec, out frameInfoArray[i]);
                    }
                }
                if ((previousFrameInfoArray.Length != frameInfoArray.Length) || (created == true))
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

                if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                    _engine.eDispatcher.selectThread(this._engine.currentThread()._id);

                return Constants.S_OK;
            }
            catch (ComponentException e)
            {
                if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                    _engine.eDispatcher.selectThread(this._engine.currentThread()._id);
                return e.HResult;
            }
            catch (Exception e)
            {
                if ((this._id != "") && (this._id != this._engine.currentThread()._id))
                    _engine.eDispatcher.selectThread(this._engine.currentThread()._id);
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Gets the logical thread associated with this physical thread. Not implemented. 
        /// (http://msdn.microsoft.com/en-ca/library/bb161676.aspx)
        /// </summary>
        /// <param name="pStackFrame"> An IDebugStackFrame2 object that represents the stack frame. </param>
        /// <param name="ppLogicalThread"> Returns an IDebugLogicalThread2 interface that represents the associated logical 
        /// thread. A debug engine implementation should set this to a null value. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 pStackFrame, out IDebugLogicalThread2 ppLogicalThread)
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
            ppProgram = _engine.m_program;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the system thread identifier. (http://msdn.microsoft.com/en-ca/library/bb161964.aspx)
        /// </summary>
        /// <param name="pdwThreadId"> Returns the system thread identifier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.GetThreadId(out uint pdwThreadId)
        {
            try
            {
                pdwThreadId = Convert.ToUInt32(this._id);
            }
            catch
            {
                pdwThreadId = 0;
            }
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the properties that describe this thread. (http://msdn.microsoft.com/en-ca/library/bb145602.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the THREADPROPERTY_FIELDS enumeration that determines which fields of 
        /// ptp are to be filled in. </param>
        /// <param name="ptp"> A THREADPROPERTIES structure that that is filled in with the properties of the thread. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] ptp)
        {
            try
            {
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
                {
                    try
                    {
                        ptp[0].dwThreadId = Convert.ToUInt32(this._id);
                    }
                    catch
                    {
                        ptp[0].dwThreadId = 0;
                    }
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT) != 0)
                {
                    // VSNDK debug engine doesn't support suspending threads
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
                {
                    if (this._state == "running")
                        ptp[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    else
                        ptp[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_STOPPED;
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_PRIORITY) != 0)
                {
                    ptp[0].bstrPriority = "Normal";
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_PRIORITY;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
                {
                    ptp[0].bstrName = _threadDisplayName;
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_LOCATION) != 0)
                {
                    ptp[0].bstrLocation = "";
                    if (__stackFrames != null)
                    {
                        foreach (AD7StackFrame frame in __stackFrames)
                        {
                            if (frame.m_functionName != "")
                            {
                                ptp[0].bstrLocation = frame.m_functionName;
                                break;
                            }
                        }
                    }
                    if (ptp[0].bstrLocation == "")
                        ptp[0].bstrLocation = "[External Code]";

                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_LOCATION;
                }

                return Constants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }


        /// <summary>
        /// Resumes execution of a thread. (http://msdn.microsoft.com/en-ca/library/bb145813.aspx)
        /// </summary>
        /// <param name="pdwSuspendCount"> Returns the suspend count after the resume operation. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.Resume(out uint pdwSuspendCount)
        {
            _suspendCount--;
            pdwSuspendCount = _suspendCount;
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
        /// <param name="pStackFrame"> Reserved for future use; set to a null value. </param>
        /// <param name="pCodeContext"> An IDebugCodeContext2 object that describes the code location about to be executed and 
        /// its context. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.SetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Sets the name of the thread. (http://msdn.microsoft.com/en-ca/library/bb162322.aspx)
        /// </summary>
        /// <param name="pszName"> The name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread2.SetThreadName(string pszName)
        {
            _threadDisplayName = pszName;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Suspends a thread. (http://msdn.microsoft.com/en-us/library/bb145297.aspx)
        /// </summary>
        /// <param name="pdwSuspendCount"> Returns the suspend count after the suspend operation. </param>
        /// <returns> VSConstants.S_OK </returns>
        int IDebugThread2.Suspend(out uint pdwSuspendCount)
        {
            _suspendCount++;
            pdwSuspendCount = _suspendCount;
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
            this._threadDisplayName = name;
            return Constants.S_OK;
        }


        /// <summary>
        /// Gets the name of a thread.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.getthreaddisplayname.aspx)
        /// </summary>
        /// <param name="name"> Returns the name of the thread. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugThread100.GetThreadDisplayName(out string name)
        {
            name = this._threadDisplayName;
            return Constants.S_OK;
        }


        /// <summary>
        /// Returns whether this thread can be used to do function/property evaluation. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.candofunceval.aspx)
        /// </summary>
        /// <returns> VSConstants.S_FALSE. </returns>
        int IDebugThread100.CanDoFuncEval()
        {
            return Constants.S_FALSE;
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
            return Constants.E_NOTIMPL;
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
            return Constants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets the properties that describe this thread.
        /// (http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.debugger.interop.idebugthread100.getthreadproperties100.aspx)
        /// </summary>
        /// <param name="dwFields"> A combination of flags from the THREADPROPERTIES100 enumeration that determines which fields of 
        /// ptp are to be filled in. </param>
        /// <param name="ptp"> A THREADPROPERTIES100 structure that that is filled in with the properties of the thread. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns an error code. </returns>
        int IDebugThread100.GetThreadProperties100(uint dwFields, THREADPROPERTIES100[] ptp)
        {
            try
            {
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_ID) != 0)
                {
                    try
                    {
                        ptp[0].dwThreadId = Convert.ToUInt32(this._id);
                    }
                    catch
                    {
                        ptp[0].dwThreadId = 0;
                    }
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_ID;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_SUSPENDCOUNT) != 0)
                {
                    // VSNDK debug engine doesn't support suspending threads
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_SUSPENDCOUNT;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_STATE) != 0)
                {
                    if (this._state == "running")
                        ptp[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    else
                        ptp[0].dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_STOPPED;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_STATE;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY) != 0)
                {
                    ptp[0].bstrPriority = "Normal";
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_NAME) != 0)
                {
                    ptp[0].bstrName = _threadDisplayName;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_NAME;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME) != 0)
                {
                    // Thread display name is being requested
                    ptp[0].bstrDisplayName = _threadDisplayName;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME;

                    // Give this display name a higher priority than the default (0)
                    // so that it will actually be displayed
                    ptp[0].DisplayNamePriority = 10;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_DISPLAY_NAME_PRIORITY;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_LOCATION) != 0)
                {
                    ptp[0].bstrLocation = "";
                    if (__stackFrames != null)
                    {
                        foreach (AD7StackFrame frame in __stackFrames)
                        {
                            if ((frame.m_functionName != "") && (frame.m_functionName != "??"))
                            {
                                ptp[0].bstrLocation = frame.m_functionName;
                                break;
                            }
                        }
                    }
                    else
                        ptp[0].bstrLocation = getFunctionName();

                    if (ptp[0].bstrLocation == "")
                        ptp[0].bstrLocation = "[External Code]";

                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_LOCATION;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY) != 0)
                {
                    if (this._id == "1")
                        ptp[0].dwThreadCategory = (uint)enum_THREADCATEGORY.THREADCATEGORY_Main;
                    else
                        ptp[0].dwThreadCategory = (uint)enum_THREADCATEGORY.THREADCATEGORY_Worker;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_CATEGORY;
                }
                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY) != 0)
                {
                    // Thread cpu affinity is being requested
                    ptp[0].AffinityMask = 0;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_AFFINITY;
                }

                if ((dwFields & (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID) != 0)
                {
                    // Thread display name is being requested
                    ptp[0].priorityId = 0;
                    ptp[0].dwFields |= (uint)enum_THREADPROPERTY_FIELDS100.TPF100_PRIORITY_ID;
                }
                return Constants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #endregion
    }
}
