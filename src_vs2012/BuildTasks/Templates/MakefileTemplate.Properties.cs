using System;
using System.Reflection;

namespace BlackBerry.BuildTasks.Templates
{
    partial class MakefileTemplate
    {
        #region Properties

        public string SolutionName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the current version of the current library.
        /// </summary>
        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        #endregion
    }
}
