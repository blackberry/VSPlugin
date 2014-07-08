using System;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Helpers
{
    /// <summary>
    /// Helper class providing info from DTE in simple way.
    /// </summary>
    internal static class DteHelper
    {
        /// <summary>
        /// Gets the list of VisualC++ projects within the solution.
        /// </summary>
        public static Project[] GetProjects(DTE2 dte)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            var result = new List<Project>();
            foreach (Project project in dte.Solution.Projects)
            {
                var vc = project.Object as VCProject;
                if (vc != null)
                {
                    result.Add(project);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets the output path for specified project.
        /// </summary>
        public static string GetOutputPath(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            ConfigurationManager config = project.ConfigurationManager;
            Configuration active = config.ActiveConfiguration;

            foreach (Property prop in active.Properties)
            {
                try
                {
                    if (prop.Name == "OutputPath")
                    {
                        return prop.Value.ToString();
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
