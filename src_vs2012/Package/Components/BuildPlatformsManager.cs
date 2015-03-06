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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackBerry.DebugEngine;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Services;
using BlackBerry.Package.Dialogs;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Options;
using BlackBerry.Package.ViewModels;
using BlackBerry.Package.Wizards;
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
    /// Manager class helpful in hijacking the Visual Studio's UI to initiate the build and deployment of BlackBerry project.
    /// It exposes some detection mechanisms for external usages.
    /// </summary>
    internal sealed class BuildPlatformsManager
    {
        #region Internal Classes

        /// <summary>
        /// Class carrying arguments required to attach to console logs for specified process.
        /// </summary>
        sealed class LogAttachRequest
        {
            /// <summary>
            /// Init constructor.
            /// </summary>
            public LogAttachRequest(Project project, uint pid)
            {
                if (project == null)
                    throw new ArgumentNullException("project");

                PID = pid;
                Project = project;
            }

            #region Properties

            /// <summary>
            /// Gets the process ID to start capturing logs.
            /// </summary>
            public uint PID
            {
                get;
                private set;
            }

            public Project Project
            {
                get;
                private set;
            }

            #endregion

            /// <summary>
            /// Issues request to attach to running process and capture output logs.
            /// </summary>
            public void Attach(DeviceDefinition device)
            {
                if (device == null)
                    throw new ArgumentNullException("device");

                Targets.Connect(device, OnConnected);
            }

            private void OnConnected(object sender, TargetConnectionEventArgs e)
            {
                if (e.Status == TargetStatus.Connected)
                {
                    Targets.Unsubscribe(e.Device, OnConnected);

                    var process = e.Client.SysInfoService.FindProcess(PID);
                    if (process != null)
                    {
                        Targets.Trace(e.Device, process, false);
                    }
                    else
                    {
                        TraceLog.WarnLine("Process to capture logs doesn't exist anymore.");
                    }
                }
            }

            /// <summary>
            /// Creates new request to attach to output logs.
            /// </summary>
            public static LogAttachRequest Create(Project project)
            {
                if (project == null)
                    throw new ArgumentNullException("project");

                // deployment was done, look for a file with run info:
                // (HINT: this file is created by BBDeploy task, as a result of successful .bar file deployment and launch)
                var runInfoFileName = ProjectHelper.GetFlagFileNameForRunInfo(project);

                if (!File.Exists(runInfoFileName))
                {
                    TraceLog.WarnLine("Unable to find 'runinfo' file to correctly capture logs.");
                    return null;
                }

                var runInfo = File.ReadAllLines(runInfoFileName);
                uint pid = 0;

                foreach (var info in runInfo)
                {
                    if (info.StartsWith("pid=", StringComparison.OrdinalIgnoreCase))
                    {
                        uint.TryParse(info.Substring(4), out pid);
                    }
                }

                if (pid != 0)
                {
                    return new LogAttachRequest(project, pid);
                }

                TraceLog.WarnLine("Invalid PID inside 'runinfo' file to correctly capture logs.");
                return null;
            }
        }

        #endregion

        private readonly DTE2 _dte;

        private List<string> _buildThese;
        private readonly List<string> _filesToDelete;
        private bool _hitPlay;
        private int _amountOfProjects;
        private bool _isDeploying;
        private Project _startProject;
        private bool _startDebugger;
        private LogAttachRequest _currentAttachRequest;

        private BuildEvents _buildEvents;
        private SolutionEvents _solutionEvents;
        private CommandEvents _deploymentEvents;
        private CommandEvents _eventsDebug;
        private CommandEvents _eventsNoDebug;
        private CommandEvents _eventsDebugContext;

        private bool _solutionOpened;
        private int _openedBlackBerryProjects;

        private IServiceProvider _serviceProvider;
        private ErrorManager _errorManager;

        private const string ConfigNameBlackBerry = "BlackBerry";
        private const string ToolbarNameStandard = "Standard";
        private const string SolutionConfigurationsName = "Solution Configurations";
        private const string SolutionPlatformsName = "Solution Platforms";
        private const string BarDescriptorFileName = "bar-descriptor.xml";
        private const string BarDescriptorTemplate = @"Shared\bar-descriptor.xml";

        public event Action<string> Navigate;
        public event Action<Type> OpenSettingsPage;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BuildPlatformsManager(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            _dte = dte;
            _filesToDelete = new List<string>();
        }

        public void Initialize()
        {
            if (_buildEvents != null)
                return;

            _serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider) _dte);

            // register for command events, when accessing build platforms:
            _deploymentEvents = CommandHelper.Register(_dte, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd2KCmdID.SolutionPlatform, null, OnNewPlatform_AfterExecute);
            //CommandHelper.Register(_dte, GuidList.guidVSDebugGroup, StandardCommands.cmdidDebugBreakatFunction, cmdNewFunctionBreakpoint_beforeExec, cmdNewFunctionBreakpoint_afterExec);

            ShowSolutionPlatformSelector();

            // INFO: the references to returned objects must be stored and live as long, as the handlers are needed,
            // since they are COM objects and will be automatically reclaimed on next GC.Collect(), causing handlers to be unsubscribed...
            _eventsDebug = CommandHelper.Register(_dte, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.Start, StartDebugCommandEvents_BeforeExecute, null);
            _eventsNoDebug = CommandHelper.Register(_dte, VSConstants.GUID_VSStandardCommandSet97, VSConstants.VSStd97CmdID.StartNoDebug, StartNoDebugCommandEvents_BeforeExecute, null);
            _eventsDebugContext = CommandHelper.Register(_dte, VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.PROJSTARTDEBUG, StartDebugCommandEvents_BeforeExecute, null);

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;
            _buildEvents.OnBuildDone += OnDeploymentDoneAttachLogs;

            // to monitor, when to disable IntelliSense errors,
            // TODO: however there is still one case not covered - when project is added as Win32 and manually platform is added and configuration converted co BlackBerry...
            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.Opened += OnSolutionOpened;
            _solutionEvents.AfterClosing += OnSolutionClosed;
            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;

            _errorManager = new ErrorManager(_serviceProvider, new Guid("{54340ee9-e59e-4ff1-8e5d-0370f700eaed}"), "BlackBerry Errors");

            PackageViewModel.Instance.TargetsChanged += (s, e) => VerifyCommonErrors();
        }

        private void OnDeploymentDoneAttachLogs(vsBuildScope scope, vsBuildAction action)
        {
            if (action == vsBuildAction.vsBuildActionDeploy)
            {
                var project = GetActiveSolutionProject(_dte);
                if (!_startDebugger && project != null)
                {
                    // deployment was done, look for a file with run info:
                    _currentAttachRequest = LogAttachRequest.Create(project);
                    if (_currentAttachRequest != null)
                    {
                        ActivateOutputWindowPane("Debug");
                        _currentAttachRequest.Attach(ActiveTarget);
                    }
                }
            }
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
        /// Gets the currently selected simulator all actions should be performed against.
        /// </summary>
        private DeviceDefinition ActiveSimulator
        {
            get { return PackageViewModel.Instance.ActiveSimulator; }
        }

        /// <summary>
        /// Gets the currently selected NDK to build against.
        /// </summary>
        private NdkInfo ActiveNDK
        {
            get { return PackageViewModel.Instance.ActiveNDK; }
        }

        /// <summary>
        /// Gets the currently selected runtime libraries of the device to debug against.
        /// </summary>
        private RuntimeInfo ActiveRuntime
        {
            get { return PackageViewModel.Instance.ActiveRuntime; }
        }

        /// <summary>
        /// Gets the active target, where the application will be deployed and run on.
        /// </summary>
        private DeviceDefinition ActiveTarget
        {
            get
            {
                if (_startProject != null && IsBlackBerryProject(_startProject))
                {
                    var projectArch = ProjectHelper.GetTargetArchitecture(_startProject);
                    var simulatorArch = DeviceDefinition.GetArchitecture(DeviceDefinitionType.Simulator);
                    if (string.Compare(projectArch, simulatorArch, StringComparison.OrdinalIgnoreCase) == 0)
                        return ActiveSimulator;
                }

                // by default, try to run on a device:
                return ActiveDevice;
            }
        }

        /// <summary>
        /// Gets the list of all available target devices.
        /// </summary>
        private DeviceDefinition[] TargetDevices
        {
            get { return PackageViewModel.Instance.TargetDevices; }
        }

        /// <summary>
        /// Gets the list of installed NDKs on the machine.
        /// </summary>
        private NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
        }

        #endregion

        /// <summary> 
        /// Terminate the manager.
        /// </summary>
        public void Close()
        {
        }

        private static Project GetActiveSolutionProject(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            if (dte.ActiveSolutionProjects != null)
            {
                foreach (Project project in (Array) dte.ActiveSolutionProjects)
                {
                    return project;
                }
            }

            return null;
        }

        private static Project GetBuildStartupProject(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            if (dte.Solution != null && dte.Solution.SolutionBuild != null && dte.Solution.SolutionBuild.StartupProjects != null)
            {
                foreach (string startupProject in (Array) dte.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (Project project in dte.Solution.Projects)
                    {
                        if (project.UniqueName == startupProject)
                        {
                            return project;
                        }
                    }
                }
            }

            return null;
        }

        #region Managing IntelliSense Error Reporting

        /// <summary>
        /// Checks, if BlackBerry-dedicated MSBuild platform has been installed.
        /// </summary>
        public static bool IsMSBuildPlatformInstalled
        {
            get
            {
                var buildTasksAssemblyPath = Path.Combine(ConfigDefaults.MSBuildVCTargetsPath, "Platforms", "BlackBerry", "BlackBerry.BuildTasks.dll");
                return File.Exists(buildTasksAssemblyPath);
            }
        }

        /// <summary>
        /// Checks the current plugin and Visual Studio state for most common errors to minimize developer's frustration, why things are not working.
        /// </summary>
        public void VerifyCommonErrors()
        {
            _errorManager.Clear();

            // check if appropriate platform exists and matches expected version:
            var buildTasksAssemblyPath = Path.Combine(ConfigDefaults.MSBuildVCTargetsPath, "Platforms", "BlackBerry", "BlackBerry.BuildTasks.dll");
            if (!File.Exists(buildTasksAssemblyPath))
            {
                _errorManager.Add(TaskErrorCategory.Error, "MSBuild \"BlackBerry\" build platform was not found. Building projects won't be possible at all. Visit " + ConfigDefaults.GithubProjectWikiInstallation + " [double-click] for details, how to install it.", OpenInstallationPage);
            }
            else
            {
                try
                {
                    var installedVersion = AssemblyName.GetAssemblyName(buildTasksAssemblyPath).Version;
                    var expectedVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    // verify versions:
                    if (installedVersion.Major != expectedVersion.Major || installedVersion.Minor != expectedVersion.Minor || installedVersion.Build != expectedVersion.Build)
                    {
                        _errorManager.Add(TaskErrorCategory.Warning, "Invalid version of existing MSBuild \"BlackBerry\" build platform (installed: " + ToShortVersion(installedVersion) + ", expected: " + ToShortVersion(expectedVersion) + "). Some features might simply stop working. Visit " + ConfigDefaults.GithubProjectWikiInstallation + " [double-click] for details, how to upgrade it.", OpenInstallationPage);
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to determine version of MSBuild \"BlackBerry\" build platform");
                }
            }

            // is Java detected?
            if (string.IsNullOrEmpty(ConfigDefaults.JavaHome) || !Directory.Exists(ConfigDefaults.JavaHome))
            {
                _errorManager.Add(TaskErrorCategory.Warning, "Java was not detected. Underlying BlackBerry tools might stop working. Specify one at \"BlackBerry -> Options -> General\" [double-click].", OpenGeneralSettings);
            }

            // check, if any project is placed on incorrect location, not supported by underlying makefile system:
            if (_dte != null && _dte.Solution != null && _dte.Solution.Projects.Count > 0)
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    if (IsBlackBerryProject(project))
                    {
                        // is path valid?
                        var projectPath = Path.GetDirectoryName(project.FullName);
                        if (!string.IsNullOrEmpty(projectPath) && !IsValidProjectPath(projectPath))
                        {
                            _errorManager.Add(TaskErrorCategory.Warning, string.Concat("Project path: \"", projectPath, "\" is invalid and might lead to problems in underlying makefile system. Move the project to the one without spaces and non-ASCII characters."), project, null, OpenGeneralSettings);
                        }

                        // is name valid?
                        var projectName = project.Name;
                        if (!string.IsNullOrEmpty(projectPath) && !IsValidProjectName(projectName))
                        {
                            _errorManager.Add(TaskErrorCategory.Error, string.Concat("Project name: \"", projectName, "\" is invalid. Remove all spaces and non-ASCII characters."), project, null, OpenGeneralSettings);
                        }
                    }
                }
            }

            // check if any NDK is installed:
            if (InstalledNDKs.Length == 0)
            {
                _errorManager.Add(TaskErrorCategory.Error, "Missing any BlackBerry NativeCore SDK. Compilation won't be possible at all. Add one on \"BlackBerry -> Options -> API-Levels\" [double-click] to be able to perform builds your applications.", OpenApiLevelSettings);

                if (string.IsNullOrEmpty(ConfigDefaults.NdkDirectory) || !Directory.Exists(ConfigDefaults.NdkDirectory))
                {
                    _errorManager.Add(TaskErrorCategory.Warning, "Customized NDK for Visual Studio (bbndk_vs) was not found. It won't be possible to automatically download new versions of BlackBerry NativeCore SDKs, simulators nor runtime-libraries for debugging. Visit " + ConfigDefaults.GithubProjectWikiInstallation + " [double-click] for details, how to install it.", OpenInstallationPage);
                }
            }

            // verify targets defined:
            if (TargetDevices.Length == 0)
            {
                _errorManager.Add(TaskErrorCategory.Warning, "No target device or simulator is defined. Specify one on \"BlackBerry -> Options -> Targets\" [double-click] to be able to deploy and test your applications.", OpenTargetsSettings);
            }

            // bring errors to front:
            if (_errorManager.Count > 0)
            {
                _errorManager.Show();
            }
        }

        /// <summary>
        /// Checks if given path matches the makefile assumptions used to build the BlackBerry projects.
        /// </summary>
        private static bool IsValidProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.IndexOf(' ') < 0;
        }

        /// <summary>
        /// Checks if given project name matches the makefile assumptions used to build the BlackBerry projects.
        /// </summary>
        private static bool IsValidProjectName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.IndexOf(' ') < 0;
        }

        private void OnProjectRemoved(Project project)
        {
            if (_solutionOpened)
            {
                if (IsBlackBerryProject(project))
                {
                    _openedBlackBerryProjects--;
                    UpdateIntelliSenseState();
                }

                VerifyCommonErrors();
            }
        }

        private void OnProjectAdded(Project project)
        {
            if (_solutionOpened)
            {
                if (IsBlackBerryProject(project))
                {
                    _openedBlackBerryProjects++;
                    UpdateIntelliSenseState();
                }

                VerifyCommonErrors();
            }
        }

        private void OnSolutionClosed()
        {
            _errorManager.Clear();

            _solutionOpened = false;
            _openedBlackBerryProjects = 0;
            UpdateIntelliSenseState();
            VerifyCommonErrors();
        }

        private void OnSolutionOpened()
        {
            _solutionOpened = true;
            _openedBlackBerryProjects = 0;

            foreach (Project project in _dte.Solution.Projects)
            {
                if (IsBlackBerryProject(project))
                {
                    _openedBlackBerryProjects++;
                }
            }

            UpdateIntelliSenseState();
            VerifyCommonErrors();
        }

        private static string ToShortVersion(Version v)
        {
            return string.Concat("v", v.Major, ".", v.Minor, ".", v.Build);
        }

        private void OpenInstallationPage(object sender, EventArgs e)
        {
            var url = ConfigDefaults.GithubProjectWikiInstallation;

            if (Navigate != null)
            {
                Navigate(url);
            }
            else
            {
                DialogHelper.StartURL(url);
            }
        }

        private void RequestSettingsOpened(Type optionPageType)
        {
            if (OpenSettingsPage != null)
            {
                OpenSettingsPage(optionPageType);
            }
        }

        private void OpenGeneralSettings(object sender, EventArgs e)
        {
            RequestSettingsOpened(typeof(GeneralOptionPage));
        }

        public void OpenTargetsSettings(object sender, EventArgs e)
        {
            RequestSettingsOpened(typeof(TargetsOptionPage));
        }

        public void OpenApiLevelSettings(object sender, EventArgs e)
        {
            RequestSettingsOpened(typeof(ApiLevelOptionPage));
        }

        private void UpdateIntelliSenseState()
        {
            UpdateIntelliSenseState(_openedBlackBerryProjects != 0);
        }

        /// <summary>
        /// Enables or disables error reporting detected by IntelliSense at runtime.
        /// Unfortunately, no idea for now, how to extend the IntelliSense with extra Qt syntax.
        /// </summary>
        private void UpdateIntelliSenseState(bool state)
        {
            var intelliPropertyGroup = _dte.Properties["TextEditor", "C/C++ Specific"];
            if (intelliPropertyGroup != null)
            {
                var intelliProperty = intelliPropertyGroup.Item("DisableErrorReporting");
                if (intelliProperty != null)
                {
                    intelliProperty.Value = state;
                }
            }
        }

        #endregion

        #region Managing Configurations

        /// <summary> 
        /// Solution Platform command is shown in the Standard toolbar by default with Visual C++ settings. Add the 
        /// command if not in the Standard toolbar. 
        /// </summary>
        private void ShowSolutionPlatformSelector()
        {
            CommandBars commandBars = (CommandBars)_dte.CommandBars;
            CommandBar standardCommandBar = commandBars[ToolbarNameStandard];

            int pos = 0;
            foreach (CommandBarControl cmd in standardCommandBar.Controls)
            {
                if (cmd.Caption == SolutionConfigurationsName)
                    pos = cmd.Index;
                if (cmd.Caption == SolutionPlatformsName)
                    return;
            }

            Command sp = null;
            string expectedGuid = VSConstants.VSStd2K.ToString("B");
            const int expectedID = (int) VSConstants.VSStd2KCmdID.SolutionPlatform;

            foreach (Command command in _dte.Commands)
            {
                if (string.Compare(command.Guid, expectedGuid, StringComparison.OrdinalIgnoreCase) == 0
                        && command.ID == expectedID)
                {
                    sp = command;
                    break;
                }
            }
            if (sp != null)
                sp.AddControl(standardCommandBar, pos + 1);
        }

        /// <summary>
        /// Adds BlackBerry specific target platforms.
        /// </summary>
        public bool AddPlatforms(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            try
            {
                AddBarDescriptorToProject(project);
                project.ConfigurationManager.AddPlatform(ConfigNameBlackBerry, ConfigNameBlackBerry, true);
                EnableDeploymentForSolutionPlatforms();

                project.ConfigurationManager.DeletePlatform("Win32");
                project.ConfigurationManager.DeletePlatform("x64");
                project.ConfigurationManager.DeletePlatform("ARM");
                project.ConfigurationManager.DeleteConfigurationRow("Win32");
                project.ConfigurationManager.DeleteConfigurationRow("x64");
                project.ConfigurationManager.DeleteConfigurationRow("ARM");
                return true;
            }
            catch (Exception e)
            {
                TraceLog.WriteException(e);
                return false;
            }
            finally
            {
                // update IntelliSense state:
                OnSolutionOpened();
            }
        }

        /// <summary>
        /// Enable deployment feature for all BlackBerry projects.
        /// </summary>
        private void EnableDeploymentForSolutionPlatforms()
        {
            SolutionConfigurations solutionConfigurations = _dte.Solution.SolutionBuild.SolutionConfigurations;
            foreach (SolutionConfiguration configuration in solutionConfigurations)
            {
                foreach (SolutionContext context in configuration.SolutionContexts)
                {
                    if (context.PlatformName == ConfigNameBlackBerry)
                    {
                        context.ShouldDeploy = true;
                    }
                }
            }
        }

        private void AddBarDescriptorToProject(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            ProjectItem existingBarItem = project.ProjectItems.Item(BarDescriptorFileName);
            if (existingBarItem == null)
            {
                TraceLog.WriteLine("Adding bar descriptor file to the project");

                string projectFolder = Path.GetDirectoryName(project.FullName);

                // Remove directory used in previous versions of this plug-in.
                if (!string.IsNullOrEmpty(project.FullName))
                {
                    if (!string.IsNullOrEmpty(projectFolder))
                    {
                        string oldFolder = Path.Combine(projectFolder, project.Name + "_barDescriptor");
                        if (Directory.Exists(oldFolder))
                        {
                            try
                            {
                                Directory.Delete(oldFolder, true);
                            }
                            catch (Exception ex)
                            {
                                TraceLog.WriteException(ex);
                            }
                        }
                    }
                }

                string templatePath = Path.Combine(PuppetMasterWizardEngine.WizardDataFolder, BarDescriptorTemplate);
                string destination = string.IsNullOrEmpty(projectFolder) ? BarDescriptorFileName : Path.Combine(projectFolder, BarDescriptorFileName);

                var tokenProcessor = PuppetMasterWizardEngine.CreateTokenProcessor(project.Name, projectFolder, destination, null);
                tokenProcessor.UntokenFile(templatePath, destination);
                project.ProjectItems.AddFromFile(destination);
            }
        }

        #endregion

        #region Build Event Handlers

        /// <summary> 
        /// New Platform After Execution Event Handler. 
        /// </summary>
        /// <param name="guid">Command GUID. </param>
        /// <param name="id">Command ID. </param>
        /// <param name="customIn">Custom IN Object. </param>
        /// <param name="customOut">Custom OUT Object. </param>
        private void OnNewPlatform_AfterExecute(string guid, int id, object customIn, object customOut)
        {
            EnableDeploymentForSolutionPlatforms();
        }

        /// <summary> 
        /// New Function Breakpoint After Execution Event Handler. 
        /// </summary>
        /// <param name="guid">Command GUID. </param>
        /// <param name="id">Command ID. </param>
        /// <param name="customIn">Custom IN Object. </param>
        /// <param name="customOut">Custom OUT Object. </param>
        private void OnNewFunctionBreakpoint_AfterExecute(string guid, int id, object customIn, object customOut)
        {
            Breakpoint functionBP = _dte.Debugger.Breakpoints.Item(_dte.Debugger.Breakpoints.Count);

            if (functionBP != null)
            {
                if ((functionBP.FunctionColumnOffset != 1) || (functionBP.FunctionLineOffset != 1))
                {
                    MessageBoxHelper.Show("The breakpoint cannot be set. Function breakpoints are only supported on the first line.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    functionBP.Delete();
                }
            }
        }

        /// <summary> 
        /// This event is fired only when user wants to build, rebuild or clean the project. 
        /// </summary>
        /// <param name="scope"> Represents the scope of the build. </param>
        /// <param name="action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (IsBlackBerrySolution() && ActiveNDK == null)
            {
                if (InstalledNDKs.Length > 0)
                {
                    var form = new MissingNdkInstalledForm();
                    form.ShowDialog();
                }
                else
                {
                    MessageBoxHelper.Show("Project build is impossible as any BlackBerry NativeCore SDK was detected. Install or add existing one in \"BlackBerry -> Options -> API-Levels\" and try again.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

            PrepareFlagFilesForDeployment();
        }


        /// <summary> 
        /// New Start Debug Command Events Before Execution Event Handler. Call the method responsible for building the app. 
        /// </summary>
        /// <param name="guid"> Command GUID. </param>
        /// <param name="id"> Command ID. </param>
        /// <param name="customIn"> Custom IN Object. </param>
        /// <param name="customOut"> Custom OUT Object. </param>
        /// <param name="cancelDefault"> Cancel the default execution of the command. </param>
        private void StartNoDebugCommandEvents_BeforeExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault)
        {
            TraceLog.WriteLine("BUILD: Start no Debug");
            _startDebugger = false;
            cancelDefault = StartBuild();
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
            TraceLog.WriteLine("BUILD: Start Debug");
            _startDebugger = true;
            cancelDefault = StartBuild();
        }

        /// <summary> 
        /// This event is fired only when the build/rebuild/clean process ends. 
        /// </summary>
        /// <param name="scope"> Represents the scope of the build. </param>
        /// <param name="action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            switch (action)
            {
                case vsBuildAction.vsBuildActionBuild:
                    _amountOfProjects--;
                    if (_amountOfProjects == 0)
                    {
                        _buildEvents.OnBuildDone -= OnBuildDone;
                        Built();
                    }
                    break;

                case vsBuildAction.vsBuildActionDeploy:
                    _buildEvents.OnBuildDone -= OnBuildDone;
                    _isDeploying = false;
                    Deployed();
                    break;
            }
        }

        private bool StartBuild()
        {
            if (DebugEngineStatus.IsRunning)
            {
                TraceLog.WriteLine("BUILD: StartBuild - Debugger running");

                // Disable the override of F5 (this allows the debugged process to continue execution)
                return false;
            }

            if (!IsBlackBerryConfigurationActive())
            {
                TraceLog.WriteLine("BUILD: StartBuild - not a BlackBerry project");

                // Disable the override of F5 (this allows the debugged process to continue execution)
                return false;
            }

            try
            {
                _buildThese = new List<String>();
                _startProject = GetBuildStartupProject(_dte);

                foreach (String startupProject in (Array) _dte.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (Project project in _dte.Solution.Projects)
                    {
                        if (project.UniqueName == startupProject)
                        {
                            _buildThese.Add(project.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex);
            }

            ActivateOutputWindowPane("Build");
            BuildBar();
            return true;
        }

        private void ActivateOutputWindowPane(string build)
        {
            try
            {
                // create a reference to the Output Window and window pane:
                OutputWindow ow = _dte.ToolWindows.OutputWindow;

                // and activate it:
                var outputWindowPane = ow.OutputWindowPanes.Item(build);
                outputWindowPane.Activate();
            }
            catch
            {
            }
        }

        /// <summary> 
        /// Identify the projects to be build and start the build process.
        /// </summary>
        /// <returns> TRUE if successful, FALSE if not. </returns>
        private bool BuildBar()
        {
            TraceLog.WriteLine("BUILD: BuildBar");

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
                        {
                            solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, projectName, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex);
                        success = false;
                    }
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex);
                success = false;
            }

            return success;
        }

        private static void DeleteFlagFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to delete flag file (\"{0}\")", fileName);
            }
        }

        /// <summary> 
        /// Verify if the build process was successful. If so, start deploying the app.
        /// </summary>
        private void Built()
        {
            TraceLog.WriteLine("BUILD: Built");

            // when build succeeded:
            if (_dte.Solution.SolutionBuild.LastBuildInfo == 0)
            {
                foreach (string startupProject in (Array) _dte.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (SolutionContext sc in _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts)
                    {
                        var project = _dte.Solution.Item(sc.ProjectName);
                        var debugNativeFlagFileName = ProjectHelper.GetFlagFileNameForDebugNative(project);

                        DeleteFlagFile(debugNativeFlagFileName);

                        if (sc.ProjectName == startupProject)
                        {
                            sc.ShouldDeploy = true;

                            if (_startDebugger)
                            {
                                // write file to flag the deploy task that it should use the -debugNative option:
                                File.WriteAllText(debugNativeFlagFileName, "Use -debugNative.\r\n");
                                _buildEvents.OnBuildDone += OnBuildDone;
                            }
                        }
                        else
                        {
                            sc.ShouldDeploy = false;
                        }
                    }
                }

                // OK, project builds, so make sure there is a connection to the device, to speed-up further debugging:
                var target = ActiveTarget;
                if (target != null)
                {
                    Targets.Connect(ActiveTarget, ConfigDefaults.SshPublicKeyPath, ConfigDefaults.SshPrivateKeyPath, null);
                }

                _isDeploying = true;
                _dte.Solution.SolutionBuild.Deploy(true);
            }

            OnWholeBuildDone();
        }

        private void OnWholeBuildDone()
        {
            // remove all temporary flag files created during build:
            foreach (var fileName in _filesToDelete)
            {
                DeleteFlagFile(fileName);
            }

            _filesToDelete.Clear();
            if (_buildThese != null)
            {
                _buildThese.Clear();
            }
            _startProject = null;
            _startDebugger = false;
            _isDeploying = false;
            _hitPlay = false;
        }

        /// <summary>
        /// Writes down all flag files used by MSBuild.
        /// NOTE: somehow passing values via environment variables seems not to work,
        /// that's why we use local files deleted automatically after the build/deployment is completed.
        /// </summary>
        private void PrepareFlagFilesForDeployment()
        {
            var developer = PackageViewModel.Instance.Developer;
            bool shouldSaveLocally = developer != null && !developer.IsPasswordSaved && !string.IsNullOrEmpty(developer.CskPassword);

            // store CSK-password inside a local flag-file, in case dev doesn't want to store it persistently:
            foreach (Project project in _dte.Solution.Projects)
            {
                try
                {
                    var cskPasswordFileName = ProjectHelper.GetFlagFileNameForCSKPassword(project);

                    // remove old value:
                    DeleteFlagFile(cskPasswordFileName);
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to delete deployment temporary flag files (project: \"{0}\")", project != null ? project.Name : "unknown");
                }
            }

            if (shouldSaveLocally)
            {
                foreach (Project project in (Array) _dte.Solution.SolutionBuild.StartupProjects)
                {
                    WriteCskPassword(project, developer);
                }

                foreach (Project project in (Array) _dte.ActiveSolutionProjects)
                {
                    WriteCskPassword(project, developer);
                }
            }
        }

        private void WriteCskPassword(Project project, DeveloperDefinition developer)
        {
            try
            {
                var cskPasswordFileName = ProjectHelper.GetFlagFileNameForCSKPassword(project);

                // write CSK-password, if not stored persistently inside registry, to let signing process to succeed:
                File.WriteAllText(cskPasswordFileName, GlobalHelper.Encrypt(developer.CskPassword));
                _filesToDelete.Add(cskPasswordFileName);
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to write deployment temporary flag files (project: \"{0}\")", project != null ? project.Name : "unknown");
            }
        }

        /// <summary> 
        /// Get the process ID and launch an executable using the BlackBerry debug engine. 
        /// </summary>
        private void Deployed()
        {
            TraceLog.WriteLine("BUILD: Deployed");

            if (_startProject == null)
            {
                MessageBoxHelper.Show("Unable to determine the executable to start.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var executablePath = ProjectHelper.GetTargetFullName(_startProject);
            var ndk = ActiveNDK;
            var device = ActiveTarget;
            var runtime = ActiveRuntime;

            if (ndk == null)
            {
                MessageBoxHelper.Show("Missing NDK selected. Install any and mark as active using BlackBerry menu options.", null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (device == null)
            {
                MessageBoxHelper.Show("Missing target device selected. Define an IP, password and mark a device as active using BlackBerry menu options.", null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // if the path to binary is invalid GDB might have problems with loading correct symbols;
            // maybe try to guess better or ask developer, what is wrong, why the project doesn't define it correctly
            // (possible causes: dev is using any kind of makefile and plugin can't detect the outcomes automatically):
            if (_startDebugger)
            {
                if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                {
                    executablePath = ProjectHelper.GuessTargetFullName(_startProject);

                    if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                    {
                        var attachDiscoveryService = (IAttachDiscoveryService) Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(IAttachDiscoveryService));
                        Debug.Assert(attachDiscoveryService != null, "Invalid project references (make sure VisualStudio.Shell.dll is not references, as it duplicates the Package definition from VisualStudio.Shell.<version>.dll)");

                        // ask the developer to specify the path:
                        executablePath = attachDiscoveryService.FindExecutable(null);
                    }
                }

                if (LaunchDebugTarget(_startProject.Name, ndk, device, runtime, null, executablePath))
                {
                    ActivateOutputWindowPane("Debug");
                }
            }
        }

        /// <summary> 
        /// Launch an executable using the BlackBerry debug engine. 
        /// </summary>
        /// <param name="pidOrTargetAppName">Process ID in string format or the binary name for debugger to attach to.</param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool LaunchDebugTarget(string pidOrTargetAppName, NdkInfo ndk, DeviceDefinition target, RuntimeInfo runtime, string sshPublicKeyPath, string executablePath)
        {
            TraceLog.WriteLine("BUILD: Starting debugger (\"{0}\", \"{1}\")", pidOrTargetAppName, executablePath);

            IVsDebugger dbg = (IVsDebugger) _serviceProvider.GetService(typeof(SVsShellDebugger));
            IVsUIShell shell = (IVsUIShell) _serviceProvider.GetService(typeof(SVsUIShell));

            VsDebugTargetInfo info = new VsDebugTargetInfo();
            info.cbSize = (uint)Marshal.SizeOf(info);
            info.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
            info.bstrExe = executablePath; // The executable path

            // Store all debugger arguments in a string
            var nvc = new Dictionary<string, string>();
            nvc["pidOrName"] = pidOrTargetAppName;
            if (!string.IsNullOrEmpty(sshPublicKeyPath))
            {
                nvc["sshKeyPath"] = sshPublicKeyPath;
            }
            CollectionHelper.AppendDevice(nvc, target);
            CollectionHelper.AppendNDK(nvc, ndk.ToDefinition());
            if (runtime != null)
            {
                CollectionHelper.AppendRuntime(nvc, runtime.ToDefinition());
            }
            info.bstrArg = CollectionHelper.Serialize(nvc);

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
                    string message;
                    shell.GetErrorInfo(out message);
                    message = message.Trim();

                    TraceLog.WriteLine("LaunchDebugTargets: " + message);
                    MessageBoxHelper.Show(message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pInfo);
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Check to see if current solution is configured with a BlackBerry configurations.
        /// </summary>
        public bool IsBlackBerrySolution()
        {
            if (_dte.Solution != null)
            {
                var projects = _dte.Solution.Projects;
                if (projects != null)
                {
                    foreach (Project project in projects)
                    {
                        if (IsBlackBerryProject(project))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if specified project supports build for BlackBerry device or simulator.
        /// </summary>
        public static bool IsBlackBerryProject(Project project)
        {
            if (project == null || !(project.Object is VCProject))
                return false;

            try
            {
                var platformName = project.ConfigurationManager != null && project.ConfigurationManager.ActiveConfiguration != null
                    ? project.ConfigurationManager.ActiveConfiguration.PlatformName
                    : null;
                if (platformName == ConfigNameBlackBerry)
                    return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to determine type of native project");
            }

            return false;
        }

        public bool IsBlackBerryConfigurationActive()
        {
            if (_dte.Solution != null && _dte.Solution.SolutionBuild != null && _dte.Solution.SolutionBuild.ActiveConfiguration != null)
            {
                foreach (SolutionContext context in _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts)
                {
                    string platformName = context.PlatformName;
                    if (platformName == ConfigNameBlackBerry)
                        return true;
                }
            }

            return false;
        }
    }
}
