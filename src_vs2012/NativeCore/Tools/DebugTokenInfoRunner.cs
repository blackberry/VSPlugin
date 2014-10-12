using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to get debug-token information.
    /// </summary>
    public sealed class DebugTokenInfoRunner : BBToolRunner
    {
        private string _location;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="debugTokenLocation">File name and directory of the debug-token bar file</param>
        public DebugTokenInfoRunner(string debugTokenLocation)
        {
            if (string.IsNullOrEmpty(debugTokenLocation))
                throw new ArgumentNullException("debugTokenLocation");

            DebugTokenLocation = debugTokenLocation;
        }

        #region Properties

        /// <summary>
        /// Gets the info about debug-token.
        /// </summary>
        public DebugTokenInfo DebugToken
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the full name with location of the debugtoken.bar file.
        /// </summary>
        public string DebugTokenLocation
        {
            get { return _location; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _location = value;
                    Arguments = string.Format(@"/C blackberry-airpackager.bat -listManifest ""{0}""", System.Environment.ExpandEnvironmentVariables(value));
                }
            }
        }

        #endregion

        protected override void ConsumeResults(string output, string error)
        {
            DebugToken = string.IsNullOrEmpty(error) ? DebugTokenInfo.Parse(output) : null;
        }
    }
}
