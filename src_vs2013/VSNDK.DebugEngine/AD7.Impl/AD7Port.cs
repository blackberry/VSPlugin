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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using VSNDK.Parser;
using System.Windows.Forms;

namespace VSNDK.DebugEngine
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
    public class AD7Port : IDebugPort2
    {

        /// <summary>
        /// The name of the port.
        /// </summary>
        private readonly string m_name;

        /// <summary>
        /// The IP of the port.
        /// </summary>
        public readonly string m_IP;

        /// <summary>
        /// The password needed to have access to the port.
        /// </summary>
        public readonly string m_password;

        /// <summary>
        /// Boolean variable that indicates if the port is associated to the Simulator or the Device.
        /// </summary>
        public readonly bool m_isSimulator;

        /// <summary>
        /// The NDK host path.
        /// </summary>
        public readonly string m_toolsPath;

        /// <summary>
        /// List of processes running on this port.
        /// </summary>
        private readonly List<AD7Process> m_processes = new List<AD7Process>();

        /// <summary>
        /// The GUID that identifies the port.
        /// </summary>
        private readonly Guid m_guid;

        /// <summary>
        /// Represents the request that was used to create the port.
        /// </summary>
        private readonly AD7PortRequest m_request;

        /// <summary>
        /// Represents the port supplier for this port.
        /// </summary>
        private readonly AD7PortSupplier m_supplier;

        /// <summary>
        /// Stores the last time that the list of running processes was refreshed. 
        /// Used to avoid issues in case the user spams the refresh button.
        /// </summary>
        private DateTime lastTimeRefresh = DateTime.Now;

        /// <summary>
        /// Stores the list of running processes in the simulator/device.
        /// </summary>
        private IDebugProcess2[] processes = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="supplier"> The port supplier for this port. </param>
        /// <param name="request"> The request used to create the port. </param>
        /// <param name="guid"> The GUID that identifies the port. </param>
        /// <param name="portName"> The name of the port. </param>
        /// <param name="password"> The password needed to have access to the port. </param>
        /// <param name="isSimulator"> Variable that indicates if the port is associated to the Simulator or the Device. </param>
        /// <param name="toolsPath"> The NDK host path. </param>
        public AD7Port(AD7PortSupplier supplier, AD7PortRequest request, Guid guid, string portName, string password, bool isSimulator, string toolsPath)
        {
            m_name = portName;
            m_request = request;
            m_supplier = supplier;
            m_guid = guid;
            if (m_name != "")
            {
                if (isSimulator)
                    m_IP = m_name.Substring(11);
                else
                    m_IP = m_name.Substring(8);
            }
            else
            {
                m_name = " ";
                m_IP = "";
            }
            m_password = password;
            m_isSimulator = isSimulator;
            m_toolsPath = toolsPath;
        }


        /// <summary>
        /// Gets the list of processes running on this port.
        /// </summary>
        /// <returns> Returns the list of processes running on this port. </returns>
        IEnumerable<AD7Process> GetProcesses()
        {
            if (m_processes.Count() != 0)
            {
                m_processes.Clear();
            }

            string publicKeyPath = Environment.GetEnvironmentVariable("AppData") + @"\BlackBerry\bbt_id_rsa.pub";

            string response = GDBParser.GetPIDsThroughGDB(m_IP, m_password, m_isSimulator, m_toolsPath, publicKeyPath, 12);

            if ((response == "TIMEOUT!") || (response.IndexOf("1^error,msg=", 0) != -1)) //found an error
            {
                if (response == "TIMEOUT!") // Timeout error, normally happen when the device is not connected.
                {
                    MessageBox.Show("Please, verify if the Device/Simulator IP in \"BlackBerry -> Settings\" menu is correct and check if it is connected.", "Device/Simulator not connected or not configured properly", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (response.IndexOf("1^error,msg=", 0) != -1) // error: 1^error,msg="169.254.0.3:8000: The requested address is not valid in its context."
                {
                    string txt = "";
                    int pos = response.IndexOf('"');
                    if (pos != -1)
                    {
                        txt = response.Substring(pos);
                    }
//                    string txt = response.Substring(13, response.IndexOf(':', 13) - 13) + response.Substring(29, response.IndexOf('"', 31) - 29);
                    string caption = "";
                    if (txt.IndexOf("The requested address is not valid in its context.") != -1)
                    {
                        txt += "\n\nPlease, verify the BlackBerry device/simulator IP settings.";
                        caption = "Invalid IP";
                    }
                    else
                    {
                        txt += "\n\nPlease, verify if the device/simulator is connected.";
                        caption = "Connection failed";
                    }
                    MessageBox.Show(txt, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                response = "";
            }
            else if (response.Contains("^done"))
            {
                response = response.Remove(response.IndexOf("^done"));
                string[] tempListOfProcesses = response.Split('\n');

                int ind = (response[0] == '&') ? 1 : 0; // ignore the first if it starts with & (&"info pidlist")
                while (ind < tempListOfProcesses.Length - 1)
                {
                    string process = tempListOfProcesses[ind];
                    int pos = process.LastIndexOf('/');
                    if (pos == -1)
                    {
                        ind++;
                        continue;
                    }
                    process = process.Remove(pos).Substring(2);
                    for (ind = ind + 1; ind < tempListOfProcesses.Length - 1; ind++) // ignore the duplicates
                    {
                        int pos2 = tempListOfProcesses[ind].LastIndexOf('/');
                        if ((pos2 <= 2) || (tempListOfProcesses[ind].Substring(2, pos2 - 2) != process))
                            break;
                    }
                    AD7Process proc = new AD7Process(this, process.Substring(process.IndexOf("- ") + 2), process.Remove(process.IndexOf(" ")));
                    m_processes.Add(proc);
                }
            }
            return m_processes;
        }


        #region Implementation of IDebugPort2

        /// <summary>
        /// Returns the port name. (http://msdn.microsoft.com/en-us/library/bb145890.aspx)
        /// </summary>
        /// <param name="pbstrName"> Returns the name of the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortName(out string pbstrName)
        {
            pbstrName = m_name;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Returns the port identifier. (http://msdn.microsoft.com/en-us/library/bb146747.aspx)
        /// </summary>
        /// <param name="pguidPort"> Returns the GUID that identifies the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortId(out Guid pguidPort)
        {
            pguidPort = m_guid;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Returns the request used to create a port. (http://msdn.microsoft.com/en-us/library/bb145127.aspx)
        /// </summary>
        /// <param name="ppRequest"> Returns an IDebugPortRequest2 object representing the request that was used to create the port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortRequest(out IDebugPortRequest2 ppRequest)
        {
            ppRequest = m_request;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Returns the port supplier for this port. (http://msdn.microsoft.com/en-us/library/bb146688.aspx)
        /// </summary>
        /// <param name="ppSupplier"> Returns an IDebugPortSupplier2 object represents the port supplier for a port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int GetPortSupplier(out IDebugPortSupplier2 ppSupplier)
        {
            ppSupplier = m_supplier;
            return VSConstants.S_OK;
        }


        /// <summary>
        /// Returns the specified process running on a port. (http://msdn.microsoft.com/en-us/library/bb145867.aspx)
        /// </summary>
        /// <param name="ProcessId"> An AD_PROCESS_ID structure that specifies the process identifier. </param>
        /// <param name="ppProcess"> Returns an IDebugProcess2 object representing the process. </param>
        /// <returns> If successful, returns VSConstants.S_OK; otherwise, returns VSConstants.S_FALSE. </returns>
        public int GetProcess(AD_PROCESS_ID ProcessId, out IDebugProcess2 ppProcess)
        {
            IEnumerable<AD7Process> procList = GetProcesses();
            var proc = from p in procList
                       where p._processID == ProcessId.dwProcessId.ToString()
                       select p;
            ppProcess = proc.FirstOrDefault();
            return ppProcess != null ? VSConstants.S_OK : VSConstants.S_FALSE;
        }


        /// <summary>
        /// Enumerates all the processes running on a port. (http://msdn.microsoft.com/en-us/library/bb161302.aspx)
        /// </summary>
        /// <param name="ppEnum"> Returns an IEnumDebugProcesses2 object that contains a list of all the processes running on a port. </param>
        /// <returns> VSConstants.S_OK. </returns>
        public int EnumProcesses(out IEnumDebugProcesses2 ppEnum)
        {
            if (this.m_IP == "")
            {
                processes = new IDebugProcess2[0];
            }
            else
            {
                DateTime now = DateTime.Now;

                TimeSpan diff = now - lastTimeRefresh;
                double seconds = diff.TotalSeconds;

                if (seconds > 1)
                {
                    IEnumerable<AD7Process> procList = GetProcesses();
                    processes = new IDebugProcess2[procList.Count()];
                    int i = 0;
                    foreach (var debugProcess in procList)
                    {
                        processes[i] = debugProcess;
                        i++;
                    }
                    lastTimeRefresh = DateTime.Now;
                }
            }
            ppEnum = new AD7ProcessEnum(processes);

            return VSConstants.S_OK;
        }

        #endregion

    }
}
