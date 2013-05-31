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

namespace VSNDK.Tasks
{
    public class BBNativePackager : TrackedVCToolTask
    {
        #region Member Variable and Constant Declarations
        protected ArrayList switchOrderList;
        private static BarDescriptor.qnx _descriptor = null;
        private string _projDir;
        private string _appName;
        private string _barDescriptorPath;
        private const string OUTPUT_FILE = "OutputFiles";
        private const string APP_DESCRIPTOR = "ApplicationDescriptorXml";
        private const string TARGET_FORMAT = "TargetFormat";
        private const string BUILD_ID = "BuildId";
        private const string BUILD_ID_FILE = "BuildIdFile";
        private const string DEV_MODE = "DevMode";
        private const string PACKAGE_MANIFEST_ONLY = "PackageManifestOnly";
        private const string DEBUG_TOKEN = "DebugToken";
        private const string SOURCES = "Sources";
        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";
        private const string GET_TARGET_FILE_MAP = "GetTargetFileMap";
        private const string WORKSPACE_LOC = "${workspace_loc:/";
        #endregion

        /// <summary>
        /// BBNativePackager Constructor
        /// </summary>
        public BBNativePackager()
            : base(new ResourceManager("VSNDK.Tasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {
            this.switchOrderList = new ArrayList();
            this.switchOrderList.Add(TARGET_FORMAT);
            this.switchOrderList.Add(BUILD_ID);
            this.switchOrderList.Add(BUILD_ID_FILE);
            this.switchOrderList.Add(DEV_MODE);
            this.switchOrderList.Add(GET_TARGET_FILE_MAP);
            this.switchOrderList.Add(PACKAGE_MANIFEST_ONLY);
            this.switchOrderList.Add(OUTPUT_FILE);
            this.switchOrderList.Add(APP_DESCRIPTOR);
            this.switchOrderList.Add(TRACKER_LOG_DIRECTORY);

            ApplicationDescriptorXml = "bar-descriptor.xml";
        }

        #region overrides


        /// <summary>
        /// Return the response file switch.
        /// Note: Don't use response file for msbuild because it is removed before qcc to run GCC compiler 
        /// </summary>
        /// <param name="responseFilePath">Response File Path</param>
        /// <returns>Response File Switch</returns>
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        /// <summary>
        /// Return the Command Line String.
        /// Note: pass the response file to command line commands
        /// </summary>
        /// <returns>Command Line String</returns>
        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        /// <summary>
        /// Return the Response File Command String
        /// </summary>
        /// <returns></returns>
        protected override string GenerateResponseFileCommands()
        {
            string cmd = base.GenerateResponseFileCommands();
            PackagerCmdBuilder clb = new PackagerCmdBuilder();
            AppendResources(clb);
            cmd += " " + clb.ToString();

            // Output the .bar file's manifest to a file so we can parse it.
            if (PackageManifestOnly)
            {
                cmd += " > localManifest.mf";
            }
            else if (GetTargetFileMap)
            {
                cmd += " > targetFileMap.txt";
            }
            
            return cmd;
        }

        /// <summary>
        /// Helper function to read the assets from the bar-descriptor.xml file and 
        /// generate the command line listing the resources to be packaged into the bar file.
        /// </summary>
        /// <param name="clb">Command Line Builder object</param>
        private void AppendResources(CommandLineBuilder clb)
        {
            ITaskItem[] sources = GetAssetsFile();
            foreach (ITaskItem item in sources)
            {
                string target = item.GetMetadata("target");
                if (item.ItemSpec == target)
                    clb.AppendFileNameIfNotNull(item);
                else
                {
                    clb.AppendSwitchIfNotNull("-e ", item);
                    clb.AppendFileNameIfNotNull(target);
                }
            }
        }

        /// <summary>
        /// Return the assets from the bardescriptor.xml
        /// </summary>
        /// <returns></returns>
        private ITaskItem[] GetAssetsFile()
        {

            //make sure the _descriptor is loaded
            if (_descriptor == null)
            {
                _descriptor = BarDescriptor.Parser.Load(Path.Combine(ProjectDir, ApplicationDescriptorXml));
            }
            BarDescriptor.asset[] globalAssets = _descriptor.assets;
            BarDescriptor.asset[] configAssets = null;

            // You can call a configuration whatever you like, but these are the ones Momentics uses for its various
            // platform + configuration combinations.  Usually this is the same as the output directory, but asset paths
            // don't have anything to do with the configuration name.  I've based the config names on the platform
            // + configuration combination, not the output directory.
            BarDescriptor.qnxConfiguration[] configs = _descriptor.configurations;
            foreach (BarDescriptor.qnxConfiguration config in configs)
            {
                if (Configuration == "Debug" && Platform == "BlackBerry" && config.name == "Device-Debug")
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Release" && Platform == "BlackBerry" && config.name == "Device-Release")
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Profile" && Platform == "BlackBerry" && config.name == "Device-Profile")
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Coverage" && Platform == "BlackBerry" && config.name == "Device-Coverage")
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Debug" && Platform == "BlackBerrySimulator" && (config.name == "Simulator" || config.name == "Simulator-Debug"))
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Profile" && Platform == "BlackBerrySimulator" && config.name == "Simulator-Profile")
                {
                    configAssets = config.asset;
                    break;
                }
                else if (Configuration == "Coverage" && Platform == "BlackBerrySimulator" && config.name == "Simulator-Coverage")
                {
                    configAssets = config.asset;
                    break;
                }
            }
            
            ITaskItem[] items = null;


            int clen = (configAssets == null) ? 0 : configAssets.Length;
            int glen = (globalAssets == null) ? 0 : globalAssets.Length;

            items = new ITaskItem[glen + clen];

            for (int i = 0; i < glen; i++)
            {
                string path = globalAssets[i].path;
                path = path.Replace("}", string.Empty).Replace(WORKSPACE_LOC, SolutionDir);
                string target = globalAssets[i].Value;
                items[i] = new TaskItem(path);
                items[i].SetMetadata("target", target);
            }

           
            for (int i = 0; i < configAssets.Length; i++)
            {
                string path = configAssets[i].path;
                path = path.Replace("}", string.Empty).Replace(WORKSPACE_LOC, SolutionDir);
                string target = configAssets[i].Value;
                items[i + glen] = new TaskItem(path);
                items[i + glen].SetMetadata("target", target);
            }

            return items;

        }

        /// <summary>
        /// Getter for the SwitchOrderList property
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get
            {
                return this.switchOrderList;
            }
        }

        /// <summary>
        /// Getter for the CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get { return "BBNativePackager.command.1.tlog"; }
        }

        /// <summary>
        /// Getter for the ReadTLogNames property.
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new string[] { "BBNativePackager.read.1.tlog", "BBNativePackager.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter for the WriteTLogNames property
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] { "BBNativePackager.write.1.tlog", "BBNativePackager.*.write.1.tlog" };
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

        #endregion overrides

        #region properties

        /// <summary>
        /// Getter/Setter for Device property
        /// </summary>
        [Output]
        public virtual string Device { get; set; }

        /// <summary>
        /// Getter/Setter for Password property
        /// </summary>
        [Output]
        public virtual string Password { get; set;}

        /// <summary>
        /// Getter/Setter for BarDeploy property
        /// </summary>
        [Output]
        public virtual string BarDeploy { get; set; }

        /// <summary>
        /// Getter/Setter for OutputFile property
        /// </summary>
        [Required]
        [Output]
        public virtual string OutputFile
        {
            get
            {
                if (base.IsPropertySet(OUTPUT_FILE) && base.IsExplicitlySetToFalse(PACKAGE_MANIFEST_ONLY))
                {
                    return base.ActiveToolSwitches[OUTPUT_FILE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(OUTPUT_FILE);

                String switchValue = "";
                if (base.IsPropertySet(OUTPUT_FILE) && base.IsExplicitlySetToFalse(PACKAGE_MANIFEST_ONLY))
                {
                    switchValue = "-package ";
                }

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Output bar file name, for example, out.bar",
                    Description = "The -package option specifies the bar file name.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = switchValue,
                    Name = OUTPUT_FILE,
                    Value = value
                };
                base.ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for ProjectDir property
        /// </summary>
        [Required]
        public virtual string ProjectDir
        { 
            get
            {
                return _projDir;
            }
            set 
            {
                _projDir = value;
                _descriptor = BarDescriptor.Parser.Load(Path.Combine(ProjectDir, ApplicationDescriptorXml));
            }
        }

        /// <summary>
        /// Getter/Setter for AppName property
        /// </summary>
        [Required]
        public virtual string AppName
        {
            get
            {
                return _appName;
            }
            set
            {
                _appName = value;
                if ((_barDescriptorPath != null) && (!_barDescriptorPath.Contains(_appName + "_barDescriptor")))
                {
                    int pos = _barDescriptorPath.LastIndexOf('\\') + 1;
                    int pos2 = _barDescriptorPath.LastIndexOf('/') + 1;
                    if (pos == 0) // if the '\\' char was not found.
                        pos = pos2;
                    if (pos < pos2)
                        pos = pos2;
                    _barDescriptorPath = _barDescriptorPath.Substring(0, pos) + _appName + "_barDescriptor\\" + _barDescriptorPath.Substring(pos);

                    base.ActiveToolSwitches.Remove(APP_DESCRIPTOR);

                    ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                    {
                        DisplayName = "Application descriptor file name, for example, bar-descriptor.xml",
                        Description = "Application descriptor file name, for example, bar-descriptor.xml, it must follows the out.bar file",
                        ArgumentRelationList = new ArrayList(),
                        Name = APP_DESCRIPTOR,
                        Value = _barDescriptorPath
                    };
                    base.ActiveToolSwitches.Add(APP_DESCRIPTOR, switch2);
                    base.AddActiveSwitchToolValue(switch2);
                }

            }
        }

        /// <summary>
        /// Getter/Setter for SolutionDir property
        /// </summary>
        [Required]
        public virtual string SolutionDir { get; set; }

        public virtual string ApplicationDescriptorXml
        {
            get
            {
                if (base.IsPropertySet(APP_DESCRIPTOR))
                {
                    return base.ActiveToolSwitches[APP_DESCRIPTOR].Value;
                }
                return null;
            }
            set
            {
                _barDescriptorPath = value.Replace("bar-descriptor.xml", "");

                if ((_appName != null) && (!_barDescriptorPath.Contains(_appName + "_barDescriptor")))
                    _barDescriptorPath = _barDescriptorPath.EndsWith(@"\") ? _barDescriptorPath + _appName + "_barDescriptor\\bar-descriptor.xml" : _barDescriptorPath + "\\" + _appName + "_barDescriptor\\bar-descriptor.xml";
                else
                    _barDescriptorPath = _barDescriptorPath.EndsWith(@"\") ? _barDescriptorPath + "bar-descriptor.xml" : _barDescriptorPath + @"\bar-descriptor.xml";

                _barDescriptorPath = _barDescriptorPath.Trim('\\');

                if (_appName != null) // means that the full bar-descriptor path is correct
                {
                    base.ActiveToolSwitches.Remove(APP_DESCRIPTOR);

                    ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                    {
                        DisplayName = "Application descriptor file name, for example, bar-descriptor.xml",
                        Description = "Application descriptor file name, for example, bar-descriptor.xml, it must follows the out.bar file",
                        ArgumentRelationList = new ArrayList(),
                        Name = APP_DESCRIPTOR,
                        Value = _barDescriptorPath
                    };
                    base.ActiveToolSwitches.Add(APP_DESCRIPTOR, switch2);
                    base.AddActiveSwitchToolValue(switch2);
                }
            }
        }

        /// <summary>
        /// Getter/Setter for TargetFormat property
        /// </summary>
        public virtual string TargetFormat
        {
            get
            {
                if (base.IsPropertySet(TARGET_FORMAT))
                {
                    return base.ActiveToolSwitches[TARGET_FORMAT].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(TARGET_FORMAT);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Target format",
                    Description = "Select the build target format( -target bar -target bar-debug)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap = new string[][] 
                { 
                    new string[] { "bar", "-target bar" }, 
                    new string[] { "bar-debug", "-target bar-debug" } 
                };
                switch2.SwitchValue = base.ReadSwitchMap(TARGET_FORMAT, switchMap, value);
                switch2.Name = TARGET_FORMAT;
                switch2.Value = value;
                switch2.MultiValues = true;
                base.ActiveToolSwitches.Add(TARGET_FORMAT, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildID property
        /// </summary>
        public virtual int BuildID
        {
            get
            {
                if (base.IsPropertySet(BUILD_ID))
                {
                    return base.ActiveToolSwitches[BUILD_ID].Number;
                }
                return 0;
            }
            set
            {
                base.ActiveToolSwitches.Remove(BUILD_ID);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Integer)
                {
                    DisplayName = "Build Id",
                    Description = "set the build id ( the fourth segment of the version ). Must be a number from 0 to 65535"
                };
                switch2.ArgumentRelationList = new ArrayList();
                switch2.IsValid = base.ValidateInteger(BUILD_ID, 0, 65535, value);
                switch2.Name = BUILD_ID;
                switch2.Number = value;
                switch2.SwitchValue = "-buildId ";
                base.ActiveToolSwitches.Add(BUILD_ID, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildIDFile property
        /// </summary>
        public virtual string BuildIDFile
        {
            get
            {
                if (base.IsPropertySet(BUILD_ID_FILE))
                {
                    return base.ActiveToolSwitches[BUILD_ID_FILE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(BUILD_ID_FILE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Build ID File",
                    Description = "set the build ID from an existing file",
                    ArgumentRelationList = new ArrayList(),
                    Name = BUILD_ID_FILE,
                    SwitchValue = "-buildIdFile ",
                    Value = value
                };
                base.ActiveToolSwitches.Add(BUILD_ID_FILE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for DevMode property
        /// </summary>
        public virtual bool DevMode
        {
            get
            {
                return (base.IsPropertySet(DEV_MODE) && base.ActiveToolSwitches[DEV_MODE].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(DEV_MODE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = DEV_MODE,
                    DisplayName = "Development mode",
                    Description = "Package in development mode ( -devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-devMode",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(DEV_MODE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for PackageManifestOnly property
        /// </summary>
        public virtual bool PackageManifestOnly
        {
            get
            {
                return (base.IsPropertySet(PACKAGE_MANIFEST_ONLY) && base.ActiveToolSwitches[PACKAGE_MANIFEST_ONLY].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(PACKAGE_MANIFEST_ONLY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Package manifest only.",
                    Description = "Package only the manifest file ( -packageManifest)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-packageManifest -listManifest ",
                    Name = PACKAGE_MANIFEST_ONLY,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(PACKAGE_MANIFEST_ONLY, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for GetTargetFileMap property
        /// </summary>
        public virtual bool GetTargetFileMap
        {
            get
            {
                return (base.IsPropertySet(GET_TARGET_FILE_MAP) && base.ActiveToolSwitches[GET_TARGET_FILE_MAP].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(GET_TARGET_FILE_MAP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = GET_TARGET_FILE_MAP,
                    DisplayName = "Get target file map",
                    Description = "Get the mapping between local and target files with the -targetFileMap option.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-targetFileMap ",
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(GET_TARGET_FILE_MAP, toolSwitch);
                base.AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for DebugToken property
        /// </summary>
        public virtual string DebugToken
        {
            get
            {
                if (base.IsPropertySet(DEBUG_TOKEN))
                {
                    return base.ActiveToolSwitches[DEBUG_TOKEN].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(DEBUG_TOKEN);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
                {
                    DisplayName = "Debug Token",
                    Description = "Use debug token to generate author and author id ( -debugToken <token> only usable with -devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-debugToken ",
                    Value = value
                };
                switch2.Parents.AddLast(DEV_MODE);
                base.ActiveToolSwitches.Add(DEBUG_TOKEN, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for TrackerLogDirectory property.
        /// </summary>
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
                    ArgumentRelationList = new ArrayList(),
                    Value = VCToolTask.EnsureTrailingSlash(value)
                };
                base.ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        #endregion properties

        /// <summary>
        /// Getter for the TrackedInputFiles
        /// </summary>
        protected override Microsoft.Build.Framework.ITaskItem[] TrackedInputFiles
        {
            get
            {
                return Sources;
            }
        }

        /// <summary>
        /// Getter for the TrackerIntermediateDirectory
        /// </summary>
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

        /// <summary>
        /// Getter/Seter for the Sources property
        /// </summary>
        [Required]
        public virtual ITaskItem[] Sources
        {
            get
            {
                if (base.IsPropertySet(SOURCES))
                {
                    return base.ActiveToolSwitches[SOURCES].TaskItemArray;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(SOURCES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                base.ActiveToolSwitches.Add(SOURCES, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the Configuration property
        /// </summary>
        [Required]
        public string Configuration
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for the Platform property
        /// </summary>
        [Required]
        public string Platform
        {
            get;
            set;
        }
    }
}
