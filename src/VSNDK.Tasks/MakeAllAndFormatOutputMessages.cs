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
    public class MakeAllAndFormatOutputMessages : ICancelableTask
    {
        private static StringBuilder stdOutput = null;
        private static StringBuilder errorOutput = null;
        private IBuildEngine buildEngine;
        private System.Diagnostics.Process proc = null;

        public IBuildEngine BuildEngine
        {
            get { return buildEngine; }
            set { buildEngine = value; }
        }
        private ITaskHost hostObject;
        public ITaskHost HostObject
        {
            get { return hostObject; }
            set { hostObject = value; }
        }

        /// <summary>
        /// Perform Task Execution 
        /// </summary>
        /// <returns>Return true on successful task completion</returns>
        public bool Execute()
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + ToolsPath + @"\make all");

                // Set UseShellExecute to false for redirection.
                procStartInfo.UseShellExecute = false;

                // The following commands are needed to redirect the standard and error outputs.
                // These streams are read asynchronously using an event handler.
                procStartInfo.RedirectStandardOutput = true;
                stdOutput = new StringBuilder("");
                procStartInfo.RedirectStandardError = true;
                errorOutput = new StringBuilder("");

                // Setting the work directory.
                ExePath = ExePath.Remove(ExePath.LastIndexOf('\\'));
                procStartInfo.WorkingDirectory = ExePath;

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
                    int lineNum = Convert.ToInt16(fileName.Substring(ini, end));
                    int colNum = 0;// Convert.ToInt16(fileName.Substring(fileName.IndexOf(',') + 1, fileName.IndexOf(')') - fileName.IndexOf(',') - 1));
                    string messageErr = outputText.Substring(outputText.IndexOf(": error:") + 9);
                    fileName = fileName.Substring(0, fileName.IndexOf('('));
                    BuildErrorEventArgs errorEvent = new BuildErrorEventArgs("", "", fileName, lineNum, colNum, 0, 0, messageErr, "", "");
                    BuildEngine.LogErrorEvent(errorEvent);
                }
                else if (outputText.IndexOf(": warning:") > 0)
                {
                    string fileName = outputText.Substring(0, outputText.IndexOf(": warning:"));
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
                    int lineNum = Convert.ToInt16(fileName.Substring(ini, end));
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
                        lineNum = Convert.ToInt16(m.Value.Trim(':'));
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength
            );

        [Required]
        public string ExePath
        {
            get;
            set;
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
