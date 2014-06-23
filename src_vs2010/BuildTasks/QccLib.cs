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

using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using System.Collections;

namespace BlackBerry.BuildTasks
{
    public sealed class QccLib : QccTask
    {
        #region Member Variables and Constants

        private const string ADDITIONAL_DEPENDENCIES = "AdditionalDependencies";
        private const string ADDITIONAL_LIB_DIR = "AdditionalLibraryDirectories";
        private const string OUTPUT_FILE = "OutputFile";
        private const string LINK_STATIC = "LinkStatic";
        private const string TREAT_LIB_WARNING_AS_ERROR = "TreatLibWarningAsErrors";

        #endregion

        /// <summary>
        /// QccLib default constructor
        /// </summary>
        public QccLib()
            : base(Resources.ResourceManager)
        {
            _switchOrderList.Add(ADDITIONAL_DEPENDENCIES);
            _switchOrderList.Add(ADDITIONAL_LIB_DIR);
            _switchOrderList.Add(TREAT_LIB_WARNING_AS_ERROR);
            _switchOrderList.Add(OUTPUT_FILE);
            _switchOrderList.Add(SOURCES);
        }

        #region Overrides

        /// <summary>
        /// Getter/Setter for the AlwaysAppend property
        /// </summary>
        protected override string AlwaysAppend
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Getter/Setter for the CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get { return "qcc_lib.command.1.tlog"; }
        }

        /// <summary>
        /// Getter/Setter for the ReadTLogNames property
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new[] { "qcc_lib.read.1.tlog", "qcc_lib.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter/Setter for the WriteTLogNames
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "qcc_lib.write.1.tlog", "qcc_lib.*.write.1.tlog" };
            }
        }

        /// <summary>
        /// Method return the EnhancedSecuritySwitch
        /// </summary>
        /// <returns></returns>
        protected override string GetEnhancedSecuritySwitchValue()
        {
            return string.Empty;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Getter/Setter for the LinkStatic property
        /// </summary>
        public bool LinkStatic
        {
            get
            {
                return (IsPropertySet(LINK_STATIC) && ActiveToolSwitches[LINK_STATIC].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(LINK_STATIC);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Link Static",
                    Description = "Static link Lib",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-static",
                    Name = LINK_STATIC,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(LINK_STATIC, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

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
        /// Getter/Setter for the TreatLibWarningAsErrors property
        /// </summary>
        public bool TreatLibWarningAsErrors
        {
            get
            {
                return (IsPropertySet(TREAT_LIB_WARNING_AS_ERROR) && ActiveToolSwitches[TREAT_LIB_WARNING_AS_ERROR].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(TREAT_LIB_WARNING_AS_ERROR);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Treat Lib Warning As Errors",
                    Description = "-Werror causes no output file to be generated if lib generates a warning.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-Werror",
                    Name = TREAT_LIB_WARNING_AS_ERROR,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(TREAT_LIB_WARNING_AS_ERROR, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// GetterSetter for the OutputFile property
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
                    //Separator = ":",
                    DisplayName = "Output File",
                    Description = "The -A option overrides the default name and location of the program that the linker creates.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-A ",
                    Name = OUTPUT_FILE,
                    Value = value
                };
                ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        #endregion
    }
}
