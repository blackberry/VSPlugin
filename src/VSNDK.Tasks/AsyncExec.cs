//* Copyright 2010-2011 BlackBerry Limited.
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
using Microsoft.Build.Tasks;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.Build.Framework;
using System.Security.Cryptography;

namespace VSNDK.Tasks
{
    // This task overrides Exec in order to run commands such as blackberry-connect asynchronously.
    public class AsyncExec : Exec
    {
        private int _processId;

        /// <summary>
        /// Execute given command asynchronously.
        /// </summary>
        /// <param name="pathToTool">Path of executable to be run.</param>
        /// <param name="responseFileCommands">Response file commands</param>
        /// <param name="commandLineCommands">Command Line Arguments</param>
        /// <returns></returns>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            string comm = "/Q /C " + Command + " -password " + Decrypt(Password);

            Process process = new Process();
            process.StartInfo = GetProcessStartInfo(pathToTool, comm);
            process.StartInfo.UseShellExecute = false;
            process.Start();
            _processId = process.Id;
            return 0;
        }

        /// <summary>
        /// Helper function to create the ProcessStartInfo object for the running process.
        /// </summary>
        /// <param name="executable">Path of executable to be run.</param>
        /// <param name="arguments">Command Line Arguments</param>
        /// <returns></returns>
        protected virtual ProcessStartInfo GetProcessStartInfo(string executable, string arguments)
        {
            if (arguments.Length > 0x7d00)
            {
                this.Log.LogWarningWithCodeFromResources("ToolTask.CommandTooLong", new object[] { base.GetType().Name });
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(executable, arguments);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = true;

            string workingDirectory = this.GetWorkingDirectory();
            if (workingDirectory != null)
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            StringDictionary environmentOverride = this.EnvironmentOverride;
            if (environmentOverride != null)
            {
                foreach (DictionaryEntry entry in environmentOverride)
                {
                    startInfo.EnvironmentVariables.Remove(entry.Key.ToString());
                    startInfo.EnvironmentVariables.Add(entry.Key.ToString(), entry.Value.ToString());
                }
            }
            
            return startInfo;
        }

        /// <summary>
        /// Getter/Setter for Password Property
        /// </summary>
        public virtual string Password
        {
            set;
            get;
        }

        /// <summary>
        /// Helper Function to Decrypt Password
        /// </summary>
        /// <param name="cipher"></param>
        /// <returns></returns>
        public string Decrypt(string cipher)
        {
            if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }
    }
}
