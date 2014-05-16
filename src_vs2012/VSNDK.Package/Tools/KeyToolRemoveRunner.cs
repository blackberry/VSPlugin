using System;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to remove instaled pair of signing keys.
    /// </summary>
    internal sealed class KeyToolRemoveRunner : ToolRunner
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        public KeyToolRemoveRunner(string workingDirectory)
            : base("cmd.exe", workingDirectory)
        {
            Arguments = @"/C blackberry-signer.bat -cskdelete";
        }

        protected override void ConsumeResults(string output, string error)
        {
            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // check, if there is any runtime error message:
                foreach (var line in lines)
                {
                    if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LastError = line.Substring(6).Trim();
                        break;
                    }
                }
            }
        }
    }
}
