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
        /// Updates specific project settings. It can be used to setup value only for particular platform (or 'null' to overwrite for all of them).
        /// </summary>
        public static void SetValue(VCProject project, string ruleName, string propertyName, string platformName, string value, string valueSeparator)
        {
            if (project == null)
                throw new ArgumentNullException("project");
            if (string.IsNullOrEmpty(value))
                return;

            foreach (VCConfiguration configuration in (IVCCollection) project.Configurations)
            {
                var rulePropertyStore = configuration.Rules.Item(ruleName) as IVCRulePropertyStorage;
                if (rulePropertyStore != null)
                {
                    var currentPlatformName = ((VCPlatform) configuration.Platform).Name;
                    if (string.IsNullOrEmpty(platformName) || string.Compare(currentPlatformName, platformName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var existingDependencies = rulePropertyStore.GetUnevaluatedPropertyValue(propertyName);
                        var dependencies = string.IsNullOrEmpty(existingDependencies) ? value : string.Concat(existingDependencies, valueSeparator, value);
                        rulePropertyStore.SetPropertyValue(propertyName, dependencies);
                    }
                }
            }
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
                        var platformName = ((VCPlatform) configuration.Platform).Name;
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

        /// <summary>
        /// Gets the project's target architecture (x86 for simulator and armle-v7 for device).
        /// </summary>
        public static string GetTargetArchitecture(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            return GetValue(project, "ConfigurationGeneral", "TargetArch");
        }

        /// <summary>
        /// Tries to guess the full path to the target outcome of specified Visual C++ project.
        /// This is a complimentary method to GetTargetFullName(). As the extended BlackBerry projects
        /// are mostly based on makefiles it might be quite hard to say sure (based on Visual Studio settings only),
        /// where the target binary is created. That's why we use some non-common knowledge
        /// about the Cascades make system.
        /// </summary>
        public static string GuessTargetFullName(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            if (project.ConfigurationManager == null || project.ConfigurationManager.ActiveConfiguration == null)
                return null;

            var appType = GetValue(project, "ConfigurationGeneral", "ConfigurationAppType");
            if (string.Compare(appType, "Regular", StringComparison.OrdinalIgnoreCase) == 0
                || string.IsNullOrEmpty(appType))
            {
                return GetTargetFullName(project);
            }

            // get file name and extension:
            var targetName = GetValue(project, "ConfigurationGeneral", "TargetName");
            if (string.IsNullOrEmpty(targetName))
                return null;

            targetName += GetValue(project, "ConfigurationGeneral", "TargetExt");

            // check, whether we compile against device or simulator:
            var targetCpu = GetValue(project, "ConfigurationGeneral", "TargetArch");
            var projectDir = Path.GetDirectoryName(project.FullName);

            if (string.Compare(targetCpu, "armle-v7", StringComparison.OrdinalIgnoreCase) == 0 && !string.IsNullOrEmpty(projectDir))
            {
                var path = Path.Combine(projectDir, "arm", "o.le-v7-g", targetName);
                if (File.Exists(path))
                    return path;
                return Path.Combine(projectDir, "arm", "o.le-v7", targetName);
            }

            if (string.Compare(targetCpu, "x86", StringComparison.OrdinalIgnoreCase) == 0 && !string.IsNullOrEmpty(projectDir))
            {
                var path = Path.Combine(projectDir, "x86", "o-g", targetName);
                if (File.Exists(path))
                    return path;
                return Path.Combine(projectDir, "x86", "o", targetName);
            }

            // unsupported architecture:
            return null;
        }
    }
}
