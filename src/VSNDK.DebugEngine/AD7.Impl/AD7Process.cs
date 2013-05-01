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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using VSNDK.Parser;

namespace VSNDK.DebugEngine
{
    public class AD7Process : IDebugProcess2
    {
        IEnumDebugPrograms2 _enumDebugPrograms = null;
        IEnumDebugThreads2 _enumDebugThreads = null;
        string _sessionName;  // has no value assigned because the method that uses it is "[DEPRECATED. SHOULD ALWAYS RETURN E_NOTIMPL.]"
        string _name; // has no value assigned. Check if method GetName() is called and the importance of _name.
        IDebugPort2 _port = null;
        public Guid _processID = Guid.NewGuid();
        IDebugCoreServer2 _server; // has no value assigned. Check if method GetServer() is called and the importance of it.
        AD_PROCESS_ID[] _adProcessId;
        protected EngineCallback _callback = null;
        protected AD7Engine _engine = null;

        internal IDebugProgram2 m_program = null;

        public AD7Process(EngineCallback aCallback, AD7Engine aEngine, IDebugPort2 aPort)
        {
            _callback = aCallback;
            _engine = aEngine;
            _port = aPort;
            m_program = aEngine.m_program;
            //                m_program = new AD7Program(this, _callback, _engine.eDispatcher);
        }

//        public int AddProgramToProcess()
//        {
//            m_program = new AD7Program(this, _callback, _engine.eDispatcher);
//            return VSConstants.S_OK;
//        }

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            return VSConstants.S_OK;
        }

        public int CanDetach()
        {
            return VSConstants.S_OK;
        }

        public int CauseBreak()
        {
            return VSConstants.S_OK;
        }

        public int Detach()
        {
            _enumDebugPrograms = null;
            _enumDebugThreads = null;
            _port = null;
            _callback = null;
            _engine = null;
            m_program = null;
            return VSConstants.S_OK;
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
//            ppEnum = _enumDebugPrograms;
            ppEnum = null;
            return VSConstants.S_OK;
        }

        // Retrieves a list of all the threads running in all programs in the process.
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
//            ppEnum = _enumDebugThreads;
            ppEnum = null;
            return VSConstants.S_OK;
        }

        // Gets the name of the session that is debugging the process. [DEPRECATED. SHOULD ALWAYS RETURN E_NOTIMPL.]
        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            pbstrSessionName = _sessionName;
            return VSConstants.E_NOTIMPL;
        }

        public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        {
            return VSConstants.S_OK;
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            pbstrName = _name;
            return VSConstants.S_OK;
        }

        public readonly Guid PhysID = Guid.NewGuid();
        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0].guidProcessId = PhysID;
            pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;

            return VSConstants.S_OK;
        }

        public int GetPort(out IDebugPort2 ppPort)
        {
            ppPort = _port;
            return VSConstants.S_OK;
        }

        public int GetProcessId(out Guid pguidProcessId)
        {
            pguidProcessId = _processID;
            return VSConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            ppServer = _server;
            return VSConstants.S_OK;
        }

        public int Terminate()
        {
            return VSConstants.S_OK;
        }

        internal void ResumeFromLaunch()
        {
            // Do not call -exec-continue here because the SDM will call AD7Engine.Continue() after responding to the first stopping event
        }
    }
}
