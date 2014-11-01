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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
using BlackBerry.Package.ViewModels;
using BlackBerry.Package.Wizards;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class BuildPlatformsManager
    {
        private readonly DTE2 _dte;

        private List<string> _buildThese;
        private readonly List<string> _filesToDelete;
        private bool _hitPlay;
        private int _amountOfProjects;
        private bool _isDeploying;
        private Project _startProject;
        private bool _startDebugger;

        private OutputWindowPane _outputWindowPane;

        private BuildEvents _buildEvents;
        private SolutionEvents _solutionEvents;
        private CommandEvents _deploymentEvents;
        private CommandEvents _eventsDebug;
        private CommandEvents _eventsNoDebug;
        private CommandEvents _eventsDebugContext;

        private bool _solutionOpened;
        private int _openedBlackBerryProjects;

        private const string ConfigNameBlackBerry = "BlackBerry";
        private const string ToolbarNameStandard = "Standard";
        private const string SolutionConfigurationsName = "Solution Configurations";
        private const string SolutionPlatformsName = "Solution Platforms";
        private const string BarDescriptorFileName = "bar-descriptor.xml";
        private const string BarDescriptorTemplate = @"Shared\bar-descriptor.xml";

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
            _buildEvents.OnBuildDone += OnWholeBuildDone;

            // to monitor, when to disable IntelliSense errors,
            // TODO: however there is still one case not covered - when project is added as Win32 and manually platform is added and configuration converted co BlackBerry...
            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.Opened += OnSolutionOpened;
            _solutionEvents.AfterClosing += OnSolutionClosed;
            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;
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

        #endregion

        /// <summary> 
        /// Terminate the manager.
        /// </summary>
        public void Close()
        {
        }

        #region Managing IntelliSense Error Reporting

        private void OnProjectRemoved(Project project)
        {
            if (_solutionOpened)
            {
                if (IsBlackBerryProject(project))
                {
                    _openedBlackBerryProjects--;
                    UpdateIntelliSenseState();
                }
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
            }
        }

        private void OnSolutionClosed()
        {
            _solutionOpened = false;
            _openedBlackBerryProjects = 0;
            UpdateIntelliSenseState();
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
            StartBuild(out cancelDefault);
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
            StartBuild(out cancelDefault);
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

        private void StartBuild(out bool cancelDefault)
        {
            if (DebugEngineStatus.IsRunning)
            {
                TraceLog.WriteLine("BUILD: StartBuild - Debugger running");

                // Disable the override of F5 (this allows the debugged process to continue execution)
                cancelDefault = false;
                return;
            }

            if (!IsBlackBerryConfigurationActive())
            {
                TraceLog.WriteLine("BUILD: StartBuild - not a BlackBerry project");

                // Disable the override of F5 (this allows the debugged process to continue execution)
                cancelDefault = false;
                return;
            }

            try
            {
                _buildThese = new List<String>();
                _startProject = null;

                foreach (String startupProject in (Array) _dte.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (Project project in _dte.Solution.Projects)
                    {
                        if (project.UniqueName == startupProject)
                        {
                            _buildThese.Add(project.FullName);
                            _startProject = project;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex);
            }


            // Create a reference to the Output window.
            // Create a tool window reference for the Output window
            // and window pane.
            OutputWindow ow = _dte.ToolWindows.OutputWindow;

            // Select the Build pane in the Output window.
            _outputWindowPane = ow.OutputWindowPanes.Item("Build");
            _outputWindowPane.Activate();

            if (_startDebugger)
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
            }

            BuildBar();
            cancelDefault = true;
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

            _outputWindowPane.TextDocument.Selection.SelectAll();
            string outputText = _outputWindowPane.TextDocument.Selection.Text;

            if (string.IsNullOrEmpty(outputText) || Regex.IsMatch(outputText, ">Build succeeded.\r\n") || !outputText.Contains("): error :"))
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
                    Targets.Connect(ActiveTarget, ConfigDefaults.SshPublicKeyPath, null);
                }

                _isDeploying = true;
                _dte.Solution.SolutionBuild.Deploy(true);
            }
        }

        private void OnWholeBuildDone(vsBuildScope scope, vsBuildAction action)
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
            foreach (string startupProject in (Array) _dte.Solution.SolutionBuild.StartupProjects)
            {
                foreach (SolutionContext sc in _dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts)
                {
                    var project = _dte.Solution.Item(sc.ProjectName);
                    var cskPasswordFileName = ProjectHelper.GetFlagFileNameForCSKPassword(project);

                    // remove old value:
                    DeleteFlagFile(cskPasswordFileName);

                    if (sc.ProjectName == startupProject && shouldSaveLocally)
                    {
                        // write CSK-password, if not stored persistently inside registry, to let signing process to succeed:
                        File.WriteAllText(cskPasswordFileName, GlobalHelper.Encrypt(developer.CskPassword));
                        _filesToDelete.Add(cskPasswordFileName);
                    }
                }
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
                MessageBoxHelper.Show("Missing NDK selected. Please install any and mark as active using BlackBerry menu options.", null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (device == null)
            {
                MessageBoxHelper.Show("Missing target device selected. Please define an IP, password and mark a device as active using BlackBerry menu options.", null,
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

                LaunchDebugTarget(_startProject.Name, ndk, device, runtime, null, executablePath);
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

            IVsDebugger dbg;
            IVsUIShell shell;
            using (var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider) _dte))
            {
                dbg = (IVsDebugger)serviceProvider.GetService(typeof(SVsShellDebugger));
                shell = (IVsUIShell)serviceProvider.GetService(typeof(SVsUIShell));
            }

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

        public bool IsBlackBerryProject(Project project)
        {
            if (project == null)
                return false;

            var platformName = project.ConfigurationManager != null && project.ConfigurationManager.ActiveConfiguration != null ? project.ConfigurationManager.ActiveConfiguration.PlatformName : null;
            if (platformName == ConfigNameBlackBerry)
                return true;

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
