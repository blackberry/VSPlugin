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
    public sealed class SSHDelete : TrackedVCToolTask
    {
        #region Member Variables and Constants

        private readonly ArrayList _switchOrderList;

        private const string DEVICE = "Device";
        private const string PRIVATE_KEY_PATH = "PrivateKeyPath";
        private const string DELETE_FILES = "DeleteFiles";
        private const string PACKAGE_ID = "PackageId";
        private const string PACKAGE_NAME = "PackageName";

        #endregion

        /// <summary>
        /// SSHDelete default constructor
        /// </summary>
        public SSHDelete()
            : base(Resources.ResourceManager)
        {
            _switchOrderList = new ArrayList();
            _switchOrderList.Add(PRIVATE_KEY_PATH);
        }

        #region Overrides

        /// <summary>
        /// Return the Response File Switch string
        /// </summary>
        /// <param name="responseFilePath"></param>
        protected override string GetResponseFileSwitch(string responseFilePath)
        {
            return string.Empty;
        }

        /// <summary>
        /// Return the command line string
        /// </summary>
        protected override string GenerateCommandLineCommands()
        {
            return GenerateResponseFileCommands();
        }

        /// <summary>
        /// Getter/Setter for the CommandTLogName property
        /// </summary>
        protected override string CommandTLogName
        {
            get { return "SSHDelete.command.1.tlog"; }
        }

        /// <summary>
        /// Getter/Setter for the ReadTLogNames string
        /// </summary>
        protected override string[] ReadTLogNames
        {
            get { return new[] { "SSHDelete.read.1.tlog", "SSHDelete.*.read.1.tlog" }; }
        }

        /// <summary>
        /// Getter/Setter for the WriteTLogNames string
        /// </summary>
        protected override string[] WriteTLogNames
        {
            get
            {
                return new[] { "SSHDelete.write.1.tlog", "SSHDelete.*.write.1.tlog" };
            }
        }

        /// <summary>
        /// Getter/Setter for the TrackedInputFiles string
        /// </summary>
        protected override ITaskItem[] TrackedInputFiles
        {
            get
            {
                if (IsPropertySet(DELETE_FILES))
                {
                    return ActiveToolSwitches[DELETE_FILES].TaskItemArray;
                }
                return null;
            }
        }

        /// <summary>
        /// Getter/Setter for the ToolName property
        /// </summary>
        protected override string ToolName
        {
            get
            {
                return ToolExe;
            }
        }

        /// <summary>
        /// Getter/Setter for the TrackerIntermediateDirectory property
        /// </summary>
        protected override string TrackerIntermediateDirectory
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Function to generate the response file commands string
        /// </summary>
        protected override string GenerateResponseFileCommands()
        {
            string cmd = base.GenerateResponseFileCommands();

            cmd += " -o \"StrictHostKeyChecking no\"";

            if (IsPropertySet(DEVICE))
            {
                cmd += " devuser@" + ActiveToolSwitches[DEVICE].Value;
            }

            if (IsPropertySet(DELETE_FILES))
            {
                string path = "../../../../apps/" + PackageName + "." + PackageId + "/";
                cmd += " '";
                foreach (ITaskItem taskItem in ActiveToolSwitches[DELETE_FILES].TaskItemArray)
                {
                    cmd += "rm " + path + taskItem.GetMetadata("Identity") + "; ";
                }
                cmd += "'";
            }
            else
            {
                cmd += "'pwd;'";
            }

            return cmd;
        }

        /// <summary>
        /// Getter/Setter for the SwitchOrderList property.
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get
            {
                return _switchOrderList;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for the Device property
        /// </summary>
        public string Device
        {
            get
            {
                if (IsPropertySet(DEVICE))
                {
                    return ActiveToolSwitches[DEVICE].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(DEVICE);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = DEVICE,
                    DisplayName = "Device IP",
                    Description = "Specifies the target device's IP address.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "devuser@",
                    Value = value
                };
                ActiveToolSwitches.Add(DEVICE, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the PrivateKeyPath property
        /// </summary>
        public string PrivateKeyPath
        {
            get
            {
                if (IsPropertySet(PRIVATE_KEY_PATH))
                {
                    return ActiveToolSwitches[PRIVATE_KEY_PATH].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PRIVATE_KEY_PATH);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PRIVATE_KEY_PATH,
                    DisplayName = "Path to public key",
                    Description = "The -i option specifies the public SSH key to use.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-i ",
                    Value = value
                };
                ActiveToolSwitches.Add(PRIVATE_KEY_PATH, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the PackageId property
        /// </summary>
        public string PackageId
        {
            get
            {
                if (IsPropertySet(PACKAGE_ID))
                {
                    return ActiveToolSwitches[PACKAGE_ID].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE_ID);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_ID,
                    DisplayName = "Package ID",
                    Description = "The -package-id option specifies the application's package id.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-package-id ",
                    Value = value
                };
                ActiveToolSwitches.Add(PACKAGE_ID, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the PackageName property
        /// </summary>
        public string PackageName
        {
            get
            {
                if (IsPropertySet(PACKAGE_NAME))
                {
                    return ActiveToolSwitches[PACKAGE_NAME].Value;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(PACKAGE_NAME);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String)
                {
                    Name = PACKAGE_NAME,
                    DisplayName = "Package Name",
                    Description = "The -package-name option specifies the application's package name.",
                    ArgumentRelationList = new ArrayList(),
                    SwitchValue = "-package-name ",
                    Value = value
                };
                ActiveToolSwitches.Add(PACKAGE_NAME, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        /// <summary>
        /// Getter/Setter for the DeleteFiles property
        /// </summary>
        public ITaskItem[] DeleteFiles
        {
            get
            {
                if (IsPropertySet(DELETE_FILES))
                {
                    return ActiveToolSwitches[DELETE_FILES].TaskItemArray;
                }
                return null;
            }
            set
            {
                ActiveToolSwitches.Remove(DELETE_FILES);
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.ITaskItemArray)
                {
                    Name = DELETE_FILES,
                    DisplayName = "Files to delete",
                    Description = "Delete all items in the array from the device.",
                    TaskItemArray = value
                };
                ActiveToolSwitches.Add(DELETE_FILES, toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        #endregion
    }
}
