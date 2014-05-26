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
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using System.Windows.Forms;
using EnvDTE80;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Model.Integration;
using RIM.VSNDK_Package.Options;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.UpdateManager.Model;
using RIM.VSNDK_Package.ViewModels;
using VSNDK.Parser;

namespace RIM.VSNDK_Package
{
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
    /// Class to store API Targets
    /// </summary>
    public class APITargetClass
    {
        private string DefaultVersion = "10.2.0.1155";

        public string TargetName { get; set; }
        public string TargetDescription { get; set; }
        public string TargetVersion { get; set; }
        public string LatestVersion { get; set; }
        public int IsInstalled { get; set; }
        public bool IsAPIDefault { get; set; }
        public bool IsUpdate { get; set; }


        public string IsDefault
        {
            get { return TargetVersion == DefaultVersion ? "True" : "False"; }
            set
            {
                if (value == "True")
                    DefaultVersion = TargetVersion;
                else
                    DefaultVersion = "";
            }
        }

        public string InstalledVisibility
        {
            get { return IsInstalled > 0 ? "visible" : "collapsed"; }
        }

        public string AvailableVisibility
        {
            get { return ((IsAPIDefault) && (IsInstalled == 0)) ? "visible" : "collapsed"; }
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
        }
    }

    /// <summary>
    /// Class to store Simulators
    /// </summary>
    public class SimulatorsClass
    {
        public string APILevel { get; set; }
        public string TargetVersion { get; set; }
        public bool LatestVersion { get; set; }
        public bool IsInstalled { get; set; }

        public string TargetVersionText
        {
            get { return LatestVersion ? "Latest Version " + TargetVersion : TargetVersion; } 
        }

        public string InstalledVersionText
        {
            get { return "BlackBerry Native SDK Simulator (" + TargetVersion + ")"; }
        }
        
        public string LabelAPIVersion
        {
            get { return LatestVersion ? "visible" : "collapsed"; }
        }

        public string SubAPIVersion
        {
            get { return LatestVersion ? "collapsed" : "visible"; }
        }

        public string LabelAPIVersionText
        {
            get 
            {
                return "Simulator for BlackBerry Native SDK " + APILevel;
            }
        }

        public string AvailableVisibility
        {
            get { return IsInstalled ? "visible" : "collapsed"; }
        }

        public string InstalledVisibility
        {
            get { return IsInstalled ? "collapsed" : "visible"; }
        }

        public SimulatorsClass(string version, string apilevel, bool latest)
        {
            TargetVersion = version;
            LatestVersion = latest;
            APILevel = apilevel;
            IsInstalled = false;
        }
    }

    /// <summary>
    /// Class to retrieve Installed API List
    /// </summary>
    public class InstalledAPIListSingleton
    {
        private static InstalledAPIListSingleton _instance;
        public List<APIClass> _installedAPIList;

        /// <summary>
        /// Constructor
        /// </summary>
        private InstalledAPIListSingleton()
        {
            GetInstalledAPIList();
        }

        /// <summary>
        /// Public function to refresh data.
        /// </summary>
        public void RefreshData()
        {
            _instance.GetInstalledAPIList();
        }

        /// <summary>
        /// Public property to retrieve the singleton instance
        /// </summary>
        public static InstalledAPIListSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InstalledAPIListSingleton();
                }
                
                return _instance;
            }
        }

        /// <summary>
        /// Get list of installed APIs
        /// </summary>
        /// <returns></returns>
        private void GetInstalledAPIList()
        {
            try
            {
                _installedAPIList = new List<APIClass>();

                string dirPaths = GlobalFunctions.bbndkPathConst + @"\..\qconfig\";

                string[] filePaths = Directory.GetFiles(dirPaths, "*.xml");
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

                        APIClass aclass = new APIClass(name.Item(0).InnerText, hostpath.Item(0).InnerText, targetpath.Item(0).InnerText, version.Item(0).InnerText);
                        _installedAPIList.Add(aclass);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
        }


    }


    /// <summary>
    /// Class to retrieve Installed API List
    /// </summary>
    public class InstalledNDKListSingleton
    {
        private static InstalledNDKListSingleton _instance;
        public List<APIClass> _installedNDKList;

        /// <summary>
        /// Constructor
        /// </summary>
        private InstalledNDKListSingleton()
        {
            GetInstalledNDKList();
        }


        /// <summary>
        /// Public function to refresh data.
        /// </summary>
        public void RefreshData()
        {
            _instance.GetInstalledNDKList();
        }

   
        /// <summary>
        /// Public property to retrieve the singleton instance
        /// </summary>
        public static InstalledNDKListSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InstalledNDKListSingleton();
                }


                return _instance;
            }
        }

        /// <summary>
        /// Get list of installed APIs
        /// </summary>
        /// <returns></returns>
        private void GetInstalledNDKList()
        {
            try
            {
                _installedNDKList = new List<APIClass>();

                string dirPaths = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";

                string[] filePaths = Directory.GetFiles(dirPaths, "*.xml");
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

                        APIClass aclass = new APIClass(name.Item(0).InnerText, hostpath.Item(0).InnerText, targetpath.Item(0).InnerText, version.Item(0) == null ? "2.1.0" : version.Item(0).InnerText);
                        _installedNDKList.Add(aclass);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }

    /// <summary>
    /// Class to retrieve API Target List
    /// </summary>
    public class APITargetListSingleton
    {
        private static APITargetListSingleton _instance;
        public List<APITargetClass> _tempAPITargetList;
        private string _error = "";

        private APITargetListSingleton()
        {
            GetAvailableAPIList();
            GetDefaultAPIList();
        }


        /// <summary>
        /// Public function to refresh data.
        /// </summary>
        public void RefreshData()
        {
            _instance.GetAvailableAPIList();
            _instance.GetDefaultAPIList();
        }

        /// <summary>
        /// Retrieve list of API's from 
        /// </summary>
        /// <returns></returns>
        private void GetDefaultAPIList()
        {
            if (GlobalFunctions.isOnline())
            {
                var p = new System.Diagnostics.Process();
                ProcessStartInfo startInfo = p.StartInfo;

                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                p.ErrorDataReceived += ErrorDataReceived;
                p.OutputDataReceived += APIListDefaultsDataReceived;


                /// Get Device PIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = string.Format(@"/C " + GlobalFunctions.bbndkPathConst + @"\eclipsec --list");

                try
                {
                    p.Start();
                    p.BeginErrorReadLine();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    p.Close();
                    if (_error != "")
                    {
                        if (!GlobalFunctions.isOnline())
                        {
                            MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show(_error, "Get default API list failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        _error = "";
                        _tempAPITargetList = null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(startInfo.Arguments);
                    Debug.WriteLine(e.Message);
                }
            }

        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void APIListDefaultsDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if ((e.Data.ToLower().Contains("error")) || (_error != ""))
                {
                    _error = _error + e.Data;
                }
                else if ((e.Data.Contains("Location:")) || (e.Data.Contains("Available")) || (e.Data.Contains("Beta")))
                {
                    // Do Nothing
                }
                else
                {
                    string version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                    string name = e.Data.Substring(e.Data.LastIndexOf(" - ") + 3);

                    APITargetClass api = _tempAPITargetList.Find(i => i.TargetVersion == version);

                    if (api != null)
                    {
                        api.IsAPIDefault = true;
                        api.TargetName = name;
                    }
                }
            }
        }


        /// <summary>
        /// Retrieve list of API's from 
        /// </summary>
        /// <returns></returns>
        private void GetAvailableAPIList()
        {
            if (GlobalFunctions.isOnline())
            {
                var p = new System.Diagnostics.Process();
                var startInfo = p.StartInfo;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                p.ErrorDataReceived += ErrorDataReceived;
                p.OutputDataReceived += APIListDataReceived;


                /// Get Device PIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = string.Format(@"/C " + GlobalFunctions.bbndkPathConst + @"\eclipsec --list-all");

                try
                {
                    _tempAPITargetList = new List<APITargetClass>();

                    p.Start();
                    p.BeginErrorReadLine();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    p.Close();
                    if (_error != "")
                    {
                        if (!GlobalFunctions.isOnline())
                        {
                            MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show(_error, "Get available API list failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        _error = "";
                        _tempAPITargetList = null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(startInfo.Arguments);
                    Debug.WriteLine(e.Message);
                }
            }

        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void APIListDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if ((e.Data.ToLower().Contains("error")) || (_error != ""))
                {
                    _error = _error + e.Data;
                }
                else if ((e.Data.Contains("Location:")) || (e.Data.Contains("Available")) || (e.Data.Contains("Beta")))
                {
                    // Do Nothing
                }
                else
                {
                    string version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                    string name = e.Data.Substring(e.Data.LastIndexOf(" - ") + 3).Replace("(EXTERNAL_NDK)", "");
                    string description = "Device Support Unknown.";
                    APITargetClass api = _tempAPITargetList.Find(i => i.TargetName == name);

                    if (api == null)
                    {
                        api = new APITargetClass(name, description, version);
                        api.IsInstalled = IsAPIInstalled(version, "");
                        _tempAPITargetList.Add(api);
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
        /// On Error received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Debug.WriteLine(e.Data);
                _error += e.Data + "\n";
//                MessageBox.Show(e.Data);
            }
        }

        /// <summary>
        /// Public interface for getting the singleton instance
        /// </summary>
        public static APITargetListSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new APITargetListSingleton();
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Class to retrieve Installed API List
    /// </summary>
    public class SimulatorListSingleton
    {
        private static SimulatorListSingleton _instance;
        public List<SimulatorsClass> _simulatorList;
        private string _error = "";

        /// <summary>
        /// Constructor
        /// </summary>
        private SimulatorListSingleton()
        {
            GetSimulatorList();
        }

        /// <summary>
        /// Public function to refresh data.
        /// </summary>
        public void RefreshData()
        {
            _instance.GetSimulatorList();
        }


        /// <summary>
        /// Public property to retrieve the singleton instance
        /// </summary>
        public static SimulatorListSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SimulatorListSingleton();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Retrieve list of Available Simulators
        /// </summary>
        /// <returns></returns>
        private void GetSimulatorList()
        {
            if (GlobalFunctions.isOnline())
            {
                var p = new System.Diagnostics.Process();
                var startInfo = p.StartInfo;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                p.ErrorDataReceived += ErrorDataReceived;
                p.OutputDataReceived += SimulatorListDataReceived;

                /// Get Device PIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = string.Format(@"/C " + GlobalFunctions.bbndkPathConst + @"\eclipsec --list-all --simulator");

                try
                {
                    _simulatorList = new List<SimulatorsClass>();

                    p.Start();
                    p.BeginErrorReadLine();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    p.Close();
                    if (_error != "")
                    {
                        if (!GlobalFunctions.isOnline())
                        {
                            MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show(_error, "Get simulator list failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        _error = "";
                        _simulatorList = null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(startInfo.Arguments);
                    Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// On Error received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Debug.WriteLine(e.Data);
                _error += e.Data + "\n";
//                MessageBox.Show(e.Data);
            }
        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulatorListDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if ((e.Data.ToLower().Contains("error")) || (_error != ""))
                {
                    _error = _error + e.Data;
                }
                else if ((e.Data.Contains("Location:")) || (e.Data.Contains("Available")))
                {
                    // Do Nothing
                }
                else
                {
                    string version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                    string apilevel = version.Split('.')[0] + "." + version.Split('.')[1];

                    SimulatorsClass sim = _simulatorList.Find(i => i.APILevel == apilevel);

                    if (sim == null)
                    {
                        sim = new SimulatorsClass(version, apilevel, true);
                        sim.IsInstalled = IsSimulatorInstalled(version);
                        _simulatorList.Add(sim);
                    }
                    else
                    {
                        //sim not the latest... mark it as false
                        sim.LatestVersion = false;

                        //create new sim
                        SimulatorsClass sim2 = new SimulatorsClass(version, apilevel, true);
                        sim2.IsInstalled = IsSimulatorInstalled(version);

                        // insert before found sim.
                        _simulatorList.Insert(_simulatorList.IndexOf(sim), sim2);
                    }
                }
            }
        }


        /// <summary>
        /// Check to see if Simulator is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <returns>true if installed</returns>
        private bool IsSimulatorInstalled(string version)
        {
            bool success = false;

            if (InstalledSimulatorListSingleton.Instance.installedSimulatorList != null)
            {
                string result = InstalledSimulatorListSingleton.Instance.installedSimulatorList.FirstOrDefault(s => s.Contains(version));

                if (result != null)
                {
                    success = true;
                }
            }

            return success;
        }

    }

    /// <summary>
    /// Class to retrieve Installed API List
    /// </summary>
    public class InstalledSimulatorListSingleton
    {
        private static InstalledSimulatorListSingleton _instance;
        public List<string> installedSimulatorList;

        /// <summary>
        /// Constructor
        /// </summary>
        private InstalledSimulatorListSingleton()
        {
            GetInstalledSimulatorList();
        }

        /// <summary>
        /// Public function to refresh data.
        /// </summary>
        public void RefreshData()
        {
            _instance.GetInstalledSimulatorList();
        }

        /// <summary>
        /// Public property to retrieve the singleton instance
        /// </summary>
        public static InstalledSimulatorListSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InstalledSimulatorListSingleton();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Retrieve a list of the installed runtimes on the PC.
        /// </summary>
        /// <returns></returns>
        public bool GetInstalledSimulatorList()
        {
            bool success = false;

            installedSimulatorList = new List<string>();

            string[] directories = Directory.GetFiles(GlobalFunctions.bbndkPathConst, "*.vmxf", SearchOption.AllDirectories);

            foreach (string directory in directories)
            {
                if (directory.Contains("simulator_"))
                {
                    installedSimulatorList.Add(directory.Substring(0, directory.LastIndexOf("\\")).Substring(directory.IndexOf("simulator_") + 10).Replace('_', '.'));
                    success = true;
                }
                else
                {
                    continue;
                }
            }

            return success;
        }
    }


    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]

    ///Register the editor factory
    [XmlEditorDesignerViewRegistration("XML", "xml", LogicalViewID.Designer, 0x60,
    DesignerLogicalViewEditor = typeof(EditorFactory),
    Namespace = "http://www.qnx.com/schemas/application/1.0",
    MatchExtensionAndNamespace = true)]
    // And which type of files we want to handle
    [ProvideEditorExtension(typeof(EditorFactory), EditorFactory.defaultExtension, 0x40, NameResourceID = 106)]
    // We register that our editor supports LOGVIEWID_Designer logical view
    [ProvideEditorLogicalView(typeof(EditorFactory), LogicalViewID.Designer)]

    // Microsoft Visual C# Project
    [EditorFactoryNotifyForProject("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}", EditorFactory.defaultExtension, GuidList.guidVSNDK_PackageEditorFactoryString)]

    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidVSNDK_PackageString)]
    [ProvideOptionPage(typeof(GeneralOptionPage), "BlackBerry", "General", 1001, 1002, true)]
    [ProvideOptionPage(typeof(LogsOptionPage), "BlackBerry", "Logs", 1001, 1003, true)]
    [ProvideOptionPage(typeof(ApiLevelOptionPage), "BlackBerry", "API Levels", 1001, 1004, true)]
    [ProvideOptionPage(typeof(TargetsOptionPage), "BlackBerry", "Targets", 1001, 1005, true)]
    [ProvideOptionPage(typeof(SigningOptionPage), "BlackBerry", "Signing", 1001, 1006, true)]
    public sealed class VSNDK_PackagePackage : Package
    {
        #region private member variables

        private BlackBerryPaneTraceListener _traceWindow;
        private DTE2 _dte;
        private VSNDKCommandEvents _commandEvents;
        private bool _isSimulator;
        private BuildEvents _buildEvents;
        private List<string[]> _targetDir;
        private List<String> _buildThese;
        private bool _hitPlay;
        private int _amountOfProjects;
        private bool _isDeploying;
        private OutputWindowPane _owP;
        private bool _isDebugConfiguration = true;
        private string _processName = "";

        #endregion

        #region Package Members

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VSNDK_PackagePackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();
            MessageBoxHelper.Initialise(this);

            // create dedicated trace-logs output window pane (available in combo-box at regular Visual Studio Output Window):
            _traceWindow = new BlackBerryPaneTraceListener("BlackBerry", true, GetService(typeof(SVsOutputWindow)) as IVsOutputWindow, GuidList.GUID_TraceOutputWindowPane);
            _traceWindow.Activate();

            // and set it to monitor all logs (they have to be marked with 'BlackBerry' category! aka TraceLog.Category):
            TraceLog.Add(_traceWindow);
            TraceLog.WriteLine("BlackBerry plugin started");

            InstalledAPIListSingleton apiList = InstalledAPIListSingleton.Instance;
            TraceLog.WriteLine(" * loaded NDK descriptions");

            // setup called before running any 'tool':
            ToolRunner.Startup += (s, e) =>
                {
                    var ndk = PackageViewModel.Instance.ActiveNDK;

                    if (ndk != null)
                    {
                        e["QNX_TARGET"] = ndk.TargetPath;
                        e["QNX_HOST"] = ndk.HostPath;
                        e["PATH"] = string.Concat(Path.Combine(ndk.HostPath, "usr", "bin"), ";",
                                                  Path.Combine(RunnerDefaults.JavaHome, "bin"), ";", e["PATH"]);
                    }
                    else
                    {
                        e["PATH"] = string.Concat(Path.Combine(RunnerDefaults.JavaHome, "bin"), ";", e["PATH"]);
                    }
                };

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            RegisterEditorFactory(new EditorFactory(this));
            TraceLog.WriteLine(" * registered editors");

            
            _dte = (DTE2)GetService(typeof(SDTE));

            if ((IsBlackBerrySolution(_dte)) && (apiList._installedAPIList.Count == 0))
            {
                UpdateManager.UpdateManagerDialog ud = new UpdateManager.UpdateManagerDialog("Please choose your default API Level to be used by the Visual Studio Plug-in.", "default", false, false);
                ud.ShowDialog();
            }


            SetNDKPath();

            _commandEvents = new VSNDKCommandEvents((DTE2)_dte);
            _commandEvents.RegisterCommand(GuidList.guidVSStd97String, CommandConstants.cmdidStartDebug, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);
            _commandEvents.RegisterCommand(GuidList.guidVSStd97String, CommandConstants.cmdidStartDebug, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);
            _commandEvents.RegisterCommand(GuidList.guidVSStd2KString, CommandConstants.cmdidStartDebugContext, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            TraceLog.WriteLine(" * subscribed to IDE events");

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryTools);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);

                // Create the command for the settings window
                CommandID wndSettingsCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerrySettings);
                MenuCommand menuSettingsWin = new MenuCommand(ShowSettingsWindow, wndSettingsCommandID);
                mcs.AddCommand(menuSettingsWin);

                // Create the command for the Debug Token window
                CommandID wndDebugTokenCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryDebugToken);
                MenuCommand menuDebugTokenWin = new MenuCommand(ShowDebugTokenWindow, wndDebugTokenCommandID);
                mcs.AddCommand(menuDebugTokenWin);

                // Create command for the 'Options...' menu
                CommandID optionsCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryOptions);
                MenuCommand optionsMenu = new MenuCommand((s, e) => ShowOptionPage(typeof(GeneralOptionPage)), optionsCommandID);
                mcs.AddCommand(optionsMenu);

                // Create dynamic command for the 'devices-list' menu
                CommandID devicesCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryTargetsDevicesPlaceholder);
                DynamicMenuCommand devicesMenu = new DynamicMenuCommand(() => PackageViewModel.Instance.TargetDevices,
                                                                        (cmd, collection, index) =>
                                                                            {
                                                                                var item = index >= 0 && index < collection.Count ? ((DeviceDefinition[])collection)[index] : null;
                                                                                PackageViewModel.Instance.ActiveDevice = item;
                                                                            },
                                                                        (cmd, collection, index) =>
                                                                            {
                                                                                var item = index >= 0 && index < collection.Count ? ((DeviceDefinition[])collection)[index] : null;
                                                                                cmd.Checked = item != null && (item == PackageViewModel.Instance.ActiveDevice || item == PackageViewModel.Instance.ActiveSimulator);
                                                                                cmd.Visible = item != null;
                                                                                cmd.Text = item != null ? item.ToString() : "-";
                                                                            },
                                                                        devicesCommandID);
                mcs.AddCommand(devicesMenu);
                // Create dynamic command for the 'api-level-list' menu
                CommandID apiLevelCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryTargetsApiLevelsPlaceholder);
                DynamicMenuCommand apiLevelMenu = new DynamicMenuCommand(() => PackageViewModel.Instance.InstalledNDKs,
                                                                         (cmd, collection, index) =>
                                                                             {
                                                                                 var item = index >= 0 && index < collection.Count ? ((NdkInfo[]) collection)[index] : null;
                                                                                 PackageViewModel.Instance.ActiveNDK = item;
                                                                             },
                                                                         (cmd, collection, index) =>
                                                                             {
                                                                                 var item = index >= 0 && index < collection.Count ? ((NdkInfo[]) collection)[index] : null;
                                                                                 cmd.Checked = item != null && item == PackageViewModel.Instance.ActiveNDK;
                                                                                 cmd.Visible = item != null;
                                                                                 cmd.Text = item != null ? item.ToString() : "-";
                                                                             },
                                                                         apiLevelCommandID);
                mcs.AddCommand(apiLevelMenu);

                // Create command for 'Help' menus
                var helpCmdIDs = new[] {
                                            PkgCmdIDList.cmdidBlackBerryHelpWelcomePage, PkgCmdIDList.cmdidBlackBerryHelpSupportForum,
                                            PkgCmdIDList.cmdidBlackBerryHelpDocNative, PkgCmdIDList.cmdidBlackBerryHelpDocCascades, PkgCmdIDList.cmdidBlackBerryHelpDocPlayBook,
                                            PkgCmdIDList.cmdidBlackBerryHelpSamplesNative, PkgCmdIDList.cmdidBlackBerryHelpSamplesCascades, PkgCmdIDList.cmdidBlackBerryHelpSamplesPlayBook, PkgCmdIDList.cmdidBlackBerryHelpSamplesOpenSource,
                                            PkgCmdIDList.cmdidBlackBerryHelpAbout
                                       };
                foreach (var cmdID in helpCmdIDs)
                {
                    CommandID helpCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, cmdID);
                    MenuCommand helpMenu = new MenuCommand(OpenHelpWebPage, helpCommandID);
                    mcs.AddCommand(helpMenu);
                }

                // Create command for 'Configure...' [targets] menu
                CommandID configureCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidBlackBerryTargetsConfigure);
                MenuCommand configureMenu = new MenuCommand((s, e) => ShowOptionPage(typeof(TargetsOptionPage)), configureCommandID);
                mcs.AddCommand(configureMenu);

                // Create the command for the menu item.
                CommandID projCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PkgCmdIDList.cmdidfooLocalBox);
                OleMenuCommand projItem = new OleMenuCommand(MenuItemCallback, projCommandID);
                mcs.AddCommand(projItem);

                TraceLog.WriteLine(" * initialized menus");
            }

            TraceLog.WriteLine("-------------------- DONE");
        }

        public Window2 OpenWebPageTab(string url)
        {
            return (Window2)_dte.ItemOperations.Navigate(url, vsNavigateOptions.vsNavigateOptionsNewWindow);
        }

        private void OpenUrl(string url)
        {
            var options = (GeneralOptionPage)GetDialogPage(typeof(GeneralOptionPage));

            if (options.IsOpeningExternal)
            {
                DialogHelper.StartURL(url);
            }
            else
            {
                OpenWebPageTab(url);
            }
        }

        private void OpenHelpWebPage(object sender, EventArgs e)
        {
            var menuCommand = sender as MenuCommand;
            int cmdID = menuCommand != null ? menuCommand.CommandID.ID : 0;

            switch (cmdID)
            {
                case PkgCmdIDList.cmdidBlackBerryHelpWelcomePage:
                    OpenUrl("http://developer.blackberry.com/cascades/momentics/");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpSupportForum:
                    OpenUrl("http://supportforums.blackberry.com/t5/Developer-Support-Forums/ct-p/blackberrydev");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpDocNative:
                    OpenUrl("http://developer.blackberry.com/native/documentation/core/framework.html");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpDocCascades:
                    OpenUrl("http://developer.blackberry.com/native/documentation/cascades/dev/index.html");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpDocPlayBook:
                    OpenUrl("http://developer.blackberry.com/playbook/native/documentation/");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpSamplesNative:
                    OpenUrl("http://developer.blackberry.com/native/sampleapps/");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpSamplesCascades:
                    OpenUrl("http://developer.blackberry.com/native/sampleapps/");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpSamplesPlayBook:
                    OpenUrl("http://developer.blackberry.com/playbook/native/sampleapps/");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpSamplesOpenSource:
                    OpenUrl("https://github.com/blackberry");
                    break;
                case PkgCmdIDList.cmdidBlackBerryHelpAbout:
                    {
                        var form = new AboutForm();
                        form.ShowDialog();
                    }
                    break;
                default:
                    TraceLog.WarnLine("Unknown Help item requested");
                    break;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Check to see if current solution is configured with a BlackBerry Configuration.
        /// </summary>
        /// <param name="dte"></param>
        /// <returns></returns>
        private bool IsBlackBerrySolution(DTE2 dte)
        {
            bool res = false;

            if (dte.Solution.FullName != "")
            {
                string fileText = System.IO.File.ReadAllText(dte.Solution.FullName);

                if (fileText.Contains("Debug|BlackBerry"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return res;
        }

        /// <summary>
        /// Set the NDK path into the registry if not already set.
        /// </summary>
        private void SetNDKPath()
        {
            //Initialize NDK if possible.  
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;
            string qnx_target = "";
            string qnx_host = "";

            try
            {

                rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                qnx_host = rkNDKPath.GetValue("NDKHostPath").ToString();
                qnx_target = rkNDKPath.GetValue("NDKHostPath").ToString();

                if (qnx_host == "")
                {
                    string[] filePaths = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", "*.xml");

                    if (filePaths.Length >= 1)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(filePaths[0]);
                        XmlNodeList name = xmlDoc.GetElementsByTagName("name");
                        XmlNodeList hostpath = xmlDoc.GetElementsByTagName("host");
                        XmlNodeList targetpath = xmlDoc.GetElementsByTagName("target");

                        qnx_target = targetpath[0].InnerText;
                        qnx_host = hostpath[0].InnerText;

                        rkNDKPath.SetValue("NDKHostPath", qnx_host);
                        rkNDKPath.SetValue("NDKTargetPath", qnx_target);

                    }
                }

                /* PH: TODO: remove following code as it affects Visual Studio and all tools running by it */
                string qnx_config = GlobalFunctions.bbndkPathConst + @"\features\com.qnx.tools.jre.win32_1.6.0.43\jre\bin";

                System.Environment.SetEnvironmentVariable("QNX_TARGET", qnx_target);
                System.Environment.SetEnvironmentVariable("QNX_HOST", qnx_host);

                string ndkpath = string.Format(@"{0}/usr/bin;{1};", qnx_host, qnx_config) +
                    System.Environment.GetEnvironmentVariable("PATH");
                System.Environment.SetEnvironmentVariable("PATH", ndkpath);
                /* */

            }
            catch (Exception ex)
            {
                string e = ex.ToString();
            }

            rkNDKPath.Close();
            rkHKCU.Close();
        }

        /// <summary> 
        /// Identify the projects to be build and start the build process. 
        /// </summary>
        /// <returns> TRUE if successful, FALSE if not. </returns>
        private bool BuildBar()
        {
            bool success = true;
            try
            {
                if (_buildThese.Count != 0)
                {
                    Microsoft.Win32.RegistryKey key;
                    key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("VSNDK");
                    key.SetValue("Run", "True");
                    key.Close();

                    _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);

                    try
                    {
                        Solution2 soln = (Solution2)_dte.Solution;
                        _hitPlay = true;
                        _amountOfProjects = _buildThese.Count; // OnBuildDone will call build() only after receiving "amountOfProjects" events
                        foreach (string projectName in _buildThese)
                            soln.SolutionBuild.BuildProject("Debug", projectName, false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        success = false;
                    }
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                success = false;
            }
            return success;
        }

        /// <summary> 
        /// Verify if the build process was successful. If so, start deploying the app. 
        /// </summary>
        private void Built()
        {
            string outputText = "";

            _owP.TextDocument.Selection.SelectAll();
            outputText = _owP.TextDocument.Selection.Text;

            if ((outputText == "") || (System.Text.RegularExpressions.Regex.IsMatch(outputText, ">Build succeeded.\r\n")) || (!outputText.Contains("): error :")))
            {
                if (_isDebugConfiguration)
                {
                    // Write file to flag the deploy task that it should use the -debugNative option
                    string fileContent = "Use -debugNative.\r\n";
                    string appData = Environment.GetEnvironmentVariable("AppData");
                    System.IO.StreamWriter file = new System.IO.StreamWriter(appData + @"\BlackBerry\vsndk-debugNative.txt");
                    file.WriteLine(fileContent);
                    file.Close();

                    _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                }

                foreach (String startupProject in (Array)_dte.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (SolutionContext sc in _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts)
                    {
                        if (sc.ProjectName == startupProject)
                        {
                            sc.ShouldDeploy = true;
                        }
                        else
                        {
                            sc.ShouldDeploy = false;
                        }
                    }
                }
                _isDeploying = true;
                _dte.Solution.SolutionBuild.Deploy(true);
            }
        }

        /// <summary> 
        /// Get the process ID and launch an executable using the VSNDK debug engine. 
        /// </summary>
        private void Deployed()
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("VSNDK");
            key.SetValue("Run", "False");
            key.Close();

            string pidString = "";
            string toolsPath = "";
            string publicKeyPath = "";
            string targetIP = "";
            string password = "";
            string executablePath = "";
            if (GetProcessInfo((DTE2)_dte, ref pidString, ref toolsPath, ref publicKeyPath, ref targetIP, ref password, ref executablePath))
            {
                bool CancelDefault = LaunchDebugTarget(pidString, toolsPath, publicKeyPath, targetIP, password, executablePath);
            }
            else
            {
                MessageBox.Show("Failed to debug the application.\n\nPlease, close the app in case it was launched in the device/simulator.", "Failed to launch debugger", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary> 
        /// Launch an executable using the VSNDK debug engine. 
        /// </summary>
        /// <param name="pidString"> Process ID in string format. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool LaunchDebugTarget(string pidString, string toolsPath, string publicKeyPath, string targetIP, string password, string executablePath)
        {
            Microsoft.VisualStudio.Shell.ServiceProvider sp =
                 new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);

            IVsDebugger dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));

            VsDebugTargetInfo info = new VsDebugTargetInfo();

            info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.dlo = Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

            // Store all debugger arguments in a string
            var nvc = new NameValueCollection();
            nvc.Add("pid", pidString);
            nvc.Add("targetIP", targetIP); // The device (IP address)
            info.bstrExe = executablePath; // The executable path
            nvc.Add("isSimulator", _isSimulator.ToString());
            nvc.Add("ToolsPath", toolsPath);
            nvc.Add("PublicKeyPath", publicKeyPath);
            nvc.Add("Password", password);

            info.bstrArg = NameValueCollectionHelper.DumpToString(nvc);

            info.bstrRemoteMachine = null; // debug locally
            info.fSendStdoutToOutputWindow = 0; // Let stdout stay with the application.
            info.clsidCustom = new Guid("{E5A37609-2F43-4830-AA85-D94CFA035DD2}"); // Set the launching engine the VSNDK engine guid
            info.grfLaunch = 0;

            IntPtr pInfo = Marshal.AllocCoTaskMem((int)info.cbSize);
            Marshal.StructureToPtr(info, pInfo, false);

            try
            {
                int result = dbg.LaunchDebugTargets(1, pInfo);

                if (result != VSConstants.S_OK)
                {
                    string msg;
                    IVsUIShell sh = (IVsUIShell)sp.GetService(typeof(SVsUIShell));
                    sh.GetErrorInfo(out msg);
                    Debug.WriteLine("LaunchDebugTargets: " + msg);

                    return true;
                }
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pInfo);
                }
            }

            return false;
        }

        /// <summary> 
        /// Get the PID of the launched native app by parsing text from the output window. 
        /// </summary>
        /// <param name="dte"> Application Object. </param>
        /// <param name="pidString"> Returns the Process ID as a string. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool GetProcessInfo(DTE2 dte, ref string pidString, ref string toolsPath, ref string publicKeyPath, ref string targetIP, ref string password, ref string executablePath)
        {
            string currentPath = "";

            foreach (string[] paths in _targetDir)
            {
                if (paths[0] == _processName)
                {
                    currentPath = paths[1];
                    break;
                }
            }

            executablePath = currentPath + _processName; // The executable path
            executablePath = executablePath.Replace('\\', '/');
            publicKeyPath = Environment.GetEnvironmentVariable("AppData") + @"\BlackBerry\bbt_id_rsa.pub";
            publicKeyPath = publicKeyPath.Replace('\\', '/');

            try
            {
                RegistryKey rkHKCU = Registry.CurrentUser;
                RegistryKey rkPluginRegKey = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                toolsPath = rkPluginRegKey.GetValue("NDKHostPath") + "/usr/bin";
                toolsPath = toolsPath.Replace('\\', '/');

                if (_isSimulator)
                {
                    targetIP = rkPluginRegKey.GetValue("simulator_IP").ToString();
                    password = rkPluginRegKey.GetValue("simulator_password").ToString();
                }
                else
                {
                    targetIP = rkPluginRegKey.GetValue("device_IP").ToString();
                    password = rkPluginRegKey.GetValue("device_password").ToString();
                }

                // Decrypt stored password.
                byte[] data = Convert.FromBase64String(password);
                if (data.Length > 0)
                {
                    byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
                    password = Encoding.Unicode.GetString(decrypted);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Microsoft Visual Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            pidString = getPIDfromGDB(_processName, targetIP, password, _isSimulator, toolsPath, publicKeyPath);

            if (pidString == "")
            {
                // Select all of the text
                _owP.TextDocument.Selection.SelectAll();
                string outputText = _owP.TextDocument.Selection.Text;

                // Check for successful deployment
                if (Regex.IsMatch(outputText, "Info: done"))
                {
                    string pattern = @"\s+result::(\d+)\r\n.+|\s+result::(\d+) \(TaskId:";
                    Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

                    // Match the regular expression pattern against a text string.
                    Match m = r.Match(outputText);

                    // Take first match
                    if (m.Success)
                    {
                        Group g = m.Groups[1];
                        CaptureCollection cc = g.Captures;
                        if (cc.Count == 0)
                        {   // Diagnostic verbosity mode
                            g = m.Groups[2];
                            cc = g.Captures;
                        }

                        if (cc.Count != 0)
                        {
                            Capture c = cc[0];
                            pidString = c.ToString();
                        }
                    }
                }
            }

            if (pidString != "")
            {

                // Store proccess name and file location into ProcessesPath.txt, so "Attach To Process" would be able to find the 
                // source code for a running process.
                // First read the file.
                _processName += "_" + _isSimulator.ToString();

                string processesPaths;
                StreamReader readProcessesPathsFile;
                try
                {
                    readProcessesPathsFile = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\ProcessesPath.txt");
                    processesPaths = readProcessesPathsFile.ReadToEnd();
                    readProcessesPathsFile.Close();
                }
                catch (Exception)
                {
                    processesPaths = "";
                }

                // Updating the contents.
                int begin = processesPaths.IndexOf(_processName + ":>");

                if (begin != -1)
                {
                    begin += _processName.Length + 2;
                    int end = processesPaths.IndexOf("\r\n", begin);
                    processesPaths = processesPaths.Substring(0, begin) + currentPath + processesPaths.Substring(end);
                }
                else
                {
                    processesPaths = processesPaths + _processName + ":>" + currentPath + "\r\n";
                }

                // Writing contents to file.
                StreamWriter writeProcessesPathsFile;
                try
                {
                    writeProcessesPathsFile = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\ProcessesPath.txt", false);
                    writeProcessesPathsFile.Write(processesPaths);
                    writeProcessesPathsFile.Close();
                }
                catch (Exception)
                {
                }

                return true;
            }
            return false;
        }

        private string getPIDfromGDB(string processName, string IP, string password, bool isSimulator, string toolsPath, string publicKeyPath)
        {
            string PID = "";
            string response = GDBParser.GetPIDsThroughGDB(IP, password, isSimulator, toolsPath, publicKeyPath, 7);

            if ((response == "TIMEOUT!") || (response.IndexOf("1^error,msg=", 0) != -1)) //found an error
            {
                if (response == "TIMEOUT!") // Timeout error, normally happen when the device is not connected.
                {
                    MessageBox.Show("Please, verify if the Device/Simulator IP in \"BlackBerry -> Settings\" menu is correct and check if it is connected.", "Device/Simulator not connected or not configured properly", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                    if (response[29] == ':') // error: 1^error,msg="169.254.0.3:8000: The requested address is not valid in its context."
                    {
                        string txt = response.Substring(13, response.IndexOf(':', 13) - 13) + response.Substring(29, response.IndexOf('"', 31) - 29);
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
                int i = response.IndexOf(processName + " - ");
                if (i != -1)
                {
                    i += processName.Length + 3;
                    PID = response.Substring(i, response.IndexOf('/', i) - i);
                }
            }
            return PID;
        }


        /// <summary>
        /// Verify if the app configuration is Debug.
        /// </summary>
        /// <returns> True if Debug configuration; False otherwise. </returns>
        private bool checkDebugConfiguration()
        {
            Solution2 soln = (Solution2)_dte.Solution;
            foreach (String startupProject in (Array)soln.SolutionBuild.StartupProjects)
            {
                foreach (Project p1 in soln.Projects)
                {
                    if (p1.UniqueName == startupProject)
                    {
                        ConfigurationManager config = p1.ConfigurationManager;
                        Configuration active = config.ActiveConfiguration;

                        if (active.ConfigurationName.ToUpper() == "DEBUG")
                            return (true);
                        else
                            return (false);
                    }
                }
            }
            return (false);
        }


        #endregion

        #region Event Handlers

        /// <summary> 
        /// New Start Debug Command Events After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        private void startDebugCommandEvents_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            Debug.WriteLine("After Start Debug");
        }


        /// <summary> 
        /// New Start Debug Command Events Before Execution Event Handler. Call the method responsible for building the app. 
        /// </summary>
        /// <param name="Guid"> Command GUID. </param>
        /// <param name="ID"> Command ID. </param>
        /// <param name="CustomIn"> Custom IN Object. </param>
        /// <param name="CustomOut"> Custom OUT Object. </param>
        /// <param name="CancelDefault"> Cancel the default execution of the command. </param>
        private void startDebugCommandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {

            bool bbPlatform = false;
            if (_dte.Solution.SolutionBuild.ActiveConfiguration != null)
            {
                _isDebugConfiguration = checkDebugConfiguration();

                SolutionContexts scCollection = _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts;
                foreach (SolutionContext sc in scCollection)
                {
                    if (sc.PlatformName == "BlackBerry" || sc.PlatformName == "BlackBerrySimulator")
                    {
                        bbPlatform = true;
                        if (sc.PlatformName == "BlackBerrySimulator")
                            _isSimulator = true;
                        else
                            _isSimulator = false;
                    }
                }
            }

            Debug.WriteLine("Before Start Debug");

            if (VSNDK.Package.ControlDebugEngine.isDebugEngineRunning || !bbPlatform)
            {
                // Disable the override of F5 (this allows the debugged process to continue execution)
                CancelDefault = false;
            }
            else
            {
                try
                {
                    Solution2 soln = (Solution2)_dte.Solution;
                    _buildThese = new List<String>();
                    _targetDir = new List<string[]>();

                    foreach (String startupProject in (Array)soln.SolutionBuild.StartupProjects)
                    {
                        foreach (Project p1 in soln.Projects)
                        {
                            if (p1.UniqueName == startupProject)
                            {
                                _buildThese.Add(p1.FullName);
                                _processName = p1.Name;

                                ConfigurationManager config = p1.ConfigurationManager;
                                Configuration active = config.ActiveConfiguration;

                                foreach (Property prop in active.Properties)
                                {
                                    try
                                    {
                                        if (prop.Name == "OutputPath")
                                        {
                                            string[] path = new string[2];
                                            path[0] = p1.Name;
                                            path[1] = prop.Value.ToString();
                                            _targetDir.Add(path);
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                
                // Create a reference to the Output window.
                // Create a tool window reference for the Output window
                // and window pane.
                OutputWindow ow = ((DTE2)_dte).ToolWindows.OutputWindow;

                // Select the Build pane in the Output window.
                _owP = ow.OutputWindowPanes.Item("Build");
                _owP.Activate();


                if (_isDebugConfiguration)
                {
                    UpdateManagerData upData;
                    if (_targetDir.Count > 0)
                        upData = new UpdateManagerData(_targetDir[0][1]);
                    else
                        upData = new UpdateManagerData();

                    if (!upData.validateDeviceVersion(_isSimulator))
                    {
                        CancelDefault = true;
                    }
                    else
                    {
                        BuildBar();
                        CancelDefault = true;
                    }
                }
                else
                {
                    BuildBar();
                    CancelDefault = true;
                }
            }
        }


        /// <summary> 
        /// This event is fired only when the build/rebuild/clean process ends. 
        /// </summary>
        /// <param name="Scope"> Represents the scope of the build. </param>
        /// <param name="Action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        public void OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            if (Action == vsBuildAction.vsBuildActionBuild)
            {
                _amountOfProjects -= 1;
                if (_amountOfProjects == 0)
                {
                    _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                    Built();
                }
            }
            else if (Action == vsBuildAction.vsBuildActionDeploy)
            {
                _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                _isDeploying = false;
                Deployed();
            }
        }

        /// <summary> 
        /// This event is fired only when user wants to build, rebuild or clean the project. 
        /// </summary>
        /// <param name="Scope"> Represents the scope of the build. </param>
        /// <param name="Action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        public void OnBuildBegin(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            InstalledAPIListSingleton apiList = InstalledAPIListSingleton.Instance;
            if ((IsBlackBerrySolution(_dte)) && (apiList._installedAPIList.Count == 0))
            {
                UpdateManager.UpdateManagerDialog ud = new UpdateManager.UpdateManagerDialog("Please choose your default API Level to be used by the Visual Studio Plug-in.", "default", false, false);
                ud.ShowDialog();
            }

            if ((Action == vsBuildAction.vsBuildActionBuild) || (Action == vsBuildAction.vsBuildActionRebuildAll))
            {
                if ((_hitPlay == false) && (_isDeploying == false)) // means that the "play" building and deploying process was cancelled before, so we have to disable the 
                // OnBuildDone event to avoid deploying in case user only wants to build.
                {
                    _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                }
                _hitPlay = false;
            }
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Create the dialog instance without Help support.
            var SigningToolDialog = new Signing.SigningDialog();
            // Show the dialog.
            var m = SigningToolDialog.ShowDialog();
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowSettingsWindow(object sender, EventArgs e)
        {
            var SettingsDialog = new Settings.SettingsDialog();
            var m = SettingsDialog.ShowDialog();
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowDebugTokenWindow(object sender, EventArgs e)
        {
            // Create the dialog instance without Help support.
            var DebugTokenDialog = new DebugToken.DebugTokenDialog();
            // Show the dialog.
            if ((!DebugTokenDialog.IsClosing) && (VSNDK_Package.DebugToken.Model.DebugTokenData._initializedCorrectly))
                DebugTokenDialog.ShowDialog();
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            string filename = "";
            string folderName = "";
            string name = "";

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".cproject"; // Default file extension
            dlg.Filter = "Native Core Application Project Files (*.cproject, *.project)|*.cproject;*.project;";
            dlg.Title = "Open BlackBerry Core Native Application Project Files";
            dlg.Multiselect = false;
            dlg.InitialDirectory = Environment.SpecialFolder.MyComputer.ToString();
            dlg.CheckFileExists = true;

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                filename = dlg.FileName;
                FileInfo fi = new FileInfo(filename);
                folderName = fi.DirectoryName;

                Array projs = (Array)_dte.ActiveSolutionProjects;
                Project project = (Project)projs.GetValue(0);
                name = project.FullName;

                // Create the dialog instance without Help support.
                var ImportSummary = new Import.Import(project, folderName, name);
                ImportSummary.ShowModel2();
            }

        }
        
        #endregion
    }
}
