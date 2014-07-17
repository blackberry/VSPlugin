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
using System.Collections;
using System.IO;
using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    public sealed class BBDeploy : BBTask
    {
        #region Member Variable Declaration

        private readonly ArrayList _switchOrdersList;

        private string _localManifest;
        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";
        private const string GET_FILE = "GetFile";
        private const string GET_FILE_SAVE_AS = "GetFileSaveAs";
        private const string PUT_FILE = "PutFile";
        private const string PUT_FILE_SAVE_AS = "PutFileSaveAs";
        private const string DEVICE = "Device";
        private const string PASSWORD = "Password";
        private const string PACKAGE_ID = "PackageId";
        private const string PACKAGE_NAME = "PackageName";
        private const string LAUNCH_APP = "LaunchApp";
        private const string INSTALL_APP = "InstallApp";
        private const string LIST_INSTALLED_APPS = "ListInstalledApps";
        private const string DEBUG_NATIVE = "DebugNative";
        private const string PACKAGE = "Package";
        private const string WORKSPACE_LOC = "${workspace_loc:/";
        private string _flagFile;

        #endregion

        /// <summary>
        /// BBDeploy default constructor
        /// </summary>
        public BBDeploy()
            : base(Resources.ResourceManager)
        {
            _switchOrdersList = new ArrayList();
            _switchOrdersList.Add(INSTALL_APP);
            _switchOrdersList.Add(LAUNCH_APP);
            _switchOrdersList.Add(LIST_INSTALLED_APPS);
            _switchOrdersList.Add(DEBUG_NATIVE);
            _switchOrdersList.Add(DEVICE);
            _switchOrdersList.Add(PASSWORD);
            _switchOrdersList.Add(PACKAGE);
            _switchOrdersList.Add(PACKAGE_ID);
            _switchOrdersList.Add(PACKAGE_NAME);
            _switchOrdersList.Add(GET_FILE);
            _switchOrdersList.Add(GET_FILE_SAVE_AS);
            _switchOrdersList.Add(PUT_FILE);
            _switchOrdersList.Add(PUT_FILE_SAVE_AS);
        }

        #region Overrides

        /// <summary>
        /// Helper function to return Response File Switch
        /// </summary>
        /// <param name="responseFilePath">Path to response file.</param>
        /// <returns>Return response file switch</returns>
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        /// <summary>
        /// Helper function to generte the command line argument string.
        /// </summary>
        /// <returns>command line argument string</returns>
        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        /// <summary>
        /// Helper function to generate response file string
        /// </summary>
        /// <returns>response file command line string</returns>
        protected override string GenerateResponseFileCommands()
        {
            string cmd = base.GenerateResponseFileCommands();

            if (ListInstalledApps)
            {
                cmd += " > installedApps.txt";
            }

            return cmd;
        }

        /// <summary>
        /// Getter for the SwitchOrderList property
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get
            {
                return _switchOrdersList;
            }
        }

        /// <summary>
        /// Getter for the CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get { return "BBDeploy.command.1.tlog"; }
        }

        /// <summary>
        /// Getter for the ReadTLogNames property
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new[] { "BBDeploy.read.1.tlog", "BBDeploy.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter for the WriteTLogNames property
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "BBDeploy.write.1.tlog", "BBDeploy.*.write.1.tlog" };
            }
        }
        
        /// <summary>
        /// Getter for the TrackedInputFiles property
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get
            {
                // I don't know what this method does but we need to return something other than 'null' or the task doesn't run,
                // and adding something like "localManifest.mf" to the list of items causes other problems.
                ITaskItem[] items = new ITaskItem[1];
                return items;
            }
        }

        /// <summary>
        /// Getter for the ToolName property
        /// </summary>
        protected override string ToolName
        {
            get
            {
                return ToolExe;
            }
        }

        /// <summary>
        /// Getter for the TrackerIntermediateDirectory property
        /// </summary>
        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Function to return the full path to the tool.
        /// </summary>
        /// <returns></returns>
        protected override string GenerateFullPathToTool()
        {
            return ToolName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for the GetFile property
        /// Retrieve specified file from default application directory
        /// </summary>
        public string GetFile
        {
            get
            {
                if (IsPropertySet(GET_FILE))
                {
                    return ActiveToolSwitches[GET_FILE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(GET_FILE);

                // Value passed in is the file to get, relative to our app's directory.
                // We determine the app's directory from the PackageName and PackageId.
                String toolValue = "../../../../apps/" + PackageName + "." + PackageId + "/" + value;
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = GET_FILE,
                    DisplayName = "GetFile",
                    Description = "Get the given file from the application's directory.",
                    SwitchValue = "-getFile ",
                    Value = toolValue
                };
                ActiveToolSwitches.Add(GET_FILE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the GetFileSaveAs property
        /// Retrieve specified file from location.
        /// </summary>
        public string GetFileSaveAs
        {
            get
            {
                if (IsPropertySet(GET_FILE_SAVE_AS))
                {
                    return ActiveToolSwitches[GET_FILE_SAVE_AS].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(GET_FILE_SAVE_AS);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = GET_FILE_SAVE_AS,
                    DisplayName = "GetFileSaveAs",
                    Description = "Save a file retrieved using GetFile to the given location.",
                    Value = value
                };
                ActiveToolSwitches.Add(GET_FILE_SAVE_AS, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the PutFile property
        /// Save given file to default application directory.
        /// </summary>
        public string PutFile
        {
            get
            {
                if (IsPropertySet(PUT_FILE))
                {
                    return ActiveToolSwitches[PUT_FILE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PUT_FILE);
                //value = value.Replace(".exe", "");

                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PUT_FILE,
                    DisplayName = "PutFile",
                    Description = "Put the given file into the application's directory.",
                    SwitchValue = "-putFile ",
                    Value = value
                };
                ActiveToolSwitches.Add(PUT_FILE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the PutFileSaveAs property
        /// Save given file to specified location
        /// </summary>
        public string PutFileSaveAs
        {
            get
            {
                if (IsPropertySet(PUT_FILE_SAVE_AS))
                {
                    return ActiveToolSwitches[PUT_FILE_SAVE_AS].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PUT_FILE_SAVE_AS);

                // We determine the app's directory from the PackageName and PackageId.
                String toolValue = "../../../../apps/" + PackageName + "." + PackageId + "/" + value;
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PUT_FILE_SAVE_AS,
                    DisplayName = "PutFileSaveAs",
                    Description = "Save a file retrieved using PutFile to the given location.",
                    Value = toolValue
                };
                ActiveToolSwitches.Add(PUT_FILE_SAVE_AS, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the InstallApp property - Switch to cause deploy to install a given application
        /// </summary>
        public bool InstallApp
        {
            get
            {
                return (IsPropertySet(INSTALL_APP) && ActiveToolSwitches[INSTALL_APP].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(INSTALL_APP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = INSTALL_APP,
                    DisplayName = "Install App",
                    Description = "The -installApp option installs an app from a given package.",
                    SwitchValue = "-installApp",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(INSTALL_APP, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the LaunchApp Property - Switch to launch given application.
        /// </summary>
        public bool LaunchApp
        {
            get
            {
                return (IsPropertySet(LAUNCH_APP) && ActiveToolSwitches[LAUNCH_APP].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(LAUNCH_APP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = LAUNCH_APP,
                    DisplayName = "Launch App",
                    Description = "The -launchApp option allows an app to be launched with or without being installed.",
                    SwitchValue = "-launchApp ",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LAUNCH_APP, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for ListInstalledApps Property.
        /// Switch for command to retrieve the list of installed apps.
        /// </summary>
        public bool ListInstalledApps
        {
            get
            {
                return (IsPropertySet(LIST_INSTALLED_APPS) && ActiveToolSwitches[LIST_INSTALLED_APPS].BooleanValue);            
            }
            set
            {
                ActiveToolSwitches.Remove(LIST_INSTALLED_APPS);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = LIST_INSTALLED_APPS,
                    DisplayName = "List Installed Apps",
                    Description = "The -listInstalledApps option does just that.",
                    SwitchValue = "-listInstalledApps ",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LIST_INSTALLED_APPS, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Setter for FlagFile property
        /// </summary>
        public string FlagFile
        {
            set
            {
                _flagFile = value;
            }
        }

        /// <summary>
        /// Getter/Setter for DebugNative property
        /// Switch to cause task to install application in debug configuration.
        /// </summary>
        public bool DebugNative
        {
            get
            {
                return (IsPropertySet(DEBUG_NATIVE) && ActiveToolSwitches[DEBUG_NATIVE].BooleanValue);
            }

            set
            {
                if (File.Exists(_flagFile))
                    value = true;

                ActiveToolSwitches.Remove(DEBUG_NATIVE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = DEBUG_NATIVE,
                    DisplayName = "Launch for debugging",
                    Description = "The -debugNative option launches the app in suspended, ready-to-debug state.",
                    SwitchValue = "-debugNative ",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(DEBUG_NATIVE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for Device property.
        /// Device property designates the IP of the target device.
        /// </summary>
        public string Device
        {
            get
            {
                if (IsPropertySet(DEVICE))
                {
                    return ActiveToolSwitches[DEVICE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(DEVICE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = DEVICE,
                    DisplayName = "Device IP",
                    Description = "The -device option specifies the target device's IP address.",
                    SwitchValue = "-device ",
                    Value = value
                };
                ActiveToolSwitches.Add(DEVICE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for Password property.
        /// Password property specifies the target device password.
        /// </summary>
        public string Password
        {
            get
            {
                if (IsPropertySet(PASSWORD))
                {
                    return ActiveToolSwitches[PASSWORD].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PASSWORD);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PASSWORD,
                    DisplayName = "Device Password",
                    Description = "The -password option specifies the target device's password.",
                    SwitchValue = "-password ",
                    Value = DecryptPassword(value)
                };

                ActiveToolSwitches.Add(PASSWORD, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for Package property.
        /// Package property specified package to be installed/launched on task execution.
        /// </summary>
        public string Package
        {
            get
            {
                if (IsPropertySet(PACKAGE))
                {
                    return ActiveToolSwitches[PACKAGE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE,
                    DisplayName = "Package",
                    Description = "The -package option specifies a .bar package.",
                    SwitchValue = "-package ",
                    Value = value
                };
                ActiveToolSwitches.Add(PACKAGE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for PackageId property
        /// </summary>
        [Output]
        public string PackageId
        {
            get
            {
                if (IsPropertySet(PACKAGE_ID))
                {
                    return ActiveToolSwitches[PACKAGE_ID].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE_ID);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_ID,
                    DisplayName = "Package ID",
                    Description = "The -package-id option specifies the application's package id.",
                    SwitchValue = "-package-id ",
                    Value = value
                };
                ActiveToolSwitches.Add(PACKAGE_ID, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for PackageName property
        /// </summary>
        [Output]
        public string PackageName
        {
            get
            {
                if (IsPropertySet(PACKAGE_NAME))
                {
                    return ActiveToolSwitches[PACKAGE_NAME].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE_NAME);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_NAME,
                    DisplayName = "Package Name",
                    Description = "The -package-name option specifies the application's package name.",
                    SwitchValue = "-package-name ",
                    Value = value
                };
                ActiveToolSwitches.Add(PACKAGE_NAME, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for LocalManifestFile property
        /// We need the local manifest in order to determine PackageId and PackageName.
        /// </summary>
        public string LocalManifestFile
        {
            get
            {
                return _localManifest;
            }
            set
            {
                _localManifest = value;

                string[] lines = File.ReadAllLines(_localManifest);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Package-Name: "))
                    {
                        string name = lines[i].Substring(14);
                        if (PackageName != name)
                            PackageName = name;
                    }
                    else if (lines[i].StartsWith("Package-Id: "))
                    {
                        string id = lines[i].Substring(12);
                        if (PackageId != id)
                            PackageId = id;
                    }
                }
            }
        }

        /// <summary>
        /// Getter/Setter for the TargetManifestFile
        /// </summary>
        [Output]
        public string TargetManifestFile
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Getter/Setter for the TrackerLogDirectory property
        /// </summary>
        public string TrackerLogDirectory
        {
            get
            {
                if (IsPropertySet(TRACKER_LOG_DIRECTORY))
                {
                    return ActiveToolSwitches[TRACKER_LOG_DIRECTORY].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(TRACKER_LOG_DIRECTORY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
                {
                    DisplayName = "Tracker Log Directory",
                    Description = "Tracker Log Directory.",
                    Value = EnsureTrailingSlash(value)
                };
                ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        #endregion
    }
}
