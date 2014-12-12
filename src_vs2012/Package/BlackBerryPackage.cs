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
using BlackBerry.DebugEngine;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Services;
using BlackBerry.Package.Components;
using BlackBerry.Package.Diagnostics;
using BlackBerry.Package.Dialogs;
using BlackBerry.Package.Editors;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;
using BlackBerry.Package.Options;
using BlackBerry.Package.Registration;
using BlackBerry.Package.ToolWindows;
using BlackBerry.Package.ViewModels;
using BlackBerry.Package.Wizards;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.IO;
using EnvDTE;
using System.Windows.Forms;
using EnvDTE80;

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
    [XmlEditorDesignerViewRegistration("BarDescriptor", BarDescriptorEditorFactory.DefaultExtension, LogicalViewID.Designer, 0x60, DesignerLogicalViewEditor = typeof(BarDescriptorEditorFactory),
        Namespace = "http://www.qnx.com/schemas/application/1.0", MatchExtensionAndNamespace = true)]
    // And which type of files we want to handle and notify about file changes, while in Microsoft Visual C++ Projects
    [ProvideEditorExtension(typeof(BarDescriptorEditorFactory), BarDescriptorEditorFactory.DefaultExtension, 0x10, NameResourceID = 106, EditorFactoryNotify = true, ProjectGuid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}")]
    // We register that our editor supports LOGVIEWID_Designer logical view
    [ProvideEditorLogicalView(typeof(BarDescriptorEditorFactory), LogicalViewID.Designer)]

    // Registration of custom debugger
    [DebugEngineRegistration(AD7PortSupplier.PublicName, AD7Engine.DebugEngineGuid,
        Attach = true, AddressBreakpoints = false, CallstackBreakpoints = true, AlwaysLoadLocal = true, AutoSelectPriority = 4,
        DebugEngineClassGUID = AD7Engine.ClassGuid, DebugEngineClassName = AD7Engine.ClassName,
        ProgramProviderClassGUID = AD7ProgramProvider.ClassGuid, ProgramProviderClassName = AD7ProgramProvider.ClassName,
        PortSupplierClassGUID = AD7PortSupplier.ClassGuid, PortSupplierClassName = AD7PortSupplier.ClassName,
#if DEBUG
        AssemblyName = ConfigDefaults.DebugEngineDebugablePath
#else
        AssemblyName = "BlackBerry.DebugEngine.dll"
#endif
)]

    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", ConfigDefaults.VersionString, IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]

    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidVSNDK_PackageString)]

    // This attribute adds top level node at [Add Project Item] dialog for Visual C++ projects only and showing
    // new item wizards from '<Package>/Templates/ProjectItems' folder. This is an easy way to inject new templates
    // without any need of copying them into Visual Studio folder itself.
    // No extra permissions for the plugin are required in that case.
    [ProvideProjectItem("{8bc9ceb8-8b4a-11d0-8d11-00a0c91bc942}", "BlackBerry", @"Templates\ProjectItems\BlackBerry", 10)]
#if PLATFORM_VS2013
    // PH: somehow for VS2013 it uses a different value for VC++ projects... don't ask, how I discovered it
    [ProvideProjectItem("{8bc9ceb8-8b4a-11d0-8d11-00a0c91bc943}", "BlackBerry", @"Templates\ProjectItems\BlackBerry", 10)]
#endif
    [ProvideProjectItem("{a2fe74e1-b743-11d0-ae1a-00a0c90fffc3}", "BlackBerry", @"Templates\ProjectItems\BlackBerry", 11)]
    [ProvideProjects(GuidList.guidVSNDK_PackageString, "BlackBerry Projects", @"Templates\Projects\BlackBerry", 31)]
    // This attribute registers a custom wizard engine, that is used to populate new projects and items into a project.
    // Reference to this engine is made directly from *.vsz file (from locations provided by both attributes above).
    [ProvideWizardEngine(typeof(PuppetMasterWizardEngine))]

    // This attribute defines set of settings pages provided by this package.
    // They are automatically instantiated and displayed inside [Visual Studio -> Tools -> Options...] dialog.
    [ProvideOptionPage(typeof(GeneralOptionPage), OptionsCategoryName, "General", 1001, 1002, true)]
    [ProvideOptionPage(typeof(LogsOptionPage), OptionsCategoryName, "Logs", 1001, 1003, true)]
    [ProvideOptionPage(typeof(ApiLevelOptionPage), OptionsCategoryName, "API Levels", 1001, 1004, true)]
    [ProvideOptionPage(typeof(TargetsOptionPage), OptionsCategoryName, "Targets", 1001, 1005, true)]
    [ProvideOptionPage(typeof(SigningOptionPage), OptionsCategoryName, "Signing", 1001, 1006, true)]
    [ProvideOptionPage(typeof(BackupOptionPage), OptionsCategoryName, "Backup", 1001, 1007, true)]

    // This attribute registers public services exposed by this package.
    // The package itself will be automatically loaded if needed.
    [ProvideService(typeof(IDeviceDiscoveryService), ServiceName = "BlackBerry Device Discovery")]
    [ProvideService(typeof(IAttachDiscoveryService), ServiceName = "BlackBerry Process-Attach Executable Discovery")]

    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(TargetNavigatorPane), Style = VsDockStyle.Tabbed, MultiInstances = false)]
    public sealed class BlackBerryPackage : Microsoft.VisualStudio.Shell.Package, IDisposable, IDeviceDiscoveryService, IAttachDiscoveryService
    {
        public const string OptionsCategoryName = "BlackBerry";

        private BlackBerryPaneTraceListener _mainTraceWindow;
        private BlackBerryPaneTraceListener _gdbTraceWindow;
        private BlackBerryPaneTraceListener _qconnTraceWindow;
        private BlackBerryPaneTraceListener _standardOutputWindow;
        private DTE2 _dte;
        private BuildPlatformsManager _buildPlatformsManager;

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

        ~BlackBerryPackage()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            _dte = (DTE2) GetService(typeof(SDTE));

            // create dedicated persistent logs
            var logOptions = (LogsOptionPage)GetDialogPage(typeof(LogsOptionPage));
            LogManager.Initialize(logOptions.Path, logOptions.LimitLogs ? logOptions.LimitCount : -1, TraceLog.Category, TraceLog.CategoryGDB);
            Targets.TraceOptions(logOptions.DebuggedOnly, logOptions.LogsInterval, logOptions.SLog2Level, logOptions.GetSLog2BufferSets(), logOptions.InjectLogs, TraceLog.CategoryDevice);

            // create dedicated trace-logs output window pane (available in combo-box at regular Visual Studio Output Window):
            var outputWindowService = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            _mainTraceWindow = new BlackBerryPaneTraceListener("BlackBerry", TraceLog.Category, true, outputWindowService, GuidList.GUID_TraceMainOutputWindowPane);
#if DEBUG
            _gdbTraceWindow = new BlackBerryPaneTraceListener("BlackBerry - GDB", TraceLog.CategoryGDB, true, outputWindowService, GuidList.GUID_TraceGdbOutputWindowPane);
#endif
            _qconnTraceWindow = new BlackBerryPaneTraceListener("BlackBerry - QConn", QTraceLog.Category, true, outputWindowService, GuidList.GUID_TraceQConnOutputWindowPane);
            _standardOutputWindow = new BlackBerryPaneTraceListener("Debug", TraceLog.CategoryDevice, false, outputWindowService, VSConstants.GUID_OutWindowDebugPane);
            _mainTraceWindow.Activate();

            // and set it to monitor all logs (they have to be marked with 'BlackBerry' category! aka TraceLog.Category):
            TraceLog.Add(_mainTraceWindow);
            TraceLog.Add(_gdbTraceWindow);
            TraceLog.Add(_qconnTraceWindow);
            TraceLog.Add(_standardOutputWindow);
            TraceLog.WriteLine("BlackBerry plugin started");

            // add this package to the globally-proffed services:
            IServiceContainer serviceContainer = this;
            serviceContainer.AddService(typeof(IDeviceDiscoveryService), this, true);
            serviceContainer.AddService(typeof(IAttachDiscoveryService), this, true);

            TraceLog.WriteLine(" * registered services");

            TraceLog.WriteLine(" * loaded NDK descriptions");

            // inform Targets, that list of target-devices has been changed and maybe it should force some disconnections:
            PackageViewModel.Instance.TargetsChanged += (s, e) => Targets.DisconnectIfOutside(e.TargetDevices);

            // Create Editor Factory. Note that the base Package class will call Dispose on it.
            RegisterEditorFactory(new BarDescriptorEditorFactory());
            TraceLog.WriteLine(" * registered editors");

            _buildPlatformsManager = new BuildPlatformsManager(_dte);
            _buildPlatformsManager.Navigate += OpenUrl;
            _buildPlatformsManager.OpenSettingsPage += ShowOptionPage;
            _buildPlatformsManager.Initialize();
            TraceLog.WriteLine(" * registered build-platforms manager");

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
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

                // Create dynamic command for the 'runtime-libraries' menu
                CommandID runtimeCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryTargetsRuntimeLibrariesPlaceholder);
                DynamicMenuCommand runtimeMenu = new DynamicMenuCommand(() => PackageViewModel.Instance.InstalledRuntimes,
                                                                         (cmd, collection, index) =>
                                                                             {
                                                                                 var item = index >= 0 && index < collection.Count ? ((RuntimeInfo[]) collection)[index] : null;
                                                                                 PackageViewModel.Instance.ActiveRuntime = item;
                                                                             },
                                                                         (cmd, collection, index) =>
                                                                             {
                                                                                 var item = index >= 0 && index < collection.Count ? ((RuntimeInfo[])collection)[index] : null;
                                                                                 cmd.Checked = item != null && item == PackageViewModel.Instance.ActiveRuntime;
                                                                                 cmd.Visible = item != null;
                                                                                 cmd.Text = item != null ? item.Name : "-";
                                                                             },
                                                                         runtimeCommandID);
                mcs.AddCommand(runtimeMenu);

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

                // Create the command for 'Add BlackBerry Platforms' menu item
                CommandID platformsCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryCommonAddPlatformTargets);
                OleMenuCommand platformsItem = new OleMenuCommand(AddBlackBerryTargetPlatforms, platformsCommandID);
                mcs.AddCommand(platformsItem);

                // Create the command for 'Import BlackBerry Native Project' menu item
                CommandID projectCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryCommonProjectImport);
                OleMenuCommand projectItem = new OleMenuCommand(ImportBlackBerryProject, projectCommandID);
                mcs.AddCommand(projectItem);

                // Create the command for 'Target Navigator' menu item
                CommandID targetNavigatorCommandID = new CommandID(GuidList.guidVSNDK_PackageCmdSet, PackageCommands.cmdidBlackBerryToolWindowsTargetNavigator);
                OleMenuCommand targetNavigatorMenu = new OleMenuCommand(ShowTargetNavigator, targetNavigatorCommandID);
                mcs.AddCommand(targetNavigatorMenu);

                TraceLog.WriteLine(" * initialized menus");
            }

            TraceLog.WriteLine("-------------------- DONE ({0})", ConfigDefaults.VersionString);

            // make sure there is an NDK selected and developer knows about it:
            EnsureActiveNDK();
            if (_buildPlatformsManager.IsBlackBerrySolution() && ActiveNDK == null && InstalledNDKs.Length > 0)
            {
                var form = new MissingNdkInstalledForm();
                form.ShowDialog();
                EnsureActiveNDK();
            }

            _buildPlatformsManager.VerifyCommonErrors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_mainTraceWindow != null)
                {
                    _mainTraceWindow.Dispose();
                    _mainTraceWindow = null;
                }
                if (_gdbTraceWindow != null)
                {
                    _gdbTraceWindow.Dispose();
                    _gdbTraceWindow = null;
                }
                if (_qconnTraceWindow != null)
                {
                    _qconnTraceWindow.Dispose();
                    _qconnTraceWindow = null;
                }

                if (_buildPlatformsManager != null)
                {
                    _buildPlatformsManager.Close();
                    _buildPlatformsManager = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override int QueryClose(out bool canClose)
        {
            var updateManager = PackageViewModel.Instance.UpdateManager;

            if (updateManager.IsRunning)
            {
                canClose = false;

                if (MessageBoxHelper.Show("Do you want to abort it and exit?",
                    "There are still some jobs running by BlackBerry Native Plugin (i.e. new NDK, simulator or runtime downloads).",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    canClose = true;
                    updateManager.CancelAll();
                }
            }
            else
            {
                canClose = true;
            }

            return VSConstants.S_OK;
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

        /// <summary>
        /// Gets the currently selected NDK to build against.
        /// </summary>
        private NdkInfo ActiveNDK
        {
            get { return PackageViewModel.Instance.ActiveNDK; }
        }

        /// <summary>
        /// Gets the list of installed NDKs on the machine.
        /// </summary>
        private NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
        }

        /// <summary>
        /// Makes sure the NDK and runtime libraries are selected
        /// and their paths are stored inside registry for build toolset.
        /// </summary>
        private void EnsureActiveNDK()
        {
            PackageViewModel.Instance.EnsureActiveNDK();
            PackageViewModel.Instance.EnsureActiveRuntime();
        }

        private void AddBlackBerryTargetPlatforms(object sender, EventArgs e)
        {
            var projects = DteHelper.GetProjects(_dte);

            if (projects == null || projects.Length == 0)
            {
                MessageBoxHelper.Show("Unable to add BlackBerry platform.", "Please open a solution with Visual C++ projects", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBoxHelper.Show("Do you want to add BlackBerry target platform for all C/C++ projects?", null, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            foreach (var project in projects)
            {
                _buildPlatformsManager.AddPlatforms(project);
            }

            MessageBoxHelper.Show("You might now:\r\n * restart Visual Studio, as it has the 'deploy' option disabled\r\n * update the Author Information within the bar-descriptor.xml", "BlackBerry target have been added to solution configurations.", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ImportBlackBerryProject(object sender, EventArgs e)
        {
            var form = new ImportProjectForm();

            // ask for .cproject location:
            if (form.UpdateSourceProject())
            {
                // configure dialog to display info about current solution:
                form.AddTargetProjects(_dte.Solution);

                // commit the list of found files:
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // create new project/use existing and add files:
                    var project = form.FillProject(_dte);
                    if (project == null)
                    {
                        MessageBoxHelper.Show("Unable to create or access requested project, please try again with saved solution", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    MessageBoxHelper.Show(form.CopyFiles ? "All files has been copied" : null, "Project has been imported into: " + project.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ShowTargetNavigator(object sender, EventArgs e)
        {
            ShowToolWindow<TargetNavigatorPane>();
        }

        /// <summary>
        /// Shows the singleton tool window or creates new one.
        /// </summary>
        private T ShowToolWindow<T>() where T : ToolWindowPane
        {
            var toolWindow = (T) FindToolWindow(typeof(T), 0, true);

            if (toolWindow == null || toolWindow.Frame == null)
                throw new InvalidOperationException("Unable to create tool window of specified type");

            // force the frame to be shown as MDI content window:
            var frame = (IVsWindowFrame) toolWindow.Frame;
            frame.SetProperty((int) __VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_MdiChild);
            ErrorHandler.ThrowOnFailure(frame.Show());

            return toolWindow;
        }

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion

        #region IDeviceDiscoveryService Implementation

        DeviceDefinition IDeviceDiscoveryService.FindDevice()
        {
            var dialog = new DeviceForm("Searching for BlackBerry device");
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.ToDevice();
            }

            return null;
        }

        #endregion

        #region IAttachDiscoveryService Implementation

        private string FindMatchingProject(ProcessInfo process)
        {
            if (process != null)
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    if (BuildPlatformsManager.IsBlackBerryProject(project))
                    {
                        string outputPath;
                        if (IsMatchingProject(project, process, out outputPath))
                        {
                            TraceLog.WarnLine("Suggesting executable path: \"{0}\"", outputPath);
                            return outputPath;
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsMatchingProject(Project project, ProcessInfo process, out string outputPath)
        {
            outputPath = ProjectHelper.GuessTargetFullName(project);
            if (string.IsNullOrEmpty(outputPath) || !File.Exists(outputPath))
            {
                outputPath = ProjectHelper.GetTargetFullName(project);
            }

            if (!string.IsNullOrEmpty(outputPath))
            {
                var name = Path.GetFileName(outputPath);
                if (process != null && string.Compare(name, process.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        string IAttachDiscoveryService.FindExecutable(ProcessInfo process)
        {
            // try to automatically select the project:
            foreach (Project project in _dte.Solution.Projects)
            {
                if (BuildPlatformsManager.IsBlackBerryProject(project))
                {
                    string outputPath;
                    if (IsMatchingProject(project, process, out outputPath))
                    {
                        if (File.Exists(outputPath))
                        {
                            TraceLog.WarnLine("Suggesting executable path: \"{0}\"", outputPath);
                            return outputPath;
                        }
                    }
                }
            }

            // PH: HINT: this code will be for sure executed, when attaching to a process without any solution opened:

            // ask developer via any UI to point the executable (binary):
            var form = new BinaryDiscoveryForm("Select Matching Target Process Binary");

            foreach (Project project in _dte.Solution.Projects)
            {
                if (BuildPlatformsManager.IsBlackBerryProject(project))
                {
                    string outputPath;
                    if (IsMatchingProject(project, process, out outputPath))
                    {
                        form.AddTarget(project, outputPath, true);
                    }
                    else
                    {
                        form.AddTarget(project, outputPath, false);
                    }
                }
            }

            form.AddCustomTarget("Custom executable");

            if (form.ShowDialog() == DialogResult.OK)
            {
                return form.SelectedPath;
            }

            return null;
        }

        #endregion
    }
}
