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
using BlackBerry.NativeCore.Model;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using VSNDK.Parser;

namespace BlackBerry.DebugEngine
{
    /// <summary>
    /// This class supplies ports to the Session Debug Manager (SDM).
    /// 
    /// It implements:
    /// 
    /// - IDebugPortSupplier2 - This interface supplies ports to the session debug manager (SDM). (http://msdn.microsoft.com/en-ca/library/bb145819.aspx)
    /// - IDebugPortSupplierDescription2 - Enables the Visual Studio UI to display text inside the Transport Information section of 
    ///   the Attach to Process dialog box. (http://msdn.microsoft.com/en-us/library/bb458056.aspx)
    /// 
    /// It is needed to enable updating the list of processes in the "attach to process" user interface.
    /// </summary>
    [ComVisible(true)]
    [Guid(ClassGuid)]
    public sealed class AD7PortSupplier : IDebugPortSupplier2, IDebugPortSupplierDescription2
    {
        public const string PublicName = "BlackBerry Native Debugger";
        public const string ClassGuid = "BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6";
        public const string ClassName = "BlackBerry.DebugEngine.AD7PortSupplier";

        private const string DevicePortGuid = "{69519DBB-5329-4CCE-88A9-EC1628AD99C2}";
        private const string SimulatorPortGuid = "{25040BDD-6683-4D5C-8EFA-EB4DDF5CA08E}";

        /// <summary>
        /// The NDK host path.
        /// </summary>
        private string _toolsPath = "";

        /// <summary>
        /// List of ports for this port supplier.
        /// </summary>
        private readonly Dictionary<Guid, AD7Port> _ports = new Dictionary<Guid, AD7Port>();

        private int VerifyAndAddPorts()
        {
            // Returning because VS can debug only one app at a time.
            if (GDBParser.s_running)
                return 0;

            var ndk = NdkDefinition.Load();
            var device = DeviceDefinition.Load(DeviceDefinitionType.Device);
            var simulator = DeviceDefinition.Load(DeviceDefinitionType.Simulator);

            if (ndk == null || string.IsNullOrEmpty(ndk.ToolsPath))
                return -1;
            _toolsPath = ndk.ToolsPath;

            if (device != null)
            {
                IDebugPort2 p;
                AddPort(new AD7PortRequest("Device: " + device.IP, device), out p);
            }

            if (simulator != null)
            {
                IDebugPort2 p;
                AddPort(new AD7PortRequest("Simulator: " + simulator.IP, simulator), out p);
            }
            return 1;
        }

        /// <summary>
        /// Creates an AD7Port.
        /// </summary>
        /// <param name="request">Port request</param>
        /// <returns>Returns an AD7Port</returns>
        AD7Port CreatePort(AD7PortRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (request.Device == null)
                throw new ArgumentOutOfRangeException("request");

            bool isSimulator = request.Device.Type == DeviceDefinitionType.Simulator;
            var guid = isSimulator ? new Guid(SimulatorPortGuid) : new Guid(DevicePortGuid);

            return new AD7Port(this, request, guid, request.Name, request.Device.Password, isSimulator, _toolsPath);
        }

        #region Implementation of IDebugPortSupplier2
        
        /// <summary>
        /// Adds a port. (http://msdn.microsoft.com/en-ca/library/bb161980.aspx)
        /// </summary>
        /// <param name="pRequest"> An IDebugPortRequest2 object that describes the port to be added. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
        {
            bool success = true;
            AD7PortRequest port_request = null;
            AD7Port port = null;

            try
            {
                port_request = (AD7PortRequest)pRequest;
            }
            catch
            {
                success = false;
                string portRequestName;
                AD7Port defaultPort = null;
                pRequest.GetPortName(out portRequestName);
                string search;
                if (portRequestName.ToLower().Contains("device"))
                    search = "device";
                else if (portRequestName.ToLower().Contains("simulator"))
                    search = "simulator";
                else
                {
                    search = portRequestName.ToLower();
                }
                foreach (var p in _ports)
                {
                    AD7Port tempPort = p.Value;
                    if (defaultPort == null)
                        defaultPort = tempPort;

                    string tempPortName;
                    tempPort.GetPortName(out tempPortName);
                    if (tempPortName.ToLower().Contains(search))
                    {
                        port = tempPort;
                        break;
                    }
                    else
                    {
                        string IP = search;
                        do
                        {
                            int pos = IP.LastIndexOf('.');
                            if (pos != -1)
                            {
                                IP = IP.Remove(pos);
                                if (tempPortName.Contains(IP))
                                {
                                    port = tempPort;
                                    break;
                                }
                            }
                            else
                                IP = "";
                        } while (IP != "");
                        if (IP != "")
                            break;
                    }
                }
                if (port == null)
                {
                    if (defaultPort != null)
                    {
                        port = defaultPort;
                    }
                    else
                        port = new AD7Port(this, port_request, Guid.NewGuid(), "", "", true, "");
                }
            }
            if (success)
            {
                port = CreatePort(port_request);
                Guid portGuid;
                port.GetPortId(out portGuid);
                _ports.Add(portGuid, port);
            }
            ppPort = port;
            return VSConstants.S_OK; 
        }


        /// <summary>
        /// Verifies that a port supplier can add new ports. (http://msdn.microsoft.com/en-ca/library/bb145880.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        public int CanAddPort()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Retrieves a list of all the ports supplied by a port supplier. (http://msdn.microsoft.com/en-ca/library/bb146984.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPorts2 object containing a list of ports supplied. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            _ports.Clear();
            int success = VerifyAndAddPorts();
            AD7Port[] ports = new AD7Port[_ports.Count];

            if (_ports.Count > 0)
            {
                int i = 0;
                foreach (var p in _ports)
                {
                    ports[i] = p.Value;
                    i++;
                }
            }
            else
            {
                if (success == 0)
                    MessageBox.Show("Visual Studio can debug only one BlackBerry application at a time.\n\nPlease, select a different transport or close the current debug session.", "Visual Studio is already debugging an application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (success == -1)
                    MessageBox.Show("You must select an API Level to be able to attach to a running process.\n\nPlease, use \"BlackBerry -> Settings -> Get more\" to download one.", "Missing NDK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show("Missing Device/Simulator information. Please, use menu BlackBerry -> Settings to add any of those information.", "Missing Device/Simulator Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ppEnum = new AD7PortEnum(ports);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets a port from a port supplier. (http://msdn.microsoft.com/en-ca/library/bb161812.aspx)
        /// </summary>
        /// <param name="guidPort"> Globally unique identifier (GUID) of the port. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            ppPort = _ports[guidPort];
            return VSConstants.S_OK; 
        }

        /// <summary>
        /// Gets the port supplier identifier. (http://msdn.microsoft.com/en-ca/library/bb146617.aspx)
        /// </summary>
        /// <param name="pguidPortSupplier"> Returns the GUID of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortSupplierId(out Guid pguidPortSupplier)
        {
            pguidPortSupplier = new Guid(ClassGuid);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the port supplier name. (http://msdn.microsoft.com/en-ca/library/bb162136.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortSupplierName(out string pbstrName)
        {
            pbstrName = PublicName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Removes a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162306.aspx)
        /// </summary>
        /// <param name="pPort"> An IDebugPort2 object that represents the port to be removed. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        public int RemovePort(IDebugPort2 pPort)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IDebugPortSupplierDescription2

        /// <summary>
        /// Retrieves the description for the port supplier. (http://msdn.microsoft.com/en-us/library/bb457978.aspx)
        /// </summary>
        /// <param name="pdwFlags"> Metadata flags for the description. </param>
        /// <param name="pbstrText"> Description of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] pdwFlags, out string pbstrText)
        {
            pbstrText = "The BlackBerry Native Transport lets you select and attach to a process that is running on a device or simulator";
            return VSConstants.S_OK;
        }

        #endregion
    }
}