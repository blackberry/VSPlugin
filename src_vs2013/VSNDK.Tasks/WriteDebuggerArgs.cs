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
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VSNDK.Tasks
{
    public class WriteDebuggerArgs : Task
    {
        #region Member Variables and Constants

        private string _projectDir;
        private string _outDir;
        private string _appName;
        private string _device;
        private bool _isSimulator;
        private string _appData;

        #endregion

        #region Public Propeties

        /// <summary>
        /// Getter/Setter for the ProjectDir property
        /// </summary>
        [Required]
        public string ProjectDir
        {
            set { _projectDir = value.Replace('\\', '/'); }
            get { return _projectDir; }
        }

        /// <summary>
        /// Getter/Setter for the _appData property
        /// </summary>
//        [Required]
        public string AppData
        {
            set { _appData = value; }
            get { return _appData; }
        }

        /// <summary>
        /// Getter/Setter for the OutDir property
        /// </summary>
        [Required]
        public string OutDir
        {
            set { _outDir = value.Replace('\\', '/'); }
            get { return _outDir; }
        }

        /// <summary>
        /// Getter/Setter for the AppName property
        /// </summary>
        [Required]
        public string AppName
        {
            set { _appName = value; }
            get { return _appName; }
        }

        /// <summary>
        /// Getter/Setter for the Device property
        /// </summary>
        [Required]
        public string Device
        {
            set
            {
                _device = value;
            }
        }

        /// <summary>
        /// Getter/Setter for the isSimulator property
        /// </summary>
        [Required]
        public bool isSimulator
        {
            set
            {
                _isSimulator = value;
            }
        }

        /// <summary>
        /// Getter/Setter for the ToolsPath property
        /// </summary>
        [Required]
        public string ToolsPath
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for the PublicKeyPath property
        /// </summary>
        [Required]
        public string PublicKeyPath
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for the Password property
        /// </summary>
        public string Password
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Execute MSBuild function
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            /// Initialize Variables
            bool result;

            try
            {
                // Setting the work directory.
                string rootedOutDir = (Path.IsPathRooted(OutDir)) ? OutDir + AppName : ProjectDir + OutDir + AppName;

                AppData = Environment.GetEnvironmentVariable("AppData") + @"\BlackBerry\vsndk-args-file.txt";

                System.IO.StreamWriter file = new System.IO.StreamWriter(AppData);

                file.WriteLine(_device);
                file.WriteLine(rootedOutDir.Replace(@"\\", @"\\\\"));
                file.WriteLine(_isSimulator);
                file.WriteLine(ToolsPath);
                file.WriteLine(PublicKeyPath);
                file.WriteLine(Password); 
                file.Close();

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }


    }
}
