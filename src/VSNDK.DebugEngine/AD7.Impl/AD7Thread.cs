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
    enum enum_THREADCATEGORY // from http://social.msdn.microsoft.com/Forums/en-US/vsx/thread/807e26cc-4f3f-4e90-a9a8-b550d484b8c1
    {
        THREADCATEGORY_Worker = 0,
        THREADCATEGORY_UI = (THREADCATEGORY_Worker + 1),
        THREADCATEGORY_Main = (THREADCATEGORY_UI + 1),
        THREADCATEGORY_RPC = (THREADCATEGORY_Main + 1),
        THREADCATEGORY_Unknown = (THREADCATEGORY_RPC + 1)
    };

    // This class implements IDebugThread2 which represents a thread running in a program.
    public class AD7Thread : IDebugThread2, IDebugThread100
    {
        public AD7Engine _engine = null;
        public string _threadDisplayName;
        private uint _suspendCount;
        public string _filename;
        public uint _line;
        public int _alreadyEvaluated = 0;
        public ArrayList __stackFrames = null;
        public FRAMEINFO[] previousFrameInfoArray = new FRAMEINFO[0];

        public bool _current;
        public string _id;
        public string _state;
        public string _targetID;
        public string _priority;

        public AD7Thread(AD7Engine aEngine, bool current, string id, string targetID, string state, string priority, string name, string fullname, string line)//, DebuggedThread debuggedThread)
//        public AD7Thread(AD7Engine aEngine, bool current, string id, string targetID, string state, string priority, string name)//, DebuggedThread debuggedThread)
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

            _current = current;
            _id = id;
            _state = state;
            _targetID = targetID;
            _priority = priority;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength
            );
        

        // Called by EventDipatcher to set current location during break mode
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

        // Called by StackFrame to get path to file and line number set during break mode
        public void getCurrentLocation(out string filename, out uint line)
        {
            filename = _filename;
            line = _line;
        }

        int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_OK;
        }

        int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 ppEnum)
        {
            // Query the stack depth inquiring GDB.
//            int numStackFrames = _engine.eDispatcher.getStackDepth();
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
                        if (frame.m_thread.__stackFrames == null) // that's weird, but sometimes VS is not initializing __stackFrames, so I added this loop to avoid other problems.
                        {
                            while (frame.m_thread.__stackFrames == null)
                                frame.m_thread.__stackFrames = new ArrayList() { frame };
                            //                        frame.m_thread.__stackFrames.Add(frame);
                        }
                        //                    if ((_filename != "") || (created == true))
                        frame.SetFrameInfo(dwFieldSpec, out frameInfoArray[i]);
                    }
                }
                // Ignoring when _filename is null to avoid duplicate entries in Call Stack Window.
//                if ((_filename == "")  && (created == false))
//                {
//                    ppEnum = null;
//                    if (this._id != "")
//                        _engine.eDispatcher.selectThread(this._engine.currentThread()._id);
//                    return Constants.S_FALSE;
//                }
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
//                GDBParser.parseCommand("-stack-select-frame 0", 17);

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

        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 pStackFrame, out IDebugLogicalThread2 ppLogicalThread)
        {
            ppLogicalThread = null;
            return VSConstants.S_OK;
        }

        int IDebugThread2.GetName(out string pbstrName)
        {
            pbstrName = _threadDisplayName;
            return VSConstants.S_OK;
        }

        int IDebugThread2.GetProgram(out IDebugProgram2 ppProgram)
        {
            ppProgram = _engine.m_program;
            return VSConstants.S_OK;
        }

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
            //                pdwThreadId = 0;// (uint)m_debuggedThread.Id;
        }

        int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] ptp)
        {
            try
            {
                //                THREADPROPERTIES props = new THREADPROPERTIES();
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
                    // sample debug engine doesn't support suspending threads
                    ptp[0].dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0)
                {
                    //                    props.dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
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

        int IDebugThread2.SetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_OK;
        }

        int IDebugThread2.SetThreadName(string pszName)
        {
            _threadDisplayName = pszName;
            return VSConstants.S_OK;
        }

        int IDebugThread2.Suspend(out uint pdwSuspendCount)
        {
            _suspendCount++;
            pdwSuspendCount = _suspendCount;
            return VSConstants.S_OK;
        }

        #region IDebugThread100 Members

        int IDebugThread100.SetThreadDisplayName(string name)
        {
            this._threadDisplayName = name;
            return Constants.S_OK;
        }

        int IDebugThread100.GetThreadDisplayName(out string name)
        {
            name = this._threadDisplayName;
            return Constants.S_OK;
        }

        // Returns whether this thread can be used to do function/property evaluation.
        int IDebugThread100.CanDoFuncEval()
        {
            return Constants.S_FALSE;
        }

        int IDebugThread100.SetFlags(uint flags)
        {
            // Not necessary to implement in the debug engine. Instead
            // it is implemented in the SDM.
            return Constants.E_NOTIMPL;
        }

        int IDebugThread100.GetFlags(out uint flags)
        {
            // Not necessary to implement in the debug engine. Instead
            // it is implemented in the SDM.
            flags = 0;
            return Constants.E_NOTIMPL;
        }

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
                    // sample debug engine doesn't support suspending threads
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
