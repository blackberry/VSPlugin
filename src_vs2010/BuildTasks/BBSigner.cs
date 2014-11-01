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

        private const string REGISTER = "Register";
        private const string KEYSTOREPASSWORD = "KeyStorePassword";
        private const string CSJPIN = "CSJPin";
        private const string CSJFILES = "Signing_AND_DEBUGTOKEN_CSJFiles";
        private const string SOURCES = "Sources";
        private const string OUTPUT_FILE = "OutputFiles";

        #endregion

        /// <summary>
        /// BBSigner default constructor
        /// </summary>
        public BBSigner()
            : base(Resources.ResourceManager)
        {
            DefineSwitchOrder("AlwaysAppend", REGISTER, CSJPIN, KEYSTOREPASSWORD, CSJFILES, OUTPUT_FILE);
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
        protected override string GenerateResponseFileCommands()
        {
            if (!Register)
            {
                RemoveFromSwithOrder(REGISTER, CSJPIN, CSJFILES);
            }
            else
            {
                RemoveFromSwithOrder(OUTPUT_FILE);
            }
            return base.GenerateResponseFileCommands();
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
            get { return new[] { "BBSigner.write.1.tlog", "BBSigner.*.write.1.tlog" }; }
        }

        /// <summary>
        /// Getter for the ToolName property
        /// </summary>
        protected override string ToolName
        {
            get { return ToolExe; }
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
            get { return GetSwitchAsBool(REGISTER); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    Name = REGISTER,
                    DisplayName = "Register",
                    Description = "Register the computer with CSJ file to sign application( -register)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-register",
                    BooleanValue = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the Sources property
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
        /// Getter/Setter for the CSJFiles property
        /// </summary>
        public ITaskItem[] CSJFiles
        {
            get { return GetSwitchAsItemArray(CSJFILES); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Name = CSJFILES,
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    TaskItemArray = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the KeyStorePassword property
        /// </summary>
        public string KeyStorePassword
        {
            get { return GetSwitchAsString(KEYSTOREPASSWORD); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = KEYSTOREPASSWORD,
                    DisplayName = "Keystore password",
                    Description = "The -storepass option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-storepass ",
                    Value = DecryptPassword(value)
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the CSJPin property
        /// </summary>
        public string CSJPin
        {
            get { return GetSwitchAsString(CSJPIN); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = CSJPIN,
                    DisplayName = "Keystore password",
                    Description = "The -csjpin option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-csjpin ",
                    Value = value
                };
                SetSwitch(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the OutputFile property
        /// </summary>
        [Required]
        [Output]
        public string OutputFile
        {
            get { return GetSwitchAsString(OUTPUT_FILE); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File)
                {
                    Name = OUTPUT_FILE,
                    DisplayName = "Output bar file name to be signed",
                    Description = "Specifies the bar file name to be signed",
                    ArgumentRelationList = new ArrayList(),
                    Value = value
                };
                SetSwitch(toolSwitch);
            }
        }

        #endregion
    }
}
 