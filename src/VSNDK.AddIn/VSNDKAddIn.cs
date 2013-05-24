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
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Project;
using Microsoft.Build.Execution;
using System.Xml;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.IO;

using Extensibility;
using System.Resources;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;

using NameValueCollection = System.Collections.Specialized.NameValueCollection;
using NameValueCollectionHelper = VSNDK.AddIn.NameValueCollectionHelper;
using System.Runtime.InteropServices;

namespace VSNDK.AddIn
{

    /// <summary>
    /// 
    /// </summary>
    public class VSNDKAddIn
    {
        private VSNDKCommandEvents _commandEvents;
        private DTE2 _applicationObject; 
        private EnvDTE.AddIn _addInInstance;
        private TokenProcessor tokenProcessor;
        public BuildEvents _buildEvents;
        private OutputWindowPane _owP;
        public int amountOfProjects = 0;
        public bool hitPlay = false; // used to know if user has clicked in the play button
        public bool isDeploying = false;

        private const string BLACKBERRY = "BlackBerry";
        private const string BLACKBERRYSIMULATOR = "BlackBerrySimulator";
        private const string STANDARD_TOOL_BAR = "Standard";
        private const string SOLUTION_CONFIGURATIONS = "Solution Configurations";
        private const string SOLUTION_PLATFORMS = "Solution Platforms";
        private const string BAR_DESCRIPTOR = "bar-descriptor.xml";
        private const string BAR_DESCRIPTOR_PATH = @"\..\VCWizards\CodeWiz\BlackBerry\BarDescriptor\Templates\1033\";
        
        public static bool isDebugEngineRunning = false;

        public VSNDKAddIn()
        {
        }


        /// <summary> 
        /// Run initialization code on first connection of the AddIn. 
        /// </summary>
        /// <param name="appObj"> Application Object. </param>
        /// <param name="addin"> Add In Object. </param>
        public void Connect(DTE2 appObj, EnvDTE.AddIn addin)
        {
            /// Initialize External and Internal Variables.
            _applicationObject = appObj;
            _addInInstance = addin;
            isDebugEngineRunning = false;

            /// Register Command Events
            _commandEvents = new VSNDKCommandEvents(appObj);
            _commandEvents.RegisterCommand(GuidList.guidVSStd2KString, CommandConstants.cmdidSolutionPlatform, cmdNewPlatform_afterExec, cmdNewPlatform_beforeExec);
            _commandEvents.RegisterCommand(GuidList.guidVSStd2KString, CommandConstants.cmdidSolutionCfg, cmdNewPlatform_afterExec, cmdNewPlatform_beforeExec);
            _commandEvents.RegisterCommand(GuidList.guidVSStd97String, CommandConstants.cmdidStartDebug, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);
            _commandEvents.RegisterCommand(GuidList.guidVSStd2KString, CommandConstants.cmdidStartDebugContext, startDebugCommandEvents_AfterExecute, startDebugCommandEvents_BeforeExecute);
            _commandEvents.RegisterCommand(GuidList.guidVSStd97String, CommandConstants.cmdidStartNoDebug, startNoDebugCommandEvents_AfterExecute, startNoDebugCommandEvents_BeforeExecute);
            _commandEvents.RegisterCommand(GuidList.guidVSDebugGroup, CommandConstants.cmdidDebugBreakatFunction, cmdNewFunctionBreakpoint_afterExec, cmdNewFunctionBreakpoint_beforeExec);

            _buildEvents = _applicationObject.Events.BuildEvents;
            _buildEvents.OnBuildBegin += new _dispBuildEvents_OnBuildBeginEventHandler(this.OnBuildBegin);

            // Create a reference to the Output window.
            // Create a tool window reference for the Output window
            // and window pane.
            OutputWindow ow = _applicationObject.ToolWindows.OutputWindow;

            // Select the Build pane in the Output window.
            _owP = ow.OutputWindowPanes.Item("Build");
            _owP.Activate();

            DisableIntelliSenseErrorReport(true);
            CheckSolutionPlatformCommand();
        }


        /// <summary> 
        /// Terminate the AddIn. 
        /// </summary>
        public void Disconnect()
        {
            DisableIntelliSenseErrorReport(false);
        }


        /// <summary> 
        /// Solution Platform command is shown in the Standard toolbar by default with Visual C++ settings. Add the 
        /// command if not in the Standard toolbar. 
        /// </summary>
        private void CheckSolutionPlatformCommand()
        {
            DTE dte = (DTE)_applicationObject;
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
                if (c.Guid == GuidList.guidVSStd2KString && c.ID == CommandConstants.cmdidSolutionPlatform)
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
            DTE dte = _applicationObject as DTE;
            EnvDTE.Properties txtEdCpp = dte.get_Properties("TextEditor", "C/C++ Specific");
            EnvDTE.Property prop = txtEdCpp.Item("DisableErrorReporting");
            if (prop != null)
                prop.Value = disable;
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
            /// Add Code Here
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
            /// Add Code Here
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
            Breakpoint functionBP = _applicationObject.Debugger.Breakpoints.Item(_applicationObject.Debugger.Breakpoints.Count);

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
        /// Add Bar Descriptor to each project. 
        /// </summary>
        private void AddBarDescriptor()
        {
            try
            {
                DTE dte = _applicationObject as DTE;
                Projects projs = dte.Solution.Projects;
                foreach (Project proj in projs)
                {
                    VCProject vcProj = proj.Object as VCProject;
                    if (vcProj == null)
                        return;
                    IVCCollection cfgs = vcProj.Configurations;

                    bool isBlackBerry = false;
                    foreach (VCConfiguration cfg in cfgs)
                    {
                        if (cfg.ConfigurationType != ConfigurationTypes.typeApplication)
                            return;

                        if (cfg.Platform.Name == BLACKBERRY || cfg.Platform.Name == BLACKBERRYSIMULATOR)
                            isBlackBerry = true;
                    }

                    if (!isBlackBerry)
                        continue;
                    
                    ProjectItem baritem = proj.ProjectItems.Item(BAR_DESCRIPTOR);
                    if (baritem == null && !File.Exists(vcProj.ProjectDirectory + BAR_DESCRIPTOR))
                    {
                        tokenProcessor = new TokenProcessor();
                        Debug.WriteLine("Add bar descriptor file to the project");
                        string templatePath = dte.Solution.ProjectItemsTemplatePath(proj.Kind);
                        templatePath += BAR_DESCRIPTOR_PATH + BAR_DESCRIPTOR;
                        tokenProcessor.AddReplace(@"[!output PROJECT_NAME]", proj.Name);
                        string destination = System.IO.Path.GetFileName(templatePath);
                        destination = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(proj.FullName), destination);
                        tokenProcessor.UntokenFile(templatePath, destination);
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
        /// Add BlackBerry configurations to the project. 
        /// </summary>
        /// <param name="proj"> Represents a project in the integrated development environment. </param>
        private void AddBlackBerryConfigurations(Project proj)
        {
            try
            {
                ConfigurationManager mgr = proj.ConfigurationManager;
                Configurations cfgs = mgr.AddPlatform(BLACKBERRY, "Win32", true);
                mgr.DeletePlatform("Win32");
                mgr.AddConfigurationRow("Device-Debug", "Debug", true);
                mgr.AddConfigurationRow("Simulator", "Debug", true);
                mgr.DeleteConfigurationRow("Debug");
                mgr.AddConfigurationRow("Device-Release", "Release", true);
                mgr.DeleteConfigurationRow("Release");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


        /// <summary> 
        /// New Start No Debug Command Events After Execution Event Handler. 
        /// </summary>
        /// <param name="Guid"> Command GUID. </param>
        /// <param name="ID"> Command ID. </param>
        /// <param name="CustomIn"> Custom IN Object. </param>
        /// <param name="CustomOut"> Custom OUT Object. </param>
        private void startNoDebugCommandEvents_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            Debug.WriteLine("After Start NoDebug");
        }


        /// <summary> 
        /// New Start No Debug Command Events Before Execution Event Handler. 
        /// </summary>
        /// <param name="Guid"> Command GUID. </param>
        /// <param name="ID"> Command ID. </param>
        /// <param name="CustomIn"> Custom IN Object. </param>
        /// <param name="CustomOut"> Custom OUT Object. </param>
        /// <param name="CancelDefault"> Cancel the default execution of the command. </param>
        private void startNoDebugCommandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            Debug.WriteLine("Before NoDebug");
        }


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
            DTE dte = _applicationObject as DTE;
            if (dte.Solution.SolutionBuild.ActiveConfiguration != null)
            {
                SolutionContexts scCollection = dte.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts;
                foreach (SolutionContext sc in scCollection)
                {
                    if (sc.PlatformName == "BlackBerry" || sc.PlatformName == "BlackBerrySimulator")
                        bbPlatform = true;
                }
            }

            Debug.WriteLine("Before Start Debug");

            if (isDebugEngineRunning || !bbPlatform)
            {
                // Disable the override of F5 (this allows the debugged process to continue execution)
                CancelDefault = false;
            }
            else
            {
                BuildBar();

                CancelDefault = true;
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
                Microsoft.Win32.RegistryKey key;
                key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("VSNDK");
                key.SetValue("Run", "True");
                key.Close();

                _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);

                try
                {
                    Solution2 soln = (Solution2)_applicationObject.Solution;
                    List<String> buildThese = new List<String>();

                    foreach (String startupProject in (Array)soln.SolutionBuild.StartupProjects)
                    {
                        foreach (Project p1 in soln.Projects)
                        {
                            if (p1.UniqueName == startupProject)
                            {
                                buildThese.Add(p1.FullName);
                                break;
                            }
                        }
                    }

                    hitPlay = true;
                    amountOfProjects = buildThese.Count(); // OnBuildDone will call build() only after receiving "amountOfProjects" events
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
        /// This event is fired only when user wants to build, rebuild or clean the project. 
        /// </summary>
        /// <param name="Scope"> Represents the scope of the build. </param>
        /// <param name="Action"> Represents the type of build action that is occurring, such as a build or a deploy action. </param>
        public void OnBuildBegin(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action)
        {
            if ((Action == vsBuildAction.vsBuildActionBuild) || (Action == vsBuildAction.vsBuildActionRebuildAll))
            {
                if ((hitPlay == false) && (isDeploying == false)) // means that the "play" building and deploying process was cancelled before, so we have to disable the 
                                                                  // OnBuildDone event to avoid deploying in case user only wants to build.
                {
                    _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                }
                hitPlay = false;
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
                amountOfProjects -= 1;
                if (amountOfProjects == 0)
                {
                    _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                    Built();
                }
            }
            else if (Action == vsBuildAction.vsBuildActionDeploy)
            {
                _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(this.OnBuildDone);
                isDeploying = false;
                Deployed();
            }
        }


        /// <summary> 
        /// Verify if the build process was successful. If so, start deploying the app. 
        /// </summary>
        public void Built()
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


                foreach (String startupProject in (Array)_applicationObject.Solution.SolutionBuild.StartupProjects)
                {
                    foreach (SolutionContext sc in _applicationObject.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts)
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
                isDeploying = true;
                _applicationObject.Solution.SolutionBuild.Deploy(true);
            }
        }


        /// <summary> 
        /// Get the process ID and launch an executable using the VSNDK debug engine. 
        /// </summary>
        public void Deployed()
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("VSNDK");
            key.SetValue("Run", "False");
            key.Close();

            string pidString = "";
            getPID(_applicationObject, ref pidString);

            bool CancelDefault = LaunchDebugTarget(pidString);
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
                    int begin = outputText.IndexOf("Project: ") + 9;
                    int end = outputText.IndexOf(", Configuration:", begin);
                    string processName = outputText.Substring(begin, end - begin);
                    begin = processesPaths.IndexOf(processName + ":>");

                    string currentPath = dte.ActiveDocument.Path;

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


        /// <summary> 
        /// Launch an executable using the VSNDK debug engine. 
        /// </summary>
        /// <param name="pidString"> Process ID in string format. </param>
        /// <returns> TRUE if successful, False if not. </returns>
        private bool LaunchDebugTarget(string pidString)
        {
            Microsoft.VisualStudio.Shell.ServiceProvider sp =
                 new Microsoft.VisualStudio.Shell.ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_applicationObject);

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
    }
}
