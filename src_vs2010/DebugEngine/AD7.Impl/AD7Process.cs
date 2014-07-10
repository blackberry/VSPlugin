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
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class represents a process running on a port. If the port is the local port, then IDebugProcess2 usually represents a 
    /// physical process on the local machine. Not implemented completely. (http://msdn.microsoft.com/en-ca/library/bb147137.aspx).
    /// 
    /// It also implements IDebugProcessEx2: This interface lets the session debug manager (SDM) notify a process that it is 
    /// attaching to or detaching from the process. (http://msdn.microsoft.com/en-us/library/bb145892.aspx)
    /// 
    /// Process "Is a container for a set of programs".
    /// 
    /// This interface is implemented by a custom port supplier to manage programs as a group. An IDebugProcess2 contains one or more 
    /// IDebugProgram2 interfaces.
    /// </summary>
    public sealed class AD7Process : IDebugProcess2, IDebugProcessEx2
    {
        /// <summary>
        /// Identifies the session in which this process is attached to.
        /// </summary>
        private IDebugSession2 _session;

        /// <summary>
        /// The name and ID of the process running on target device.
        /// </summary>
        private readonly ProcessInfo _details;

        /// <summary>
        /// The AD7Port object that represents the port used in Attach to Process UI or on which the process was launched.
        /// </summary>
        private AD7Port _port;

        /// <summary>
        /// The list of programs that are running in this process.
        /// </summary>
        private readonly List<IDebugProgram2> _listOfPrograms;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"> The IDebugPort2 object that represents the port on which the process was launched. </param>
        public AD7Process(AD7Port port)
        {
            if (port == null)
                throw new ArgumentNullException("port");

            _port = port;
            _listOfPrograms = new List<IDebugProgram2>();
            UID = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"> The AD7Port object that represents the port used in Attach to Process UI. </param>
        /// <param name="details"> The name and ID of the process running on device. </param>
        public AD7Process(AD7Port port, ProcessInfo details)
        {
            if (port == null)
                throw new ArgumentNullException("port");
            if (details == null)
                throw new ArgumentNullException("details");

            _port = port;
            _details = details;
            _listOfPrograms = new List<IDebugProgram2>();
            UID = Guid.NewGuid();
        }

        #region Properties

        public uint ID
        {
            get { return _details != null ? _details.ID : 0; }
        }

        public Guid UID
        {
            get;
            private set;
        }

        public ProcessInfo Details
        {
            get { return _details; }
        }

        public AD7Port Port
        {
            get { return _port; }
        }

        public DeviceDefinition Device
        {
            get { return _port != null ? _port.Device : null; }
        }

        #endregion

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
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Retrieves a list of all the programs contained by this process.
        /// (http://msdn.microsoft.com/en-us/library/bb162305.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPrograms2 object that contains a list of all the programs in the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            if (_listOfPrograms.Count == 0)
            {
                AD7ProgramNodeAttach pn = new AD7ProgramNodeAttach(this, new Guid(AD7Engine.DebugEngineGuid));
                _listOfPrograms.Add(pn);
            }

            ppEnum = new AD7ProgramEnum(_listOfPrograms.ToArray());
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
        /// Gets a description of the process. (http://msdn.microsoft.com/en-us/library/bb145895.aspx)
        /// </summary>
        /// <param name="fields"> A combination of values from the PROCESS_INFO_FIELDS enumeration that specifies which fields of 
        /// the pProcessInfo parameter are to be filled in. </param>
        /// <param name="pProcessInfo"> A PROCESS_INFO structure that is filled in with a description of the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetInfo(enum_PROCESS_INFO_FIELDS fields, PROCESS_INFO[] pProcessInfo)
        {
            try
            {
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME) != 0)
                {
                    pProcessInfo[0].bstrFileName = _details.ExecutablePath;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_BASE_NAME) != 0)
                {
                    pProcessInfo[0].bstrBaseName = _details.Name;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_BASE_NAME;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_TITLE) != 0)
                {
                    pProcessInfo[0].bstrTitle = _details.ShortExecutablePath;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_TITLE;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_PROCESS_ID) != 0)
                {
                    pProcessInfo[0].ProcessId.dwProcessId = _details.ID;
                    pProcessInfo[0].ProcessId.ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_PROCESS_ID;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_SESSION_ID) != 0)
                {
//                    pProcessInfo[0].dwSessionId = 0;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_SESSION_ID;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_ATTACHED_SESSION_NAME) != 0)
                {
//                    pProcessInfo[0].bstrAttachedSessionName = null;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_ATTACHED_SESSION_NAME;
                }
                if ((fields & enum_PROCESS_INFO_FIELDS.PIF_CREATION_TIME) != 0)
                {
//                    pProcessInfo[0].CreationTime = null;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_CREATION_TIME;
                }

                return VSConstants.S_OK;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        /// <summary>
        /// Gets the name of the process. (http://msdn.microsoft.com/en-us/library/bb161270.aspx)
        /// </summary>
        /// <param name="gnType"> A value from the GETNAME_TYPE enumeration that specifies what type of name to return. </param>
        /// <param name="pbstrName"> Returns the name of the process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            pbstrName = _details.Name;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the system process identifier. (http://msdn.microsoft.com/en-us/library/bb146648.aspx)
        /// </summary>
        /// <param name="pProcessId"> An AD_PROCESS_ID structure that is filled in with the system process identifier information. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            if (_details != null)
            {
                pProcessId[0].dwProcessId = _details.ID;
                pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM;
            }
            else
            {
                pProcessId[0].guidProcessId = UID;
                pProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            }

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
            pguidProcessId = UID;
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
            ppServer = null;
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


        #region IDebugProcessEx2 Members

        /// <summary>
        /// Informs the process that a session is now debugging the process. (http://msdn.microsoft.com/en-us/library/bb162300.aspx)
        /// </summary>
        /// <param name="pSession"> A value that uniquely identifies the session attaching to this process. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int Attach(IDebugSession2 pSession)
        {
            _session = pSession;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Informs the process that a session is no longer debugging the process. (http://msdn.microsoft.com/en-us/library/bb146313.aspx)
        /// </summary>
        /// <param name="pSession"> A value that uniquely identifies the session to detach this process from. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int Detach(IDebugSession2 pSession)
        {
            _session = pSession;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Adds program nodes for a list of debug engines. (http://msdn.microsoft.com/en-us/library/bb146990.aspx)
        /// In this project, it is used only one debug engine, that's why its GUID is assigned to the guidLaunchingEngine explicitly.
        /// </summary>
        /// <param name="guidLaunchingEngine"> The GUID of a DE that is to be used to launch programs (and is assumed to add its own 
        /// program nodes). </param>
        /// <param name="rgguidSpecificEngines"> Array of GUIDs of DEs for which program nodes will be added. </param>
        /// <param name="celtSpecificEngines"> The number of GUIDs in the rgguidSpecificEngines array. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int AddImplicitProgramNodes(ref Guid guidLaunchingEngine, Guid[] rgguidSpecificEngines, uint celtSpecificEngines)
        {
            guidLaunchingEngine = new Guid(AD7Engine.DebugEngineGuid);
            return VSConstants.S_OK;
        }

        #endregion
        
    }
}
