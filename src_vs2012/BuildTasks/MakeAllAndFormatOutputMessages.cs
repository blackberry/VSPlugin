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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BlackBerry.BuildTasks.Helpers;
using BlackBerry.NativeCore;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// Cancelable MSBuild Task for Running the Make file for the build.
    /// </summary>
    public sealed class MakeAllAndFormatOutputMessages : ICancelableTask
    {
        /// <summary>
        /// Type of parsed messages from makefile's (qcc's) output/error streams.
        /// </summary>
        public enum MessageType
        {
            Nothing,
            Message,
            Warning,
            Error
        }

        #region Member Variables and Constants

        private string _projectDir;
        private string _outDir;

        private Process _process;

        private string _processorCount;
        private int _errorCount;

        private static readonly Regex UndefinedReferenceLineRegex = new Regex(@"(\(\s*\d+\s*\)|:\s*\d+\s*:)");
        private bool _isCascadesProject;
        private string _makefileSourceReferencePath;

        #endregion

        /// <summary>
        /// Getter/Setter for the BuildEngine property
        /// </summary>
        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for the HostObject property
        /// </summary>
        public ITaskHost HostObject
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for CompileItems property
        /// </summary>
        public ITaskItem[] CompileItems
        {
            set;
            get;
        }

        public string ConfigurationType
        {
            get;
            set;
        }

        public string ConfigurationAppType
        {
            get;
            set;
        }

        public string QnxHost
        {
            get;
            set;
        }

        public string QnxTarget
        {
            get;
            set;
        }

        public string MakefileTargetName
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for the number of processors property.
        /// </summary>
        public string ProcessorCount
        {
            get
            {
                if (_processorCount != null)
                    return _processorCount;

                _processorCount = Environment.ProcessorCount.ToString();
                return _processorCount; 
            }
            set { _processorCount = value; }
        }

        private bool IsMultiProcessorsBuild()
        {
            foreach (ITaskItem compileItem in CompileItems)
            {
                bool result;

                // Get the metadata we need from this compile item.
                return bool.TryParse(compileItem.GetMetadata("MultiProcess"), out result) && result;
            }

            return false;
        }

        [Required]
        public string ProjectDir
        {
            set { _projectDir = value.Replace('\\', '/'); }
            get { return _projectDir; }
        }

        [Required]
        public string OutDir
        {
            set { _outDir = value.Replace('\\', '/'); }
            get { return _outDir; }
        }

        [Required]
        public string ToolsPath
        {
            get;
            set;
        }

        /// <summary>
        /// Perform Task Execution 
        /// </summary>
        /// <returns>Return true on successful task completion</returns>
        public bool Execute()
        {
            _isCascadesProject = ConfigurationAppType == "Cascades";
            _errorCount = 0;

            try
            {
                var makeArgs = string.Concat(IsMultiProcessorsBuild() ? " -j" + ProcessorCount : string.Empty,
                    !string.IsNullOrEmpty(MakefileTargetName) ? " " : string.Empty, MakefileTargetName);
                var args = string.Concat("/c \"", ToolsPath, "\\make\"", makeArgs);

                NotifyMessage(string.Concat("Building: make", makeArgs));

                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", args);

                // set some BlackBerry default environment variables:
                // (taken from any bbndk-env_xxx.bat script)
                ProcessSetupHelper.Update(startInfo.EnvironmentVariables, QnxHost, QnxTarget);

                // Set UseShellExecute to false for redirection.
                startInfo.UseShellExecute = false;

                // The following commands are needed to redirect the standard and error outputs.
                // These streams are read asynchronously using an event handler.
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                // Setting the working directory, if we use makefiles generated by the VSNDK plugin
                // they all are placed inside the output dir, however the ones for Cascades Application
                // and custom is supposed to be located next to the project:
                if (ConfigurationAppType == "Cascades" || ConfigurationAppType == "Custom")
                {
                    startInfo.WorkingDirectory = ProjectDir;
                    _makefileSourceReferencePath = ProjectDir.Replace('/', '\\');
                }
                else
                {
                    string rootedOutDir = Path.IsPathRooted(OutDir) ? OutDir : ProjectDir + OutDir;
                    startInfo.WorkingDirectory = rootedOutDir;
                    _makefileSourceReferencePath = null; // NOTE: the 'Regular' type of builds contains full-paths already, so no need to process them
                }

                // Do not create the black window.
                startInfo.CreateNoWindow = true;

                // Create a process and assign its ProcessStartInfo
                _process = new Process();
                _process.StartInfo = startInfo;

                // Set ours events handlers to asynchronously read the standard and error outputs.
                _process.OutputDataReceived += StandardOutputHandler;
                _process.ErrorDataReceived += ErrorOutputHandler;

                // Start the process
                _process.Start();

                // Start the asynchronous read of the standard and error output stream.
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                do
                {
                    Application.DoEvents();

                    // Wait until finished
                    // This code is correct, don't remove it. WaitForExit freezes the IDE while waiting for the end of the build process.
                    // With this loop and the time out, the IDE won't be frozen enabling the build process to be cancelled.
                }
                while (!_process.WaitForExit(100));
                _process = null;
            }
            catch (Exception ex)
            {
                _process = null;
                Console.WriteLine("Failed to execute make all. Reason: " + ex.Message);
                return false;
            }

            // build succeeded, when no errors printed:
            return _errorCount == 0;
        }

        /// <summary>
        /// Parses the standard output and presents its content to upper layer.
        /// </summary>
        public static MessageType ProcessStandardOutput(string text, out string message)
        {
            if (!string.IsNullOrEmpty(text))
            {
                message = text;
                return MessageType.Message;
            }

            message = null;
            return MessageType.Nothing;
        }

        public static MessageType ProcessErrorOutput(string text, out string message, out string fileName, out int line, out int column)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // send build output to the Output Window and to the ErrorList window:
                int separatorStart = text.IndexOf(": error:");
                if (separatorStart > 0)
                {
                    fileName = ExtractFileName(text, separatorStart, out line, out column);
                    message = text.Substring(separatorStart + 9);
                    return MessageType.Error;
                }

                separatorStart = text.IndexOf(": warning:");
                if (separatorStart > 0)
                {
                    fileName = ExtractFileName(text, separatorStart, out line, out column);
                    message = text.Substring(separatorStart + 11);
                    return MessageType.Warning;
                }

                separatorStart = text.IndexOf(": note:");
                if (separatorStart > 0)
                {
                    fileName = ExtractFileName(text, separatorStart, out line, out column);
                    message = text.Substring(separatorStart + 7);
                    return MessageType.Warning;
                }

                if (text.Contains("undefined reference"))
                {
                    return ProcessUndefinedReference(text, out message, out fileName, out line, out column);
                }

                fileName = null;
                line = 0;
                column = 0;
                message = text;
                return MessageType.Message;
            }

            fileName = null;
            line = 0;
            column = 0;
            message = null;
            return MessageType.Nothing;
        }

        /// <summary>
        /// Extracts file name from specified text with additional info if available.
        /// </summary>
        public static string ExtractFileName(string text, out int line, out int column)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            return ExtractFileName(text, text.Length, out line, out column);
        }

        /// <summary>
        /// Extracts file name from specified text with additional info if available.
        /// </summary>
        private static string ExtractFileName(string text, int length, out int line, out int column)
        {
            var extendedInfoStartAt = text.IndexOf('(', 0, length);
            char separator;

            // check if line and column are specified in brackets:
            if (extendedInfoStartAt < 0)
            {
                extendedInfoStartAt = length > 2 ? text.IndexOf(':', 2, length - 2) : -1; // since 2 as it might be a drive letter separator inside path

                // check if using ':line:column' format:
                if (extendedInfoStartAt < 0)
                {
                    line = 0;
                    column = 0;
                    return PostProcessPath(text.Substring(0, length));
                }

                //////////////////////////////
                // Format: <name>:line:column
                separator = ':';
            }
            else
            {
                //////////////////////////////
                // Format: <name>(line,column)
                separator = ',';
            }

            var fileName = text.Substring(0, extendedInfoStartAt);
            var separatorAt = text.IndexOf(separator, extendedInfoStartAt + 1, length - extendedInfoStartAt - 1);
            var extendedInfoEndAt = text.IndexOf(')', extendedInfoStartAt + 1, length - extendedInfoStartAt - 1);

            // there is only line provided:
            if (separatorAt < 0)
            {
                var lineText = extendedInfoEndAt < 0
                    ? text.Substring(extendedInfoStartAt + 1, length - extendedInfoStartAt - 1)
                    : text.Substring(extendedInfoStartAt + 1, extendedInfoEndAt - extendedInfoStartAt - 1);

                int.TryParse(lineText, out line);
                column = 1;
            }
            else
            {
                var lineText = text.Substring(extendedInfoStartAt + 1, separatorAt - extendedInfoStartAt - 1);
                var columnText = extendedInfoEndAt < 0
                   ? text.Substring(separatorAt + 1, length - separatorAt - 1)
                   : text.Substring(separatorAt + 1, extendedInfoEndAt - separatorAt - 1);

                int.TryParse(lineText, out line);
                int.TryParse(columnText, out column);
            }

            return PostProcessPath(fileName);
        }

        private static string PostProcessPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            return NativeMethods.GetLongPathName(fileName.Trim());
        }

        private static MessageType ProcessUndefinedReference(string text, out string message, out string fileName, out int line, out int column)
        {
            var match = UndefinedReferenceLineRegex.Match(text);

            if (match.Success)
            {
                var lineText = match.Value.Substring(1, match.Value.Length - 2).Trim();
                int.TryParse(lineText, out line);

                fileName = PostProcessPath(text.Substring(0, match.Index).TrimEnd(':', ' ', '\t'));
                message = text.Substring(match.Index + match.Length + 1).Trim();
            }
            else
            {
                line = 0;

                int separatorStartAt = text.IndexOf(':');
                if (separatorStartAt < 0)
                {
                    fileName = null;
                    message = text.Trim();
                }
                else
                {
                    fileName = PostProcessPath(text.Substring(0, separatorStartAt));

                    int separatorEndAt = text.IndexOf(':', separatorStartAt + 1);
                    message = text.Substring((separatorEndAt < 0 ? separatorStartAt : separatorEndAt) + 1).Trim();
                }
            }
            column = 1;

            return MessageType.Error;
        }

        /// <summary>
        /// Standard Output Event Handler
        /// </summary>
        /// <param name="sendingProcess">Sending Process</param>
        /// <param name="outLine">Output Text</param>
        private void StandardOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string message;

            if (ProcessStandardOutput(outLine.Data, out message) != MessageType.Nothing)
            {
                NotifyMessage(message);
            }
        }

        /// <summary>
        /// Error Output Event Handler
        /// </summary>
        /// <param name="sendingProcess">Sending process</param>
        /// <param name="outLine">Error Message</param>
        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string fileName;
            int line;
            int column;
            string message;

            var type = ProcessErrorOutput(outLine.Data, out message, out fileName, out line, out column);
            if (type != MessageType.Nothing)
            {
                switch (type)
                {
                    case MessageType.Message:
                        NotifyMessage(message);
                        break;
                    case MessageType.Warning:
                        NotifyWarning(message, fileName, line, column);
                        break;
                    case MessageType.Error:
                        //NotifyMessage(outLine.Data);
                        NotifyError(message, fileName, line, column);
                        _errorCount++;
                        break;
                }
            }
        }

        #region Notifications

        private void NotifyMessage(string message)
        {
            if (!string.IsNullOrEmpty(message) && BuildEngine != null)
            {
                try
                {
                    BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, string.Empty, MessageImportance.High));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Log");
                }
            }
        }

        private void NotifyWarning(string message, string fileName, int line, int column)
        {
            if (!string.IsNullOrEmpty(message) && BuildEngine != null)
            {
                try
                {
#if PLATFORM_VS2010
                    // PH: somehow in VS2010 the column count starts from 0, not 1 as the whole code assumes:
                    column--;
#endif
                    BuildEngine.LogWarningEvent(new BuildWarningEventArgs(string.Empty, string.Empty, GetProjectAwareFileName(fileName), line, column, 0, 0, message, string.Empty, string.Empty));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Warning");
                }
            }
        }

        private void NotifyError(string message, string fileName, int line, int column)
        {
            if (!string.IsNullOrEmpty(message) && BuildEngine != null)
            {
                try
                {
#if PLATFORM_VS2010
                    // PH: somehow in VS2010 the column count starts from 0, not 1 as the whole code assumes:
                    column--;
#endif
                    BuildEngine.LogErrorEvent(new BuildErrorEventArgs(string.Empty, string.Empty, GetProjectAwareFileName(fileName), line, column, 0, 0, message, string.Empty, string.Empty));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Error");
                }
            }
        }

        #endregion

        /// <summary>
        /// Returns full path to the file within the project.
        /// </summary>
        private string GetProjectAwareFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // is it a 'Regular' project with full-paths already:
            if (string.IsNullOrEmpty(_makefileSourceReferencePath))
                return fileName;

            if (fileName.StartsWith(_makefileSourceReferencePath))
                return fileName.Replace('/', '\\');

            if (_isCascadesProject)
                return Path.Combine(_makefileSourceReferencePath, DropFirstDirectory(fileName.Replace('/', '\\')));

            // for 'Custom' project type:
            return Path.Combine(_makefileSourceReferencePath, fileName.Replace('/', '\\'));
        }

        private static string DropFirstDirectory(string path)
        {
            int at = path.IndexOf('\\');
            if (at < 0)
                return path;

            return path.Substring(at + 1);
        }

        /// <summary>
        /// Perform Cancel Code
        /// </summary>
        public void Cancel()
        {
            try
            {
                if (_process != null)
                {
                    _process.Kill();
                }
            }
            catch (Exception ex)
            {
                NotifyMessage(ex.Message);
            }
        }
    }
}
