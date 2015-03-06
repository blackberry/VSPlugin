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
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.Package.Helpers;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;
using System.Windows.Forms;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class implements a port.
    /// 
    /// It implements:
    /// 
    /// - IDebugPort2 - This interface represents a debug port on a machine. (http://msdn.microsoft.com/en-us/library/bb147021.aspx)
    /// - IConnectionPointContainer - Supports connection points for connectable objects. Indicates to a client that the object is 
    ///   connectable and provides the IConnectionPoint interface. (http://msdn.microsoft.com/en-CA/library/ms683857.aspx)
    /// - IConnectionPoint - Supports connection points for connectable objects. (http://msdn.microsoft.com/en-us/library/ms694318.aspx)
    /// </summary>
    public sealed class AD7Port : IDebugPort2
    {
        /// <summary>
        /// Represents the request that was used to create the port.
        /// </summary>
        private readonly AD7PortRequest _request;

        /// <summary>
        /// Represents the port supplier for this port.
        /// </summary>
        private readonly AD7PortSupplier _supplier;

        /// <summary>
        /// Stores the last time that the list of running processes was refreshed. 
        /// Used to avoid issues in case the user spams the refresh button.
        /// </summary>
        private DateTime _lastTimeRefresh = DateTime.MinValue;

        /// <summary>
        /// Stores the list of running processes in the simulator/device.
        /// </summary>
        private IDebugProcess2[] _processes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="supplier">The port supplier for this port.</param>
        /// <param name="request">The request used to create the port.</param>
        /// <param name="guid">The GUID that identifies the port.</param>
        /// <param name="name">The name of the port.</param>
        /// <param name="device">Description of the device it should communicate with.</param>
        /// <param name="ndk">The description of NDK installed on host machine.</param>
        public AD7Port(AD7PortSupplier supplier, AD7PortRequest request, Guid guid, string name, DeviceDefinition device, NdkDefinition ndk)
        {
            if (supplier == null)
                throw new ArgumentNullException("supplier");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (device == null)
                throw new ArgumentNullException("device");
            if (ndk == null)
                throw new ArgumentNullException("ndk");

            Name = name;
            _request = request;
            _supplier = supplier;
            Guid = guid;
            Device = device;
            NDK = ndk;
        }

        #region Properties

        /// <summary>
        /// Gets or set the GUID that identifies the port.
        /// </summary>
        public Guid Guid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the name of the port.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        public DeviceDefinition Device
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the NDK reference.
        /// </summary>
        public NdkDefinition NDK
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Gets the list of processes running on this port.
        /// </summary>
        /// <returns> Returns the list of processes running on this port. </returns>
        private AD7Process[] GetProcesses()
        {
            ////////////////////////////////////////////////////////
            // "the old way", which seemed not to work on PlayBook
            //var processesRequest = GdbWrapper.ListProcesses(NDK, Device);
            //processesRequest.Processes... 

            ////////////////////////////////////////////////////////
            // "the new way" is using a dedicated separate connection to the device,
            // that servers extra data for IDEs:

            // get QConnClient and connect to the device, if not yet connected:
            SystemInfoProcess[] processes = null;

            try
            {
                var qClient = Targets.Get(Device, ConfigDefaults.SshPublicKeyPath, ConfigDefaults.SshPrivateKeyPath);
                if (qClient != null)
                {
                    processes = qClient.SysInfoService.LoadProcesses();
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load list of processes");

                // try to disconnect, so retry will have a clean-state:
                Targets.Disconnect(Device);
            }

            // timeout or other problem?
            if (processes == null)
            {
                string deviceType = Device.Type == DeviceDefinitionType.Device ? "device" : "simulator";
                MessageBoxHelper.Show(string.Concat("Please, verify if the ", deviceType, " (", Device.IP, ") is defined correctly. It can be modified at \"BlackBerry -> Options -> Targets\" tab."),
                    "Connection failure or timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return new AD7Process[0];
            }

            // ok, got the valid response
            var result = new List<AD7Process>();
            foreach (var p in processes)
            {
                result.Add(new AD7Process(this, p));
            }

            return result.ToArray();
        }

        #region Implementation of IDebugPort2

        /// <summary>
        /// Returns the port name. (http://msdn.microsoft.com/en-us/library/bb145890.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortName(out string pbstrName)
        {
            pbstrName = Name;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the port identifier. (http://msdn.microsoft.com/en-us/library/bb146747.aspx)
        /// </summary>
        /// <param name="pguidPort"> Returns the GUID that identifies the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortId(out Guid pguidPort)
        {
            pguidPort = Guid;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the request used to create a port. (http://msdn.microsoft.com/en-us/library/bb145127.aspx)
        /// </summary>
        /// <param name="ppRequest"> Returns an IDebugPortRequest2 object representing the request that was used to create the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortRequest(out IDebugPortRequest2 ppRequest)
        {
            ppRequest = _request;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the port supplier for this port. (http://msdn.microsoft.com/en-us/library/bb146688.aspx)
        /// </summary>
        /// <param name="ppSupplier"> Returns an IDebugPortSupplier2 object represents the port supplier for a port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortSupplier(out IDebugPortSupplier2 ppSupplier)
        {
            ppSupplier = _supplier;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the specified process running on a port. (http://msdn.microsoft.com/en-us/library/bb145867.aspx)
        /// </summary>
        /// <param name="processId"> An AD_PROCESS_ID structure that specifies the process identifier. </param>
        /// <param name="ppProcess"> Returns an IDebugProcess2 object representing the process. </param>
        /// <returns> If successful, returns VSConstants.S_OK; otherwise, returns VSConstants.S_FALSE. </returns>
        public int GetProcess(AD_PROCESS_ID processId, out IDebugProcess2 ppProcess)
        {
            IEnumerable<AD7Process> list = GetProcesses();
            uint id = processId.dwProcessId;

            ppProcess = null;
            foreach (var process in list)
            {
                if (process.ID == id)
                {
                    ppProcess = process;
                    break;
                }
            }
            return ppProcess != null ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        /// <summary>
        /// Enumerates all the processes running on a port. (http://msdn.microsoft.com/en-us/library/bb161302.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugProcesses2 object that contains a list of all the processes running on a port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumProcesses(out IEnumDebugProcesses2 ppEnum)
        {
            if (string.IsNullOrEmpty(Device.IP))
            {
                _processes = new IDebugProcess2[0];
            }
            else
            {
                DateTime now = DateTime.Now;

                TimeSpan diff = now - _lastTimeRefresh;
                double seconds = diff.TotalSeconds;

                if (seconds > 1)
                {
                    var processlist = GetProcesses();
                    _processes = new IDebugProcess2[processlist.Length];
                    int i = 0;
                    foreach (var debugProcess in processlist)
                    {
                        _processes[i] = debugProcess;
                        i++;
                    }
                    _lastTimeRefresh = DateTime.Now;
                }
            }

            ppEnum = new AD7ProcessEnum(_processes);
            return VSConstants.S_OK;
        }

        #endregion
    }
}
