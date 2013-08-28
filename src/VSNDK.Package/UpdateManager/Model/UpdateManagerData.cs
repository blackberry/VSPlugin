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
using RIM.VSNDK_Package.DebugToken;
using System.IO;
using System.Xml;
using System.Linq;
using System.Windows;
using RIM.VSNDK_Package.DebugToken.Model;
using Microsoft.Win32;

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
        public string LatestVersion { get; set; }
        public int IsInstalled { get; set; }
        public bool IsUpdate { get; set; }
        public bool IsBeta { get; set; }

        public string InstalledVisibility
        {
            get { return IsInstalled > 0 ? "visible" : "collapsed"; }
        }

        public string AvailableVisibility
        {
            get { return IsInstalled == 0 ? "visible" : "collapsed"; }
        }

        public string UpdateVisibility
        {
            get { return IsUpdate ? "visible" : "collapsed"; }
        }

        public string NoUpdateVisibility
        {
            get { return IsUpdate ? "collapsed" : "visible"; }
        }

        public APITargetClass(string name, string description, string version)
        {
            TargetName = name;
            TargetDescription = description;
            TargetVersion = version;
            LatestVersion = version;
            IsInstalled = 0;
            IsUpdate = false;
            IsBeta = false;
        }
    }

    /// <summary>
    /// Class to store Installed APIs
    /// </summary>
    public class APIClass
    {
        public string Name { get; set; }
        public string HostName { get; set; }
        public string TargetName { get; set; }
        public string Version { get; set; }

        public APIClass(string name, string hostName, string targetName, string version)
        {
            Name = name;
            Version = version;
            HostName = hostName;
            TargetName = targetName;
        }
    }


    /// <summary>
    /// Data Model for the Update Manager Dialog
    /// </summary>
    class UpdateManagerData : INotifyPropertyChanged
    {
        #region Constants

        private const string _colAPITarget = "APITarget";
        private const string _colAPITargets = "APITargets";
        private const string _colStatus = "Status";
        private const string _colIsInstalling = "IsInstalling";

        #endregion

        #region Member Variables

        private bool isAvailable = false;
        private bool isInstalling = true;
        private string installVersion = "";
        private CollectionView _apiTargets;
        private APITargetClass _apiTarget;
        private string _errors;
        public string bbndkPathConst = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + "bbndk_vs";
        private string _status = "";


        public List<APITargetClass> tempAPITargetList;
        private List<APIClass> installedAPIList;
        private List<APIClass> installedNDKList;
        private List<string> installedRuntimeList;
        private List<string> installedSimulatorList;


        #endregion

        #region Public Member Functions

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManagerData()
        {
            Status = "";
            RefreshScreen();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Refresh">Force Refresh</param>
        public UpdateManagerData(bool Refresh)
        {
            Status = "";
            if (Refresh) RefreshScreen();
        }

        /// <summary>
        /// Refresh all the lists
        /// </summary>
        public void RefreshScreen()
        {
            GetInstalledAPIList();
            GetAvailableAPIList();
        }

        /// <summary>
        /// Install Specified API
        /// </summary>
        /// <param name="version">version of API to install</param>
        /// <returns>true if successful</returns>
        public bool InstallAPI(string version, bool isRuntime, bool isSimulator)
        {
            bool success = false;

            installVersion = version;

            Status = "Installing API Level";

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);
            p.Exited += new EventHandler(p_Exited);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --install {0} {1} {2}", version, isRuntime ? "--runtime" : "", isSimulator ? "--simulator" : "");

            try
            {
                isAvailable = false;
                IsInstalling = false;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
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

            Status = "Uninstalling API Level";

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);
            p.Exited += new EventHandler(p_Exited);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --uninstall {0}", version);

            try
            {
                isAvailable = false;
                IsInstalling = false;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
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
        /// Validate to make sure device matches the API Level chosen.
        /// </summary>
        /// <returns></returns>
        public bool validateDeviceVersion(bool isSimulator)
        {
            bool retVal = false;
            string baseVersion = "10.2.0.0";

            if (isSimulator)
            {
                string currentAPIVersion = getCurrentAPIVersion();
                if (IsSimulatorInstalled(currentAPIVersion))
                {
                    retVal = true;
                }
                else
                {
                    UpdateManagerDialog umd = new UpdateManagerDialog("The Simulator for your selected API Level is not currently installed.  Would you like to install it now?", currentAPIVersion, false, true);

                    if (umd.ShowDialog() == true)
                    {
                        retVal = false; //In this case return false to cancel build so that the user can start the newly installed simulator after install.
                    }
                    else
                    {
                        retVal = false;
                    }
                }
            }
            else
            {
                DebugTokenData dtokenData = new DebugTokenData();
                if (dtokenData.getDeviceInfo())
                {
                    GetInstalledAPIList();
                    if (IsAPIInstalled(dtokenData.DeviceOSVersion, "") > 0)
                    {
                        retVal = true;
                    }
                    else
                    {
                        if (baseVersion.CompareTo(dtokenData.DeviceOSVersion) > 0)
                        {
                            UpdateManagerDialog umd = new UpdateManagerDialog("The API Level for the operating system version of the attached device is not currently installed.  Would you like to install it now?", dtokenData.DeviceOSVersion, false, false);

                            if (umd.ShowDialog() == true)
                            {
                                retVal = true;
                            }
                            else
                            {
                                retVal = false;
                            }
                        }
                        else
                        {
                            if (IsRuntimeInstalled(dtokenData.DeviceOSVersion))
                            {
                                retVal = true;
                            }
                            else
                            {
                                if (IsAPIInstalled(dtokenData.DeviceOSVersion.Substring(0, dtokenData.DeviceOSVersion.LastIndexOf('.')), "") == 0)
                                {
                                    UpdateManagerDialog umd = new UpdateManagerDialog("The API Level for the operating system version of the attached device is not currently installed.  Would you like to install it now?", GetAPILevel(dtokenData.DeviceOSVersion.Substring(0, dtokenData.DeviceOSVersion.LastIndexOf('.'))), false, false);
                                    if (umd.ShowDialog() == true)
                                    {
                                        umd = new UpdateManagerDialog("The Runtime Libraries for the operating system version of the attached deice are not currently installed.  Would you like to install them now?", dtokenData.DeviceOSVersion, true, false);
                                        if (umd.ShowDialog() == true)
                                        {
                                            retVal = true;
                                        }
                                        else
                                        {
                                            retVal = false;
                                        }
                                    }
                                    else
                                    {
                                        retVal = false;
                                    }
                                }
                                else
                                {
                                    UpdateManagerDialog umd = new UpdateManagerDialog("The Runtime Libraries for the operating system version of the attached deice are not currently installed.  Would you like to install them now?", dtokenData.DeviceOSVersion, true, false);
                                    if (umd.ShowDialog() == true)
                                    {
                                        retVal = true;
                                    }
                                    else
                                    {
                                        retVal = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    retVal = true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Retrieve a list of the installed runtimes on the PC.
        /// </summary>
        /// <returns></returns>
        public bool getInstalledRuntimeTargetList()
        {
            bool success = false;

            installedRuntimeList = new List<string>();

            string[] directories = Directory.GetDirectories(bbndkPathConst);

            foreach (string directory in directories)
            {
                if (directory.Contains("runtime_"))
                {
                    installedRuntimeList.Add(directory.Substring(directory.IndexOf("runtime_") + 8).Replace('_', '.'));
                    success = true;
                }
                else
                {
                    continue;
                }
            }

            return success;

        }

        /// <summary>
        /// Retrieve a list of the installed runtimes on the PC.
        /// </summary>
        /// <returns></returns>
        public bool getInstalledSimulatorList()
        {
            bool success = false;

            installedSimulatorList = new List<string>();

            string[] directories = Directory.GetDirectories(bbndkPathConst);

            foreach (string directory in directories)
            {
                if (directory.Contains("simulator_"))
                {
                    installedSimulatorList.Add(directory.Substring(directory.IndexOf("simulator_") + 8).Replace('_', '.'));
                    success = true;
                }
                else
                {
                    continue;
                }
            }

            return success;

        }

        /// <summary>
        /// Update API Level from Server
        /// </summary>
        /// <param name="vesion"></param>
        /// <returns></returns>
        public bool UpdateAPI(string oldversion, string newversion)
        {
            bool success = false;

            Status = "Updating API";

            if (!((oldversion.StartsWith("10.1")) || (oldversion.StartsWith("10.0"))))
            {
                UninstallAPI(oldversion);
            }

            InstallAPI(newversion, false, false);

            return success;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Getter for the NDKEntries property
        /// </summary>
        public CollectionView APITargets
        {
            get { return _apiTargets; }
            set
            {
                _apiTargets = value;
                OnPropertyChanged(_colAPITargets);
            }
        }

        /// <summary>
        /// Errors property
        /// </summary>
        public String Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }

        /// <summary>
        /// Status property
        /// </summary>
        public String Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(_colStatus);
            }
        }

        /// <summary>
        /// Is Installing property
        /// </summary>
        public bool IsInstalling
        {
            get { return isInstalling; }
            set
            {
                isInstalling = value;
                OnPropertyChanged(_colIsInstalling);
            }
        }

        /// <summary>
        /// Getter/Setter for the NDKEntryClass property
        /// </summary>
        public APITargetClass APITarget
        {
            get { return _apiTarget; }
            set
            {
                _apiTarget = value;
                OnPropertyChanged(_colAPITarget);
            }
        }

        #endregion

        #region Private Member Functions

        /// <summary>
        /// Given the version set the selected API version
        /// </summary>
        /// <param name="version"></param>
        public void SetSelectedAPI(string version)
        {

            if (installedAPIList != null)
            {
                APIClass result = installedAPIList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    RegistryKey rkHKCU = Registry.CurrentUser;
                    RegistryKey rkNDKPath = null;

                    try
                    {
                        rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                        rkNDKPath.SetValue("NDKHostPath", result.HostName);
                        rkNDKPath.SetValue("NDKTargetPath", result.TargetName);

                        string qnx_config = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK";

                        System.Environment.SetEnvironmentVariable("QNX_TARGET", result.TargetName);
                        System.Environment.SetEnvironmentVariable("QNX_HOST", result.HostName);
                        System.Environment.SetEnvironmentVariable("QNX_CONFIGURATION", qnx_config);

                        string ndkpath = string.Format(@"{0}/usr/bin;{1}\bin;{0}/usr/qde/eclipse/jre/bin;", result.HostName, qnx_config) +
                            System.Environment.GetEnvironmentVariable("PATH");
                        System.Environment.SetEnvironmentVariable("PATH", ndkpath);
                    }
                    catch
                    {

                    }
                    rkNDKPath.Close();
                    rkHKCU.Close();
                }
            }

        }

        /// <summary>
        /// Check to see if API is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <param name="name">Check API name</param>
        /// <returns>true if installed</returns>
        private int IsAPIInstalled(string version, string name)
        {
            int success = 0;

            /// Check for 2.1 version
            if (version.StartsWith("2.1.0"))
                version = "2.1.0";

            if (installedAPIList != null)
            {
                APIClass result = installedAPIList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 1;
                }
            }

            if (installedNDKList != null)
            {
                APIClass result = installedNDKList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 2;
                }
            }

            return success;
        }

        /// <summary>
        /// Check to see if Simulator is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <returns>true if installed</returns>
        private bool IsSimulatorInstalled(string version)
        {
            bool success = false;

            getInstalledSimulatorList();

            if (installedSimulatorList != null)
            {
                string result = installedSimulatorList.FirstOrDefault(s => s.Contains(version));

                if (result != null)
                {
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Return the NDK Path from the registry
        /// </summary>
        /// <returns></returns>
        public string getCurrentAPIVersion()
        {
            string retVal = "";

            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;

            try
            {
                rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                retVal = rkNDKPath.GetValue("NDKTargetPath").ToString();
                retVal = retVal.Replace(bbndkPathConst.Replace("\\", @"/"), "");
                retVal = retVal.Substring(retVal.IndexOf('_') + 1);
                retVal = retVal.Substring(0, retVal.IndexOf('/'));
                retVal = retVal.Replace('_', '.');
                rkNDKPath.Close();
                rkHKCU.Close();
            }
            catch
            {
                if (rkNDKPath != null)
                    rkNDKPath.Close();
                rkHKCU.Close();
            }

            return retVal;
        }


        /// <summary>
        /// Check to see if Runtime is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <param name="name">Check API name</param>
        /// <returns>true if installed</returns>
        private bool IsRuntimeInstalled(string version)
        {
            bool success = false;

            if (installedRuntimeList != null)
            {
                string result = installedRuntimeList.FirstOrDefault(s => s.Contains(version));

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

            try
            {
                installedAPIList = new List<APIClass>();
                installedNDKList = new List<APIClass>();

                string[] dirPaths = new string[2];
                dirPaths[0] = bbndkPathConst + @"\..\qconfig\";
                dirPaths[1] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";

                for (int i = 0; i < 2; i++)
                {
                    string[] filePaths = Directory.GetFiles(dirPaths[i], "*.xml");
                    foreach (string file in filePaths)
                    {
                        try
                        {
                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.Load(file);
                            XmlNodeList name = xmlDoc.GetElementsByTagName("name");
                            XmlNodeList version = xmlDoc.GetElementsByTagName("version");
                            XmlNodeList hostpath = xmlDoc.GetElementsByTagName("host");
                            XmlNodeList targetpath = xmlDoc.GetElementsByTagName("target");

                            if (i == 0)
                            {
                                APIClass aclass = new APIClass(name.Item(0).InnerText, hostpath.Item(0).InnerText, targetpath.Item(0).InnerText, version.Item(0).InnerText);
                                installedAPIList.Add(aclass);
                            }
                            else
                            {
                                APIClass aclass = new APIClass(name.Item(0).InnerText, hostpath.Item(0).InnerText, targetpath.Item(0).InnerText, version.Item(0) == null ? "2.1.0" : version.Item(0).InnerText);
                                installedNDKList.Add(aclass);
                            }


                            success = true;
                        }
                        catch
                        {
                            success = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Given a runtime version get the associated API Level version.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private string GetAPILevel(string version)
        {
            string retVal = "";

            GetAvailableAPIList();

            if (tempAPITargetList != null)
            {
                retVal = tempAPITargetList.FindLast(i => i.TargetVersion.Contains(version)).TargetVersion;
            }

            return retVal;
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
            startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --list");

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
        /// Event that handles the return of a process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_Exited(object sender, System.EventArgs e)
        {
            Status = "Complete";
            IsInstalling = true;
            RefreshScreen();

            if (installVersion != "")
            {
                SetSelectedAPI(installVersion);
                installVersion = "";
            }
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
            APITargetClass api = null;

            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);

                if (!IsInstalling)
                {
                    Status = e.Data;
                }
                else
                {
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

                            api = tempAPITargetList.Find(i => i.TargetName == name);

                            if (api == null)
                            {
                                api = new APITargetClass(name, description, version);
                                tempAPITargetList.Add(api);
                            }
                            else
                            {
                                switch (api.IsInstalled)
                                {
                                    case 0:
                                        api.TargetVersion = version;
                                        api.LatestVersion = version;
                                        break;
                                    case 1:
                                        api.IsUpdate = true;
                                        api.LatestVersion = version;
                                        break;
                                    case 2:
                                        api.TargetVersion = version;
                                        api.LatestVersion = "NDK";
                                        break;
                                }
                            }

                            api.IsInstalled = IsAPIInstalled(api.TargetVersion, api.TargetName);

                            api.IsBeta = name.Contains("Beta");

                        }
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

                MessageBox.Show(e.Data);
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
