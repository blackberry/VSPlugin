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

using BlackBerry.NativeCore.Debugger.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class implements IDebugProgramNode2. (http://msdn.microsoft.com/en-ca/library/bb146735.aspx)
    /// This interface represents a program that can be debugged. 
    /// A running process is viewed as a ProgramNode by VS. 
    /// A debug engine (DE) or a custom port supplier implements this interface to represent a program that can be debugged. 
    /// </summary>
    public sealed class AD7ProgramNode : IDebugProgramNode2
    {
        /// <summary>
        ///  The GUID of the hosting process. 
        /// </summary>
        private readonly Guid _processGuid;

        private readonly ProcessInfo _details;

        /// <summary>
        /// Constructor.
        /// At this moment, used only for testing purposes, because GetProviderProcessData (AD7ProgramProvider.cs) needs a program node.
        /// </summary>
        /// <param name="processGuid"> The GUID for this process. </param>
        public AD7ProgramNode(Guid processGuid)
        {
            _processGuid = processGuid;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="details"> The description of the the debugged program. </param>
        /// <param name="processGuid"> The GUID for this process. </param>
        /// <param name="engineGuid"> The GUID of the VSNDK debug engine. </param>
        public AD7ProgramNode(ProcessInfo details, Guid processGuid)
        {
            if (details == null)
                throw new ArgumentNullException("details");

            _details = details;
            _processGuid = processGuid;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="process">The process associated with the node.</param>
        public AD7ProgramNode(AD7Process process)
        {
            if (process == null)
                throw new ArgumentNullException("process");
            if (process.Details == null)
                throw new ArgumentOutOfRangeException("process");

            _details = process.Details;
            _processGuid = process.UID;
        }

        #region Properties

        public string Name
        {
            get { return _details.Name; }
        }

        #endregion

        #region IDebugProgramNode2 Members


        /// <summary>
        /// Gets the name and identifier of the DE running this program. (http://msdn.microsoft.com/en-ca/library/bb146303.aspx)
        /// </summary>
        /// <param name="engineName"> Returns the name of the DE running the program (C++-specific: this can be a null pointer 
        /// indicating that the caller is not interested in the name of the engine). </param>
        /// <param name="engineGuid"> Returns the globally unique identifier of the DE running the program (C++-specific: this can 
        /// be a null pointer indicating that the caller is not interested in the GUID of the engine). </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramNode2.GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            engineName = string.Empty;
            engineGuid = new Guid(AD7Engine.DebugEngineGuid);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the system process identifier for the process hosting a program. (http://msdn.microsoft.com/en-us/library/bb162159.aspx)
        /// </summary>
        /// <param name="pHostProcessId"> Returns the system process identifier for the hosting process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramNode2.GetHostPid(AD_PROCESS_ID[] pHostProcessId)
        {
            // According to the MSDN documentation (http://msdn.microsoft.com/en-us/library/bb162159.aspx),
            // it should return the process id of the hosting process, but what is expected is the program ID...
            pHostProcessId[0].ProcessIdType = (uint) enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pHostProcessId[0].guidProcessId = _processGuid;

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the name of the process hosting a program. (http://msdn.microsoft.com/en-us/library/bb145135.aspx)
        /// </summary>
        /// <param name="dwHostNameType"> A value from the GETHOSTNAME_TYPE enumeration that specifies the type of name to return. </param>
        /// <param name="processName"> Returns the name of the hosting process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramNode2.GetHostName(enum_GETHOSTNAME_TYPE dwHostNameType, out string processName)
        {
            if (dwHostNameType == enum_GETHOSTNAME_TYPE.GHN_FILE_NAME)
                processName = "(BB-pid = " + _details.ID + ") " + _details.ExecutablePath;
            else
                processName = _details.Name;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the name of a program. (http://msdn.microsoft.com/en-us/library/bb145928.aspx)
        /// </summary>
        /// <param name="programName"> Returns the name of the program. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramNode2.GetProgramName(out string programName)
        {
            programName = _details.Name;
            return VSConstants.S_OK;
        }

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        /// <summary>
        /// DEPRECATED. DO NOT USE. (http://msdn.microsoft.com/en-us/library/bb161399.aspx)
        /// </summary>
        /// <param name="pMDMProgram"> The IDebugProgram2 interface that represents the program to attach to. </param>
        /// <param name="pCallback"> The IDebugEventCallback2 interface to be used to send debug events to the SDM. </param>
        /// <param name="dwReason"> A value from the ATTACH_REASON enumeration that specifies the reason for attaching. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugProgramNode2.Attach_V7(IDebugProgram2 pMDMProgram, IDebugEventCallback2 pCallback, uint dwReason)
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// DEPRECATED. DO NOT USE. (http://msdn.microsoft.com/en-us/library/bb161803.aspx)
        /// </summary>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugProgramNode2.DetachDebugger_V7()
        {
            return EngineUtils.NotImplemented();
        }

        /// <summary>
        /// DEPRECATED. DO NOT USE. (http://msdn.microsoft.com/en-us/library/bb161297.aspx)
        /// </summary>
        /// <param name="hostMachineName"> Returns the name of the machine in which the program is running. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugProgramNode2.GetHostMachineName_V7(out string hostMachineName)
        {
            hostMachineName = null;
            return EngineUtils.NotImplemented();
        }

        #endregion
    }
}
