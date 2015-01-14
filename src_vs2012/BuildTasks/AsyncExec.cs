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

using System.Diagnostics;
using System.IO;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Helpers;
using Microsoft.Build.Tasks;

namespace BlackBerry.BuildTasks
{
    // This task overrides Exec in order to run commands such as blackberry-connect asynchronously.
    public sealed class AsyncExec : Exec
    {
        private int _processId;

        /// <summary>
        /// Execute given command asynchronously.
        /// </summary>
        /// <param name="pathToTool">Path of executable to be run.</param>
        /// <param name="responseFileCommands">Response file commands</param>
        /// <param name="commandLineCommands">Command Line Arguments</param>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            string comm = "/Q /C " + Command + " -password " + GlobalHelper.Decrypt(Password);

            Process process = new Process();
            process.StartInfo = GetProcessStartInfo(pathToTool, comm);
            process.Start();
            _processId = process.Id;
            return 0;
        }

        /// <summary>
        /// Helper function to create the ProcessStartInfo object for the running process.
        /// </summary>
        /// <param name="executable">Path of executable to be run.</param>
        /// <param name="arguments">Command Line Arguments</param>
        private ProcessStartInfo GetProcessStartInfo(string executable, string arguments)
        {
            if (arguments.Length > 0x7d00)
            {
                Log.LogWarningWithCodeFromResources("ToolTask.CommandTooLong", new object[] { GetType().Name });
            }

            var startInfo = new ProcessStartInfo(executable, arguments);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;

            string workingDirectory = GetWorkingDirectory();
            if (workingDirectory != null)
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            var env = startInfo.EnvironmentVariables;
            env["PATH"] = string.Concat(Path.Combine(ConfigDefaults.JavaHome, "bin"), ";", env["PATH"]);

            if (EnvironmentVariables != null)
            {
                foreach (var item in EnvironmentVariables)
                {
                    int separatorIndex = item.IndexOf('=');
                    if (separatorIndex >= 0)
                    {
                        var name = item.Substring(0, separatorIndex).Trim();
                        var value = item.Substring(separatorIndex + 1).Trim();

                        if (!string.IsNullOrEmpty(name))
                        {
                            env[name] = value;
                        }
                    }
                }
            }
            return startInfo;
        }

        /// <summary>
        /// Getter/Setter for Password Property
        /// </summary>
        public string Password
        {
            set;
            get;
        }
    }
}
