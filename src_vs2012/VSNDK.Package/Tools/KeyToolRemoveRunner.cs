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
                LastError = ExtractErrorMessages(output);
            }
        }
    }
}
