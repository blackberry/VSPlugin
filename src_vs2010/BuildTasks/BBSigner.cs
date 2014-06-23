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

using System.Collections;
using BlackBerry.BuildTasks.Properties;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// MSBuild Task responsible for the signing of the BlackBerry Bar files 
    /// for deploy to a secure device not in development mode. 
    /// </summary>
    public sealed class BBSigner : BBTask
    {
        #region Member Variables and Constants Declaration

        private readonly ArrayList _switchOrderList;

        private const string REGISTER = "Register";
        private const string KEYSTOREPASSWORD = "KeyStorePassword";
        private const string CSJPIN = "CSJPin";
        private const string CSJFILES = "Signing_AND_DEBUGTOKEN_CSJFiles";
        private const string SOURCES = "Sources";
        private const string OUTPUT_FILE = "OutputFiles";
        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";

        #endregion

        /// <summary>
        /// BBSigner default constructor
        /// </summary>
        public BBSigner()
            : base(Resources.ResourceManager)
        {
            _switchOrderList = new ArrayList();
            _switchOrderList.Add("AlwaysAppend");
            _switchOrderList.Add(REGISTER);
            _switchOrderList.Add(CSJPIN);
            _switchOrderList.Add(KEYSTOREPASSWORD);
            _switchOrderList.Add(CSJFILES);
            _switchOrderList.Add(OUTPUT_FILE);
            _switchOrderList.Add(TRACKER_LOG_DIRECTORY);
        }

        #region Overrides

        /// <summary>
        /// Return the GetResposeFile Switch
        /// Note: don't use response file for msbuild because it is removed before qcc to run GCC compiler 
        /// </summary>
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        /// <summary>
        /// Return the command line argument string
        /// Note: pass the response file to command line commands
        /// </summary>
        /// <returns></returns>
        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        /// <summary>
        /// Return the Response File Commands string.
        /// </summary>
        /// <returns></returns>
        protected override string GenerateResponseFileCommands()
        {
            if (!Register)
            {
                _switchOrderList.Remove(REGISTER);
                _switchOrderList.Remove(CSJPIN);
                _switchOrderList.Remove(CSJFILES);
            }
            else
                _switchOrderList.Remove(OUTPUT_FILE);
            return base.GenerateResponseFileCommands();
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
            get { return "BBSigner.command.1.tlog"; }
        }

        /// <summary>
        /// Getter for the ReadTLogNames property
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new[] { "BBSigner.read.1.tlog", "BBSigner.*.read.1.tlog" }; }
        }
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "BBSigner.write.1.tlog", "BBSigner.*.write.1.tlog" };
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
        /// Getter for the TrackedInputFiles property
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get { return Sources; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for the Register property
        /// </summary>
        [Required]
        public bool Register
        {
            get
            {
                return (IsPropertySet(REGISTER) && ActiveToolSwitches[REGISTER].BooleanValue);
            }
            set
            {
                ActiveToolSwitches.Remove(REGISTER);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Register",
                    Description = "Register the computer with CSJ file to sign application( -register)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-register",
                    Name = REGISTER,
                    BooleanValue = value
                };
                ActiveToolSwitches.Add(REGISTER, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the Sources property
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
        /// Getter/Setter for the CSJFiles property
        /// </summary>
        public ITaskItem[] CSJFiles
        {
            get
            {
                if (IsPropertySet(CSJFILES))
                {
                    return ActiveToolSwitches[CSJFILES].TaskItemArray;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(CSJFILES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    Name = CSJFILES,
                    TaskItemArray = value
                };
                ActiveToolSwitches.Add(CSJFILES, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the KeyStorePassword property
        /// </summary>
        public string KeyStorePassword
        {
            get
            {
                if (IsPropertySet(KEYSTOREPASSWORD))
                {
                    return ActiveToolSwitches[KEYSTOREPASSWORD].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(KEYSTOREPASSWORD);

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Keystore password",
                    Description = "The -storepass option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-storepass ",
                    Name = KEYSTOREPASSWORD,
                    Value = DecryptPassword(value)
                };

                ActiveToolSwitches.Add(KEYSTOREPASSWORD, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the CSJPin property
        /// </summary>
        public string CSJPin
        {
            get
            {
                if (IsPropertySet(CSJPIN))
                {
                    return ActiveToolSwitches[CSJPIN].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(CSJPIN);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Keystore password",
                    Description = "The -csjpin option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-csjpin ",
                    Name = CSJPIN,
                    Value = value
                };
                ActiveToolSwitches.Add(CSJPIN, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        /// <summary>
        /// Getter/Setter for the OutputFile property
        /// </summary>
        [Required]
        [Output]
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
                    DisplayName = "Output bar file name to be signed",
                    Description = "Specifies the bar file name to be signed",
                    ArgumentRelationList = new ArrayList(),
                    Name = OUTPUT_FILE,
                    Value = value
                };
                ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                AddActiveSwitchToolValue(switch2);
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
                    ArgumentRelationList = new ArrayList(),
                    Value = EnsureTrailingSlash(value)
                };
                ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                AddActiveSwitchToolValue(switch2);
            }
        }

        #endregion
    }
}
 