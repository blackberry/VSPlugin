namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to remove instaled pair of signing keys.
    /// </summary>
    public sealed class KeyToolRemoveRunner : BBToolRunner
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public KeyToolRemoveRunner()
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
