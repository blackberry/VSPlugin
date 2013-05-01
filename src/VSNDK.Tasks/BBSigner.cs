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
    public class BBSigner : TrackedVCToolTask
    {
        protected ArrayList switchOrderList;
        private const string REGISTER = "Register";
        private const string KEYSTOREPASSWORD = "KeyStorePassword";
        private const string CSJPIN = "CSJPin";
        private const string CSJFILES = "Signing_AND_DEBUGTOKEN_CSJFiles";
        private const string SOURCES = "Sources";
        private const string OUTPUT_FILE = "OutputFiles";
        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";


        public BBSigner()
            : base(new ResourceManager("VSNDK.Tasks.Properties.Resources", Assembly.GetExecutingAssembly()))
        {
            this.switchOrderList = new ArrayList();
            this.switchOrderList.Add("AlwaysAppend");
            this.switchOrderList.Add(REGISTER);
            this.switchOrderList.Add(CSJPIN);
            this.switchOrderList.Add(KEYSTOREPASSWORD);
            this.switchOrderList.Add(CSJFILES);
            this.switchOrderList.Add(OUTPUT_FILE);
            this.switchOrderList.Add(TRACKER_LOG_DIRECTORY);
        }

        #region overrides
        //don't use response file for msbuild because it is removed before qcc to run GCC compiler 
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        //instead pass the response file to command line commands
        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        protected override string GenerateResponseFileCommands()
        {
            if (!Register)
            {
                switchOrderList.Remove(REGISTER);
                switchOrderList.Remove(CSJPIN);
                switchOrderList.Remove(CSJFILES);
            }
            else
                switchOrderList.Remove(OUTPUT_FILE);
            return base.GenerateResponseFileCommands();
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
            get { return "BBSigner.command.1.tlog"; }
        }

        protected override string[] ReadTLogNames
        {
            get { return new string[] { "BBSigner.read.1.tlog", "BBSigner.*.read.1.tlog" }; }
        }
        protected override string[] WriteTLogNames
        {
            get
            {
                return new string[] { "BBSigner.write.1.tlog", "BBSigner.*.write.1.tlog" };
            }
        }

        protected override string ToolName
        {
            get
            {
                return ToolExe;
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

        protected override ITaskItem[] TrackedInputFiles
        {
            get { return Sources; }
        }

        #endregion overrides

        #region properties

        [Required]
        public virtual bool Register
        {
            get
            {
                return (base.IsPropertySet(REGISTER) && base.ActiveToolSwitches[REGISTER].BooleanValue);
            }
            set
            {
                base.ActiveToolSwitches.Remove(REGISTER);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.Boolean)
                {
                    DisplayName = "Register",
                    Description = "Register the computer with CSJ file to sign application( -register)",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-register",
                    Name = REGISTER,
                    BooleanValue = value
                };
                base.ActiveToolSwitches.Add(REGISTER, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

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

        public virtual ITaskItem[] CSJFiles
        {
            get
            {
                if (base.IsPropertySet(CSJFILES))
                {
                    return base.ActiveToolSwitches[CSJFILES].TaskItemArray;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(CSJFILES);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Separator = " ",
                    Required = true,
                    ArgumentRelationList = new ArrayList(),
                    Name = CSJFILES,
                    TaskItemArray = value
                };
                base.ActiveToolSwitches.Add(CSJFILES, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }


        public virtual string KeyStorePassword
        {
            get
            {
                if (base.IsPropertySet(KEYSTOREPASSWORD))
                {
                    return base.ActiveToolSwitches[KEYSTOREPASSWORD].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(KEYSTOREPASSWORD);

                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    //Separator = ":",
                    DisplayName = "Keystore password",
                    Description = "The -storepass option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-storepass ",
                    Name = KEYSTOREPASSWORD,
                    Value = Decrypt(value)
                };



                base.ActiveToolSwitches.Add(KEYSTOREPASSWORD, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

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

        public virtual string CSJPin
        {
            get
            {
                if (base.IsPropertySet(CSJPIN))
                {
                    return base.ActiveToolSwitches[CSJPIN].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove(CSJPIN);
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    //Separator = ":",
                    DisplayName = "Keystore password",
                    Description = "The -csjpin option specifies the password.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-csjpin ",
                    Name = CSJPIN,
                    Value = value
                };
                base.ActiveToolSwitches.Add(CSJPIN, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        [Required]
        [Output]
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
                ToolSwitch switch2 = new ToolSwitch(ToolSwitchType.File)
                {
                    DisplayName = "Output bar file name to be signed",
                    Description = "Specifies the bar file name to be signed",
                    ArgumentRelationList = new ArrayList(),
                    Name = OUTPUT_FILE,
                    Value = value
                };
                base.ActiveToolSwitches.Add(OUTPUT_FILE, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
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
                    ArgumentRelationList = new ArrayList(),
                    Value = VCToolTask.EnsureTrailingSlash(value)
                };
                base.ActiveToolSwitches.Add(TRACKER_LOG_DIRECTORY, switch2);
                base.AddActiveSwitchToolValue(switch2);
            }
        }

        #endregion properties

    }
}
 