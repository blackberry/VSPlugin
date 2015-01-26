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
using System.Collections.Generic;
using System.IO;
using System.Text;
using BlackBerry.BarDescriptor.Model;
using BlackBerry.BuildTasks.BarDescriptor;
using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlackBerry.BuildTasks
{
    public sealed class BBNativePackager : BBTask
    {
        #region Member Variable and Constant Declarations

        private QnxRootType _descriptor;
        private string _projectDirectory;
        private string _appName;
        private string _barDescriptorPath = "";

        private const string OUTPUT_FILE = "OutputFiles";
        private const string APP_DESCRIPTOR = "ApplicationDescriptorXml";
        private const string TARGET_FORMAT = "TargetFormat";
        private const string BUILD_ID = "BuildId";
        private const string BUILD_ID_FILE = "BuildIdFile";
        private const string DEV_MODE = "DevMode";
        private const string CONFIGURATION = "Configuration";
        private const string PACKAGE_MANIFEST_ONLY = "PackageManifestOnly";
        private const string DEBUG_TOKEN = "DebugToken";
        private const string SOURCES = "Sources";
        private const string GET_TARGET_FILE_MAP = "GetTargetFileMap";
        private const string WORKSPACE_LOC = "${workspace_loc:/";

        private StreamWriter _localLogWriter;

        #endregion

        /// <summary>
        /// BBNativePackager default constructor
        /// </summary>
        public BBNativePackager()
            : base(Resources.ResourceManager)
        {
            DefineSwitchOrder(TARGET_FORMAT, BUILD_ID, BUILD_ID_FILE, DEV_MODE, CONFIGURATION, GET_TARGET_FILE_MAP, PACKAGE_MANIFEST_ONLY, OUTPUT_FILE, APP_DESCRIPTOR);
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
        protected override string GenerateResponseFileCommands()
        {
            StringBuilder resultCommand = new StringBuilder(base.GenerateResponseFileCommands());
            PackagerCmdBuilder packagerBuilder = new PackagerCmdBuilder();

            AppendResources(packagerBuilder);
            resultCommand.Append(' ').Append(packagerBuilder);

            return resultCommand.ToString();
        }

        /// <summary>
        /// Creates a temporary response (.rsp) file and runs the executable file.
        /// </summary>
        /// <returns>
        /// The returned exit code of the executable file. If the task logged errors, but the executable returned an exit code of 0, this method returns -1.
        /// </returns>
        /// <param name="pathToTool">The path to the executable file.</param><param name="responseFileCommands">The command line arguments to place in the .rsp file.</param><param name="commandLineCommands">The command line arguments to pass directly to the executable file.</param>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            // Output the .bar file's manifest to a file so we can parse it.
            if (PackageManifestOnly)
            {
                _localLogWriter = File.CreateText("localManifest.mf");
                _localLogWriter.WriteLine("Info: Generating local manifest ({0})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else if (GetTargetFileMap)
            {
                _localLogWriter = File.CreateText("targetFileMap.txt");
                _localLogWriter.WriteLine("Info: Generating target map-file ({0})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            var result = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

            if (_localLogWriter != null)
            {
                _localLogWriter.Dispose();
                _localLogWriter = null;
            }

            return result;
        }

        /// <summary>
        /// Parses a single line of text to identify any errors or warnings in canonical format.
        /// </summary>
        /// <param name="singleLine">A single line of text for the method to parse.</param><param name="messageImportance">A value of <see cref="T:Microsoft.Build.Framework.MessageImportance"/> that indicates the importance level with which to log the message.</param>
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            // dump all printouts of this tool into dedicated logger:
            if (_localLogWriter != null)
            {
                _localLogWriter.WriteLine(singleLine);
                LogToolError(singleLine);
                return;
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        /// <summary>
        /// Helper function to read the assets from the bar-descriptor.xml file and 
        /// generate the command line listing the resources to be packaged into the bar file.
        /// </summary>
        /// <param name="commandBuilder">Command Line Builder object</param>
        private void AppendResources(CommandLineBuilder commandBuilder)
        {
            // is it a regular application?
            if (string.Compare(AppType, "Regular", StringComparison.OrdinalIgnoreCase) == 0 || string.IsNullOrEmpty(MakefileTargetName))
            {
                IEnumerable<ITaskItem> sources = GetAssetsFile();
                foreach (ITaskItem item in sources)
                {
                    string target = item.GetMetadata("target");
                    if (item.ItemSpec == target)
                    {
                        commandBuilder.AppendFileNameIfNotNull(item);
                    }
                    else
                    {
                        commandBuilder.AppendSwitchIfNotNull("-e ", item);
                        commandBuilder.AppendFileNameIfNotNull(target);
                    }
                }
            }
            else
            {
                if (string.Compare(AppType, "Cascades", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // or Cascades application, where we can use directly the configuration with specified name from bar-descriptor file?
                    commandBuilder.AppendTextUnquoted("-configuration \"" + MakefileTargetName + "\"");

                    /*
                    ActiveToolSwitches.Remove(CONFIGURATION);

                    ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.String)
                    {
                        DisplayName = "Configuration Name",
                        Description = "Name of the configuration from bar-descriptor.xml to use during BAR build",
                        ArgumentRelationList = new ArrayList(),
                        SwitchValue = "-configuration ",
                        Name = CONFIGURATION,
                        Value = MakefileTargetName
                    };
                    ActiveToolSwitches.Add(CONFIGURATION, switch2);
                    AddActiveSwitchToolValue(switch2);
                     */
                }
                else
                {
                    // custom makefile:
                    var expectedTargetName = string.Concat(Platform, "-", Configuration);
                    commandBuilder.AppendTextUnquoted("-configuration \"" + expectedTargetName + "\"");
                }
            }
        }

        /// <summary>
        /// Return the assets from the bar-descriptor.xml
        /// </summary>
        private IEnumerable<ITaskItem> GetAssetsFile()
        {
            //make sure the _descriptor is loaded
            if (_descriptor == null)
            {
                _descriptor = Parser.Load(Path.Combine(ProjectDir, ApplicationDescriptorXml));
            }
            AssetType[] globalAssets = _descriptor.asset;
            AssetType[] configAssets = null;

            // You can call a configuration whatever you like, but these are the ones Momentics uses for its various
            // platform + configuration combinations.  Usually this is the same as the output directory, but asset paths
            // don't have anything to do with the configuration name.  I've based the config names on the platform
            // + configuration combination, not the output directory.
            var configs = _descriptor.configuration;
            var expectedConfigName = string.Concat(Platform, "-", Configuration);

            foreach (var config in configs)
            {
                if (config.name == expectedConfigName)
                {
                    configAssets = config.asset;
                    break;
                }
                if (expectedConfigName == "Simulator-Debug" && (config.name == "Simulator" || config.name == expectedConfigName))
                {
                    configAssets = config.asset;
                    break;
                }
            }

            var items = new List<ITaskItem>();

            AppendAssets(items, globalAssets);
            AppendAssets(items, configAssets);

            return items.ToArray();
        }

        private void AppendAssets(ICollection<ITaskItem> result, IEnumerable<AssetType> assets)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    string path = asset.path;
                    path = path.Replace("}", string.Empty).Replace(WORKSPACE_LOC, ProjectDir);
                    string target = asset.Value;

                    if (!string.IsNullOrEmpty(target))
                    {
                        var item = new TaskItem(path);
                        item.SetMetadata("target", target);
                        result.Add(item);
                    }
                    else
                    {
                        Log.LogWarning("Asset \"{0}\" has no value set. Ignoring.", path);
                    }
                }
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
            get { return new[] { "BBNativePackager.write.1.tlog", "BBNativePackager.*.write.1.tlog" }; }
        }

        /// <summary>
        /// Getter for the ToolName property
        /// </summary>
        protected override string ToolName
        {
            get { return ToolExe; }
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
                String switchValue = "";
                if (IsPropertySet(OUTPUT_FILE) && IsExplicitlySetToFalse(PACKAGE_MANIFEST_ONLY))
                {
                    switchValue = "-package ";
                }

                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = OUTPUT_FILE,
                    DisplayName = "Output bar file name, for example, out.bar",
                    Description = "The -package option specifies the bar file name.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = switchValue,
                    Value = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for ProjectDir property
        /// </summary>
        [Required]
        public string ProjectDir
        {
            get { return _projectDirectory; }
            set
            {
                _projectDirectory = value;
                _descriptor = Parser.Load(Path.Combine(ProjectDir, ApplicationDescriptorXml));
            }
        }

        /// <summary>
        /// Getter/Setter for AppName property
        /// </summary>
        [Required]
        public string AppName
        {
            get { return _appName; }
            set
            {
                _appName = value;

                if (string.IsNullOrEmpty(_barDescriptorPath))
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

                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = APP_DESCRIPTOR,
                    DisplayName = "Application descriptor file name, for example, bar-descriptor.xml",
                    Description = "Application descriptor file name, for example, bar-descriptor.xml, it must follows the out.bar file",
                    ArgumentRelationList = new ArrayList(),
                    Value = _barDescriptorPath
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for SolutionDir property
        /// </summary>
        [Required]
        public string SolutionDir { get; set; }

        [Required]
        public string ApplicationDescriptorXml
        {
            get { return GetSwitchAsString(APP_DESCRIPTOR); }
            set { _barDescriptorPath = value; }
        }

        /// <summary>
        /// Getter/Setter for TargetFormat property
        /// </summary>
        public string TargetFormat
        {
            get { return GetSwitchAsString(TARGET_FORMAT); }
            set
            {
                string[][] switchMap =
                {
                    new[] { "bar", "-target bar" },
                    new[] { "bar-debug", "-target bar-debug" }
                };

                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = TARGET_FORMAT,
                    DisplayName = "Target format",
                    Description = "Select the build target format (-target bar -target bar-debug)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = ReadSwitchMap(TARGET_FORMAT, switchMap, value),
                    Value = value,
#if PLATFORM_VS2010
                    MultiValues = true
#else
                    MultipleValues = true
#endif
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildID property
        /// </summary>
        public int BuildID
        {
            get { return GetSwitchAsInt32(BUILD_ID); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Integer)
                {
                    Name = BUILD_ID,
                    DisplayName = "Build Id",
                    Description = "Set the build id (the fourth segment of the version). Must be a numbers from 0 to 65535",
                    ArgumentRelationList = new ArrayList(),
                    IsValid = ValidateInteger(BUILD_ID, 0, 65535, value),
                    SwitchValue = "-buildId ",
                    Number = value,
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for BuildIDFile property
        /// </summary>
        public string BuildIDFile
        {
            get { return GetSwitchAsString(BUILD_ID_FILE); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = BUILD_ID_FILE,
                    DisplayName = "Build ID File",
                    Description = "set the build ID from an existing file",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-buildIdFile ",
                    Value = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for DevMode property
        /// </summary>
        public bool DevMode
        {
            get { return GetSwitchAsBool(DEV_MODE); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = DEV_MODE,
                    DisplayName = "Development mode",
                    Description = "Package in development mode (-devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-devMode",
                    BooleanValue = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for PackageManifestOnly property
        /// </summary>
        public bool PackageManifestOnly
        {
            get { return GetSwitchAsBool(PACKAGE_MANIFEST_ONLY); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Package manifest only.",
                    Description = "Package only the manifest file ( -packageManifest)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-packageManifest -listManifest ",
                    Name = PACKAGE_MANIFEST_ONLY,
                    BooleanValue = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for GetTargetFileMap property
        /// </summary>
        public bool GetTargetFileMap
        {
            get { return GetSwitchAsBool(GET_TARGET_FILE_MAP); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = GET_TARGET_FILE_MAP,
                    DisplayName = "Get target file map",
                    Description = "Get the mapping between local and target files with the -targetFileMap option.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-targetFileMap ",
                    BooleanValue = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for DebugToken property
        /// </summary>
        public string DebugToken
        {
            get { return GetSwitchAsString(DEBUG_TOKEN); }
            set
            {
                ActiveToolSwitches.Remove(DEBUG_TOKEN);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Directory)
                {
                    Name = DEBUG_TOKEN,
                    DisplayName = "Debug Token",
                    Description = "Use debug token to generate author and author id ( -debugToken <token> only usable with -devMode)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-debugToken ",
                    Value = value
                };
                toolSwitch.Parents.AddLast(DEV_MODE);
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter for the TrackedInputFiles
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get { return Sources; }
        }

        /// <summary>
        /// Getter/Seter for the Sources property
        /// </summary>
        [Required]
        public ITaskItem[] Sources
        {
            get { return GetSwitchAsItemArray(SOURCES); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Name = SOURCES,
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                SetSwitch(toolSwitch);
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

        /// <summary>
        /// Gets or sets the type of application, which is currently built.
        /// </summary>
        [Required]
        public string AppType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or set the name of makefile's target issuing the build.
        /// </summary>
        public string MakefileTargetName
        {
            get;
            set;
        }

        #endregion
    }
}
