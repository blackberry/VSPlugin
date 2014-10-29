using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Model.Wizards;
using BlackBerry.Package.ViewModels;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.Wizards
{
    /// <summary>
    /// Advanced template wizard, that will do a real magic based on parameters specified from .vsz files of BlackBerry new items and new projects.
    /// </summary>
    [ComVisible(true)]
    [Guid("0cb934ba-06c8-4919-9498-bc6517940bcd")]
    [ProgId("BlackBerry.PuppetMasterWizardEngine")]
    public sealed class PuppetMasterWizardEngine : BaseWizardEngine
    {
        private const char ProjectNumberSeparator = '#';
        private const char PlatformSeparator = '@';

        private static string _wizardDataFolder;

        /// <summary>
        /// Gets the path, where the additional data files of the wizard are located.
        /// </summary>
        public static string WizardDataFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_wizardDataFolder))
                {
                    string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Templates"));
                    _wizardDataFolder = Path.Combine(path, @"VCWizards\BlackBerry");
                }

                return _wizardDataFolder;
            }
        }

        /// <summary>
        /// Method that creates new project for existing or new solution.
        /// </summary>
        internal override wizardResult ExecuteNewProject(DTE2 dte, IntPtr owner, NewProjectParams context, KeyValuePair<string, string>[] customParams)
        {
            int count = GetProjectsCount(customParams);
            string masterProjectName = null;

            foreach (var data in customParams)
            {
                if (IsMatchingProjectKey(data.Key))
                {
                    // add the project itself:
                    var destinationPath = CreateProject(context, data.Value, masterProjectName ?? context.ProjectName, count == 1);
                    var project = dte.Solution.AddFromFile(destinationPath);

                    // PH: HINT: remember the name of the 'first' project and assume it later, that this is what the developer input in VisualStudio as project name;
                    // as it could overwrite that value by renaming rules of the template:
                    if (string.IsNullOrEmpty(masterProjectName))
                    {
                        masterProjectName = project.Name;
                    }

                    // get project identifier, in case the template wants to add more than one:
                    var projectRoot = Path.GetDirectoryName(project.FullName);
                    var tokenProcessor = CreateTokenProcessor(project.Name, projectRoot, destinationPath, masterProjectName);
                    var projectNumber = GetTag(data.Key, ProjectNumberSeparator);
                    var filtersParamName = "filters";
                    var fileParamName = "file";
                    var dependencyParamName = "dependency";
                    var defineParamName = "define";

                    if (!string.IsNullOrEmpty(projectNumber))
                    {
                        filtersParamName = string.Concat(filtersParamName, ProjectNumberSeparator, projectNumber);
                        fileParamName = string.Concat(fileParamName, ProjectNumberSeparator, projectNumber);
                        dependencyParamName = string.Concat(dependencyParamName, ProjectNumberSeparator, projectNumber);
                        defineParamName = string.Concat(defineParamName, ProjectNumberSeparator, projectNumber);
                    }

                    // add project items:
                    foreach (var subItem in customParams)
                    {
                        // file:
                        if (IsMatchingKey(subItem.Key, fileParamName))
                        {
                            bool ignore;
                            var itemName = Path.Combine(projectRoot, Path.GetFileName(GetSourceName(subItem.Value, out ignore)));
                            var itemTokenProcessor = CreateTokenProcessor(project.Name, projectRoot, GetDestinationName(itemName, subItem.Value, tokenProcessor), masterProjectName);
                            AddFile(project, itemName, subItem.Value, itemTokenProcessor);
                        }

                        // filters:
                        if (IsMatchingKey(subItem.Key, filtersParamName))
                        {
                            var vcProject = project.Object as VCProject;
                            if (vcProject != null)
                            {
                                CreateFilters(subItem.Value, vcProject);
                            }
                        }

                        // library references:
                        if (IsMatchingKey(subItem.Key, dependencyParamName))
                        {
                            var vcProject = project.Object as VCProject;
                            if (vcProject != null)
                            {
                                UpdateProjectProperty(subItem.Value, GetTag(projectNumber, PlatformSeparator), vcProject, "Link", "AdditionalDependencies", ";");
                            }
                        }

                        // defines:
                        if (IsMatchingKey(subItem.Key, defineParamName))
                        {
                            var vcProject = project.Object as VCProject;
                            if (vcProject != null)
                            {
                                // HINT: you can specify per-platform settings using '@':
                                UpdateProjectProperty(subItem.Value, GetTag(projectNumber, PlatformSeparator), vcProject, "CL", "PreprocessorDefinitions", ";");
                            }
                        }
                    }

                    project.Save();
                }
            }

            if (context.IsExclusive)
            {
                var solutionPath = string.IsNullOrEmpty(context.SolutionName) ? context.LocalDirectory : Path.GetDirectoryName(context.LocalDirectory);
                dte.Solution.SaveAs(Path.Combine(solutionPath, context.ProjectName));
            }
            return wizardResult.wizardResultSuccess;
        }

        /// <summary>
        /// Creates new project based on a given template.
        /// It also allows changing the project name on the fly using the same rules as for items transformations.
        /// </summary>
        private static string CreateProject(NewProjectParams context, string projectTemplateName, string masterProjectName, bool singleProject)
        {
            // calculate default location, where the project should be written:
            bool reserved;
            var templatePath = Path.Combine(WizardDataFolder, GetSourceName(projectTemplateName, out reserved));
            var projectRoot = context.IsExclusive || !singleProject ? context.LocalDirectory : Path.Combine(context.LocalDirectory, context.ProjectName);
            var destinationPath = Path.Combine(projectRoot, context.ProjectName + Path.GetExtension(templatePath));

            // apply the name change transformations as for ordinary files to the project:
            destinationPath = GetDestinationName(destinationPath, projectTemplateName,
                CreateTokenProcessor(context.ProjectName, Path.GetDirectoryName(destinationPath), destinationPath, masterProjectName));

            // process the tokens from the template file and generate project file:
            var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, destinationPath, masterProjectName);
            tokenProcessor.UntokenFile(templatePath, destinationPath);

            return destinationPath;
        }

        /// <summary>
        /// Updates specific project settings. It can be used to setup value only for particular platform (or 'null' to overwrite for all of them).
        /// </summary>
        private void UpdateProjectProperty(string value, string platformName, VCProject vcProject, string ruleName, string propertyName, string valueSeparator)
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (vcProject == null)
                return;

            foreach (VCConfiguration configuration in (IVCCollection) vcProject.Configurations)
            {
                var rulePropertyStore = configuration.Rules.Item(ruleName) as IVCRulePropertyStorage;
                if (rulePropertyStore != null)
                {
                    var currentPlatformName = ((VCPlatform)configuration.Platform).Name;
                    if (string.IsNullOrEmpty(platformName) || string.Compare(currentPlatformName, platformName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var existingDependencies = rulePropertyStore.GetUnevaluatedPropertyValue(propertyName);
                        var dependencies = string.IsNullOrEmpty(existingDependencies) ? value : string.Concat(existingDependencies, valueSeparator, value);
                        rulePropertyStore.SetPropertyValue(propertyName, dependencies);
                    }
                }
            }
        }

        private static void CreateFilters(string filtersDefinition, VCProject vcProject)
        {
            if (string.IsNullOrEmpty(filtersDefinition))
                return;

            foreach (var filterName in filtersDefinition.Split(';', ',', ' '))
            {
                if (string.Compare(filterName, "sources", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = vcProject.AddFilter("Source Files");
                    filter.Filter = "cpp;c;cc;cxx;def;bat";
                }

                if (string.Compare(filterName, "headers", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = vcProject.AddFilter("Header Files");
                    filter.Filter = "h;hpp;hxx;hm;inl;inc;xsd";
                }

                if (string.Compare(filterName, "src", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = vcProject.AddFilter("Source Files");
                    filter.Filter = "cpp;c;cc;cxx;def;bat;h;hpp;hxx;hm;inl;inc;xsd";
                }

                if (string.Compare(filterName, "assets", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = vcProject.AddFilter("Assets");
                    filter.Filter = "qml;js;jpg;png;gif";
                }

                if (string.Compare(filterName, "config", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    VCFilter filter = vcProject.AddFilter("Config Files");
                    filter.Filter = "pri;pro;xml";
                }
            }
        }

        private static string GetTag(string name, char separator)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var index = name.IndexOf(separator);
            return index < 0 ? null : name.Substring(index + 1).Trim();
        }

        private static int GetProjectsCount(IEnumerable<KeyValuePair<string, string>> customParams)
        {
            int count = 0;

            if (customParams != null)
            {
                foreach (var item in customParams)
                {
                    if (IsMatchingProjectKey(item.Key))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool IsMatchingKey(string key, string patternDefinition)
        {
            if (string.Compare(key, patternDefinition, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            if (key != null && key.StartsWith(patternDefinition, StringComparison.OrdinalIgnoreCase)
                && key.Length > patternDefinition.Length && key[patternDefinition.Length] == PlatformSeparator)
                return true;

            return false;
        }

        private static bool IsMatchingProjectKey(string key)
        {
            return key != null &&
                   (string.Compare(key, "project", StringComparison.OrdinalIgnoreCase) == 0 || (key.StartsWith("project", StringComparison.OrdinalIgnoreCase) && key.Length > 7 && key[7] == ProjectNumberSeparator));
        }

        /// <summary>
        /// Method that creates new project item for existing project.
        /// </summary>
        internal override wizardResult ExecuteNewProjectItem(DTE2 dte, IntPtr owner, NewProjectItemParams context, KeyValuePair<string, string>[] customParams)
        {
            var project = context.ProjectItems.ContainingProject;
            var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, context.ItemName, context.ProjectName);

            foreach (var data in customParams)
            {
                if (string.Compare(data.Key, "file", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AddFile(project, context.ItemName, data.Value, tokenProcessor);
                }
            }

            return wizardResult.wizardResultSuccess;
        }

        /// <summary>
        /// Processes item and adds it to the project.
        /// </summary>
        private void AddFile(Project project, string itemName, string templateDefinition, TokenProcessor tokenProcessor)
        {
            if (project == null)
                throw new ArgumentNullException("project");
            if (string.IsNullOrEmpty(itemName))
                throw new ArgumentNullException("itemName");
            if (string.IsNullOrEmpty(templateDefinition))
                throw new ArgumentNullException("templateDefinition");

            bool canAddToProject;
            var templatePath = Path.Combine(WizardDataFolder, GetSourceName(templateDefinition, out canAddToProject));
            var destinationPath = GetDestinationName(itemName, templateDefinition, tokenProcessor);

            // update tokens withing the file and copy to destination:
            tokenProcessor.UntokenFile(templatePath, destinationPath);

            // then add that file to the project items:
            if (canAddToProject)
            {
                project.ProjectItems.AddFromFile(destinationPath);
                TraceLog.WriteLine("Added file: \"{0}\"", destinationPath);
            }
            else
            {
                TraceLog.WriteLine("Generated file: \"{0}\", but omitted adding to project", destinationPath);
            }
        }

        /// <summary>
        /// Creates token processor to update dynamically token markers within the template file.
        /// </summary>
        public static TokenProcessor CreateTokenProcessor(string projectName, string projectRoot, string destinationName, string masterProjectName)
        {
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentNullException("projectName");
            if (string.IsNullOrEmpty(projectRoot))
                throw new ArgumentNullException("projectRoot");
            if (string.IsNullOrEmpty(destinationName))
                throw new ArgumentNullException("destinationName");

            var tokenProcessor = new TokenProcessor();
            var name = Path.GetFileNameWithoutExtension(destinationName);
            var ext = Path.GetExtension(destinationName);
            var safeName = CreateSafeName(name);
            var safeNameUpper = safeName.ToUpperInvariant();

            var author = PackageViewModel.Instance.Developer.Name ?? "Example Inc.";
            var authorID = "ABCD1234";
            var now = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var authorSafe = CreateSafeName(author).ToLowerInvariant();

            // project & file info:
            tokenProcessor.AddReplace("$projectname$", projectName);
            tokenProcessor.AddReplace("$ProjectName$", projectName);
            tokenProcessor.AddReplace("$projectroot$", projectRoot);
            tokenProcessor.AddReplace("$ProjectRoot$", projectRoot);
            if (!string.IsNullOrEmpty(masterProjectName))
            {
                tokenProcessor.AddReplace("$masterprojectname$", masterProjectName);
                tokenProcessor.AddReplace("$MasterProjectName$", masterProjectName);
            }
            tokenProcessor.AddReplace("$filename$", name);
            tokenProcessor.AddReplace("$FileName$", name);
            tokenProcessor.AddReplace("$name$", name);
            tokenProcessor.AddReplace("$Name$", name);
            tokenProcessor.AddReplace("$ext$", ext);
            tokenProcessor.AddReplace("$Ext$", ext);
            tokenProcessor.AddReplace("$extension$", ext);
            tokenProcessor.AddReplace("$Extension$", ext);

            // developer info:
            tokenProcessor.AddReplace("$user$", Environment.UserName);
            tokenProcessor.AddReplace("$User$", Environment.UserName);
            tokenProcessor.AddReplace("$author$", author);
            tokenProcessor.AddReplace("$Author$", author);
            tokenProcessor.AddReplace("$authorid$", authorID);
            tokenProcessor.AddReplace("$AuthorID$", authorID);
            tokenProcessor.AddReplace("$now$", now);
            tokenProcessor.AddReplace("$Now$", now);
            tokenProcessor.AddReplace("$authorsafe$", authorSafe);
            tokenProcessor.AddReplace("$AuthorSafe$", authorSafe);

            // C++ development info:
            tokenProcessor.AddReplace("$safename$", safeName); // safe name of the file to be used as class or control name...
            tokenProcessor.AddReplace("$SafeName$", safeName);
            tokenProcessor.AddReplace("$safenameupper$", safeNameUpper);
            tokenProcessor.AddReplace("$SafeNameUpper$", safeNameUpper);

            // QML development info:
            tokenProcessor.AddReplace("$cascadesversion$", "1.2"); // PH: TODO: could be provided dynamically from the current NDK...
            tokenProcessor.AddReplace("$CascadesVersion$", "1.2");

            return tokenProcessor;
        }
    }
}
