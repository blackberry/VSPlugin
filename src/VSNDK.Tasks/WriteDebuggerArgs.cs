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
        private string _exePath;
        private string _device;
        private bool _isSimulator;

        public override bool Execute()
        {            
            string fileContent = _device;

            // Escape backslashes
            string pattern = @"\\";
            string replacement = @"\\\\";
            Regex r = new Regex(pattern);
            string regexResult = r.Replace(_exePath, replacement);
            fileContent += "\r\n" + regexResult;

            fileContent += "\r\n" + _isSimulator;
            fileContent += "\r\n" + ToolsPath;
            fileContent += "\r\n" + PublicKeyPath;
            fileContent += "\r\n" + Password; // Encrypted

            string appData = Environment.GetEnvironmentVariable("AppData");
            System.IO.StreamWriter file = new System.IO.StreamWriter(appData + @"\BlackBerry\vsndk-args-file.txt");            
            
            file.WriteLine(fileContent);
            file.Close();

            return true;
        }

        [Required]
        public string ExePath
        {
            set
            {
                _exePath = value;
            }
        }

        [Required]
        public string Device
        {
            set
            { 
                _device = value;
            }
        }

        [Required]
        public bool isSimulator
        {
            set
            {
                _isSimulator = value;
            }
        }
        [Required]
        public string ToolsPath
        {
            get;
            set;
        }

        [Required]
        public string PublicKeyPath
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }
    }
}
