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
using Microsoft.VisualStudio.Shell;
using System.Security.Cryptography;

namespace RIM.VSNDK_Package.UpdateManager.Model
{

     /// <summary>
    /// Data Model for the Update Manager Dialog
    /// </summary>
    public class UpdateManagerData : INotifyPropertyChanged
    {
        #region Constants

        private const string _colAPITarget = "APITarget";
        private const string _colAPITargets = "APITargets";
        private const string _colSimulator = "Simulator";
        private const string _colSimulators = "Simulators";
        private const string _colStatus = "Status";
        private const string _colIsInstalling = "IsInstalling";

        #endregion

        #region Member Variables

        private bool isInstalling = true;
        private string installVersion = "";
        private bool _isRuntime = false;
        private bool _isSimulator = false;
        private CollectionView _apiTargets;
        private CollectionView _simulators;
        private CollectionView _simulators2;
        private APITargetClass _apiTarget;
        private SimulatorsClass _simulator;
        private string _errors = "";
        public string bbndkPathConst = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + "bbndk_vs";
        private string _status = "";
        private string _error = "";
        private string DeviceIP;
        private string DevicePassword;

        private List<APIClass> installedAPIList;
        private List<APIClass> installedNDKList;
        private List<string> installedRuntimeList;
        private string _deviceosversion;

        #endregion

        #region Public Member Functions

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManagerData()
        {
            Status = "";

            installedAPIList = InstalledAPIListSingleton.Instance._installedAPIList;
            installedNDKList = InstalledNDKListSingleton.Instance._installedNDKList;
            APITargets = new CollectionView(APITargetListSingleton.Instance._tempAPITargetList);
            Simulators = new CollectionView(SimulatorListSingleton.Instance._simulatorList);
            
        }

        /// <summary>
        /// Function to filter the sublist of simulators
        /// </summary>
        /// <param name="apiLevel"></param>
        public void FilterSubList(string apiLevel)
        {
            Simulators2 = new CollectionView(SimulatorListSingleton.Instance._simulatorList.FindAll(i => i.APILevel.Contains(apiLevel)));
        }

        /// <summary>
        /// Refresh all the lists
        /// </summary>
        public void RefreshScreen()
        {
            //((VSNDK_PackagePackage)_pkg).GetInstalledAPIList();
            //// ((VSNDK_PackagePackage)_pkg).GetAvailableAPIList();
            //((VSNDK_PackagePackage)_pkg).GetInstalledSimulatorList();
            //((VSNDK_PackagePackage)_pkg).GetSimulatorList();

            //installedAPIList = ((VSNDK_PackagePackage)_pkg).InstalledAPIList;
            //installedNDKList = ((VSNDK_PackagePackage)_pkg).InstalledNDKList;
            //APITargets = new CollectionView(((VSNDK_PackagePackage)_pkg).APITargetList);
            //Simulators = new CollectionView(((VSNDK_PackagePackage)_pkg).SimulatorList);
        }

        /// <summary>
        /// Install Specified API
        /// </summary>
        /// <param name="version">version of API to install</param>
        /// <returns>true if successful</returns>
        public bool InstallAPI(string version, bool isRuntime, bool isSimulator)
        {
            bool success = false;
            _isRuntime = isRuntime;
            _isSimulator = isSimulator;
            _error = "";

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
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(InstallDataReceived);
            p.Exited += new EventHandler(p_Exited);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --install {0} {1} {2}", version, isRuntime ? "--runtime" : "", isSimulator ? "--simulator" : "");

            try
            {
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
        public bool UninstallAPI(string version, bool isSimulator)
        {
            bool success = false;
            _error = "";
            _isSimulator = isSimulator;

            Status = "Uninstalling API Level";

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(InstallDataReceived);
            p.Exited += new EventHandler(p_Exited);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --uninstall {0} {1}", version, isSimulator ? "--simulator" : "");

            try
            {
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
        /// Function to retrieve device info from the registry
        /// </summary>
        /// <returns></returns>
        public void getDeviceSimInfo(bool isSim)
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkSettingsPath = null;
            object pwd = null;
            object ip = null;

            try
            {
                rkSettingsPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");

                if (isSim)
                {
                    pwd = rkSettingsPath.GetValue("simulator_password");
                    ip = rkSettingsPath.GetValue("simulator_IP");
                }
                else
                {
                    pwd = rkSettingsPath.GetValue("device_password");
                    ip = rkSettingsPath.GetValue("device_IP");
                }

                
                if (pwd != null)
                    DevicePassword = GlobalFunctions.Decrypt(pwd.ToString());

                if (ip != null)
                    DeviceIP = ip.ToString();
            }
            catch
            {

            }

            rkSettingsPath.Close();
            rkHKCU.Close();
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

            if (InstalledAPIListSingleton.Instance._installedAPIList != null)
            {
                APIClass result = InstalledAPIListSingleton.Instance._installedAPIList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 1;
                }
            }

            if (InstalledNDKListSingleton.Instance._installedNDKList != null)
            {
                APIClass result = InstalledNDKListSingleton.Instance._installedNDKList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 2;
                }
            }

            return success;
        }


        /// <summary>
        /// Validate to make sure device matches the API Level chosen.
        /// </summary>
        /// <returns></returns>
        public bool validateDeviceVersion(bool isSim)
        {
            bool retVal = false;
            string baseVersion = "10.2.0.0";
            DebugTokenData dtokenData;

            if (!isSim)
                dtokenData = new DebugTokenData();

            getDeviceSimInfo(isSim);

            if (getDeviceInfo())
            { //** Device Info retrieved - validate API's 
                if (getCurrentAPIVersion() != _deviceosversion)
                { //** Currently selected API version is different from attached device OS version.  
                    if (IsAPIInstalled(_deviceosversion, "") > 0)
                    {
                        retVal = true;
                    }
                    else
                    {
                        if (baseVersion.CompareTo(_deviceosversion) > 0)
                        {
                            UpdateManagerDialog umd = new UpdateManagerDialog("The API Level for the operating system version of the attached device is not currently installed.  Would you like to install it now?", _deviceosversion, false, false);

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
                            if (IsRuntimeInstalled(_deviceosversion))
                            {
                                retVal = true;
                            }
                            else
                            {
                                if (IsAPIInstalled(_deviceosversion.Substring(0, _deviceosversion.LastIndexOf('.')), "") == 0)
                                {
                                    string apiLevel = GetAPILevel(_deviceosversion.Substring(0, _deviceosversion.LastIndexOf('.')));

                                    if (apiLevel != "")
                                    {
                                        UpdateManagerDialog umd = new UpdateManagerDialog("The API Level for the operating system version of the attached device is not currently installed.  Would you like to install it now?", GetAPILevel(_deviceosversion.Substring(0, _deviceosversion.LastIndexOf('.'))), false, false);
                                        if (umd.ShowDialog() == true)
                                        {
                                            umd = new UpdateManagerDialog("The Runtime Libraries for the operating system version of the attached device are not currently installed.  Would you like to install them now?", _deviceosversion, true, false);
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
                                        System.Diagnostics.Debug.WriteLine("API level not supported at this moment. Aborting...");
                                }
                                else
                                {
                                    UpdateManagerDialog umd = new UpdateManagerDialog("The Runtime Libraries for the operating system version of the attached device are not currently installed.  Would you like to install them now?", _deviceosversion, true, false);
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
            else
            {
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Get the device Info of the connected device
        /// </summary>
        /// <returns>True if successful</returns>
        public bool getDeviceInfo()
        {
            bool success = false;

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(DeviceInfoDataReceived);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\";
            startInfo.Arguments = string.Format("/C blackberry-deploy.bat -listDeviceInfo {0} {1}", DeviceIP, DevicePassword == "" ? "" : "-password " + DevicePassword);

            try
            {
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
                if (_errors != "")
                {
                    int begin = _errors.IndexOf("java.io.IOException: ");
                    if (begin != -1)
                    {
                        begin += 20;
                        int end = _errors.IndexOf("\n", begin);
                        MessageBox.Show(_errors.Substring(begin, end - begin) + "\n\nSee the Debug Output window for details.", "Could not get the device Info of the connected device.", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                        MessageBox.Show(_errors + "See the Debug Output window for details.", "Could not get the device Info of the connected device.", MessageBoxButton.OK, MessageBoxImage.Error);

                    _errors = "";
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
                if (_errors != "")
                {
                    int begin = _errors.IndexOf("java.io.IOException: ");
                    if (begin != -1)
                    {
                        begin += 20;
                        int end = _errors.IndexOf("\n", begin);
                        MessageBox.Show(_errors.Substring(begin, end - begin) + "\n\nSee the Debug Output window for details.", "Could not get the device Info of the connected device.", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                        MessageBox.Show(_errors + "See the Debug Output window for details.", "Could not get the device Info of the connected device.", MessageBoxButton.OK, MessageBoxImage.Error);

                    _errors = "";
                }
            }

            return success;

        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceInfoDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
                if (e.Data.Contains("Error:"))
                    _errors += e.Data + "\n";
                else if (e.Data.Contains("scmbundle::"))
                    _deviceosversion = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
            }
        }        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
                if (e.Data.Contains("Error:"))
                    _errors += e.Data + "\n";
                else if (e.Data.Contains("scmbundle::"))
                    _deviceosversion = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
            }
        }

        /// <summary>
        /// Retrieve a list of the installed runtimes on the PC.
        /// </summary>
        /// <returns></returns>
        private bool getInstalledRuntimeTargetList()
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
                UninstallAPI(oldversion, false);
            }

            InstallAPI(newversion, false, false);

            return success;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Getter for the APITargets property
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
        /// Getter for the Simulators property
        /// </summary>
        public CollectionView Simulators
        {
            get { return _simulators; }
            set
            {
                _simulators = value;
                OnPropertyChanged(_colSimulators);
            }
        }

        /// <summary>
        /// Getter for the Simulators property
        /// </summary>
        public CollectionView Simulators2
        {
            get { return _simulators2; }
            set
            {
                _simulators2 = value;
                OnPropertyChanged(_colSimulators);
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
        /// Getter/Setter for the APITarget property
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

        /// <summary>
        /// Getter/Setter for the Simulator property
        /// </summary>
        public SimulatorsClass Simulator
        {
            get { return _simulator; }
            set
            {
                _simulator = value;
                OnPropertyChanged(_colSimulator);
            }
        }

        #endregion

        #region Private Member Functions

        /// <summary>
        /// Given the version set the selected API version
        /// </summary>
        /// <param name="version"></param>
        public void SetRuntime(string version)
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;

            try
            {
                string remotePath = bbndkPathConst + @"\runtime_" + version.Replace('.', '_') + @"\qnx6\armle-v7\";

                rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                rkNDKPath.SetValue("NDKRemotePath", remotePath);
            }
            catch
            {

            }
            rkNDKPath.Close();
            rkHKCU.Close();
        }

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
        /// Return the Current API Version from the registry
        /// </summary>
        /// <returns>Currently selected API Version.</returns>
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
        public bool IsRuntimeInstalled(string version)
        {
            bool success = false;

            getInstalledRuntimeTargetList();

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
        /// Given a runtime version get the associated API Level version.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public string GetAPILevel(string version)
        {
            string retVal = "";

            if (APITargetListSingleton.Instance._tempAPITargetList != null)
            {
                APITargetClass apiLevel = APITargetListSingleton.Instance._tempAPITargetList.FindLast(i => i.TargetVersion.Contains(version)); 

                if (apiLevel != null)
                {
                    retVal = apiLevel.TargetVersion;
                }
            }

            return retVal;
        }

           /// <summary>
        /// Event that handles the return of a process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_Exited(object sender, System.EventArgs e)
        {


            if (_error != "")
            {
                Status = "Error";
                IsInstalling = true;

                MessageBox.Show(_error, "Update Manager", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);

                RefreshScreen();

            }
            else
            {
                Status = "Complete";
                IsInstalling = true;

                RefreshScreen();

                //if (installVersion != "")
                //{
                //    SetSelectedAPI(installVersion);

                if (_isRuntime)
                {
                    SetRuntime(installVersion);
                    _isRuntime = false;
                }

                //    installVersion = "";

                //}
            }
        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {

                if ((e.Data.ToLower().Contains("error")) || (_error != ""))
                {
                    _error = _error + e.Data + "\n";
                }
                else
                {
                    Status = e.Data;
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
                _errors += e.Data + "\n";
//                MessageBox.Show(e.Data);
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
