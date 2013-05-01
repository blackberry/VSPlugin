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

namespace VSNDK.DebugEngine
{
    // This class implements IDebugProgramNode2.
    // This interface represents a program that can be debugged. 
    // A running process is viewed as a ProgramNode by VS. ProgramNode = Debugged process while Process = Debugging process (I am not sure about this last one, process, need to clarify!); 
    // A debug engine (DE) or a custom port supplier implements this interface to represent a program that can be debugged. 
    public class AD7ProgramNode : IDebugProgramNode2
    {
        readonly Guid m_processGuid;
        public string m_programID;
        public string m_programName;
        readonly string m_exePath;
        readonly Guid m_engineGuid;
//        protected AD7Engine _engine;
//        public AD7Engine eEngine
//        {
//            get { return _engine; }
//        }

        public AD7ProgramNode(Guid processGuid)
        // only for testing purposes, because GetProviderProcessData (AD7ProgramProvider.cs) needs a program node, but I don't know why.
        // I have to figure it out, later.
        {
            m_processGuid = processGuid;
            m_programID = "";
            m_exePath = "";
            m_programName = "";
            m_engineGuid = new Guid(AD7Engine.Id);
        }

        public AD7ProgramNode(Guid processGuid, string programID, string exePath, Guid engineGuid)
        {
            m_processGuid = processGuid;
            m_programID = programID;
//            _engine = aEngine;

            m_exePath = exePath;
            do
            {
                m_exePath = m_exePath.Replace("\\\\", "\\");
            }
            while (m_exePath.IndexOf("\\\\") != -1);

            int i = exePath.LastIndexOf('\\');
            if ((i != -1) && (i < (exePath.Length - 1)))
                m_programName = exePath.Substring(i + 1);
            else
                m_programName = exePath;

            m_engineGuid = engineGuid;
        }

        #region IDebugProgramNode2 Members

        // Gets the name and identifier of the DE running this program.
        int IDebugProgramNode2.GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            engineName = "";
            engineGuid = m_engineGuid;

            return VSConstants.S_OK;
        }

        // Gets the system process identifier for the process hosting a program.
        int IDebugProgramNode2.GetHostPid(AD_PROCESS_ID[] pHostProcessId)
        {
            // According to the MSDN documentation (http://msdn.microsoft.com/en-us/library/bb162159.aspx),
            // it should return the process id of the hosting process, but what is expected is the program ID...
            pHostProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pHostProcessId[0].guidProcessId = m_processGuid;
//            pHostProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM;
//            pHostProcessId[0].dwProcessId = Convert.ToUInt32(m_programID);

            return VSConstants.S_OK;
        }

        // Gets the name of the process hosting a program.
        int IDebugProgramNode2.GetHostName(enum_GETHOSTNAME_TYPE dwHostNameType, out string processName)
        {
//??            // Since we are using default transport and don't want to customize the process name, this method doesn't need
//??            // to be implemented.
            //            processName = null;
            //            return VSConstants.E_NOTIMPL;
            if (dwHostNameType == enum_GETHOSTNAME_TYPE.GHN_FILE_NAME)
//                processName = m_exePath;
                processName = "(BB-pid = " + m_programID + ") " + m_exePath;
            else
                processName = m_programName;
            return VSConstants.S_OK;
        }

        // Gets the name of a program.
        int IDebugProgramNode2.GetProgramName(out string programName)
        {
//??            // Since we are using default transport and don't want to customize the process name, this method doesn't need
//??            // to be implemented.
            programName = m_programName;
            return VSConstants.S_OK;
        }

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugProgramNode2.Attach_V7(IDebugProgram2 pMDMProgram, IDebugEventCallback2 pCallback, uint dwReason)
        {
            Debug.Fail("This function is not called by the debugger");

            return VSConstants.E_NOTIMPL;
        }

        int IDebugProgramNode2.DetachDebugger_V7()
        {
            Debug.Fail("This function is not called by the debugger");

            return VSConstants.E_NOTIMPL;
        }

        int IDebugProgramNode2.GetHostMachineName_V7(out string hostMachineName)
        {
            Debug.Fail("This function is not called by the debugger");

            hostMachineName = null;
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }

}