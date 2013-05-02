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
using System.Reflection;
using System.Resources;
using Microsoft.Build.CPPTasks;
using System.Collections;
using Microsoft.Build.Framework;
using System.IO;
using Microsoft.Build.Utilities;
using System.Security.Cryptography;

namespace VSNDK.Tasks
{
    public class BBDeploy : TrackedVCToolTask
    {
        protected ArrayList switchOrderList;
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

        public BBDeploy()
            : base(new ResourceManager("VSNDK.Tasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {
            this.switchOrderList = new ArrayList();
            this.switchOrderList.Add(INSTALL_APP);
            this.switchOrderList.Add(LAUNCH_APP);
            this.switchOrderList.Add(LIST_INSTALLED_APPS);
            this.switchOrderList.Add(DEBUG_NATIVE);
            this.switchOrderList.Add(DEVICE);
            this.switchOrderList.Add(PASSWORD);
            this.switchOrderList.Add(PACKAGE);
            this.switchOrderList.Add(PACKAGE_ID);
            this.switchOrderList.Add(PACKAGE_NAME);
            this.switchOrderList.Add(GET_FILE);
            this.switchOrderList.Add(GET_FILE_SAVE_AS);
            this.switchOrderList.Add(PUT_FILE);
            this.switchOrderList.Add(PUT_FILE_SAVE_AS);
        }

        #region overrides
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        protected override string GenerateResponseFileCommands()
        {
            string cmd = base.GenerateResponseFileCommands();

            if (ListInstalledApps)
            {
                cmd += " > installedApps.txt";
            }

            return cmd;
        }

        protected override ArrayList SwitchOrderList
        {
            get
            {
                return this.switchOrderList;
            }
        }

        protected override string CommandTLogName
        {
            get { return "BBDeploy.command.1.tlog"; }
        }

        protected override string[] ReadTLogNames
        {
            get { return new string[] { "BBDeploy.read.1.tlog", "BBDeploy.*.read.1.tlog" }; }
        }

        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] { "BBDeploy.write.1.tlog", "BBDeploy.*.write.1.tlog" };
            }
        }
        
        protected override Microsoft.Build.Framework.ITaskItem[] TrackedInputFiles
        {
            get
            {
                // I don't know what this method does but we need to return something other than 'null' or the task doesn't run,
                // and adding something like "localManifest.mf" to the list of items causes other problems.
                ITaskItem[] items = new ITaskItem[1];
                return items;
            }
        }

        protected override string ToolName
        {
            get
            {
                return ToolExe;
            }
        }

        #endregion overrides

        #region properties
        public virtual string GetFile
        {
            get
            {
                if (base.IsPropertySet(GET_FILE))
                {
                    return base.ActiveToolSwitches[GET_FILE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(GET_FILE);

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
                base.ActiveToolSwitches.Add(GET_FILE, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string GetFileSaveAs
        {
            get
            {
                if (base.IsPropertySet(GET_FILE_SAVE_AS))
                {
                    return base.ActiveToolSwitches[GET_FILE_SAVE_AS].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(GET_FILE_SAVE_AS);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = GET_FILE_SAVE_AS,
                    DisplayName = "GetFileSaveAs",
                    Description = "Save a file retrieved using GetFile to the given location.",
                    Value = value
                };
                base.ActiveToolSwitches.Add(GET_FILE_SAVE_AS, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string PutFile
        {
            get
            {
                if (base.IsPropertySet(PUT_FILE))
                {
                    return base.ActiveToolSwitches[PUT_FILE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(PUT_FILE);
                //value = value.Replace(".exe", "");

                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PUT_FILE,
                    DisplayName = "PutFile",
                    Description = "Put the given file into the application's directory.",
                    SwitchValue = "-putFile ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(PUT_FILE, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string PutFileSaveAs
        {
            get
            {
                if (base.IsPropertySet(PUT_FILE_SAVE_AS))
                {
                    return base.ActiveToolSwitches[PUT_FILE_SAVE_AS].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(PUT_FILE_SAVE_AS);

                // We determine the app's directory from the PackageName and PackageId.
                String toolValue = "../../../../apps/" + PackageName + "." + PackageId + "/" + value;
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PUT_FILE_SAVE_AS,
                    DisplayName = "PutFileSaveAs",
                    Description = "Save a file retrieved using PutFile to the given location.",
                    Value = toolValue
                };
                base.ActiveToolSwitches.Add(PUT_FILE_SAVE_AS, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool InstallApp
        {
            get
            {
                return (base.IsPropertySet(INSTALL_APP) && base.ActiveToolSwitches[INSTALL_APP].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(INSTALL_APP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = INSTALL_APP,
                    DisplayName = "Install App",
                    Description = "The -installApp option installs an app from a given package.",
                    SwitchValue = "-installApp",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(INSTALL_APP, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool LaunchApp
        {
            get
            {
                return (base.IsPropertySet(LAUNCH_APP) && base.ActiveToolSwitches[LAUNCH_APP].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(LAUNCH_APP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = LAUNCH_APP,
                    DisplayName = "Launch App",
                    Description = "The -launchApp option allows an app to be launched with or without being installed.",
                    SwitchValue = "-launchApp ",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(LAUNCH_APP, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool ListInstalledApps
        {
            get
            {
                return (base.IsPropertySet(LIST_INSTALLED_APPS) && base.ActiveToolSwitches[LIST_INSTALLED_APPS].BooleanValue);            
            }
            set
            {
                base.ActiveToolSwitches.Remove(LIST_INSTALLED_APPS);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = LIST_INSTALLED_APPS,
                    DisplayName = "List Installed Apps",
                    Description = "The -listInstalledApps option does just that.",
                    SwitchValue = "-listInstalledApps ",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(LIST_INSTALLED_APPS, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool DebugNative
        {
            get
            {
                return (base.IsPropertySet(DEBUG_NATIVE) && base.ActiveToolSwitches[DEBUG_NATIVE].BooleanValue);
            }

            set
            {
                base.ActiveToolSwitches.Remove(DEBUG_NATIVE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = DEBUG_NATIVE,
                    DisplayName = "Launch for debugging",
                    Description = "The -debugNative option launches the app in suspended, ready-to-debug state.",
                    SwitchValue = "-debugNative ",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(DEBUG_NATIVE, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string Device
        {
            get
            {
                if (base.IsPropertySet(DEVICE))
                {
                    return base.ActiveToolSwitches[DEVICE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(DEVICE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = DEVICE,
                    DisplayName = "Device IP",
                    Description = "The -device option specifies the target device's IP address.",
                    SwitchValue = "-device ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(DEVICE, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string Password
        {
            get
            {
                if (base.IsPropertySet(PASSWORD))
                {
                    return base.ActiveToolSwitches[PASSWORD].Value;
                }
                return null;
            }
            set
            {

                base.ActiveToolSwitches.Remove(PASSWORD);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PASSWORD,
                    DisplayName = "Device Password",
                    Description = "The -password option specifies the target device's password.",
                    SwitchValue = "-password ",
                    Value = Decrypt(value)
                };

                base.ActiveToolSwitches.Add(PASSWORD, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string Package
        {
            get
            {
                if (base.IsPropertySet(PACKAGE))
                {
                    return base.ActiveToolSwitches[PACKAGE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(PACKAGE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE,
                    DisplayName = "Package",
                    Description = "The -package option specifies a .bar package.",
                    SwitchValue = "-package ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(PACKAGE, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        [Output]
        public virtual string PackageId
        {
            get
            {
                if (base.IsPropertySet(PACKAGE_ID))
                {
                    return base.ActiveToolSwitches[PACKAGE_ID].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(PACKAGE_ID);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_ID,
                    DisplayName = "Package ID",
                    Description = "The -package-id option specifies the application's package id.",
                    SwitchValue = "-package-id ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(PACKAGE_ID, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        [Output]
        public virtual string PackageName
        {
            get
            {
                if (base.IsPropertySet(PACKAGE_NAME))
                {
                    return base.ActiveToolSwitches[PACKAGE_NAME].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(PACKAGE_NAME);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_NAME,
                    DisplayName = "Package Name",
                    Description = "The -package-name option specifies the application's package name.",
                    SwitchValue = "-package-name ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(PACKAGE_NAME, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        // We need the local manifest in order to determine PackageId and PackageName.
        public virtual string LocalManifestFile
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

        [Output]
        public virtual string TargetManifestFile
        {
            get
            {
                return null;
            }
        }
        #endregion

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        /// <param name="cipher">A base64 encoded string that was created
        /// through the <see cref="Encrypt(string)"/> or
        /// <see cref="Encrypt(SecureString)"/> extension methods.</param>
        /// <returns>The decrypted string.</returns>
        /// <remarks>Keep in mind that the decrypted string remains in memory
        /// and makes your application vulnerable per se. If runtime protection
        /// is essential, <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="cipher"/>
        /// is a null reference.</exception>
        public string Decrypt(string cipher)
        {
        if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }

        public virtual string TrackerLogDirectory
        {
            get
            {
                if (base.IsPropertySet(TRACKER_LOG_DIRECTORY))
                {
                    return base.ActiveToolSwitches[TRACKER_LOG_DIRECTORY].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(TRACKER_LOG_DIRECTORY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
                {
                    DisplayName = "Tracker Log Directory",
                    Description = "Tracker Log Directory.",
                    Value = VCToolTask.EnsureTrailingSlash(value)
                };
                base.ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (this.TrackerLogDirectory != null)
                {
                    return this.TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        protected override string GenerateFullPathToTool()
        {
            return this.ToolName;
        }
    }
}
