using System;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to gets debug-token information.
    /// </summary>
    internal sealed class DebugTokenInfoRunner : ToolRunner
    {
        private string _location;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="debugTokenLocation">File name and directory of the debug-token bar file</param>
        public DebugTokenInfoRunner(string workingDirectory, string debugTokenLocation)
        {
            if (string.IsNullOrEmpty(debugTokenLocation))
                throw new ArgumentNullException("debugTokenLocation");

            FileName = "cmd.exe";
            WorkingDirectory = workingDirectory;
            DebugTokenLocation = debugTokenLocation;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="debugTokenLocation">File name and directory of the debug-token bar file</param>
        public DebugTokenInfoRunner(string debugTokenLocation)
            : this(null, debugTokenLocation)
        {
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
                    Arguments = string.Format(@"/C blackberry-airpackager.bat -listManifest ""{0}""", Environment.ExpandEnvironmentVariables(value));
                }
            }
        }

        #endregion

        protected override void ConsumeResults(string output, string error)
        {
            if (string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error))
            {
                DebugToken = null;
            }
            else
            {
                DebugToken = DebugTokenInfo.Parse(output);
            }
        }
    }
}
