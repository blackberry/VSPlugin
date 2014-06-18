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
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Components;
using BlackBerry.Package.Diagnostics;
using BlackBerry.Package.Editors;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;
using BlackBerry.Package.Options;
using BlackBerry.Package.Options.Dialogs;
using BlackBerry.Package.ViewModels;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Collections.Generic;
using EnvDTE;
using System.Windows.Forms;
using EnvDTE80;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using VSNDK.Parser;

namespace BlackBerry.Package
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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // Register the editor factory
    [XmlEditorDesignerViewRegistration("XML", "xml", LogicalViewID.Designer, 0x60, DesignerLogicalViewEditor = typeof(BarDescriptorEditorFactory),
        Namespace = "http://www.qnx.com/schemas/application/1.0", MatchExtensionAndNamespace = true)]
    // And which type of files we want to handle
    [ProvideEditorExtension(typeof(BarDescriptorEditorFactory), BarDescriptorEditorFactory.DefaultExtension, 0x40, NameResourceID = 106)]
    // We register that our editor supports LOGVIEWID_Designer logical view
    [ProvideEditorLogicalView(typeof(BarDescriptorEditorFactory), LogicalViewID.Designer)]
    // Microsoft Visual C# Project
    [EditorFactoryNotifyForProject("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}", BarDescriptorEditorFactory.DefaultExtension, GuidList.guidVSNDK_PackageEditorFactoryString)]

    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", VersionString, IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidVSNDK_PackageString)]

    // This attribute defines set of settings pages provided by this package.
    // They are automatically instantiated and displayed inside [Visual Studio -> Tools -> Options...] dialog.
    [ProvideOptionPage(typeof(GeneralOptionPage), "BlackBerry", "General", 1001, 1002, true)]
    [ProvideOptionPage(typeof(LogsOptionPage), "BlackBerry", "Logs", 1001, 1003, true)]
    [ProvideOptionPage(typeof(ApiLevelOptionPage), "BlackBerry", "API Levels", 1001, 1004, true)]
    [ProvideOptionPage(typeof(TargetsOptionPage), "BlackBerry", "Targets", 1001, 1005, true)]
    [ProvideOptionPage(typeof(SigningOptionPage), "BlackBerry", "Signing", 1001, 1006, true)]
    public sealed class BlackBerryPackage : Microsoft.VisualStudio.Shell.Package
    {
        public const string VersionString = "2.1.2014.0616";

        #region private member variables

        private BlackBerryPaneTraceListener _traceWindow;
        private DTE2 _dte;
        private BuildPlatformsManager _buildPlatformsManager;
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
        public BlackBerryPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
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

            TraceLog.WriteLine(" * loaded NDK descriptions");

            // setup called before running any 'tool':
            ToolRunner.Startup += (s, e) =>
                {
                    var ndk = PackageViewModel.Instance.ActiveNDK;

                    if (ndk != null)
                    {
                        e["QNX_TARGET"] = ndk.TargetPath;
                        e["QNX_HOST"] = ndk.HostPath;
                        e["PATH"] = string.Concat(Path.Combine(RunnerDefaults.JavaHome, "bin"), ";", e["PATH"],
                                                  Path.Combine(ndk.HostPath, "usr", "bin"), ";");
                    }
                    else
                    {
                        e["PATH"] = string.Concat(Path.Combine(RunnerDefaults.JavaHome, "bin"), ";", e["PATH"]);
                    }
                };

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            RegisterEditorFactory(new BarDescriptorEditorFactory());
            TraceLog.WriteLine(" * registered editors");

            
            _dte = (DTE2)GetService(typeof(SDTE));

            // PH: FIXME: introduce this functionality using new APIs
            /*
            if ((IsBlackBerrySolution(_dte)) && (apiList._installedAPIList.Count == 0))
            {
                UpdateManager.UpdateManagerDialog ud = new UpdateManager.UpdateManagerDialog("Please choose your default API Level to be used by the Visual Studio Plug-in.", "default", false, false);
                ud.ShowDialog();
            }
            */

            EnsureActiveNDK();

            _buildPlatformsManager = new BuildPlatformsManager(_dte);

            CommandHelper.Register(_dte, GuidList.guidVSStd97String, StandardCommands.cmdidStartDebug, StartDebugCommandEvents_AfterExecute, StartDebugCommandEvents_BeforeExecute);
            CommandHelper.Register(_dte, GuidList.guidVSStd97String, StandardCommands.cmdidStartDebug, StartDebugCommandEvents_AfterExecute, StartDebugCommandEvents_BeforeExecute);
            CommandHelper.Register(_dte, GuidList.guidVSStd2KString, StandardCommands.cmdidStartDebugContext, StartDebugCommandEvents_AfterExecute, StartDebugCommandEvents_BeforeExecute);
            TraceLog.WriteLine(" * registered build-platforms manager");

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            TraceLog.WriteLine(" * subscribed to IDE events");

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create command for the 'Options...' menu
                CommandID optionsCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryOptions);
                MenuCommand optionsMenu = new MenuCommand((s, e) => ShowOptionPage(typeof(GeneralOptionPage)), optionsCommandID);
                mcs.AddCommand(optionsMenu);

                // Create dynamic command for the 'devices-list' menu
                CommandID devicesCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryTargetsDevicesPlaceholder);
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
                CommandID apiLevelCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryTargetsApiLevelsPlaceholder);
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
                                            PackageCommands.cmdidBlackBerryHelpWelcomePage, PackageCommands.cmdidBlackBerryHelpSupportForum,
                                            PackageCommands.cmdidBlackBerryHelpDocNative, PackageCommands.cmdidBlackBerryHelpDocCascades, PackageCommands.cmdidBlackBerryHelpDocPlayBook,
                                            PackageCommands.cmdidBlackBerryHelpSamplesNative, PackageCommands.cmdidBlackBerryHelpSamplesCascades, PackageCommands.cmdidBlackBerryHelpSamplesPlayBook, PackageCommands.cmdidBlackBerryHelpSamplesOpenSource,
                                            PackageCommands.cmdidBlackBerryHelpAbout
                                       };
                foreach (var cmdID in helpCmdIDs)
                {
                    CommandID helpCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, cmdID);
                    MenuCommand helpMenu = new MenuCommand(OpenHelpWebPage, helpCommandID);
                    mcs.AddCommand(helpMenu);
                }

                // Create command for 'Configure...' [targets] menu
                CommandID configureCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryTargetsConfigure);
                MenuCommand configureMenu = new MenuCommand((s, e) => ShowOptionPage(typeof(TargetsOptionPage)), configureCommandID);
                mcs.AddCommand(configureMenu);

                // Create the command for the menu item.
                CommandID projectCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidfooLocalBox);
                OleMenuCommand projectItem = new OleMenuCommand(MenuItemCallback, projectCommandID);
                mcs.AddCommand(projectItem);

                TraceLog.WriteLine(" * initialized menus");
            }

            TraceLog.WriteLine("-------------------- DONE");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_buildPlatformsManager != null)
                {
                    _buildPlatformsManager.Close();
                    _buildPlatformsManager = null;
                }
            }

            base.Dispose(disposing);
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
                case PackageCommands.cmdidBlackBerryHelpWelcomePage:
                    OpenUrl("http://developer.blackberry.com/cascades/momentics/");
                    break;
                case PackageCommands.cmdidBlackBerryHelpSupportForum:
                    OpenUrl("http://supportforums.blackberry.com/t5/Developer-Support-Forums/ct-p/blackberrydev");
                    break;
                case PackageCommands.cmdidBlackBerryHelpDocNative:
                    OpenUrl("http://developer.blackberry.com/native/documentation/core/framework.html");
                    break;
                case PackageCommands.cmdidBlackBerryHelpDocCascades:
                    OpenUrl("http://developer.blackberry.com/native/documentation/cascades/dev/index.html");
                    break;
                case PackageCommands.cmdidBlackBerryHelpDocPlayBook:
                    OpenUrl("http://developer.blackberry.com/playbook/native/documentation/");
                    break;
                case PackageCommands.cmdidBlackBerryHelpSamplesNative:
                    OpenUrl("http://developer.blackberry.com/native/sampleapps/");
                    break;
                case PackageCommands.cmdidBlackBerryHelpSamplesCascades:
                    OpenUrl("http://developer.blackberry.com/native/sampleapps/");
                    break;
                case PackageCommands.cmdidBlackBerryHelpSamplesPlayBook:
                    OpenUrl("http://developer.blackberry.com/playbook/native/sampleapps/");
                    break;
                case PackageCommands.cmdidBlackBerryHelpSamplesOpenSource:
                    OpenUrl("https://github.com/blackberry");
                    break;
                case PackageCommands.cmdidBlackBerryHelpAbout:
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
        private bool IsBlackBerrySolution(DTE2 dte)
        {
            if (dte.Solution.FullName != "")
            {
                string fileText = File.ReadAllText(dte.Solution.FullName);
                return fileText.Contains("Debug|BlackBerry");
            }

            return false;
        }

        /// <summary>
        /// Makes sure the NDK is selected and its paths are stored inside registry for build toolset.
        /// </summary>
        private void EnsureActiveNDK()
        {
            PackageViewModel.Instance.EnsureActiveNDK();
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
                    RegistryKey key = Registry.CurrentUser.CreateSubKey("VSNDK");
                    key.SetValue("Run", "True");
                    key.Close();

                    _buildEvents.OnBuildDone += OnBuildDone;

                    try
                    {
                        Solution2 solution = (Solution2)_dte.Solution;
                        _hitPlay = true;
                        _amountOfProjects = _buildThese.Count; // OnBuildDone will call build() only after receiving "amountOfProjects" events
                        foreach (string projectName in _buildThese)
                            solution.SolutionBuild.BuildProject("Debug", projectName, false);
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
            _owP.TextDocument.Selection.SelectAll();
            string outputText = _owP.TextDocument.Selection.Text;

            if ((outputText == "") || (Regex.IsMatch(outputText, ">Build succeeded.\r\n")) || (!outputText.Contains("): error :")))
            {
                if (_isDebugConfiguration)
                {
                    // Write file to flag the deploy task that it should use the -debugNative option
                    string fileContent = "Use -debugNative.\r\n";
                    using (var file = new StreamWriter(RunnerDefaults.BuildDebugNativePath))
                    {
                        file.WriteLine(fileContent);
                    }

                    _buildEvents.OnBuildDone += OnBuildDone;
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
        /// Get the process ID and launch an executable using the BlackBerry debug engine. 
        /// </summary>
        private void Deployed()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey("VSNDK");
            key.SetValue("Run", "False");
            key.Close();

            string pidString = "";
            string toolsPath = "";
            string publicKeyPath = "";
            string targetIP = "";
            string password = "";
            string executablePath = "";
            if (GetProcessInfo(_dte, ref pidString, ref toolsPath, ref publicKeyPath, ref targetIP, ref password, ref executablePath))
            {
                bool CancelDefault = LaunchDebugTarget(pidString, toolsPath, publicKeyPath, targetIP, password, executablePath);
            }
            else
            {
                MessageBox.Show("Failed to debug the application.\n\nPlease, close the app in case it was launched in the device/simulator.", "Failed to launch debugger", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary> 
        /// Launch an executable using the BlackBerry debug engine. 
        /// </summary>
        /// <param name="pidString"> Process ID in string format. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool LaunchDebugTarget(string pidString, string toolsPath, string publicKeyPath, string targetIP, string password, string executablePath)
        {
            ServiceProvider sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);
            IVsDebugger dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));
            VsDebugTargetInfo info = new VsDebugTargetInfo();

            info.cbSize = (uint)Marshal.SizeOf(info);
            info.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

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
            info.clsidCustom = new Guid("{E5A37609-2F43-4830-AA85-D94CFA035DD2}"); // Set the launching engine as the BlackBerry debug-engine
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
            publicKeyPath = RunnerDefaults.SshPublicKeyPath;
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

            pidString = GetPIDfromGDB(_processName, targetIP, password, _isSimulator, toolsPath, publicKeyPath);

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

                // Store process name and file location into ProcessesPath.txt, so "Attach To Process" would be able to find the 
                // source code for a running process.
                // First read the file.
                _processName += "_" + _isSimulator;

                string processesPaths;
                try
                {
                    StreamReader readProcessesPathsFile = new StreamReader(Path.Combine(RunnerDefaults.DataDirectory, "ProcessesPath.txt"));
                    processesPaths = readProcessesPathsFile.ReadToEnd();
                    readProcessesPathsFile.Close();
                }
                catch (Exception)
                {
                    processesPaths = "";
                }

                // Updating the contents.
                int begin = processesPaths.IndexOf(_processName + ":>", StringComparison.Ordinal);

                if (begin != -1)
                {
                    begin += _processName.Length + 2;
                    int end = processesPaths.IndexOf("\r\n", begin, System.StringComparison.Ordinal);
                    processesPaths = processesPaths.Substring(0, begin) + currentPath + processesPaths.Substring(end);
                }
                else
                {
                    processesPaths = processesPaths + _processName + ":>" + currentPath + "\r\n";
                }

                // Writing contents to file.
                try
                {
                    StreamWriter writeProcessesPathsFile = new StreamWriter(Path.Combine(RunnerDefaults.DataDirectory, "ProcessesPath.txt"), false);
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

        private string GetPIDfromGDB(string processName, string ip, string password, bool isSimulator, string toolsPath, string publicKeyPath)
        {
            string pid = "";
            string response = GDBParser.GetPIDsThroughGDB(ip, password, isSimulator, toolsPath, publicKeyPath, 7);

            if ((response == "TIMEOUT!") || (response.IndexOf("1^error,msg=", 0, StringComparison.Ordinal) != -1)) //found an error
            {
                if (response == "TIMEOUT!") // Timeout error, normally happen when the device is not connected.
                {
                    MessageBox.Show("Please, verify if the Device/Simulator IP in \"BlackBerry -> Settings\" menu is correct and check if it is connected.", "Device/Simulator not connected or not configured properly", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                    if (response[29] == ':') // error: 1^error,msg="169.254.0.3:8000: The requested address is not valid in its context."
                    {
                        string txt = response.Substring(13, response.IndexOf(':', 13) - 13) + response.Substring(29, response.IndexOf('"', 31) - 29);
                        string caption;
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
            }
            else if (response.Contains("^done"))
            {
                int i = response.IndexOf(processName + " - ", StringComparison.Ordinal);
                if (i != -1)
                {
                    i += processName.Length + 3;
                    pid = response.Substring(i, response.IndexOf('/', i) - i);
                }
            }
            return pid;
        }


        /// <summary>
        /// Verify if the app configuration is Debug.
        /// </summary>
        /// <returns> True if Debug configuration; False otherwise. </returns>
        private bool CheckDebugConfiguration()
        {
            Solution2 solution = (Solution2)_dte.Solution;
            foreach (String startupProject in (Array)solution.SolutionBuild.StartupProjects)
            {
                foreach (Project p1 in solution.Projects)
                {
                    if (p1.UniqueName == startupProject)
                    {
                        ConfigurationManager config = p1.ConfigurationManager;
                        Configuration active = config.ActiveConfiguration;

                        return active.ConfigurationName.ToUpper() == "DEBUG";
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
        /// <param name="guid">Command GUID. </param>
        /// <param name="id">Command ID. </param>
        /// <param name="customIn">Custom IN Object. </param>
        /// <param name="customOut">Custom OUT Object. </param>
        private void StartDebugCommandEvents_AfterExecute(string guid, int id, object customIn, object customOut)
        {
            Debug.WriteLine("After Start Debug");
        }

        /// <summary> 
        /// New Start Debug Command Events Before Execution Event Handler. Call the method responsible for building the app. 
        /// </summary>
        /// <param name="guid"> Command GUID. </param>
        /// <param name="id"> Command ID. </param>
        /// <param name="customIn"> Custom IN Object. </param>
        /// <param name="customOut"> Custom OUT Object. </param>
        /// <param name="cancelDefault"> Cancel the default execution of the command. </param>
        private void StartDebugCommandEvents_BeforeExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault)
        {
            bool bbPlatform = false;
            if (_dte.Solution.SolutionBuild.ActiveConfiguration != null)
            {
                _isDebugConfiguration = CheckDebugConfiguration();

                SolutionContexts scCollection = _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts;
                foreach (SolutionContext sc in scCollection)
                {
                    if (sc.PlatformName == "BlackBerry" || sc.PlatformName == "BlackBerrySimulator")
                    {
                        bbPlatform = true;
                        _isSimulator = sc.PlatformName == "BlackBerrySimulator";
                    }
                }
            }

            Debug.WriteLine("Before Start Debug");

            if (ControlDebugEngine.isDebugEngineRunning || !bbPlatform)
            {
                // Disable the override of F5 (this allows the debugged process to continue execution)
                cancelDefault = false;
            }
            else
            {
                try
                {
                    Solution2 solution = (Solution2)_dte.Solution;
                    _buildThese = new List<String>();
                    _targetDir = new List<string[]>();

                    foreach (String startupProject in (Array)solution.SolutionBuild.StartupProjects)
                    {
                        foreach (Project p1 in solution.Projects)
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
                OutputWindow ow = _dte.ToolWindows.OutputWindow;

                // Select the Build pane in the Output window.
                _owP = ow.OutputWindowPanes.Item("Build");
                _owP.Activate();


                // PH: FIXME: implement using new APIs
                /*
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
                 */
            }
        }


        /// <summary> 
        /// This event is fired only when the build/rebuild/clean process ends. 
        /// </summary>
        /// <param name="scope"> Represents the scope of the build. </param>
        /// <param name="action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (action == vsBuildAction.vsBuildActionBuild)
            {
                _amountOfProjects -= 1;
                if (_amountOfProjects == 0)
                {
                    _buildEvents.OnBuildDone -= OnBuildDone;
                    Built();
                }
            }
            else if (action == vsBuildAction.vsBuildActionDeploy)
            {
                _buildEvents.OnBuildDone -= OnBuildDone;
                _isDeploying = false;
                Deployed();
            }
        }

        /// <summary> 
        /// This event is fired only when user wants to build, rebuild or clean the project. 
        /// </summary>
        /// <param name="Scope"> Represents the scope of the build. </param>
        /// <param name="Action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            // PH: FIXME: implement using new APIs
            /*
            InstalledAPIListSingleton apiList = InstalledAPIListSingleton.Instance;
            if ((IsBlackBerrySolution(_dte)) && (apiList._installedAPIList.Count == 0))
            {
                UpdateManager.UpdateManagerDialog ud = new UpdateManager.UpdateManagerDialog("Please choose your default API Level to be used by the Visual Studio Plug-in.", "default", false, false);
                ud.ShowDialog();
            }

            if ((action == vsBuildAction.vsBuildActionBuild) || (action == vsBuildAction.vsBuildActionRebuildAll))
            {
                if ((_hitPlay == false) && (_isDeploying == false)) // means that the "play" building and deploying process was cancelled before, so we have to disable the 
                // OnBuildDone event to avoid deploying in case user only wants to build.
                {
                    _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                }
                _hitPlay = false;
            }
             */
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
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
                string filename = dlg.FileName;
                FileInfo fi = new FileInfo(filename);
                string folderName = fi.DirectoryName;

                Array projects = (Array)_dte.ActiveSolutionProjects;
                Project project = (Project)projects.GetValue(0);
                string name = project.FullName;

                // Create the dialog instance without Help support.
                var importSummary = new Import.Import(project, folderName, name);
                importSummary.ShowModel2();
            }
        }
        
        #endregion
    }
}
