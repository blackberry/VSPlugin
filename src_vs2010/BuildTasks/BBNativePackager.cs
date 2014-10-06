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
using Microsoft.Build.Utilities;

namespace BlackBerry.BuildTasks
{
    public sealed class BBNativePackager : BBTask
    {
        #region Member Variable and Constant Declarations

        private readonly ArrayList _switchOrderList;
        private static BarDescriptor.qnx _descriptor;
        private string _projectDirectory;
        private string _appName;
        private string _barDescriptorPath = "";

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
        /// BBNativePackager default constructor
        /// </summary>
        public BBNativePackager()
            : base(Resources.ResourceManager)
        {
            _switchOrderList = new ArrayList();
            _switchOrderList.Add(TARGET_FORMAT);
            _switchOrderList.Add(BUILD_ID);
            _switchOrderList.Add(BUILD_ID_FILE);
            _switchOrderList.Add(DEV_MODE);
            _switchOrderList.Add(GET_TARGET_FILE_MAP);
            _switchOrderList.Add(PACKAGE_MANIFEST_ONLY);
            _switchOrderList.Add(OUTPUT_FILE);
            _switchOrderList.Add(APP_DESCRIPTOR);
            _switchOrderList.Add(TRACKER_LOG_DIRECTORY);
        }

        #region Overrides

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
            cmd += " " + clb;

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
        /// Return the assets from the bar-descriptor.xml
        /// </summary>
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
            foreach (var config in configs)
            {
                if (Configuration == "Debug" && Platform == "Device" && config.name == "Device-Debug")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Release" && Platform == "Device" && config.name == "Device-Release")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Profile" && Platform == "Device" && config.name == "Device-Profile")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Coverage" && Platform == "Device" && config.name == "Device-Coverage")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Debug" && Platform == "Simulator" && (config.name == "Simulator" || config.name == "Simulator-Debug"))
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Profile" && Platform == "Simulator" && config.name == "Simulator-Profile")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Coverage" && Platform == "Simulator" && config.name == "Simulator-Coverage")
                {
                    configAssets = config.asset;
                    break;
                }
                if (Configuration == "Release" && Platform == "Simulator" && config.name == "Simulator-Release")
                {
                    configAssets = config.asset;
                    break;
                }
            }

            int clen = configAssets == null ? 0 : configAssets.Length;
            int glen = globalAssets == null ? 0 : globalAssets.Length;
            var items = new ITaskItem[glen + clen];

            if (globalAssets != null)
            {
                for (int i = 0; i < glen; i++)
                {
                    string path = globalAssets[i].path;
                    path = path.Replace("}", string.Empty).Replace(WORKSPACE_LOC, SolutionDir);
                    string target = globalAssets[i].Value;
                    items[i] = new TaskItem(path);
                    items[i].SetMetadata("target", target);
                }
            }

            if (configAssets != null)
            {
                for (int i = 0; i < configAssets.Length; i++)
                {
                    string path = configAssets[i].path;
                    path = path.Replace("}", string.Empty).Replace(WORKSPACE_LOC, SolutionDir);
                    string target = configAssets[i].Value;
                    items[i + glen] = new TaskItem(path);
                    items[i + glen].SetMetadata("target", target);
                }
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
                return _switchOrderList;
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
            get { return new[] { "BBNativePackager.read.1.tlog", "BBNativePackager.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter for the WriteTLogNames property
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "BBNativePackager.write.1.tlog", "BBNativePackager.*.write.1.tlog" };
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

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for Device property
        /// </summary>
        [Output]
        public string Device { get; set; }

        /// <summary>
        /// Getter/Setter for Password property
        /// </summary>
        [Output]
        public string Password { get; set;}

        /// <summary>
        /// Getter/Setter for BarDeploy property
        /// </summary>
        [Output]
        public string BarDeploy { get; set; }

        /// <summary>
        /// Getter/Setter for OutputFile property
        /// </summary>
        [Required]
        [Output]
        public string OutputFile
        {
            get
            {
                if (IsPropertySet(OUTPUT_FILE) && IsExplicitlySetToFalse(PACKAGE_MANIFEST_ONLY))
                {
                    return ActiveToolSwitches[OUTPUT_FILE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(OUTPUT_FILE);

                String switchValue = "";
                if (IsPropertySet(OUTPUT_FILE) && IsExplicitlySetToFalse(PACKAGE_MANIFEST_ONLY))
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
                ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for ProjectDir property
        /// </summary>
        [Required]
        public string ProjectDir
        { 
            get
            {
                return _projectDirectory;
            }
            set
            {
                _projectDirectory = value;
                _descriptor = BarDescriptor.Parser.Load(Path.Combine(ProjectDir, ApplicationDescriptorXml));
            }
        }

        /// <summary>
        /// Getter/Setter for AppName property
        /// </summary>
        [Required]
        public string AppName
        {
            get
            {
                return _appName;
            }
            set
            {
                _appName = value;

                if (_barDescriptorPath == "")
                {   
                    // Default location of bar-descriptor file, if not specified in the Properties Configurations.
                    _barDescriptorPath = "BlackBerry-" + _appName + "\\bar-descriptor.xml";
                    if (!File.Exists(_barDescriptorPath))
                    {   
                        // Just to support the default locations of bar-descriptor from previous versions of the plug-in.
                        _barDescriptorPath = _appName + "_barDescriptor\\bar-descriptor.xml";
                        if (!File.Exists(_barDescriptorPath))
                        {
                            _barDescriptorPath = "bar-descriptor.xml";
                        }
                    }
                }

                ActiveToolSwitches.Remove(APP_DESCRIPTOR);

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Application descriptor file name, for example, bar-descriptor.xml",
                    Description = "Application descriptor file name, for example, bar-descriptor.xml, it must follows the out.bar file",
                    ArgumentRelationList = new ArrayList(),
                    Name = APP_DESCRIPTOR,
                    Value = _barDescriptorPath
                };
                ActiveToolSwitches.Add(APP_DESCRIPTOR, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for SolutionDir property
        /// </summary>
        [Required]
        public string SolutionDir { get; set; }

        public string ApplicationDescriptorXml
        {
            get
            {
                if (IsPropertySet(APP_DESCRIPTOR))
                {
                    return ActiveToolSwitches[APP_DESCRIPTOR].Value;
                }
                return null;
            }
            set
            {
                _barDescriptorPath = value;
            }
        }

        /// <summary>
        /// Getter/Setter for TargetFormat property
        /// </summary>
        public string TargetFormat
        {
            get
            {
                if (IsPropertySet(TARGET_FORMAT))
                {
                    return ActiveToolSwitches[TARGET_FORMAT].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(TARGET_FORMAT);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                {
                    DisplayName = "Target format",
                    Description = "Select the build target format( -target bar -target bar-debug)",
                    ArgumentRelationList = new ArrayList()
                };
                string[][] switchMap =
                {
                    new[] { "bar", "-target bar" }, 
                    new[] { "bar-debug", "-target bar-debug" } 
                };
                switch2.SwitchValue = ReadSwitchMap(TARGET_FORMAT, switchMap, value);
                switch2.Name = TARGET_FORMAT;
                switch2.Value = value;
#if PLATFORM_VS2010
                switch2.MultiValues = true;
#else
                switch2.MultipleValues = true;
#endif
                ActiveToolSwitches.Add(TARGET_FORMAT, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildID property
        /// </summary>
        public int BuildID
        {
            get
            {
                if (IsPropertySet(BUILD_ID))
                {
                    return ActiveToolSwitches[BUILD_ID].Number;
                }
                return 0;
            }
            set
            {
                ActiveToolSwitches.Remove(BUILD_ID);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Integer)
                {
                    DisplayName = "Build Id",
                    Description = "set the build id ( the fourth segment of the version ). Must be a number from 0 to 65535"
                };
                switch2.ArgumentRelationList = new ArrayList();
                switch2.IsValid = ValidateInteger(BUILD_ID, 0, 65535, value);
                switch2.Name = BUILD_ID;
                switch2.Number = value;
                switch2.SwitchValue = "-buildId ";
                ActiveToolSwitches.Add(BUILD_ID, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildIDFile property
        /// </summary>
        public string BuildIDFile
        {
            get
            {
                if (IsPropertySet(BUILD_ID_FILE))
                {
                    return ActiveToolSwitches[BUILD_ID_FILE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(BUILD_ID_FILE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Build ID File",
                    Description = "set the build ID from an existing file",
                    ArgumentRelationList = new ArrayList(),
                    Name = BUILD_ID_FILE,
                    SwitchValue = "-buildIdFile ",
                    Value = value
                };
                ActiveToolSwitches.Add(BUILD_ID_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for DevMode property
        /// </summary>
        public bool DevMode
        {
            get
            {
                return (IsPropertySet(DEV_MODE) && ActiveToolSwitches[DEV_MODE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(DEV_MODE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = DEV_MODE,
                    DisplayName = "Development mode",
                    Description = "Package in development mode ( -devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-devMode",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(DEV_MODE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for PackageManifestOnly property
        /// </summary>
        public bool PackageManifestOnly
        {
            get
            {
                return (IsPropertySet(PACKAGE_MANIFEST_ONLY) && ActiveToolSwitches[PACKAGE_MANIFEST_ONLY].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE_MANIFEST_ONLY);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Package manifest only.",
                    Description = "Package only the manifest file ( -packageManifest)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-packageManifest -listManifest ",
                    Name = PACKAGE_MANIFEST_ONLY,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(PACKAGE_MANIFEST_ONLY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for GetTargetFileMap property
        /// </summary>
        public bool GetTargetFileMap
        {
            get
            {
                return (IsPropertySet(GET_TARGET_FILE_MAP) && ActiveToolSwitches[GET_TARGET_FILE_MAP].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(GET_TARGET_FILE_MAP);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = GET_TARGET_FILE_MAP,
                    DisplayName = "Get target file map",
                    Description = "Get the mapping between local and target files with the -targetFileMap option.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-targetFileMap ",
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(GET_TARGET_FILE_MAP, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for DebugToken property
        /// </summary>
        public string DebugToken
        {
            get
            {
                if (IsPropertySet(DEBUG_TOKEN))
                {
                    return ActiveToolSwitches[DEBUG_TOKEN].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(DEBUG_TOKEN);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Directory)
                {
                    DisplayName = "Debug Token",
                    Description = "Use debug token to generate author and author id ( -debugToken <token> only usable with -devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-debugToken ",
                    Value = value
                };
                switch2.Parents.AddLast(DEV_MODE);
                ActiveToolSwitches.Add(DEBUG_TOKEN, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for TrackerLogDirectory property.
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
                    ArgumentRelationList = new ArrayList(),
                    Value = EnsureTrailingSlash(value)
                };
                ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter for the TrackedInputFiles
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
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
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Getter/Seter for the Sources property
        /// </summary>
        [Required]
        public ITaskItem[] Sources
        {
            get
            {
                if (IsPropertySet(SOURCES))
                {
                    return ActiveToolSwitches[SOURCES].TaskItemArray;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(SOURCES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                ActiveToolSwitches.Add(SOURCES, switch2);
                AddActiveSwitchToolValue(switch2);
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

        #endregion
    }
}
