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
    /// <summary>
    /// This class represents a process running on a port. If the port is the local port, then IDebugProcess2 usually represents a 
    /// physical process on the local machine. Not implemented completely. (http://msdn.microsoft.com/en-ca/library/bb147137.aspx).
    /// 
    /// Process "Is a container for a set of programs".
    /// 
    /// This interface is implemented by a custom port supplier to manage programs as a group. An IDebugProcess2 contains one or more 
    /// IDebugProgram2 interfaces.
    /// </summary>
    public class AD7Process : IDebugProcess2
    {
        /// <summary>
        /// The name of the process. Not used till now. Has no value assigned to it.
        /// </summary>
        string _name;

        /// <summary>
        /// The IDebugPort2 object that represents the port on which the process was launched.
        /// </summary>
        IDebugPort2 _port = null;

        /// <summary>
        /// Process GUID.
        /// </summary>
        public Guid _processID = Guid.NewGuid();

        /// <summary>
        /// The server on which this process is running. Not used till now. Has no value assigned to it.
        /// </summary>
        IDebugCoreServer2 _server;

        /// <summary>
        /// The AD7Engine object that represents the DE.
        /// </summary>
        protected AD7Engine _engine = null;

        /// <summary>
        /// A program that is running in this process. It should be an array of programs, as a process "is a container for 
        /// a set of programs" but, at this moment, the VSNDK supports only one program at a time.
        /// </summary>
        internal IDebugProgram2 m_program = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="aEngine"> The AD7Engine object that represents the DE. </param>
        /// <param name="aPort"> The IDebugPort2 object that represents the port on which the process was launched. </param>
        public AD7Process(AD7Engine aEngine, IDebugPort2 aPort)
        {
            _engine = aEngine;
            _port = aPort;
            m_program = aEngine.m_program;
        }


        /// <summary>
        /// Attaches the session debug manager (SDM) to the process. Not implemented. Currently using IDebugEngine2.Attach.
        /// (http://msdn.microsoft.com/en-ca/library/bb145875.aspx)
        /// </summary>
        /// <param name="pCallback"> An IDebugEventCallback2 object that is used for debug event notification. </param>
        /// <param name="rgguidSpecificEngines"> An array of GUIDs of debug engines to be used to debug programs running in the process. 
        /// This parameter can be a null value. </param>
        /// <param name="celtSpecificEngines"> The number of debug engines in the rgguidSpecificEngines array and the size of the 
        /// rghrEngineAttach array. </param>
        /// <param name="rghrEngineAttach"> An array of HRESULT codes returned by the debug engines. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Determines if the session debug manager (SDM) can detach the process. Not implemented. 
        /// (http://msdn.microsoft.com/en-us/library/bb145567.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int CanDetach()
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Requests that the next program that is running code in this process halt and send an IDebugBreakEvent2 event object.
        /// Not implemented. (http://msdn.microsoft.com/en-us/library/bb162292.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int CauseBreak()
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Detaches the debugger from this process by detaching all of the programs in the process. 
        /// (http://msdn.microsoft.com/en-us/library/bb162197.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int Detach()
        {
            _port = null;
            _engine = null;
            m_program = null;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Retrieves a list of all the programs contained by this process. Not implemented. 
        /// (http://msdn.microsoft.com/en-us/library/bb162305.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPrograms2 object that contains a list of all the programs in the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Retrieves a list of all the threads running in all programs in the process. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/bb144981.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugThreads2 object that contains a list of all threads in all programs in the 
        /// process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the name of the session that is debugging the process. [DEPRECATED. SHOULD ALWAYS RETURN E_NOTIMPL.]
        /// (http://msdn.microsoft.com/en-us/library/bb146718.aspx)
        /// </summary>
        /// <param name="pbstrSessionName"> DEPRECATED. Return null. </param>
        /// <returns> This method should always return E_NOTIMPL. </returns>
        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            pbstrSessionName = null;
            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Gets a description of the process. Not implemented. (http://msdn.microsoft.com/en-us/library/bb145895.aspx)
        /// </summary>
        /// <param name="Fields"> A combination of values from the PROCESS_INFO_FIELDS enumeration that specifies which fields of 
        /// the pProcessInfo parameter are to be filled in. </param>
        /// <param name="pProcessInfo"> A PROCESS_INFO structure that is filled in with a description of the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        {
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the title, friendly name, or file name of the process. Not implemented completely because _name has no value.
        /// (http://msdn.microsoft.com/en-us/library/bb161270.aspx)
        /// </summary>
        /// <param name="gnType"> A value from the GETNAME_TYPE enumeration that specifies what type of name to return. </param>
        /// <param name="pbstrName"> Returns the name of the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            pbstrName = _name;
            return VSConstants.S_OK;
        }


        public readonly Guid PhysID = Guid.NewGuid();
        /// <summary>
        /// Gets the system process identifier. (http://msdn.microsoft.com/en-us/library/bb146648.aspx)
        /// </summary>
        /// <param name="pProcessId"> An AD_PROCESS_ID structure that is filled in with the system process identifier information. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0].guidProcessId = PhysID;
            pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;

            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the port that the process is running on. (http://msdn.microsoft.com/en-us/library/bb162121.aspx)
        /// </summary>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port on which the process was launched. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPort(out IDebugPort2 ppPort)
        {
            ppPort = _port;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the GUID for this process. (http://msdn.microsoft.com/en-us/library/bb161938.aspx)
        /// </summary>
        /// <param name="pguidProcessId"> Returns the GUID for this process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetProcessId(out Guid pguidProcessId)
        {
            pguidProcessId = _processID;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Gets the server that this process is running on. Not implemented completely because _server has no value assigned to it.
        /// (http://msdn.microsoft.com/en-us/library/bb147017.aspx)
        /// </summary>
        /// <param name="ppServer"> Returns an IDebugCoreServer2 object that represents the server on which this process is running. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            ppServer = _server;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Terminates the process. Not implemented. (http://msdn.microsoft.com/en-us/library/bb146226.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int Terminate()
        {
            return VSConstants.S_OK;
        }
    }
}
