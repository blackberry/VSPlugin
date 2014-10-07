using System;
using System.Diagnostics;
using System.IO;

namespace BlackBerry.BuildTasks.Helpers
{
    /// <summary>
    /// Helper class for configuring processes to start.
    /// </summary>
    static class ProcessSetupHelper
    {
        /// <summary>
        /// Adds basic set of BlackBerry-build-system specific variables.
        /// </summary>
        public static void UpdateEnvironmentVariables(ProcessStartInfo startInfo, string qnxHost, string qnxTarget)
        {
            if (startInfo == null)
                throw new ArgumentNullException("startInfo");

            // set some BlackBerry default environment variables:
            // (taken from any bbndk-env_xxx.bat script)
            if (string.IsNullOrEmpty(qnxHost))
                qnxHost = Environment.GetEnvironmentVariable("QNX_HOST");
            if (string.IsNullOrEmpty(qnxTarget))
                qnxTarget = Environment.GetEnvironmentVariable("QNX_TARGET");

            var env = startInfo.EnvironmentVariables;
            if (qnxHost != null)
            {
                env["QNX_HOST"] = qnxHost.Replace('\\', '/');
                env["PATH"] = string.Concat(Path.Combine(qnxHost, "usr", "bin"), ";", env["PATH"]);
            }
            if (qnxTarget != null)
            {
                qnxTarget = qnxTarget.Replace('\\', '/');
                env["QNX_TARGET"] = qnxTarget;
                env["MAKEFLAGS"] = "-I" + qnxTarget + "/usr/include";
            }
        }
    }
}
