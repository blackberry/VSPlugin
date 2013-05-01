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
        private EnvDTE.DTE _dte;


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

 
        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

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
    }
}
