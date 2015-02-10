using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Components;
using BlackBerry.Package.Helpers;
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
            TraceLog.WriteLine("Adding new project...");

            if (!BuildPlatformsManager.IsMSBuildPlatformInstalled)
            {
                MessageBoxHelper.Show("More info at: " + ConfigDefaults.GithubProjectWikiInstallation,
                    "Unable to create any BlackBerry project.\r\nPlease make sure whether \"BlackBerry\" build platform has been added to MSBuild.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return wizardResult.wizardResultFailure;
            }

            int count = GetProjectsCount(customParams);
            string masterProjectName = null;

            foreach (var data in customParams)
            {
                if (IsMatchingProjectKey(data.Key))
                {
                    // add the project itself:
                    var destinationPath = CreateProject(context, data.Value, masterProjectName ?? context.ProjectName, count == 1);
                    var project = dte.Solution.AddFromFile(destinationPath);
                    if (project == null)
                        continue;

                    var vcProject = project.Object as VCProject;
                    var folders = new ProjectFolderTree(project, false);

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
                    var dependencyDirectoryParamName = "dependency-dir";
                    var defineParamName = "define";

                    if (!string.IsNullOrEmpty(projectNumber))
                    {
                        filtersParamName = string.Concat(filtersParamName, ProjectNumberSeparator, projectNumber);
                        fileParamName = string.Concat(fileParamName, ProjectNumberSeparator, projectNumber);
                        dependencyParamName = string.Concat(dependencyParamName, ProjectNumberSeparator, projectNumber);
                        dependencyDirectoryParamName = string.Concat(dependencyDirectoryParamName, ProjectNumberSeparator, projectNumber);
                        defineParamName = string.Concat(defineParamName, ProjectNumberSeparator, projectNumber);
                    }

                    // add project items:
                    foreach (var subItem in customParams)
                    {
                        // file:
                        if (IsMatchingKey(subItem.Key, fileParamName))
                        {
                            SourceActions reserved;
                            var itemName = Path.Combine(projectRoot, Path.GetFileName(GetSourceName(subItem.Value, out reserved)));
                            var itemTokenProcessor = CreateTokenProcessor(project.Name, projectRoot, GetDestinationName(itemName, subItem.Value, tokenProcessor), masterProjectName);
                            AddFile(dte, folders, itemName, subItem.Value, itemTokenProcessor, false);
                        }

                        // filters:
                        if (IsMatchingKey(subItem.Key, filtersParamName))
                        {
                            folders.CreateFilters(subItem.Value);
                        }

                        // library references:
                        if (vcProject != null && IsMatchingKey(subItem.Key, dependencyParamName))
                        {
                            ProjectHelper.AddAdditionalDependencies(vcProject, null, GetTag(projectNumber, PlatformSeparator), subItem.Value);
                        }

                        // library reference directories:
                        if (vcProject != null && IsMatchingKey(subItem.Key, dependencyDirectoryParamName))
                        {
                            ProjectHelper.AddAdditionalDependencyDirectories(vcProject, null, GetTag(projectNumber, PlatformSeparator), subItem.Value);
                        }

                        // defines:
                        if (vcProject != null && IsMatchingKey(subItem.Key, defineParamName))
                        {
                            // HINT: you can specify per-platform settings using '@':
                            ProjectHelper.AddPreprocessorDefines(vcProject, null, GetTag(projectNumber, PlatformSeparator), subItem.Value);
                        }
                    }

                    project.Save();
                }
            }

            if (context.IsExclusive)
            {
                try
                {
                    var solutionPath = string.IsNullOrEmpty(context.SolutionName) ? context.LocalDirectory : Path.GetDirectoryName(context.LocalDirectory);
                    dte.Solution.SaveAs(Path.Combine(solutionPath, context.ProjectName));
                }
                catch (Exception)
                {
                    // user cancelled save...
                }
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
            SourceActions reserved;
            var templatePath = Path.Combine(WizardDataFolder, GetSourceName(projectTemplateName, out reserved));
            var projectRoot = context.IsExclusive || singleProject ? context.LocalDirectory : Path.Combine(context.LocalDirectory, context.ProjectName);
            var destinationPath = Path.Combine(projectRoot, context.ProjectName + Path.GetExtension(templatePath));

            // apply the name change transformations as for ordinary files to the project:
            destinationPath = GetDestinationName(destinationPath, projectTemplateName,
                CreateTokenProcessor(context.ProjectName, Path.GetDirectoryName(destinationPath), destinationPath, masterProjectName));

            // process the tokens from the template file and generate project file:
            var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, destinationPath, masterProjectName);
            tokenProcessor.UntokenFile(templatePath, destinationPath);

            return destinationPath;
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
            TraceLog.WriteLine("Adding new project item...");

            if (!BuildPlatformsManager.IsMSBuildPlatformInstalled)
            {
                MessageBoxHelper.Show("More info at: " + ConfigDefaults.GithubProjectWikiInstallation,
                    "Unable to create any BlackBerry project item.\r\nPlease make sure whether \"BlackBerry\" build platform has been added to MSBuild.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return wizardResult.wizardResultFailure;
            }

            var project = context.ProjectItems.ContainingProject;
            var folders = new ProjectFolderTree(project, false);
            var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, context.ItemName, context.ProjectName);

            foreach (var data in customParams)
            {
                if (string.Compare(data.Key, "file", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AddFile(dte, folders, context.ItemName, data.Value, tokenProcessor, true);
                }
            }

            return wizardResult.wizardResultSuccess;
        }

        /// <summary>
        /// Processes item and adds it to the project.
        /// </summary>
        private void AddFile(DTE2 dte, ProjectFolderTree folders, string itemName, string templateDefinition, TokenProcessor tokenProcessor, bool forceOpen)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");
            if (folders == null)
                throw new ArgumentNullException("folders");
            if (string.IsNullOrEmpty(itemName))
                throw new ArgumentNullException("itemName");
            if (string.IsNullOrEmpty(templateDefinition))
                throw new ArgumentNullException("templateDefinition");

            SourceActions flags;
            var templatePath = Path.Combine(WizardDataFolder, GetSourceName(templateDefinition, out flags));
            var destinationPath = GetDestinationName(itemName, templateDefinition, tokenProcessor);

            // update tokens withing the file and copy to destination:
            tokenProcessor.UntokenFile(templatePath, destinationPath);

            // then add that file to the project items:
            if ((flags & SourceActions.AddToProject) == SourceActions.AddToProject)
            {
                folders.AddFile(destinationPath);
                TraceLog.WriteLine("Added file: \"{0}\"", destinationPath);
            }
            else
            {
                TraceLog.WriteLine("Generated file: \"{0}\", but omitted adding to project", destinationPath);
            }

            // should it be opened?
            if ((forceOpen || (flags & SourceActions.Open) == SourceActions.Open) && File.Exists(destinationPath))
            {
                dte.ItemOperations.OpenFile(destinationPath, Constants.vsViewKindTextView);
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

            var authorInfo = PackageViewModel.Instance.Developer != null ? PackageViewModel.Instance.Developer.CachedAuthor : null;
            var authorName = authorInfo != null && !string.IsNullOrEmpty(authorInfo.Name) ? authorInfo.Name : "Example Inc.";
            var authorID = authorInfo != null && !string.IsNullOrEmpty(authorInfo.ID) ? authorInfo.ID : "ABCD1234";
            var now = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var authorSafe = CreateSafeName(authorName).ToLowerInvariant();

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
            tokenProcessor.AddReplace("$author$", authorName);
            tokenProcessor.AddReplace("$Author$", authorName);
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
            var cascadesVersion = PackageViewModel.Instance.ActiveNDK != null ? PackageViewModel.Instance.ActiveNDK.CascadesVersion.ToString() : "1.2";
            tokenProcessor.AddReplace("$cascadesversion$", cascadesVersion);
            tokenProcessor.AddReplace("$CascadesVersion$", cascadesVersion);

            return tokenProcessor;
        }
    }
}
