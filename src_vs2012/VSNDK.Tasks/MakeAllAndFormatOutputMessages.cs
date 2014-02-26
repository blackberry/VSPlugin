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
using EnvDTE;
using EnvDTE80;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Diagnostics;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Text.RegularExpressions;

namespace VSNDK.Tasks
{
    /// <summary>
    /// Cancelabe MSBuild Task for Running the Make file for the build.
    /// </summary>
    public class MakeAllAndFormatOutputMessages : ICancelableTask
    {
        #region Member Variables and Constants

        private string _projectDir;
        private string _intDir;
        private string _outDir;

        private static StringBuilder stdOutput = null;
        private static StringBuilder errorOutput = null;
        private IBuildEngine buildEngine;
        private System.Diagnostics.Process proc = null;
        private ITaskHost hostObject;

        private string _numProcessors;

        #endregion

        /// <summary>
        /// Getter/Setter for the BuildEngine property
        /// </summary>
        public IBuildEngine BuildEngine
        {
            get { return buildEngine; }
            set { buildEngine = value; }
        }

        /// <summary>
        /// Getter/Setter for the HostObject property
        /// </summary>
        public ITaskHost HostObject
        {
            get { return hostObject; }
            set { hostObject = value; }
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

                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(pCommand, pArgs);

                // Set UseShellExecute to false for redirection.
                procStartInfo.UseShellExecute = false;

                // The following commands are needed to redirect the standard and error outputs.
                // These streams are read asynchronously using an event handler.
                procStartInfo.RedirectStandardOutput = true;
                stdOutput = new StringBuilder("");
                procStartInfo.RedirectStandardError = true;
                errorOutput = new StringBuilder("");

                // Setting the work directory.
                string rootedOutDir = (Path.IsPathRooted(OutDir)) ? OutDir : ProjectDir + OutDir;
                procStartInfo.WorkingDirectory = rootedOutDir;

                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;

                // Create a process and assign its ProcessStartInfo
                proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;

                // Set ours events handlers to asynchronously read the standard and error outputs.
                proc.OutputDataReceived += new DataReceivedEventHandler(StdOutputHandler);
                proc.ErrorDataReceived += new DataReceivedEventHandler(ErrorOutputHandler);

                // Start the process
                proc.Start();

                // Start the asynchronous read of the standard and error output stream.
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                do
                {
                    // Wait until finished
                    // This code is correct, don't remove it. WaitForExit freezes the IDE while waiting for the end of the build process.
                    // With this loop and the time out, the IDE won't be frozen enabling the build process to be cancelled.
                }
                while (!proc.WaitForExit(1000));

            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to execute make all. Reason: " + e.Message);
                return false;
            }

            if (errorOutput.ToString().IndexOf(": error:") == -1)
            {
                int pos = errorOutput.ToString().LastIndexOf("\\make:");
                if (pos == -1)
                    return true;
                else
                {
                    pos = errorOutput.ToString().IndexOf("Error ", pos);
                    if (pos == -1)
                        return true;
                }
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
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                string outputText = outLine.Data;

                int tilde = outputText.IndexOf("~");
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
                    StringBuilder longPathName = new StringBuilder(1024);
                    string shortPathName = outputText.Substring(begin, end - begin);
                    GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                    if (longPathName.ToString() == "")
                    {
                        int sep = shortPathName.LastIndexOf('/');
                        int sep2 = shortPathName.LastIndexOf('\\');
                        if (sep2 > sep)
                            sep = sep2;
                        if (sep != -1)
                        {
                            shortPathName = shortPathName.Remove(sep + 1);
                            GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                        }
                    }
                    if (longPathName.ToString() != "")
                        outputText = outputText.Replace(shortPathName, longPathName.ToString());

                    tilde = outputText.IndexOf("~", tilde + 1);
                }


                BuildMessageEventArgs taskEvent = new BuildMessageEventArgs(outputText, "", "", MessageImportance.High);
                BuildEngine.LogMessageEvent(taskEvent);

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
                                    StringBuilder longPathName = new StringBuilder(1024);
                                    string shortPathName = newPath.Substring(0, end);
                                    GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                                    newPath = longPathName.ToString() + newPath.Substring(end);
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
                                    StringBuilder longPathName = new StringBuilder(1024);
                                    string shortPathName = newPath.Substring(0, end);
                                    GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                                    newPath = longPathName.ToString() + newPath.Substring(end);
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
                    StringBuilder longPathName = new StringBuilder(1024);
                    string shortPathName = outputText.Substring(begin, end - begin);
                    GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                    if (longPathName.ToString() == "")
                    {
                        int sep = shortPathName.LastIndexOf('/');
                        int sep2 = shortPathName.LastIndexOf('\\');
                        if (sep2 > sep)
                            sep = sep2;
                        if (sep != -1)
                        {
                            shortPathName = shortPathName.Remove(sep + 1);
                            GetLongPathName(shortPathName, longPathName, longPathName.Capacity);
                        }
                    }
                    if (longPathName.ToString() != "")
                        outputText = outputText.Replace(shortPathName, longPathName.ToString());

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
                    BuildErrorEventArgs errorEvent = new BuildErrorEventArgs("", "", fileName, lineNum, colNum, 0, 0, messageErr, "", "");
                    BuildEngine.LogErrorEvent(errorEvent);
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
                    BuildWarningEventArgs warningEvent = new BuildWarningEventArgs("", "", fileName, lineNum, colNum, 0, 0, messageErr, "", "");
                    BuildEngine.LogWarningEvent(warningEvent);
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

                    BuildErrorEventArgs errorEvent = new BuildErrorEventArgs("", "", fileName, lineNum, 1, 0, 0, message, "", "");
                    BuildEngine.LogErrorEvent(errorEvent);
                }
                else
                {
                    BuildMessageEventArgs taskEvent = new BuildMessageEventArgs(outputText, "", "", MessageImportance.High);
                    BuildEngine.LogMessageEvent(taskEvent);
                }

                // Add the text to the collected output. 
                errorOutput.Append(Environment.NewLine + outputText);
            }
        }


        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the long path name for a given short one. </summary>
        /// <param name="path">Short path name. </param>
        /// <param name="longPath">Returns this long path name. </param>
        /// <param name="longPathLength"> Lenght of this long path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength
            );

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
                proc.Kill();
            }
            catch (Exception ex)
            {
                BuildMessageEventArgs taskEvent2 = new BuildMessageEventArgs(ex.Message, "", "", MessageImportance.High);
                BuildEngine.LogMessageEvent(taskEvent2);
            }
            
        }
    }
}
