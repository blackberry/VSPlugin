using System;
using System.IO;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Specific base-class for 
    /// </summary>
    public class BBToolRunner : ToolRunner
    {
        /// <summary>
        /// Default constructor. It sets 'ToolsDirectory' as the default working directory.
        /// </summary>
        protected BBToolRunner()
            : base("cmd.exe", ConfigDefaults.ToolsDirectory)
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        protected BBToolRunner(string workingDirectory)
            : base("cmd.exe", workingDirectory)
        {
        }

        protected override void PrepareStartup()
        {
            base.PrepareStartup();

            // make sure proper Java installation for BB NDK plugin is used:
            if (!string.IsNullOrEmpty(Environment["PATH"]) && !Environment["PATH"].StartsWith(Path.Combine(ConfigDefaults.JavaHome, "bin"), StringComparison.OrdinalIgnoreCase))
            {
                Environment["PATH"] = string.Concat(Path.Combine(ConfigDefaults.JavaHome, "bin"), ";", Environment["PATH"]);
            }
        }
    }
}
