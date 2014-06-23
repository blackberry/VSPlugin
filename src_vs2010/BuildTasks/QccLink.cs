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

using System.Collections.Generic;
using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using System.Collections;

namespace BlackBerry.BuildTasks
{
    public sealed class QccLink : QccTask
    {
        #region Member Variables and Constants

        private const string ADDITIONAL_DEPENDENCIES = "AdditionalDependencies";
        private const string LINK_ERROR_REPORTING = "LinkErrorReporting";
        private const string ADDITIONAL_LIB_DIR = "AdditionalLibraryDirectories";        
        private const string GENERATE_MAP_FILE = "GenerateMapFile"; 
        private const string IGNORE_ALL_DEFAULT_LIB = "IgnoreAllDefaultLibraries";
        private const string IGNOR_ALL_DEFAULT_CPP_LIB = "IgnoreAllDefaultCppLibraries";
        private const string LINK_INCREMENTAL = "LinkIncremental";
        private const string LINK_STATUS = "LinkStatus";
        private const string LINK_LIB_DEP = "LinkLibraryDependencies";
        private const string OUTPUT_FILE = "OutputFile";
        private const string TREAT_LINK_WARNING_AS_ERR = "TreatLinkerWarningAsErrors";

        #endregion

        /// <summary>
        /// QccLink default constructor
        /// </summary>
        public QccLink()
            : base(Resources.ResourceManager)
        {
            _switchOrderList.Add(ADDITIONAL_DEPENDENCIES);
            _switchOrderList.Add(LINK_ERROR_REPORTING);
            _switchOrderList.Add(ADDITIONAL_LIB_DIR);
            _switchOrderList.Add(GENERATE_MAP_FILE);
            _switchOrderList.Add(IGNORE_ALL_DEFAULT_LIB);
            _switchOrderList.Add(IGNOR_ALL_DEFAULT_CPP_LIB);
            _switchOrderList.Add(LINK_INCREMENTAL);
            _switchOrderList.Add(LINK_STATUS);
            _switchOrderList.Add(LINK_LIB_DEP);
            _switchOrderList.Add(OUTPUT_FILE);
            _switchOrderList.Add(TREAT_LINK_WARNING_AS_ERR);
            _switchOrderList.Add(SOURCES);
        }

        #region Overrides

        /// <summary>
        /// Getter/Setter for AlwaysAppend property
        /// </summary>
        protected override string AlwaysAppend
        {
            get
            {
                return LinkSharedLibrary? "-shared" : string.Empty;
            }
        }

        /// <summary>
        /// Getter/Setter for CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get { return "qcc_linker.command.1.tlog"; }
        }

        /// <summary>
        /// Getter/Setter for ReadTLog
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new[] { "qcc_linker.read.1.tlog", "qcc_linker.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter/Setter WriteTLogNames property
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "qcc_linker.write.1.tlog", "qcc_linker.*.write.1.tlog" };
            }
        }

        /// <summary>
        /// Function to return the Enhanced Security value.
        /// </summary>
        protected override string GetEnhancedSecuritySwitchValue()
        {
            return "-Wl,-z,relro -Wl,-z,now";
        }
        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for the AdditionalDependencies property
        /// </summary>
        public string[] AdditionalDependencies
        {
            get
            {
                if (IsPropertySet(ADDITIONAL_DEPENDENCIES))
                {
                    return ActiveToolSwitches[ADDITIONAL_DEPENDENCIES].StringList;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(ADDITIONAL_DEPENDENCIES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Additional Dependencies",
                    Description = "Specifies additional items to add to the link command line [i.e. bps] ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-l",
                    Name = ADDITIONAL_DEPENDENCIES,
                    StringList = value
                };
                ActiveToolSwitches.Add(ADDITIONAL_DEPENDENCIES, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the AdditionalLibraryDirectories property
        /// </summary>
        public string[] AdditionalLibraryDirectories
        {
            get
            {
                if (IsPropertySet(ADDITIONAL_LIB_DIR))
                {
                    return ActiveToolSwitches[ADDITIONAL_LIB_DIR].StringList;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(ADDITIONAL_LIB_DIR);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Additional Library Directories",
                    Description = "Allows the user to override the environmental library path (-Lfolder)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-L",
                    Name = ADDITIONAL_LIB_DIR,
                    StringList = value
                };
                ActiveToolSwitches.Add(ADDITIONAL_LIB_DIR, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the GenerateMapFile property
        /// </summary>
        public bool GenerateMapFile
        {
            get
            {
                return (IsPropertySet(GENERATE_MAP_FILE) && ActiveToolSwitches[GENERATE_MAP_FILE].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(GENERATE_MAP_FILE);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Generate Map File",
                    Description = "The -M option tells the linker to create a mapfile.",
                    ArgumentRelationList = new ArrayList()
                };
                switch2.ArgumentRelationList.Add(new ArgumentRelation("MapFileName", "true", false, ""));
                switch2.SwitchValue = "-M";
                switch2.Name = GENERATE_MAP_FILE;
                switch2.BooleanValue = value;
                ActiveToolSwitches.Add(GENERATE_MAP_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the IgnoreAllDefaultLibraries
        /// </summary>
        public bool IgnoreAllDefaultLibraries
        {
            get
            {
                return (IsPropertySet(IGNORE_ALL_DEFAULT_LIB) && ActiveToolSwitches[IGNORE_ALL_DEFAULT_LIB].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(IGNORE_ALL_DEFAULT_LIB);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Ignore All Default Libraries",
                    Description = "The -nostdlib option tells the linker to remove one or more default libraries from the list of libraries it searches when resolving external references. ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-nostdlib",
                    Name = IGNORE_ALL_DEFAULT_LIB,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(IGNORE_ALL_DEFAULT_LIB, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the IgnoreAllDefaultCppLibraries property
        /// </summary>
        public bool IgnoreAllDefaultCppLibraries
        {
            get
            {
                return (IsPropertySet(IGNOR_ALL_DEFAULT_CPP_LIB) && ActiveToolSwitches[IGNOR_ALL_DEFAULT_CPP_LIB].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(IGNOR_ALL_DEFAULT_CPP_LIB);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Ignore All Default C++ Libraries",
                    Description = "The -nostdlib++ option tells the linker to remove one or more default libraries from the list of libraries it searches when resolving external references. ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-nostdlib++",
                    Name = IGNOR_ALL_DEFAULT_CPP_LIB,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(IGNOR_ALL_DEFAULT_CPP_LIB, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the LinkIncremental Property
        /// </summary>
        public bool LinkIncremental
        {
            get
            {
                return (IsPropertySet(LINK_INCREMENTAL) && ActiveToolSwitches[LINK_INCREMENTAL].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(LINK_INCREMENTAL);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Enable Incremental Linking",
                    Description = "Enables incremental linking.     (/INCREMENTAL, /INCREMENTAL:NO)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/INCREMENTAL",
                    ReverseSwitchValue = "/INCREMENTAL:NO",
                    Name = LINK_INCREMENTAL,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LINK_INCREMENTAL, switch2);
                AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("INCREMENTAL", "INCREMENTAL:NO"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("INCREMENTAL:NO", "INCREMENTAL"));
            }
        }

        /// <summary>
        /// Getter/Setter for the LinkStatus property
        /// </summary>
        public bool LinkStatus
        {
            get
            {
                return (IsPropertySet(LINK_STATUS) && ActiveToolSwitches[LINK_STATUS].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(LINK_STATUS);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Link Status",
                    Description = "Specifies whether the linker should display a progress indicator showing what percentage of the link is complete. The default is to not display this status information. (/LTCG:STATUS|LTCG:NOSTATUS)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/LTCG:STATUS",
                    ReverseSwitchValue = "/LTCG:NOSTATUS",
                    Name = LINK_STATUS,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LINK_STATUS, switch2);
                AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("LTCG:STATUS", "LTCG:NOSTATUS"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("LTCG:NOSTATUS", "LTCG:STATUS"));
            }
        }

        /// <summary>
        /// Getter/Setter for the LinkLibraryDependencies
        /// </summary>
        public bool LinkLibraryDependencies
        {
            get
            {
                return (IsPropertySet(LINK_LIB_DEP) && ActiveToolSwitches[LINK_LIB_DEP].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(LINK_LIB_DEP);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Link Library Dependencies",
                    Description = "Specifies whether or not library outputs from project dependencies are automatically linked in.",
                    ArgumentRelationList = new ArrayList(),
                    Name = LINK_LIB_DEP,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LINK_LIB_DEP, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the OutputFile property
        /// </summary>
        public string OutputFile
        {
            get
            {
                if (IsPropertySet(OUTPUT_FILE))
                {
                    return ActiveToolSwitches[OUTPUT_FILE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(OUTPUT_FILE);

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Output File",
                    Description = "The -o option overrides the default name and location of the program that the linker creates.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-o ",
                    Name = OUTPUT_FILE,
                    Value = value
                };
                ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the TreatLinkerWarningAsErrors property.
        /// </summary>
        public bool TreatLinkerWarningAsErrors
        {
            get
            {
                return (IsPropertySet(TREAT_LINK_WARNING_AS_ERR) && ActiveToolSwitches[TREAT_LINK_WARNING_AS_ERR].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(TREAT_LINK_WARNING_AS_ERR);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Treat Linker Warning As Errors",
                    Description = "/WX causes no output file to be generated if the linker generates a warning.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "/WX",
                    ReverseSwitchValue = "/WX:NO",
                    Name = TREAT_LINK_WARNING_AS_ERR,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(TREAT_LINK_WARNING_AS_ERR, switch2);
                AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("WX", "WX:NO"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("WX:NO", "WX"));
            }
        }

        #endregion
    }
}
