using System;
using System.Collections;
using System.IO;
using System.Resources;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Helpers;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// Base class for all BlackBerry-related MSBuild tasks.
    /// </summary>
    public abstract class BBTask : TrackedVCToolTask
    {
        private readonly ArrayList _switchOrderList;

        private const string TRACKER_LOG_DIRECTORY = "TrackerLogDirectory";

        /// <summary>
        /// Init constructor.
        /// </summary>
        internal BBTask(ResourceManager taskResources)
            : base(taskResources)
        {
            var newPath = string.Concat("PATH=", Path.Combine(ConfigDefaults.JavaHome, "bin"), ";", Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process));
            EnvironmentVariables = new[] { newPath };

            _switchOrderList = new ArrayList();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the directory for logs.
        /// </summary>
        public string TrackerLogDirectory
        {
            get { return GetSwitchAsString(TRACKER_LOG_DIRECTORY); }
            set
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Directory)
                {
                    Name = TRACKER_LOG_DIRECTORY,
                    DisplayName = "Tracker Log Directory",
                    Description = "Tracker Log Directory.",
                    Value = EnsureTrailingSlash(value)
                };
                SetSwitch(toolSwitch);
            }
        }

        #endregion

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        protected string DecryptPassword(string value)
        {
            return GlobalHelper.Decrypt(value);
        }

        /// <summary>
        /// Defines the order of switches appearing in generated command-line.
        /// </summary>
        protected void DefineSwitchOrder(params string[] switchNames)
        {
            if (switchNames != null)
            {
                _switchOrderList.Clear();
                foreach (var name in switchNames)
                {
                    _switchOrderList.Add(name);
                }

                _switchOrderList.Add(TRACKER_LOG_DIRECTORY);
            }
        }

        /// <summary>
        /// Removes specified switches from the order list to appear in generated command-line.
        /// </summary>
        protected void RemoveFromSwithOrder(params string[] switchNames)
        {
            if (switchNames != null)
            {
                foreach (var name in switchNames)
                {
                    _switchOrderList.Remove(name);
                }
            }
        }

        /// <summary>
        /// Getter for the SwitchOrderList property
        /// </summary>
        protected override ArrayList SwitchOrderList
        {
            get { return _switchOrderList; }
        }

        /// <summary>
        /// Getter for the TrackerIntermediateDirectory property
        /// </summary>
        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the values for generated command-line switch.
        /// </summary>
        protected void SetSwitch(ToolSwitch toolSwitch)
        {
            if (toolSwitch == null)
                throw new ArgumentNullException("toolSwitch");
            if (string.IsNullOrEmpty(toolSwitch.Name))
                throw new ArgumentOutOfRangeException("toolSwitch");

            // remove the switch, in case it was already defined:
            ActiveToolSwitches.Remove(toolSwitch.Name);
            RemoveSwitchToolBasedOnValue(toolSwitch.Name);

            // and add new value:
            ActiveToolSwitches.Add(toolSwitch.Name, toolSwitch);
            AddActiveSwitchToolValue(toolSwitch);
        }

        /// <summary>
        /// Gets the string value of existing tool-switch or null.
        /// </summary>
        protected string GetSwitchAsString(string name)
        {
            if (IsPropertySet(name))
            {
                return ActiveToolSwitches[name].Value;
            }

            return null;
        }

        /// <summary>
        /// Gets the numeric value of existing tool-switch or 0.
        /// </summary>
        protected int GetSwitchAsInt32(string name)
        {
            if (IsPropertySet(name))
            {
                return ActiveToolSwitches[name].Number;
            }

            return 0;
        }

        /// <summary>
        /// Gets the bool value of existing tool-switch or false.
        /// </summary>
        protected bool GetSwitchAsBool(string name)
        {
            return IsPropertySet(name) && ActiveToolSwitches[name].BooleanValue;
        }

        /// <summary>
        /// Gets the array value of existing tool-switch or null.
        /// </summary>
        protected ITaskItem[] GetSwitchAsItemArray(string name)
        {
            if (IsPropertySet(name))
            {
                return ActiveToolSwitches[name].TaskItemArray;
            }
            return null;
        }

        /// <summary>
        /// Reports specified error returned from BlackBerry tool (by removing leading 'Error: ' prefix).
        /// </summary>
        protected bool LogToolError(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string message = null;
            bool skipPrinting = false;

            if (text.StartsWith("Error: ", StringComparison.OrdinalIgnoreCase))
            {
                message = text.Substring(7).Trim();
                skipPrinting = true;
            }
            else
            {
                if (text.StartsWith("[ERROR] ", StringComparison.OrdinalIgnoreCase))
                {
                    message = text.Substring(8).Trim();
                    skipPrinting = true;
                }
                else
                {
                    if (text.StartsWith("result::failure ", StringComparison.OrdinalIgnoreCase))
                    {
                        // skip the error-id, if specified before message:
                        int i = text.Length > 16 && char.IsDigit(text[16]) ? text.IndexOf(' ', 16) : 15;
                        if (i > 0)
                        {
                            message = text.Substring(i + 1);
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                // add a dedicated error for the tool failure:
                BuildEngine.LogErrorEvent(new BuildErrorEventArgs(null, null, GetType().Name, 0, 0, 0, 0, message, null, GetType().Name));
                return skipPrinting;
            }

            return false;
        }

        /// <summary>
        /// Parses a single line of text to identify any errors or warnings in canonical format.
        /// </summary>
        /// <param name="singleLine">A single line of text for the method to parse.</param><param name="messageImportance">A value of <see cref="T:Microsoft.Build.Framework.MessageImportance"/> that indicates the importance level with which to log the message.</param>
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            // is it a failure result?
            // then add a dedicated error-task for it:
            if (LogToolError(singleLine))
            {
                return;
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }
    }
}
