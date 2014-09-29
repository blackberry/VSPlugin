using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Helpers
{
    /// <summary>
    /// Helper class for accessing Visual C++ project properties.
    /// </summary>
    internal static class ProjectHelper
    {
        /// <summary>
        /// Gets the project out of any selected item inside Solution Explorer of Visual Studio.
        /// </summary>
        public static Project GetProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy");

            object obj;
            Project project = null;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj)) && obj != null)
            {
                project = obj as Project;
            }
            return project;
        }

        /// <summary>
        /// Gets the evaluated value of specific Visual C++ project property.
        /// </summary>
        public static string GetValue(Project project, string rule, string propertyName)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            if (project.ConfigurationManager == null || project.ConfigurationManager.ActiveConfiguration == null)
                return null;

            var activeConfiguration = project.ConfigurationManager.ActiveConfiguration;
            var currentConfigurationName = activeConfiguration.ConfigurationName;
            var currentPlatformName = activeConfiguration.PlatformName;
            var cppProject = project.Object as VCProject;

            if (cppProject != null)
            {
                foreach (VCConfiguration configuration in (IVCCollection) cppProject.Configurations)
                {
                    if (string.Compare(configuration.ConfigurationName, currentConfigurationName, StringComparison.InvariantCulture) == 0)
                    {
                        var platformName = configuration.GetEvaluatedPropertyValue("PlatformName");
                        if (string.Compare(platformName, currentPlatformName, StringComparison.InvariantCulture) == 0)
                        {
                            var rulePropertyStorage = configuration.Rules.Item(rule) as IVCRulePropertyStorage;
                            if (rulePropertyStorage != null)
                            {
                                var value = rulePropertyStorage.GetEvaluatedPropertyValue(propertyName);
                                return value;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the full path to the target outcome of specified Visual C++ project.
        /// </summary>
        public static string GetTargetFullName(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            var targetName = GetValue(project, "ConfigurationGeneral", "TargetName");
            if (string.IsNullOrEmpty(targetName))
                return null;

            targetName += GetValue(project, "ConfigurationGeneral", "TargetExt");

            // The output folder can be anything, let's assume it's any of these patterns:
            // 1) "\\server\folder"
            // 2) "drive:\folder"
            // 3) "..\..\folder"
            // 4) "folder"
            // 5) ""
            var outputPath = GetValue(project, "ConfigurationGeneral", "OutDir");

            if (string.IsNullOrEmpty(outputPath))
            {
                // 5) ""
                var projectFolder = Path.GetDirectoryName(project.FullName);
                if (string.IsNullOrEmpty(projectFolder))
                    return targetName;
                return Path.Combine(projectFolder, targetName);
            }

            if (outputPath.Length >= 2 && outputPath[0] == Path.DirectorySeparatorChar && outputPath[1] == Path.DirectorySeparatorChar)
            {
                // 1) "\\server\folder"
                return Path.Combine(outputPath, targetName);
            }

            if (outputPath.Length >= 3 && outputPath[1] == Path.VolumeSeparatorChar && outputPath[2] == Path.DirectorySeparatorChar)
            {
                // 2) "drive:\folder"
                return Path.Combine(outputPath, targetName);
            }

            if (outputPath.StartsWith("..\\") || outputPath.StartsWith("../"))
            {
                // 3) "..\..\folder"

                var projectFolder = Path.GetDirectoryName(project.FullName);
                while (outputPath.StartsWith("..\\") || outputPath.StartsWith("../"))
                {
                    outputPath = outputPath.Substring(3);
                    if (!string.IsNullOrEmpty(projectFolder))
                    {
                        projectFolder = Path.GetDirectoryName(projectFolder);
                    }
                }

                if (string.IsNullOrEmpty(projectFolder))
                    return Path.Combine(outputPath, targetName);
                return Path.Combine(projectFolder, outputPath, targetName);
            }

            // 4) "folder"
            var folder = Path.GetDirectoryName(project.FullName);
            if (string.IsNullOrEmpty(folder))
                return Path.Combine(outputPath, targetName);
            return Path.Combine(folder, outputPath, targetName);
        }
    }
}
