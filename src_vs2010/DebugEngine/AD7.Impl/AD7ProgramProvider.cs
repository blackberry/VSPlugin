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

namespace VSNDK.DebugEngine
{
    /// <summary>
    /// This class implments IDebugProgramProvider2. (http://msdn.microsoft.com/en-us/library/bb161298.aspx)
    /// This registered interface allows the session debug manager (SDM) to obtain information about programs 
    /// that have been "published" through the IDebugProgramPublisher2 interface.
    /// 
    /// Partially implemented and not used at this moment.
    /// </summary>
    [ComVisible(true)]
    [Guid("AD06FD46-C790-4D5C-A274-8815DF9511B8")]
    public class AD7ProgramProvider : IDebugProgramProvider2
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public AD7ProgramProvider()
        {
        }

        #region IDebugProgramProvider2 Members


        /// <summary>
        /// Obtains information about programs running, filtered in a variety of ways. 
        /// (http://msdn.microsoft.com/en-us/library/bb147025.aspx)
        /// </summary>
        /// <param name="Flags">  A combination of flags from the PROVIDER_FLAGS enumeration. </param>
        /// <param name="port"> The port the calling process is running on. </param>
        /// <param name="ProcessId"> An AD_PROCESS_ID structure holding the ID of the process that contains the program in question. </param>
        /// <param name="EngineFilter"> An array of GUIDs for debug engines assigned to debug this process (these will be used to filter 
        /// the programs that are actually returned based on what the supplied engines support; if no engines are specified, then all 
        /// programs will be returned). </param>
        /// <param name="processArray"> A PROVIDER_PROCESS_DATA structure that is filled in with the requested information. </param>
        /// <returns> If successful, returns S_OK; otherwise, returns VSConstants.S_FALSE. </returns>
        int IDebugProgramProvider2.GetProviderProcessData(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 port, AD_PROCESS_ID ProcessId, CONST_GUID_ARRAY EngineFilter, PROVIDER_PROCESS_DATA[] processArray)
        {
            processArray[0] = new PROVIDER_PROCESS_DATA();

            if (((uint)Flags & (uint)enum_PROVIDER_FLAGS.PFLAG_GET_PROGRAM_NODES) != 0)
            {
                // The debugger is asking the engine to return the program nodes it can debug. The VSNDK debug engine claims that it can 
                // debug all processes, and returns exactly one program node for each process. A full-featured debugger may wish to 
                // examine the target process and determine if it understands how to debug it.

                IDebugProgramNode2 node = (IDebugProgramNode2)(new AD7ProgramNode(ProcessId.guidProcessId));             

                IntPtr[] programNodes = { Marshal.GetComInterfaceForObject(node, typeof(IDebugProgramNode2)) };

                IntPtr destinationArray = Marshal.AllocCoTaskMem(IntPtr.Size * programNodes.Length);                
                Marshal.Copy(programNodes, 0, destinationArray, programNodes.Length);

                processArray[0].Fields = enum_PROVIDER_FIELDS.PFIELD_PROGRAM_NODES;
                processArray[0].ProgramNodes.Members = destinationArray;
                processArray[0].ProgramNodes.dwCount = (uint)programNodes.Length;

                return VSConstants.S_OK;
            }

            return VSConstants.S_FALSE;
        }


        /// <summary>
        /// Gets a program node, given a specific process ID. Not implemented. 
        /// (http://msdn.microsoft.com/en-us/library/bb162155.aspx)
        /// </summary>
        /// <param name="Flags"> A combination of flags from the PROVIDER_FLAGS enumeration. </param>
        /// <param name="port"> The port the calling process is running on. </param>
        /// <param name="ProcessId"> An AD_PROCESS_ID structure holding the ID of the process that contains the program in question. </param>
        /// <param name="guidEngine"> GUID of the debug engine that the program is attached to (if any). </param>
        /// <param name="programId"> ID of the program for which to get the program node. </param>
        /// <param name="programNode"> An IDebugProgramNode2 object representing the requested program node. </param>
        /// <returns> VSConstants.E_NOTIMPL. </returns>
        int IDebugProgramProvider2.GetProviderProgramNode(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 port, AD_PROCESS_ID ProcessId, ref Guid guidEngine, ulong programId, out IDebugProgramNode2 programNode)
        {
            // This method is used for Just-In-Time debugging support, which this program provider does not support
            programNode = null;
            return VSConstants.E_NOTIMPL;
        }


        /// <summary>
        /// Establishes a locale for any language-specific resources needed by the DE. Not implemented.
        /// (http://msdn.microsoft.com/en-us/library/bb161383.aspx)
        /// </summary>
        /// <param name="wLangID"> Language ID to establish. For example, 1033 for English. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramProvider2.SetLocale(ushort wLangID)
        {           
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Establishes a callback to watch for provider events associated with specific kinds of processes, allowing the process to 
        /// be notified of port events. Not implemented. (http://msdn.microsoft.com/en-us/library/bb145594.aspx)
        /// </summary>
        /// <param name="Flags">  A combination of flags from the PROVIDER_FLAGS enumeration. </param>
        /// <param name="port"> The port the calling process is running on. </param>
        /// <param name="ProcessId"> An AD_PROCESS_ID structure holding the ID of the process that contains the program in question. </param>
        /// <param name="EngineFilter"> An array of GUIDs of debug engines associated with the process. </param>
        /// <param name="guidLaunchingEngine"> GUID of the debug engine that launched this process (if any). </param>
        /// <param name="ad7EventCallback"> An IDebugPortNotify2 object that receives the event notifications. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugProgramProvider2.WatchForProviderEvents(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 port, AD_PROCESS_ID ProcessId, CONST_GUID_ARRAY EngineFilter, ref Guid guidLaunchingEngine, IDebugPortNotify2 ad7EventCallback)
        {
            // The VSNDK debug engine is a native debugger, and can therefore always provide a program node
            // in GetProviderProcessData. Non-native debuggers may wish to implement this method as a way
            // of monitoring the process before code for their runtime starts. For example, if implementing a 
            // 'foo script' debug engine, one could attach to a process which might eventually run 'foo script'
            // before this 'foo script' started.
            //
            // To implement this method, an engine would monitor the target process and call AddProgramNode
            // when the target process started running code which was debuggable by the engine. The 
            // enum_PROVIDER_FLAGS.PFLAG_ATTACHED_TO_DEBUGGEE flag indicates if the request is to start
            // or stop watching the process.
            
            return VSConstants.S_OK;
        }

        #endregion
    }
}
