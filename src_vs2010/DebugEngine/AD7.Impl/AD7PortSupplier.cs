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

using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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
    public sealed class AD7PortSupplier : IDebugPortSupplier2, IDebugPortSupplierDescription2, IDebugPortPicker
    {
        public const string PublicName = "BlackBerry Native Debugger";
        public const string ClassGuid = "BDC2218C-D50C-4A5A-A2F6-66BDC94FF8D6";
        public const string ClassName = "BlackBerry.DebugEngine.AD7PortSupplier";

        /// <summary>
        /// The NDK reference.
        /// </summary>
        private NdkDefinition _ndk;
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// List of ports for this port supplier.
        /// </summary>
        private readonly Dictionary<Guid, AD7Port> _ports = new Dictionary<Guid, AD7Port>();

        private IDebugPort2[] CreateDefaultPorts()
        {
            var ndk = NdkDefinition.Load();
            var devices = DeviceDefinition.LoadAll();

            if (ndk == null || string.IsNullOrEmpty(ndk.ToolsPath))
                return null;
            _ndk = ndk;

            var result = new List<IDebugPort2>();

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    result.Add(CreatePort(this, new AD7PortRequest(GetPortRequestName(device), device), _ndk));
                }
            }

            return result.ToArray();
        }

        private static string GetPortRequestName(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (string.IsNullOrEmpty(device.Name))
                return string.Concat(DeviceHelper.GetTypeToString(device.Type), ": ", device.IP);

            return string.Concat(DeviceHelper.GetTypeToString(device.Type), ": ", device.Name, " (", device.IP, ")");
        }

        private static string GetAdHocPortRequestName(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (string.IsNullOrEmpty(device.Name))
                return string.Concat(DeviceHelper.GetTypeToString(device.Type), " ", device.IP, " ", device.Password);
            return string.Concat(DeviceHelper.GetTypeToString(device.Type), " ", device.Name.Replace(' ', '_'), " ", device.IP, " ", device.Password);
        }

        /// <summary>
        /// Creates an AD7Port.
        /// </summary>
        private static AD7Port CreatePort(AD7PortSupplier supplier, AD7PortRequest request, NdkDefinition ndk)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (request.Device == null)
                throw new ArgumentOutOfRangeException("request");

            return new AD7Port(supplier, request, Guid.NewGuid(), request.Name, request.Device, ndk);
        }

        #region Implementation of IDebugPortSupplier2
        
        /// <summary>
        /// Adds a port. (http://msdn.microsoft.com/en-ca/library/bb161980.aspx)
        /// </summary>
        /// <param name="request"> An IDebugPortRequest2 object that describes the port to be added. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.AddPort(IDebugPortRequest2 request, out IDebugPort2 ppPort)
        {
            if (request == null || _ndk == null)
            {
                ppPort = null;
                return VSConstants.S_OK;
            }

            // is it one of the default requests created by the AD7PortSupplier?
            var portRequest = request as AD7PortRequest;
            if (portRequest != null)
            {
                var port = CreatePort(this, portRequest, _ndk);
                _ports[port.Guid] = port;
                ppPort = port;
                return VSConstants.S_OK;
            }

            string requestName;
            if (request.GetPortName(out requestName) == VSConstants.S_OK)
            {
                requestName = requestName.Trim();
                var port = FindPort(requestName);

                // create something at hoc?
                if (port == null)
                {
                    var device = CreateAdHocDevice(requestName);
                    if (device != null)
                    {
                        ppPort = new AD7Port(this, null, Guid.NewGuid(), GetPortRequestName(device), device, _ndk);
                        return VSConstants.S_OK;
                    }
                    
                    MessageBox.Show("Too few information about device you are trying to connect.\n\nPlease, follow the pattern: \"(device|simulator) IP password\".", "Unable to create device connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    ppPort = port;
                    return VSConstants.S_OK;
                }
            }

            ppPort = null;
            return VSConstants.E_FAIL;
        }

        private AD7Port FindPort(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            foreach (var port in _ports.Values)
            {
                if (port.Device.HasIdenticalIP(text) || port.Device.HasIdenticalName(text))
                    return port;
            }

            return null;
        }

        private static DeviceDefinition CreateAdHocDevice(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var items = text.Split(new[] { ' ', '|', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length < 2)
                return null;

            int i = 0;
            var type = DeviceDefinitionType.Device;
            string name = "Ad-hoc Connection";
            string ip = null;
            string password = null;

            try
            {
                type = DeviceHelper.GetTypeFromString(items[i++], true);
            }
            catch
            {
                // unable to decipher type: device/simulator...
                return null;
            }

            // name:
            if (items.Length > i && items[i].IndexOf('.') < 0)
            {
                name = items[i++];
            }

            // ip:
            if (items.Length > i && items[i].IndexOf('.') > 0)
            {
                ip = items[i++];
            }

            // password:
            if (items.Length > i)
            {
                password = items[i];
            }

            // verify input:
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(password))
                return null;

            return new DeviceDefinition(name, ip, password, type);
        }

        /// <summary>
        /// Verifies that a port supplier can add new ports. (http://msdn.microsoft.com/en-ca/library/bb145880.aspx)
        /// </summary>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.CanAddPort()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Retrieves a list of all the ports supplied by a port supplier. (http://msdn.microsoft.com/en-ca/library/bb146984.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugPorts2 object containing a list of ports supplied. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            // Returning because VS can debug only one app at a time.
            if (DebugEngineStatus.IsRunning)
            {
                MessageBox.Show("Visual Studio can debug only one BlackBerry application at a time.\n\nPlease, select a different transport or close the current debug session.", "Visual Studio is already debugging an application", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ppEnum = null;
                return VSConstants.S_FALSE;
            }

            var defaultPorts = CreateDefaultPorts();
            if (defaultPorts == null)
            {
                MessageBox.Show("You must select an API Level to be able to attach to a running process.\n\nPlease, use \"BlackBerry -> Options -> API Level\" to download or select one.", "Missing NDK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ppEnum = null;
                return VSConstants.S_FALSE;
            }
            if (defaultPorts.Length == 0)
            {
                MessageBox.Show("Missing Device/Simulator information. Please, use menu BlackBerry -> Settings to add any of those information.", "Missing Device/Simulator Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ppEnum = null;
                return VSConstants.S_FALSE;
            }

            _ports.Clear();
            foreach (var port in defaultPorts)
            {
                Guid portGuid;
                if (port.GetPortId(out portGuid) == VSConstants.S_OK)
                {
                    _ports[portGuid] = (AD7Port) port;
                }
            }

            ppEnum = new AD7PortEnum(defaultPorts);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets a port from a port supplier. (http://msdn.microsoft.com/en-ca/library/bb161812.aspx)
        /// </summary>
        /// <param name="guidPort"> Globally unique identifier (GUID) of the port. </param>
        /// <param name="ppPort"> Returns an IDebugPort2 object that represents the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            ppPort = _ports[guidPort];
            return VSConstants.S_OK; 
        }

        /// <summary>
        /// Gets the port supplier identifier. (http://msdn.microsoft.com/en-ca/library/bb146617.aspx)
        /// </summary>
        /// <param name="pguidPortSupplier"> Returns the GUID of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.GetPortSupplierId(out Guid pguidPortSupplier)
        {
            pguidPortSupplier = new Guid(ClassGuid);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Gets the port supplier name. (http://msdn.microsoft.com/en-ca/library/bb162136.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplier2.GetPortSupplierName(out string pbstrName)
        {
            pbstrName = PublicName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Removes a port. Not implemented. (http://msdn.microsoft.com/en-ca/library/bb162306.aspx)
        /// </summary>
        /// <param name="port"> An IDebugPort2 object that represents the port to be removed. </param>
        /// <returns> Not implemented. It should returns S_OK if successful; or an error code. </returns>
        int IDebugPortSupplier2.RemovePort(IDebugPort2 port)
        {
            return EngineUtils.NotImplemented();
        }

        #endregion

        #region Implementation of IDebugPortSupplierDescription2

        /// <summary>
        /// Retrieves the description for the port supplier. (http://msdn.microsoft.com/en-us/library/bb457978.aspx)
        /// </summary>
        /// <param name="pdwFlags"> Metadata flags for the description. </param>
        /// <param name="pbstrText"> Description of the port supplier. </param>
        /// <returns> VSConstants.S_OK. </returns>
        int IDebugPortSupplierDescription2.GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] pdwFlags, out string pbstrText)
        {
            pbstrText = "The BlackBerry Native Transport lets you select and attach to a process that is running on a device or simulator";
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// Sets the service provider. (http://msdn.microsoft.com/en-us/library/bb491408.aspx)
        /// </summary>
        /// <param name="serviceProvider">Reference to the interface of the service provider.</param>
        int IDebugPortPicker.SetSite(IServiceProvider serviceProvider)
        {
            _serviceProvider = new ServiceProvider(serviceProvider);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Displays the specified dialog box that allows the user to select a port. (http://msdn.microsoft.com/en-us/library/bb491260.aspx)
        /// </summary>
        /// <param name="hwndParentDialog">Handle for the parent dialog box.</param>
        /// <param name="pbstrPortId">Port identifier string.</param>
        /// <returns>If successful, returns S_OK; otherwise, returns an error code. A return value of S_FALSE (or a return value of S_OK with the BSTR set to NULL) indicates that the user clicked Cancel.</returns>
        int IDebugPortPicker.DisplayPortPicker(IntPtr hwndParentDialog, out string pbstrPortId)
        {
            var service = _serviceProvider != null ? _serviceProvider.GetService(typeof(IDeviceDiscoveryService)) as IDeviceDiscoveryService : null;

            if (service == null)
            {
                MessageBox.Show(
                    "Searching is not supported.\r\nPlease add more devices at \"BlackBerry -> Options -> Targets\", if you want to quickly switch between them. They will automatically appear, when \"Qualifier\" list is expanded.",
                    "Microsoft Visual Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
                pbstrPortId = null;
                return VSConstants.S_FALSE;
            }

            var device = service.FindDevice();

            // did we found the device?
            if (device != null)
            {
                pbstrPortId = GetAdHocPortRequestName(device);
                return VSConstants.S_OK;
            }

            // or cancelled:
            pbstrPortId = null;
            return VSConstants.S_FALSE;
        }
    }
}
