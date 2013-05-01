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
using Microsoft.Build.CPPTasks;
using System.Resources;
using System.Reflection;
using Microsoft.Build.Framework;
using System.Collections;
using Microsoft.Build.Utilities;
using System.IO;

namespace VSNDK.Tasks
{
    public class QccLink : VSNDKTasks
    {
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

        public QccLink()
            : base(new ResourceManager("VSNDK.Tasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {
            this.switchOrderList.Add(ADDITIONAL_DEPENDENCIES);
            this.switchOrderList.Add(LINK_ERROR_REPORTING);
            this.switchOrderList.Add(ADDITIONAL_LIB_DIR);
            this.switchOrderList.Add(GENERATE_MAP_FILE);
            this.switchOrderList.Add(IGNORE_ALL_DEFAULT_LIB);
            this.switchOrderList.Add(IGNOR_ALL_DEFAULT_CPP_LIB);
            this.switchOrderList.Add(LINK_INCREMENTAL);
            this.switchOrderList.Add(LINK_STATUS);
            this.switchOrderList.Add(LINK_LIB_DEP);
            this.switchOrderList.Add(OUTPUT_FILE);
            this.switchOrderList.Add(TREAT_LINK_WARNING_AS_ERR);
            this.switchOrderList.Add(SOURCES);
        }

        #region overrides
        protected override string AlwaysAppend
        {
            get
            {
                return LinkSharedLibrary? "-shared" : string.Empty;
            }
        }

        protected override string CommandTLogName
        {
            get { return "qcc_linker.command.1.tlog"; }
        }

        protected override string[] ReadTLogNames
        {
            get { return new string[] { "qcc_linker.read.1.tlog", "qcc_linker.*.read.1.tlog" }; }
        }

        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] { "qcc_linker.write.1.tlog", "qcc_linker.*.write.1.tlog" };
            }
        }

        protected override string GetEnhancedSecuritySwitchValue()
        {
            return "-Wl,-z,relro -Wl,-z,now";
        }
        #endregion

        #region properties
        public virtual string[] AdditionalDependencies
        {
            get
            {
                if (base.IsPropertySet(ADDITIONAL_DEPENDENCIES))
                {
                    return base.ActiveToolSwitches[ADDITIONAL_DEPENDENCIES].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(ADDITIONAL_DEPENDENCIES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Additional Dependencies",
                    Description = "Specifies additional items to add to the link command line [i.e. bps] ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-l",
                    Name = ADDITIONAL_DEPENDENCIES,
                    StringList = value
                };
                base.ActiveToolSwitches.Add(ADDITIONAL_DEPENDENCIES, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual string[] AdditionalLibraryDirectories
        {
            get
            {
                if (base.IsPropertySet(ADDITIONAL_LIB_DIR))
                {
                    return base.ActiveToolSwitches[ADDITIONAL_LIB_DIR].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(ADDITIONAL_LIB_DIR);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.StringArray)
                {
                    DisplayName = "Additional Library Directories",
                    Description = "Allows the user to override the environmental library path (-Lfolder)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-L",
                    Name = ADDITIONAL_LIB_DIR,
                    StringList = value
                };
                base.ActiveToolSwitches.Add(ADDITIONAL_LIB_DIR, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual bool GenerateMapFile
        {
            get
            {
                return (base.IsPropertySet(GENERATE_MAP_FILE) && base.ActiveToolSwitches[GENERATE_MAP_FILE].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(GENERATE_MAP_FILE);
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
                base.ActiveToolSwitches.Add(GENERATE_MAP_FILE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual bool IgnoreAllDefaultLibraries
        {
            get
            {
                return (base.IsPropertySet(IGNORE_ALL_DEFAULT_LIB) && base.ActiveToolSwitches[IGNORE_ALL_DEFAULT_LIB].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(IGNORE_ALL_DEFAULT_LIB);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Ignore All Default Libraries",
                    Description = "The -nostdlib option tells the linker to remove one or more default libraries from the list of libraries it searches when resolving external references. ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-nostdlib",
                    Name = IGNORE_ALL_DEFAULT_LIB,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(IGNORE_ALL_DEFAULT_LIB, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual bool IgnoreAllDefaultCppLibraries
        {
            get
            {
                return (base.IsPropertySet(IGNOR_ALL_DEFAULT_CPP_LIB) && base.ActiveToolSwitches[IGNOR_ALL_DEFAULT_CPP_LIB].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(IGNOR_ALL_DEFAULT_CPP_LIB);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Ignore All Default C++ Libraries",
                    Description = "The -nostdlib++ option tells the linker to remove one or more default libraries from the list of libraries it searches when resolving external references. ",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-nostdlib++",
                    Name = IGNOR_ALL_DEFAULT_CPP_LIB,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(IGNOR_ALL_DEFAULT_CPP_LIB, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual bool LinkIncremental
        {
            get
            {
                return (base.IsPropertySet(LINK_INCREMENTAL) && base.ActiveToolSwitches[LINK_INCREMENTAL].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(LINK_INCREMENTAL);
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
                base.ActiveToolSwitches.Add(LINK_INCREMENTAL, switch2);
                base.AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("INCREMENTAL", "INCREMENTAL:NO"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("INCREMENTAL:NO", "INCREMENTAL"));
            }
        }

        public virtual bool LinkStatus
        {
            get
            {
                return (base.IsPropertySet(LINK_STATUS) && base.ActiveToolSwitches[LINK_STATUS].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(LINK_STATUS);
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
                base.ActiveToolSwitches.Add(LINK_STATUS, switch2);
                base.AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("LTCG:STATUS", "LTCG:NOSTATUS"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("LTCG:NOSTATUS", "LTCG:STATUS"));
            }
        }

        public virtual bool LinkLibraryDependencies
        {
            get
            {
                return (base.IsPropertySet(LINK_LIB_DEP) && base.ActiveToolSwitches[LINK_LIB_DEP].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(LINK_LIB_DEP);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Link Library Dependencies",
                    Description = "Specifies whether or not library outputs from project dependencies are automatically linked in.",
                    ArgumentRelationList = new ArrayList(),
                    Name = LINK_LIB_DEP,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(LINK_LIB_DEP, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual string OutputFile
        {
            get
            {
                if (base.IsPropertySet(OUTPUT_FILE))
                {
                    return base.ActiveToolSwitches[OUTPUT_FILE].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(OUTPUT_FILE);
                //value = value.Replace(".exe", "");

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    //Separator = ":",
                    DisplayName = "Output File",
                    Description = "The -o option overrides the default name and location of the program that the linker creates.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-o ",
                    Name = OUTPUT_FILE,
                    Value = value
                };
                base.ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        public virtual bool TreatLinkerWarningAsErrors
        {
            get
            {
                return (base.IsPropertySet(TREAT_LINK_WARNING_AS_ERR) && base.ActiveToolSwitches[TREAT_LINK_WARNING_AS_ERR].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(TREAT_LINK_WARNING_AS_ERR);
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
                base.ActiveToolSwitches.Add(TREAT_LINK_WARNING_AS_ERR, switch2);
                base.AddActiveSwitchToolValue(switch2);
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("WX", "WX:NO"));
                switch2.Overrides.AddLast(new KeyValuePair<string, string>("WX:NO", "WX"));
            }
        }
        #endregion

    }
}
