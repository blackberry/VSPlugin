using System;
using System.IO;
using System.Resources;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Helpers;
using Microsoft.Build.CPPTasks;

namespace BlackBerry.BuildTasks
{
    public abstract class BBTask : TrackedVCToolTask
    {
        internal BBTask(ResourceManager taskResources)
            : base(taskResources)
        {
            var newPath = string.Concat("PATH=", Path.Combine(ConfigDefaults.JavaHome, "bin"), ";", Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process));
            EnvironmentVariables = new[] { newPath };
        }

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        protected string DecryptPassword(string value)
        {
            return GlobalHelper.Decrypt(value);
        }
    }
}
