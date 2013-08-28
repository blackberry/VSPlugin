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
        private static bool _isDebugEngineRunning = false;
        private BuildEvents _buildEvents;
        private List<string[]> _targetDir = null;
        private bool _hitPlay = false;
        private int _amountOfProjects = 0;
        private bool _isDeploying = false;
        private OutputWindowPane _owP;

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

                string qnx_config = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK";

                System.Environment.SetEnvironmentVariable("QNX_TARGET", qnx_target);
                System.Environment.SetEnvironmentVariable("QNX_HOST", qnx_host);
                System.Environment.SetEnvironmentVariable("QNX_CONFIGURATION", qnx_config);

                string ndkpath = string.Format(@"{0}/usr/bin;{1}\bin;{0}/usr/qde/eclipse/jre/bin;", qnx_host, qnx_config) +
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

            if ((outputText == "") || (System.Text.RegularExpressions.Regex.IsMatch(outputText, ">Build succeeded.\r\n")))
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

            if (_isDebugEngineRunning || !bbPlatform)
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

                UpdateManagerData upData = new UpdateManagerData(false);

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
            // Create the dialog instance without Help support.
            var SettingsDialog = new Settings.SettingsDialog();
            // Show the dialog.
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
