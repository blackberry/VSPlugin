using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public static void Update(StringDictionary environment, string qnxHost, string qnxTarget)
        {
            if (environment == null)
                throw new ArgumentNullException("environment");

            // set some BlackBerry default environment variables:
            // (idea taken from any bbndk-env_xxx.bat script)
            if (string.IsNullOrEmpty(qnxHost))
                qnxHost = Environment.GetEnvironmentVariable("QNX_HOST");
            if (string.IsNullOrEmpty(qnxTarget))
                qnxTarget = Environment.GetEnvironmentVariable("QNX_TARGET");

            if (qnxHost != null)
            {
                environment["QNX_HOST"] = qnxHost.Replace('\\', '/');
                environment["PATH"] = string.Concat(Path.Combine(qnxHost, "usr", "bin"), ";", environment["PATH"]);
            }
            if (qnxTarget != null)
            {
                qnxTarget = qnxTarget.Replace('\\', '/');
                environment["QNX_TARGET"] = qnxTarget;
                environment["MAKEFLAGS"] = "-I" + qnxTarget + "/usr/include";
            }
        }

        /// <summary>
        /// Adds basic set of BlackBerry-build-system specific variables.
        /// </summary>
        public static string[] Update(string[] environment, string qnxHost, string qnxTarget)
        {
            var result = new List<string>();

            // set some BlackBerry default environment variables:
            // (idea taken from any bbndk-env_xxx.bat script)

            string path = null;

            if (environment != null)
            {
                foreach (var env in environment)
                {
                    if (env.StartsWith("PATH=", StringComparison.OrdinalIgnoreCase))
                    {
                        path = env.Substring(5);
                        continue;
                    }

                    if (env.StartsWith("QNX_HOST=", StringComparison.OrdinalIgnoreCase))
                    {
                        qnxHost = env.Substring(9);
                        continue;
                    }

                    if (env.StartsWith("QNX_TARGET=", StringComparison.OrdinalIgnoreCase))
                    {
                        qnxTarget = env.Substring(11);
                        continue;
                    }

                    if (env.StartsWith("MAKEFLAGS=", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    result.Add(env);
                }
            }

            if (string.IsNullOrEmpty(qnxHost))
                qnxHost = Environment.GetEnvironmentVariable("QNX_HOST");
            if (string.IsNullOrEmpty(qnxTarget))
                qnxTarget = Environment.GetEnvironmentVariable("QNX_TARGET");

            if (!string.IsNullOrEmpty(qnxHost))
            {
                result.Add("QNX_HOST=" + qnxHost.Replace('\\', '/'));

                if (string.IsNullOrEmpty(path))
                    path = Environment.GetEnvironmentVariable("PATH");
                path = string.Concat(Path.Combine(qnxHost, "usr", "bin"), ";", path);
            }

            if (!string.IsNullOrEmpty(qnxTarget))
            {
                qnxTarget = qnxTarget.Replace('\\', '/');
                result.Add("QNX_TARGET=" + qnxTarget);
                result.Add("MAKEFLAGS=-I" + qnxTarget + "/usr/include");
            }

            if (!string.IsNullOrEmpty(path))
            {
                result.Add("PATH=" + path);
            }

            return result.ToArray();
        }
    }
}
