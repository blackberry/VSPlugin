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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BlackBerry.DebugEngine;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Dialogs;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BuildPlatformsManager
    {
        #region Internal Classes

        sealed class ConfigTableEntry
        {
            public string config;
            public string platform;
            public bool deployable;
        }

        #endregion

        private readonly DTE2 _dte;
        private TokenProcessor _tokenProcessor;
        private readonly List<ConfigTableEntry> _configTable;

        private bool _isSimulator;
        private List<string[]> _targetDir;
        private List<String> _buildThese;
        private bool _hitPlay;
        private int _amountOfProjects;
        private bool _isDeploying;
        private OutputWindowPane _owP;
        private bool _isDebugConfiguration = true;
        private string _processName = "";

        private BuildEvents _buildEvents;

        private CommandEvents _eventsDebug;
        private CommandEvents _eventsDebugContext;


        private const string BLACKBERRY = "BlackBerry";
        private const string BLACKBERRYSIMULATOR = "BlackBerrySimulator";
        private const string STANDARD_TOOL_BAR = "Standard";
        private const string SOLUTION_CONFIGURATIONS = "Solution Configurations";
        private const string SOLUTION_PLATFORMS = "Solution Platforms";
        private const string BAR_DESCRIPTOR = "bar-descriptor.xml";
        private const string BAR_DESCRIPTOR_PATH = @"\..\VCWizards\CodeWiz\BlackBerry\BarDescriptor\Templates\1033\";

        public BuildPlatformsManager(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            // initialize variables:
            _dte = dte;
            _configTable = new List<ConfigTableEntry>();
        }

        public void Initialize()
        {
            // register for command events, when accessing build platforms:
            CommandHelper.Register(_dte, GuidList.guidVSStd2KString, StandardCommands.cmdidSolutionPlatform, cmdNewPlatform_beforeExec, cmdNewPlatform_afterExec);
            CommandHelper.Register(_dte, GuidList.guidVSDebugGroup, StandardCommands.cmdidDebugBreakatFunction, cmdNewFunctionBreakpoint_beforeExec, cmdNewFunctionBreakpoint_afterExec);

            //DisableIntelliSenseErrorReport(true);
            CheckSolutionPlatformCommand();

            // INFO: the references to returned objects must be stored and live as long, as the handlers are needed,
            // since they are COM objects and will be automatically reclaimed on next GC.Collect(), causing handlers to be unsubscribed...
            _eventsDebug = CommandHelper.Register(_dte, GuidList.guidVSStd97String, StandardCommands.cmdidStartDebug, StartDebugCommandEvents_BeforeExecute, StartDebugCommandEvents_AfterExecute);
            _eventsDebugContext = CommandHelper.Register(_dte, GuidList.guidVSStd2KString, StandardCommands.cmdidStartDebugContext, StartDebugCommandEvents_BeforeExecute, StartDebugCommandEvents_AfterExecute);

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;
        }

        #region Properties

        /// <summary>
        /// Gets the currently selected device all actions should be performed against.
        /// </summary>
        private DeviceDefinition ActiveDevice
        {
            get { return PackageViewModel.Instance.ActiveDevice; }
        }

        /// <summary>
        /// Gets the currently selected NDK to build against.
        /// </summary>
        private NdkInfo ActiveNDK
        {
            get { return PackageViewModel.Instance.ActiveNDK; }
        }

        #endregion

        /// <summary> 
        /// Terminate the manager.
        /// </summary>
        public void Close()
        {
            //DisableIntelliSenseErrorReport(false);
        }

        /// <summary> 
        /// Solution Platform command is shown in the Standard toolbar by default with Visual C++ settings. Add the 
        /// command if not in the Standard toolbar. 
        /// </summary>
        private void CheckSolutionPlatformCommand()
        {
            DTE dte = (DTE)_dte;
            CommandBars commandBars = (CommandBars)dte.CommandBars;
            CommandBar standardCommandBar = commandBars[STANDARD_TOOL_BAR];
            int pos = 0;
            foreach (CommandBarControl cmd in standardCommandBar.Controls)
            {
                if (cmd.Caption == SOLUTION_CONFIGURATIONS)
                    pos = cmd.Index;
                if (cmd.Caption == SOLUTION_PLATFORMS)
                    return;
            }

            Command sp = null;
            foreach (Command c in dte.Commands)
            {
                if (c.Guid == GuidList.guidVSStd2KString && c.ID == StandardCommands.cmdidSolutionPlatform)
                {
                    sp = c;
                    break;
                }
            }
            if (sp != null)
                sp.AddControl(standardCommandBar, pos + 1);
        }

        /// <summary> 
        /// Set the DisableErrorReporting property value. 
        /// </summary>
        /// <param name="disable"> The property value to set. </param>
        private void DisableIntelliSenseErrorReport(bool disable)
        {
            DTE dte = _dte as DTE;
            var txtEdCpp = dte.get_Properties("TextEditor", "C/C++ Specific");
            if (txtEdCpp != null)
            {
                var prop = txtEdCpp.Item("DisableErrorReporting");
                if (prop != null)
                    prop.Value = disable;
            }
        }

        /// <summary> 
        /// New Platform Before Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        /// <param name="CancelDefault">Cancel the default execution of the command. </param>
        private void cmdNewPlatform_beforeExec(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            GetSolutionPlatformConfig();
        }

        /// <summary> 
        /// New Platform After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        private void cmdNewPlatform_afterExec(string Guid, int ID, object CustomIn, object CustomOut)
        {
            SolutionPlatformConfig();
            AddBarDescriptor();
        }

        /// <summary> 
        /// New Function Breakpoint Before Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        /// <param name="CancelDefault">Cancel the default execution of the command. </param>
        private void cmdNewFunctionBreakpoint_beforeExec(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            // Add Code Here
        }

        /// <summary> 
        /// New Function Breakpoint After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid">Command GUID. </param>
        /// <param name="ID">Command ID. </param>
        /// <param name="CustomIn">Custom IN Object. </param>
        /// <param name="CustomOut">Custom OUT Object. </param>
        private void cmdNewFunctionBreakpoint_afterExec(string Guid, int ID, object CustomIn, object CustomOut)
        {
            Breakpoint functionBP = _dte.Debugger.Breakpoints.Item(_dte.Debugger.Breakpoints.Count);

            if (functionBP != null)
            {
                if ((functionBP.FunctionColumnOffset != 1) || (functionBP.FunctionLineOffset != 1))
                {
                    System.Windows.Forms.MessageBox.Show("The breakpoint cannot be set.  Function breakpoints are only supported on the first line.", "Microsoft Visual Studio", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    functionBP.Delete();
                }
            }
        }

        /// <summary>
        /// Set solution config after edit
        /// </summary>
        private void SolutionPlatformConfig()
        {
            DTE dte = _dte as DTE;

            SolutionConfigurations SGS = dte.Solution.SolutionBuild.SolutionConfigurations;

            foreach (SolutionConfiguration SG in SGS)
            {
                string name = SG.Name;

                SolutionContexts SCS = SG.SolutionContexts;
                foreach (SolutionContext SC in SCS)
                {
                    string cname = SC.ConfigurationName;
                    string pname = SC.PlatformName;
                    string prname = SC.ProjectName;

                    ConfigTableEntry e = _configTable.Find(i => (i.config == cname) && (i.platform == pname));

                    if (e != null)
                    {
                        _configTable.Remove(e);
                    }
                    else
                    {
                        SC.ShouldDeploy = true;
                    }

                }
            }
        }

        /// <summary>
        /// Get solution configuration before edit
        /// </summary>
        private void GetSolutionPlatformConfig()
        {
            DTE dte = _dte as DTE;

            SolutionConfigurations SGS = dte.Solution.SolutionBuild.SolutionConfigurations;

            foreach (SolutionConfiguration SG in SGS)
            {
                string name = SG.Name;

                SolutionContexts SCS = SG.SolutionContexts;
                foreach (SolutionContext SC in SCS)
                {
                    string cname = SC.ConfigurationName;
                    string pname = SC.PlatformName;
                    string prname = SC.ProjectName;

                    ConfigTableEntry c = new ConfigTableEntry();
                    c.platform = pname;
                    c.config = cname;
                    c.deployable = SC.ShouldDeploy;

                    _configTable.Add(c);
                }
            }
        }

        /// <summary> 
        /// Add Bar Descriptor to each project. 
        /// </summary>
        private void AddBarDescriptor()
        {
            try
            {
                DTE dte = _dte as DTE;
                Projects projs = dte.Solution.Projects;


                List<Project> projList = new List<Project>();
                foreach (Project proj in projs)
                {
                    projList.Add(proj);
                }

                while (projList.Count > 0)
                {
                    Project proj = projList.ElementAt(0);
                    projList.RemoveAt(0);

                    Configuration config;
                    Property prop;
                    try
                    {
                        config = proj.ConfigurationManager.ActiveConfiguration;
                        prop = config.Properties.Item("ConfigurationType");
                    }
                    catch
                    {
                        config = null;
                        prop = null;
                    }

                    if (prop == null)
                    {
                        if (proj.ProjectItems != null)
                        {
                            foreach (ProjectItem projItem in proj.ProjectItems)
                            {
                                if (projItem.SubProject != null)
                                    projList.Add(projItem.SubProject);
                            }
                        }
                        continue;
                    }
                    
                    if (Convert.ToInt16(prop.Value) != Convert.ToInt16(ConfigurationTypes.typeApplication))
                        continue;

                    if (config.PlatformName != BLACKBERRY && config.PlatformName != BLACKBERRYSIMULATOR)
                        continue;
                    
                    ProjectItem baritem = proj.ProjectItems.Item(BAR_DESCRIPTOR);
                    string n = proj.Name;
                    if (baritem == null)
                    {
                        _tokenProcessor = new TokenProcessor();
                        Debug.WriteLine("Add bar descriptor file to the project");
                        string templatePath = dte.Solution.ProjectItemsTemplatePath(proj.Kind);
                        templatePath += BAR_DESCRIPTOR_PATH + BAR_DESCRIPTOR;
                        _tokenProcessor.AddReplace(@"[!output PROJECT_NAME]", proj.Name);
                        string destination = Path.GetFileName(templatePath);

                        // Remove directory used in previous versions of this plug-in.
                        string folder = Path.Combine(Path.GetDirectoryName(proj.FullName), proj.Name + "_barDescriptor");
                        if (Directory.Exists(folder))
                        {
                            try
                            {
                                Directory.Delete(folder);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        folder = Path.Combine(Path.GetDirectoryName(proj.FullName), "BlackBerry-" + proj.Name);
                        Directory.CreateDirectory(folder);
                        destination = Path.Combine(folder, destination);
                        _tokenProcessor.UntokenFile(templatePath, destination);
                        ProjectItem projectitem = proj.ProjectItems.AddFromFile(destination);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
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
                    using (var file = new StreamWriter(ConfigDefaults.BuildDebugNativePath))
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
            string currentPath = "";
            string executablePath;

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

            var ndk = ActiveNDK;
            var device = ActiveDevice;

            if (ndk == null)
            {
                MessageBoxHelper.Show("Missing NDK selected. Please install any and mark as active using BlackBerry menu options.", null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (device == null)
            {
                MessageBoxHelper.Show("Missing device selected. Please define an IP, password and mark a device as active using BlackBerry menu options.", null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LaunchDebugTarget(_processName, ndk.ToDefinition().ToolsPath, ConfigDefaults.SshPublicKeyPath.Replace('\\', '/'), device.IP, device.Password, executablePath);
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
            info.clsidCustom = new Guid(AD7Engine.DebugEngineGuid); // Set the launching engine as the BlackBerry debug-engine
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
            Debug.WriteLine("Before Start Debug");

            if (DebugEngineStatus.IsRunning)
            {
                // Disable the override of F5 (this allows the debugged process to continue execution)
                cancelDefault = false;
                return;
            }

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

            if (!bbPlatform)
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

                if (_isDebugConfiguration)
                {
                    /*
                     PH: FIXME: update API Level vs current project verification...
                    UpdateManagerData upData;
                    if (_targetDir.Count > 0)
                        upData = new UpdateManagerData(_targetDir[0][1]);
                    else
                        upData = new UpdateManagerData();

                    if (!upData.validateDeviceVersion(_isSimulator))
                    {
                        cancelDefault = true;
                    }
                    else
                     */
                    {
                        BuildBar();
                        cancelDefault = true;
                    }
                }
                else
                {
                    BuildBar();
                    cancelDefault = true;
                }
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
        /// <param name="scope"> Represents the scope of the build. </param>
        /// <param name="action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (IsBlackBerrySolution(_dte) && ActiveNDK == null)
            {
                var form = new MissingNdkInstalledForm();
                form.ShowDialog();
                return;
            }

            if (action == vsBuildAction.vsBuildActionBuild || action == vsBuildAction.vsBuildActionRebuildAll)
            {
                if (!_hitPlay && !_isDeploying)
                {
                    // means that the "play" building and deploying process was cancelled before, so we have to disable the 
                    // OnBuildDone event to avoid deploying in case user only wants to build.
                    _buildEvents.OnBuildDone -= OnBuildDone;
                }
                _hitPlay = false;
            }
        }

        #endregion

        /// <summary>
        /// Check to see if current solution is configured with a BlackBerry Configuration.
        /// </summary>
        public bool IsBlackBerrySolution(DTE2 dte)
        {
            if (dte.Solution != null)
            {
                var projects = dte.Solution.Projects;
                if (projects != null)
                {
                    foreach (Project project in projects)
                    {
                        var platformName = project.ConfigurationManager != null ? project.ConfigurationManager.ActiveConfiguration.PlatformName : null;
                        if (platformName == "BlackBerry" || platformName == "BlackBerrySimulator")
                            return true;
                    }
                }
            }

            return false;
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
            return false;
        }
    }
}
