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
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using System.Windows.Forms;
using VSNDK.Package;
using EnvDTE80;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using RIM.VSNDK_Package.UpdateManager.Model;

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
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [Guid(GuidList.guidVSNDK_PackagePkgString)]
    public sealed class VSNDK_PackagePackage : Package
    {

        #region private member variables

        private EnvDTE.DTE _dte;
        private VSNDKCommandEvents _commandEvents;
        private bool _isSimulator;
        private BuildEvents _buildEvents;
        private List<string[]> _targetDir = null;
        private bool _hitPlay = false;
        private int _amountOfProjects = 0;
        private string _error = "";
        private bool _isDeploying = false;
        private OutputWindowPane _owP;
        private string bbndkPathConst = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + "bbndk_vs";
        private List<APIClass> _installedAPIList;
        private List<APIClass> _installedNDKList;
        private List<SimulatorsClass> _simulatorList;
        private List<APITargetClass> _tempAPITargetList;
        private List<string> installedSimulatorList;

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
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            base.RegisterEditorFactory(new EditorFactory(this));

            _dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));

            SetNDKPath();
            GetInstalledAPIList();
            GetAvailableAPIList();
            GetInstalledSimulatorList();
            GetSimulatorList();

            _commandEvents = new VSNDKCommandEvents((DTE2)_dte);
            _commandEvents.RegisterCommand(GuidList.guidVSStd97String, CommandConstants.cmdidStartDebug, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += new _dispBuildEvents_OnBuildBeginEventHandler(this.OnBuildBegin);



            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, (int)PkgCmdIDList.cmdidBlackBerryTools);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );

                // Create the command for the settings window
                CommandID wndSettingsCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, (int)PkgCmdIDList.cmdidBlackBerrySettings);
                MenuCommand menuSettingsWin = new MenuCommand(ShowSettingsWindow, wndSettingsCommandID);
                mcs.AddCommand(menuSettingsWin);

                // Create the command for the Debug Token window
                CommandID wndDebugTokenCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, (int)PkgCmdIDList.cmdidBlackBerryDebugToken);
                MenuCommand menuDebugTokenWin = new MenuCommand(ShowDebugTokenWindow, wndDebugTokenCommandID);
                mcs.AddCommand(menuDebugTokenWin);

                // Create the command for the menu item.
                CommandID projCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, (int)PkgCmdIDList.cmdidfooLocalBox);
                OleMenuCommand projItem = new OleMenuCommand(MenuItemCallback, projCommandID);
                mcs.AddCommand(projItem);
            }

        }
        #endregion

        #region public methods

        /// <summary>
        /// Public property for the list of installed API's 
        /// </summary>
        public List<APIClass> InstalledAPIList
        {
            get
            {
                return _installedAPIList;
            }
        }

        /// <summary>
        /// Public property for the list of installed NDK's 
        /// </summary>
        public List<APIClass> InstalledNDKList
        {
            get
            {
                return _installedNDKList;
            }
        }

        /// <summary>
        /// Public property for the list of installed NDK's 
        /// </summary>
        public List<APITargetClass> APITargetList
        {
            get
            {
                return _tempAPITargetList;
            }
        }

        /// <summary>
        /// Public property for the list of installed NDK's 
        /// </summary>
        public List<SimulatorsClass> SimulatorList
        {
            get
            {
                return _simulatorList;
            }
        }

        #endregion

        #region private methods

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

                string qnx_config = bbndkPathConst + @"\features\com.qnx.tools.jre.win32_1.6.0.43\jre\bin";

                System.Environment.SetEnvironmentVariable("QNX_TARGET", qnx_target);
                System.Environment.SetEnvironmentVariable("QNX_HOST", qnx_host);

                string ndkpath = string.Format(@"{0}/usr/bin;{1}", qnx_host, qnx_config) +
                    System.Environment.GetEnvironmentVariable("PATH");
                System.Environment.SetEnvironmentVariable("PATH", ndkpath);


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
                Microsoft.Win32.RegistryKey key;
                key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("VSNDK");
                key.SetValue("Run", "True");
                key.Close();

                _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);

                try
                {
                    Solution2 soln = (Solution2)_dte.Solution;
                    List<String> buildThese = new List<String>();
                    _targetDir = new List<string[]>();

                    foreach (String startupProject in (Array)soln.SolutionBuild.StartupProjects)
                    {
                        foreach (Project p1 in soln.Projects)
                        {
                            if (p1.UniqueName == startupProject)
                            {
                                buildThese.Add(p1.FullName);

                                ConfigurationManager config = p1.ConfigurationManager;
                                Configuration active = config.ActiveConfiguration;
                                foreach (Property prop in active.Properties)
                                {
                                    try
                                    {
                                        if (prop.Name == "OutputPath")
                                        {
                                            string[] path = new string[2];
                                            path[0] = p1.Name + "_" + _isSimulator.ToString();
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

                    _hitPlay = true;
                    _amountOfProjects = buildThese.Count; // OnBuildDone will call build() only after receiving "amountOfProjects" events
                    foreach (string projectName in buildThese)
                        soln.SolutionBuild.BuildProject("Debug", projectName, false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
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
                // Write file to flag the deploy task that it should use the -debugNative option
                string fileContent = "Use -debugNative.\r\n";
                string appData = Environment.GetEnvironmentVariable("AppData");
                System.IO.StreamWriter file = new System.IO.StreamWriter(appData + @"\BlackBerry\vsndk-debugNative.txt");
                file.WriteLine(fileContent);
                file.Close();

                _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);


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
            getPID((DTE2)_dte, ref pidString);

            bool CancelDefault = LaunchDebugTarget(pidString);
        }

        /// <summary> 
        /// Launch an executable using the VSNDK debug engine. 
        /// </summary>
        /// <param name="pidString"> Process ID in string format. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool LaunchDebugTarget(string pidString)
        {
            Microsoft.VisualStudio.Shell.ServiceProvider sp =
                 new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);

            IVsDebugger dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));

            VsDebugTargetInfo info = new VsDebugTargetInfo();

            info.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.dlo = Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

            // Read debugger args from a file (it is set when the Deploy task is run)
            System.IO.StreamReader argsFile = null;
            try
            {
                string localAppData = Environment.GetEnvironmentVariable("AppData");
                argsFile = new System.IO.StreamReader(localAppData + @"\BlackBerry\vsndk-args-file.txt");
            }
            catch (Exception e)
            {
                Debug.Fail("Unexpected exception in LaunchDebugTarget");
            }

            // Store all debugger arguments in a string
            var nvc = new NameValueCollection();
            nvc.Add("pid", pidString);
            nvc.Add("targetIP", argsFile.ReadLine()); // The device (IP address)
            info.bstrExe = argsFile.ReadLine(); // The executable path
            nvc.Add("isSimulator", argsFile.ReadLine());
            nvc.Add("ToolsPath", argsFile.ReadLine());
            nvc.Add("PublicKeyPath", argsFile.ReadLine());

            // Decrypt stored password.
            byte[] data = Convert.FromBase64String(argsFile.ReadLine());
            if (data.Length > 0)
            {
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);
                nvc.Add("Password", Encoding.Unicode.GetString(decrypted));
            }

            info.bstrArg = NameValueCollectionHelper.DumpToString(nvc);
            argsFile.Close();

            info.bstrRemoteMachine = null; // debug locally
            info.fSendStdoutToOutputWindow = 0; // Let stdout stay with the application.
            info.clsidCustom = new Guid("{E5A37609-2F43-4830-AA85-D94CFA035DD2}"); // Set the launching engine the VSNDK engine guid
            info.grfLaunch = 0;

            IntPtr pInfo = System.Runtime.InteropServices.Marshal.AllocCoTaskMem((int)info.cbSize);
            System.Runtime.InteropServices.Marshal.StructureToPtr(info, pInfo, false);

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
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pInfo);
                }
            }

            return false;
        }

        /// <summary>
        /// Get list of installed APIs
        /// </summary>
        /// <returns></returns>
        public void GetInstalledAPIList()
        {
            try
            {
                _installedAPIList = new List<APIClass>();
                _installedNDKList = new List<APIClass>();

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
                                _installedAPIList.Add(aclass);
                            }
                            else
                            {
                                APIClass aclass = new APIClass(name.Item(0).InnerText, hostpath.Item(0).InnerText, targetpath.Item(0).InnerText, version.Item(0) == null ? "2.1.0" : version.Item(0).InnerText);
                                _installedNDKList.Add(aclass);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

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

            string[] directories = Directory.GetFiles(bbndkPathConst, "*.vmxf", SearchOption.AllDirectories);

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

        /// <summary>
        /// Check to see if Simulator is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <returns>true if installed</returns>
        private bool IsSimulatorInstalled(string version)
        {
            bool success = false;

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
        /// Get the PID of the launched native app by parsing text from the output window. 
        /// </summary>
        /// <param name="dte"> Application Object. </param>
        /// <param name="pidString"> Returns the Process ID as a string. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool getPID(DTE2 dte, ref string pidString)
        {
            // Select all of the text
            _owP.TextDocument.Selection.SelectAll();
            string outputText = _owP.TextDocument.Selection.Text;

            // Check for successful deployment
            if (System.Text.RegularExpressions.Regex.IsMatch(outputText, "Info: done"))
            {
                string pattern = @"\s+result::(\d+)\r\n.+";
                Regex r = new Regex(pattern, RegexOptions.IgnoreCase);

                // Match the regular expression pattern against a text string.
                Match m = r.Match(outputText);

                // Take first match
                if (m.Success)
                {
                    Group g = m.Groups[1];
                    CaptureCollection cc = g.Captures;
                    Capture c = cc[0];
                    pidString = c.ToString();

                    // Store proccess name and file location into ProcessesPath.txt, so "Attach To Process" would be able to find the 
                    // source code for a running process.
                    // First read the file.
                    string processesPaths = "";
                    System.IO.StreamReader readProcessesPathsFile = null;
                    try
                    {
                        readProcessesPathsFile = new System.IO.StreamReader(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\ProcessesPath.txt");
                        processesPaths = readProcessesPathsFile.ReadToEnd();
                        readProcessesPathsFile.Close();
                    }
                    catch (Exception e)
                    {
                        processesPaths = "";
                    }

                    // Updating the contents.
                    int begin = outputText.IndexOf("Deploy started: Project: ") + 25;
                    if (begin == -1)
                        begin = outputText.IndexOf("Project: ") + 9;
                    int end = outputText.IndexOf(", Configuration:", begin);
                    string processName = outputText.Substring(begin, end - begin) + "_" + _isSimulator.ToString();
                    begin = processesPaths.IndexOf(processName + ":>");

                    //                    string currentPath = dte.ActiveDocument.Path;
                    string currentPath = "";

                    foreach (string[] paths in _targetDir)
                    {
                        if (paths[0] == processName)
                        {
                            currentPath = paths[1];
                            break;
                        }
                    }

                    if (begin != -1)
                    {
                        begin += processName.Length + 2;
                        end = processesPaths.IndexOf("\r\n", begin);
                        processesPaths = processesPaths.Substring(0, begin) + currentPath + processesPaths.Substring(end);
                    }
                    else
                    {
                        processesPaths = processesPaths + processName + ":>" + currentPath + "\r\n";
                    }

                    // Writing contents to file.
                    System.IO.StreamWriter writeProcessesPathsFile = null;
                    try
                    {
                        writeProcessesPathsFile = new System.IO.StreamWriter(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\ProcessesPath.txt", false);
                        writeProcessesPathsFile.Write(processesPaths);
                        writeProcessesPathsFile.Close();
                    }
                    catch (Exception e)
                    {
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }



        #endregion

        #region Get Simulator List command

        /// <summary>
        /// Retrieve list of Available Simulators
        /// </summary>
        /// <returns></returns>
        public void GetSimulatorList()
        {
            if (GlobalFunctions.isOnline())
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorDataReceived);
                p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(SimulatorListDataReceived);


                /// Get Device PIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --list-all --simulator");

                try
                {
                    _simulatorList = new List<SimulatorsClass>();

                    p.Start();
                    p.BeginErrorReadLine();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    p.Close();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulatorListDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            string apilevel = "";
            string version = "";

            SimulatorsClass sim = null;

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
                    version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                    apilevel = version.Split('.')[0] + "." + version.Split('.')[1]; 

                    sim = _simulatorList.Find(i => i.APILevel  == apilevel);

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


        #endregion

        #region Get Available API List command

        /// <summary>
        /// Retrieve list of API's from 
        /// </summary>
        /// <returns></returns>
        public void GetAvailableAPIList()
        {
            if (GlobalFunctions.isOnline())
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorDataReceived);
                p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(APIListDataReceived);


                /// Get Device PIN
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = string.Format(@"/C " + bbndkPathConst + @"\eclipsec --list");

                try
                {
                    _tempAPITargetList = new List<APITargetClass>();

                    p.Start();
                    p.BeginErrorReadLine();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                    p.Close();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void APIListDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            string name = "";
            string description = "";
            string version = "";
            string error = "";
            APITargetClass api = null;

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
                    version = e.Data.Substring(0, e.Data.LastIndexOf(" - "));
                    name = e.Data.Substring(e.Data.LastIndexOf(" - ") + 3);
                    description = "Device Support Unknown.";

                    api = _tempAPITargetList.Find(i => i.TargetName == name);

                    if (api == null)
                    {
                        api = new APITargetClass(name, description, version);
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

                    api.IsInstalled = IsAPIInstalled(api.TargetVersion, api.TargetName);

                    api.IsBeta = name.Contains("Beta");

                }
            }
        }

        /// <summary>
        /// On Error received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);

                MessageBox.Show(e.Data);
            }
        }

        /// <summary>
        /// Check to see if API is installed
        /// </summary>
        /// <param name="version">Check version number</param>
        /// <param name="name">Check API name</param>
        /// <returns>true if installed</returns>
        public int IsAPIInstalled(string version, string name)
        {
            int success = 0;

            /// Check for 2.1 version
            if (version.StartsWith("2.1.0"))
                version = "2.1.0";

            if (_installedAPIList != null)
            {
                APIClass result = _installedAPIList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 1;
                }
            }

            if (_installedNDKList != null)
            {
                APIClass result = _installedNDKList.Find(i => i.Version.Contains(version));

                if (result != null)
                {
                    success = 2;
                }
            }

            return success;
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
                // Create a reference to the Output window.
                // Create a tool window reference for the Output window
                // and window pane.
                OutputWindow ow = ((DTE2)_dte).ToolWindows.OutputWindow;

                // Select the Build pane in the Output window.
                _owP = ow.OutputWindowPanes.Item("Build");
                _owP.Activate();

                
                UpdateManagerData upData = new UpdateManagerData(this);

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
            var m = SigningToolDialog.ShowModal();
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowSettingsWindow(object sender, EventArgs e)
        {
            var SettingsDialog = new Settings.SettingsDialog(this);
            var m = SettingsDialog.ShowModal();
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
            if (!DebugTokenDialog.IsClosing)
                DebugTokenDialog.ShowModal();
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
