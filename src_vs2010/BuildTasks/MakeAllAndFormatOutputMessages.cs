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
using System.Text;
using System.Text.RegularExpressions;
using BlackBerry.NativeCore;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// Cancelable MSBuild Task for Running the Make file for the build.
    /// </summary>
    public sealed class MakeAllAndFormatOutputMessages : ICancelableTask
    {
        #region Member Variables and Constants

        private string _projectDir;
        private string _outDir;

        private static StringBuilder _stdOutput;
        private static StringBuilder _errorOutput;
        private IBuildEngine _buildEngine;
        private Process _process;
        private ITaskHost _hostObject;

        private string _numProcessors;

        #endregion

        /// <summary>
        /// Getter/Setter for the BuildEngine property
        /// </summary>
        public IBuildEngine BuildEngine
        {
            get { return _buildEngine; }
            set { _buildEngine = value; }
        }

        /// <summary>
        /// Getter/Setter for the HostObject property
        /// </summary>
        public ITaskHost HostObject
        {
            get { return _hostObject; }
            set { _hostObject = value; }
        }

        /// <summary>
        /// Getter/Setter for CompileItems property
        /// </summary>
        public ITaskItem[] CompileItems
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for the number of processors property.
        /// </summary>
        public string numProcessors
        {
            get 
            {
                _numProcessors = Environment.ProcessorCount.ToString();
                return _numProcessors; 
            }
            set { _numProcessors = value; }
        }

        /// <summary>
        /// Perform Task Execution 
        /// </summary>
        /// <returns>Return true on successful task completion</returns>
        public bool Execute()
        {
            string pCommand = "";
            string pArgs = "";
            bool mprocess = false;

            try
            {
                foreach (ITaskItem compileItem in CompileItems)
                {
                    // Get the metadata we need from this compile item.
                    mprocess = (compileItem.GetMetadata("MultiProcess") == "true");
                }

                pCommand = "cmd";
                if (mprocess)
                    pArgs = "/c " + ToolsPath + @"\make -j" + numProcessors + " all";
                else
                    pArgs = "/c " + ToolsPath + @"\make all";

                ProcessStartInfo startInfo = new ProcessStartInfo(pCommand, pArgs);

                // Set UseShellExecute to false for redirection.
                startInfo.UseShellExecute = false;

                // The following commands are needed to redirect the standard and error outputs.
                // These streams are read asynchronously using an event handler.
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                _stdOutput = new StringBuilder();
                _errorOutput = new StringBuilder();

                // Setting the work directory.
                string rootedOutDir = Path.IsPathRooted(OutDir) ? OutDir : ProjectDir + OutDir;
                startInfo.WorkingDirectory = rootedOutDir;

                // Do not create the black window.
                startInfo.CreateNoWindow = true;

                // Create a process and assign its ProcessStartInfo
                _process = new Process();
                _process.StartInfo = startInfo;

                // Set ours events handlers to asynchronously read the standard and error outputs.
                _process.OutputDataReceived += StdOutputHandler;
                _process.ErrorDataReceived += ErrorOutputHandler;

                // Start the process
                _process.Start();

                // Start the asynchronous read of the standard and error output stream.
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                do
                {
                    // Wait until finished
                    // This code is correct, don't remove it. WaitForExit freezes the IDE while waiting for the end of the build process.
                    // With this loop and the time out, the IDE won't be frozen enabling the build process to be cancelled.
                }
                while (!_process.WaitForExit(1000));

            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute make all. Reason: " + e.Message);
                return false;
            }

            if (_errorOutput.ToString().IndexOf(": error:") == -1)
            {
                int pos = _errorOutput.ToString().LastIndexOf("\\make:");
                if (pos == -1)
                    return true;

                pos = _errorOutput.ToString().IndexOf("Error ", pos);
                if (pos == -1)
                    return true;
            }

            return false;

        }

        /// <summary>
        /// Standard Output Event Handler
        /// </summary>
        /// <param name="sendingProcess">Sending Process</param>
        /// <param name="outLine">Output Text</param>
        private void StdOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                string outputText = outLine.Data;

                int tilde = outputText.IndexOf("~", StringComparison.InvariantCulture);
                while (tilde != -1)
                {
                    int end = outputText.IndexOf(':', tilde);
                    int end1 = outputText.IndexOf(' ', tilde);
                    int end2 = outputText.IndexOf('\n', tilde);
                    if ((end == -1) && (end1 == -1) && (end2 == -1))
                        break;
                    if (end == -1)
                        end = outputText.Length - 1;
                    if (end1 == -1)
                        end1 = outputText.Length - 1;
                    if (end2 == -1)
                        end2 = outputText.Length - 1;
                    if (end > end1)
                        end = end1;
                    if (end > end2)
                        end = end2;
                    int begin = outputText.LastIndexOf('\n', tilde) + 1;
                    int begin1 = outputText.LastIndexOf(' ', tilde) + 1;
                    if (begin < begin1)
                        begin = begin1;
                    string shortPathName = outputText.Substring(begin, end - begin);
                    var longPathName = NativeMethods.GetLongPathName(shortPathName);
                    if (string.IsNullOrEmpty(longPathName))
                    {
                        int sep = shortPathName.LastIndexOf('/');
                        int sep2 = shortPathName.LastIndexOf('\\');
                        if (sep2 > sep)
                            sep = sep2;
                        if (sep != -1)
                        {
                            shortPathName = shortPathName.Remove(sep + 1);
                            longPathName = NativeMethods.GetLongPathName(shortPathName);
                        }
                    }
                    if (!string.IsNullOrEmpty(longPathName))
                        outputText = outputText.Replace(shortPathName, longPathName);

                    tilde = outputText.IndexOf("~", tilde + 1, StringComparison.InvariantCulture);
                }

                NotifyMessage(new BuildMessageEventArgs(outputText, "", "", MessageImportance.High));
            }
        }

        /// <summary>
        /// Error Output Event Handler
        /// </summary>
        /// <param name="sendingProcess">Sending process</param>
        /// <param name="outLine">Error Message</param>
        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                string outputText = outLine.Data;
                int pos = outputText.IndexOf(": warning:");
                if (pos == -1)
                    pos = outputText.IndexOf(": note:");
                int diff = 10;
                int tilde;
                while (pos != -1)
                {
                    if (outputText[pos - 1] != ')')
                    {
                        int end = pos;
                        int begin = outputText.LastIndexOf('\n', end) + 1;
                        string oldPath = outputText.Substring(begin, end - begin + 1);
                        string newPath = oldPath;
                        newPath = newPath.Insert(newPath.Length - 1, ")");
                        end = newPath.LastIndexOf(":", newPath.Length - 2);
                        if (end != -1)
                        {
                            newPath = newPath.Remove(end, 1);
                            newPath = newPath.Insert(end, ",");
                            end = newPath.LastIndexOf(":", end - 1);
                            if (end != -1)
                            {
                                if (end > 5)
                                {
                                    newPath = newPath.Remove(end, 1);
                                    newPath = newPath.Insert(end, "(");
                                }
                                else
                                {
                                    end = newPath.IndexOf(",", end);
                                    newPath = newPath.Remove(end, 1);
                                    newPath = newPath.Insert(end, "(");
                                }

                                tilde = newPath.IndexOf("~");
                                if (tilde != -1)
                                {
                                    string shortPathName = newPath.Substring(0, end);
                                    var longPathName = NativeMethods.GetLongPathName(shortPathName);
                                    newPath = longPathName + newPath.Substring(end);
                                }

                                diff += (newPath.Length - oldPath.Length);
                                outputText = outputText.Replace(oldPath, newPath);
                            }
                        }
                    }
                    pos = outputText.IndexOf(": warning:", pos + diff);
                }

                pos = outputText.IndexOf(": error:");
                diff = 8;
                while (pos != -1)
                {
                    if (outputText[pos - 1] != ')')
                    {
                        int end = pos;
                        int begin = outputText.LastIndexOf('\n', end) + 1;
                        string oldPath = outputText.Substring(begin, end - begin + 1);
                        string newPath = oldPath;
                        newPath = newPath.Insert(newPath.Length - 1, ")");
                        end = newPath.LastIndexOf(":", newPath.Length - 2);
                        if (end != -1)
                        {
                            newPath = newPath.Remove(end, 1);
                            newPath = newPath.Insert(end, ",");
                            end = newPath.LastIndexOf(":", end - 1);
                            if (end != -1)
                            {
                                if (end > 5)
                                {
                                    newPath = newPath.Remove(end, 1);
                                    newPath = newPath.Insert(end, "(");
                                }
                                else
                                {
                                    end = newPath.IndexOf(",", end);
                                    newPath = newPath.Remove(end, 1);
                                    newPath = newPath.Insert(end, "(");
                                }

                                tilde = newPath.IndexOf("~");
                                if (tilde != -1)
                                {
                                    string shortPathName = newPath.Substring(0, end);
                                    var longPathName = NativeMethods.GetLongPathName(shortPathName);
                                    newPath = longPathName + newPath.Substring(end);
                                }

                                diff += (newPath.Length - oldPath.Length);
                                outputText = outputText.Replace(oldPath, newPath);
                            }
                        }
                    }
                    pos = outputText.IndexOf(": error:", pos + diff);
                }

                tilde = outputText.IndexOf("~");
                while (tilde != -1)
                {
                    int end = outputText.IndexOf(':', tilde);
                    int end1 = outputText.IndexOf(' ', tilde);
                    int end2 = outputText.IndexOf('\n', tilde);
                    if ((end == -1) && (end1 == -1) && (end2 == -1))
                        break;
                    if (end == -1)
                        end = outputText.Length - 1;
                    if (end1 == -1)
                        end1 = outputText.Length - 1;
                    if (end2 == -1)
                        end2 = outputText.Length - 1;
                    if (end > end1)
                        end = end1;
                    if (end > end2)
                        end = end2;
                    int begin = outputText.LastIndexOf('\n', tilde) + 1;
                    int begin1 = outputText.LastIndexOf(' ', tilde) + 1;
                    if (begin < begin1)
                        begin = begin1;
                    string shortPathName = outputText.Substring(begin, end - begin);
                    var longPathName = NativeMethods.GetLongPathName(shortPathName);
                    if (string.IsNullOrEmpty(longPathName))
                    {
                        int sep = shortPathName.LastIndexOf('/');
                        int sep2 = shortPathName.LastIndexOf('\\');
                        if (sep2 > sep)
                            sep = sep2;
                        if (sep != -1)
                        {
                            shortPathName = shortPathName.Remove(sep + 1);
                            longPathName = NativeMethods.GetLongPathName(shortPathName);
                        }
                    }
                    if (!string.IsNullOrEmpty(longPathName))
                        outputText = outputText.Replace(shortPathName, longPathName);

                    tilde = outputText.IndexOf("~", tilde + 1);
                }

                /// Send build output to the Output window and to the ErrorList window
                if (outputText.IndexOf(": error:") > 0)
                {
                    string fileName = outputText.Substring(0, outputText.IndexOf(": error:"));
                    int lineNum = 0;

                    int ini = fileName.IndexOf('(') + 1;
                    int end = fileName.IndexOf(')');

                    if (fileName.IndexOf(',', ini) > 0)
                    {
                        if (fileName.IndexOf(',', ini) < end)
                        {
                            end = fileName.IndexOf(',', ini);
                        }
                    }
                    end = end - fileName.IndexOf('(') - 1;

                    try
                    {
                        lineNum = Convert.ToInt16(fileName.Substring(ini, end));
                    }
                    catch
                    {
                        lineNum = 0;
                    }
                    
                    int colNum = 0;
                    string messageErr = outputText.Substring(outputText.IndexOf(": error:") + 9);
                    fileName = fileName.Substring(0, fileName.IndexOf('('));
                    NotifyError(new BuildErrorEventArgs("", "", fileName, lineNum, colNum, 0, 0, messageErr, "", ""));
                }
                else if (outputText.IndexOf(": warning:") > 0)
                {
                    string fileName = outputText.Substring(0, outputText.IndexOf(": warning:"));
                    int lineNum = 0;

                    int ini = fileName.IndexOf('(') + 1;
                    int end = fileName.IndexOf(')');
                    if (fileName.IndexOf(',', ini) > 0)
                    {
                        if (fileName.IndexOf(',', ini) < end)
                        {
                            end = fileName.IndexOf(',', ini);
                        }
                    }
                    end = end - fileName.IndexOf('(') - 1;

                    try
                    {
                        lineNum = Convert.ToInt16(fileName.Substring(ini, end));
                    }
                    catch
                    {
                        lineNum = 0;
                    }

                    int colNum = 0; 
                    string messageErr = outputText.Substring(outputText.IndexOf(": warning:") + 11);
                    fileName = fileName.Substring(0, fileName.IndexOf('('));
                    NotifyWarning(new BuildWarningEventArgs("", "", fileName, lineNum, colNum, 0, 0, messageErr, "", ""));
                }
                else if (outputText.Contains("undefined reference"))
                {
                    Regex re = new Regex(@":\d+:");
                    Match m = re.Match(outputText);
                    string fileName = "";
                    int lineNum = 0;
                    string message = "";

                    if (m.Success)
                    {
                        fileName = outputText.Substring(0, m.Index);
                        try
                        {
                            lineNum = Convert.ToInt16(m.Value.Trim(':'));
                        }
                        catch
                        {
                            lineNum = 0;
                        }
                        message = outputText.Substring(m.Index + m.Length).Trim();
                    }
                    else
                    {
                        lineNum = 0;
                        fileName = outputText.Substring(0, outputText.IndexOf(':'));
                        message = outputText.Substring(outputText.LastIndexOf(':') + 1).Trim();
                    }

                    NotifyError(new BuildErrorEventArgs("", "", fileName, lineNum, 1, 0, 0, message, "", ""));
                }
                else
                {
                    NotifyMessage(new BuildMessageEventArgs(outputText, "", "", MessageImportance.High));
                }

                // Add the text to the collected output. 
                _errorOutput.Append(Environment.NewLine + outputText);
            }
        }

        private void NotifyMessage(BuildMessageEventArgs e)
        {
            if (e != null)
            {
                try
                {
                    if (BuildEngine != null)
                    {
                        BuildEngine.LogMessageEvent(e);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Log");
                }
            }
        }

        private void NotifyWarning(BuildWarningEventArgs e)
        {
            if (e != null)
            {
                try
                {
                    if (BuildEngine != null)
                    {
                        BuildEngine.LogWarningEvent(e);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Warning");
                }
            }
        }

        private void NotifyError(BuildErrorEventArgs e)
        {
            if (e != null)
            {
                try
                {
                    if (BuildEngine != null)
                    {
                        BuildEngine.LogErrorEvent(e);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Error");
                }
            }
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
        /// Perform Cancel Code
        /// </summary>
        public void Cancel()
        {
            try
            {
                _process.Kill();
            }
            catch (Exception ex)
            {
                NotifyMessage(new BuildMessageEventArgs(ex.Message, "", "", MessageImportance.High));
            }
        }
    }
}
