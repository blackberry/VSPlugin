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
using System.ComponentModel;
using System.Windows.Data;
using RIM.VSNDK_Package.Settings;
using System.IO;
using System.Xml;
using System.Linq;

namespace RIM.VSNDK_Package.UpdateManager.Model
{

    /// <summary>
    /// Class to store API Targets
    /// </summary>
    public class APITargetClass
    {
        public string TargetName { get; set; }
        public string TargetDescription { get; set; }
        public string TargetVersion { get; set; }
        public string IsInstalled { get; set; }
        public string IsAvailable { get; set; }
        public bool IsBeta { get; set; }

        public APITargetClass(string name, string description, string version)
        {
            TargetName = name;
            TargetDescription = description;
            TargetVersion = version;
            IsInstalled = "visible";
            IsAvailable = "visible";
            IsBeta = false;
        }
    }

    /// <summary>
    /// Data Model for the Update Manager Dialog
    /// </summary>
    class UpdateManagerData : INotifyPropertyChanged
    {
        #region Member Variables

        private bool isAvailable = false;
        private CollectionView _apiTargets;
        private APITargetClass _apiTarget;

        public IList<APITargetClass> tempAPITargetList;
        private List<string> installedAPIList;

        private const string _colAPITarget = "APITarget";

        #endregion

        #region Public Member Functions

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManagerData()
        {
            RefreshScreen();
        }

        public void RefreshScreen()
        {
            GetInstalledAPIList();
            GetAvailableAPIList();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Getter for the NDKEntries property
        /// </summary>
        public CollectionView APITargets
        {
            get { return _apiTargets; }
            set { _apiTargets = value; }
        }

        /// <summary>
        /// Getter/Setter for the NDKEntryClass property
        /// </summary>
        public APITargetClass APITarget
        {
            get { return _apiTarget; }
            set
            {
                if (_apiTarget == value) return;
                _apiTarget = value;
                OnPropertyChanged(_colAPITarget);
            }
        }

        #endregion

        #region Private Member Functions

        /// <summary>
        /// Check to see if API is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <param name="name">Check API name</param>
        /// <returns>true if installed</returns>
        private bool IsAPIInstalled(string version, string name)
        {
            bool success = false;

            if (installedAPIList.Contains(name))
            {
                success = true;
            }
            else
            {
                string result = installedAPIList.FirstOrDefault(s => s.Contains(version));

                if (result != null)
                {
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Get list of installed APIs
        /// </summary>
        /// <returns></returns>
        private bool GetInstalledAPIList()
        {
            bool success = false;

            string[] filePaths = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", "*.xml");
            installedAPIList = new List<string>();

            foreach (string file in filePaths)
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);
                    XmlNodeList name = xmlDoc.GetElementsByTagName("name");

                    installedAPIList.Add(name.Item(0).InnerText);

                    success = true;
                }
                catch
                {
                    success = false;
                    break;
                }
            }

            return success;
        }

        /// <summary>
        /// Retrieve list of API's from 
        /// </summary>
        /// <returns></returns>
        private bool GetAvailableAPIList()
        {
            bool success = false;

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);


            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C C:\bbndk\sdkinstall --list");

            try
            {
                tempAPITargetList = new List<APITargetClass>();

                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    APITargets = new CollectionView(tempAPITargetList);
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;

        }

        /// <summary>
        /// Install Specified API
        /// </summary>
        /// <param name="version">version of API to install</param>
        /// <returns>true if successful</returns>
        public bool InstallAPI(string version)
        {
            bool success = false;

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);


            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C C:\bbndk\sdkinstall --install {0}", version);

            try
            {
                isAvailable = false;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;

                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Uninstall Specified API
        /// </summary>
        /// <param name="version">version of API to uninstall</param>
        /// <returns>true if successful</returns>
        public bool UninstallAPI(string version)
        {
            bool success = false;

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);


            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C C:\bbndk\sdkinstall --uninstall {0}", version);

            try
            {
                isAvailable = false;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            string name = "";
            string description = "";
            string version = "";

            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);

                if (e.Data.Contains("Available SDKs:"))
                {
                    isAvailable = true;
                }
                else
                {
                    if (isAvailable)
                    {
                        version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                        name = e.Data.Substring(e.Data.LastIndexOf(" - ") + 3);
                        description = "Device Support Unknown.";

                        APITargetClass api = new APITargetClass(name, description, version);

                        if (IsAPIInstalled(version, name))
                        {
                            api.IsInstalled = "visible";
                            api.IsAvailable = "collapsed";
                        }
                        else
                        {
                            api.IsInstalled = "collapsed";
                            api.IsAvailable = "visible";
                        }

                        api.IsBeta = name.Contains("Beta");

                        tempAPITargetList.Add(api);
                    }
                }
            }
        }

        /// <summary>
        /// On Error received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire the PropertyChnaged event handler on change of property
        /// </summary>
        /// <param name="propName"></param>
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }
}
