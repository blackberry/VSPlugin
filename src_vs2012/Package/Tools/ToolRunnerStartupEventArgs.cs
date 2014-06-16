using System;
using System.Collections.Specialized;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Arguments passed along with Startup event of the ToolRunner class.
    /// </summary>
    internal sealed class ToolRunnerStartupEventArgs : EventArgs
    {
        private readonly ToolRunner _runner;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ToolRunnerStartupEventArgs(ToolRunner runner)
        {
            if (runner == null)
                throw new ArgumentNullException("runner");

            _runner = runner;
        }

        #region Properties

        /// <summary>
        /// Gets or set the working directory of the runner.
        /// </summary>
        public string WorkingDirectory
        {
            get { return _runner.WorkingDirectory; }
            set { _runner.WorkingDirectory = value; }
        }

        /// <summary>
        /// Gets the reference to environment variables.
        /// </summary>
        public StringDictionary Environment
        {
            get { return _runner.Environment; }
        }

        #endregion

        /// <summary>
        /// Indexer to access and update environment variables.
        /// </summary>
        public string this[string key]
        {
            get { return _runner.Environment[key]; }
            set { _runner.Environment[key] = value; }
        }
    }
}
