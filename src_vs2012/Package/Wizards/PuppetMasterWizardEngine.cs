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
            foreach (var data in customParams)
            {
                if (data.Key != null && 
                    (string.Compare(data.Key, "project", StringComparison.OrdinalIgnoreCase) == 0 || data.Key.StartsWith("project#", StringComparison.OrdinalIgnoreCase)))
                {
                    // add the project itself:
                    var templatePath = Path.Combine(WizardDataFolder, data.Value);
                    var projectRoot = context.IsExclusive ? context.LocalDirectory : Path.Combine(context.LocalDirectory, context.ProjectName);
                    var destinationPath = Path.Combine(projectRoot, context.ProjectName + Path.GetExtension(data.Value));

                    var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, destinationPath);
                    tokenProcessor.UntokenFile(templatePath, destinationPath);
                    var project = dte.Solution.AddFromFile(destinationPath);

                    // get project identifier, in case the template wants to add more than one:
                    var projectNumber = GetProjectNumber(data.Key);
                    string filtersParamName;
                    string fileParamName;

                    if (string.IsNullOrEmpty(projectNumber))
                    {
                        filtersParamName = "filters";
                        fileParamName = "file";
                    }
                    else
                    {
                        filtersParamName = "filters#" + projectNumber;
                        fileParamName = "file#" + projectNumber;
                    }

                    // add project items:
                    foreach (var subItem in customParams)
                    {
                        // file:
                        if (string.Compare(subItem.Key, fileParamName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            var itemName = Path.Combine(projectRoot, Path.GetFileName(GetSourceName(subItem.Value)));
                            var itemTokenProcessor = CreateTokenProcessor(context.ProjectName, projectRoot, GetDestinationName(itemName, subItem.Value, tokenProcessor));
                            AddFile(project, itemName, subItem.Value, itemTokenProcessor);
                        }

                        // filters:
                        if (string.Compare(subItem.Key, filtersParamName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            var vcProject = project.Object as VCProject;
                            if (vcProject != null)
                            {
                                CreateFilters(subItem.Value, vcProject);
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

        private string GetProjectNumber(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var index = name.IndexOf('#');
            return index < 0 ? null : name.Substring(index + 1).Trim();
        }

        /// <summary>
        /// Method that creates new project item for existing project.
        /// </summary>
        internal override wizardResult ExecuteNewProjectItem(DTE2 dte, IntPtr owner, NewProjectItemParams context, KeyValuePair<string, string>[] customParams)
        {
            var project = context.ProjectItems.ContainingProject;
            var tokenProcessor = CreateTokenProcessor(context.ProjectName, context.LocalDirectory, context.ItemName);

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

            var templatePath = Path.Combine(WizardDataFolder, GetSourceName(templateDefinition));
            var destinationPath = GetDestinationName(itemName, templateDefinition, tokenProcessor);

            // update tokens withing the file and copy to destination:
            tokenProcessor.UntokenFile(templatePath, destinationPath);

            // then add that file to the project items:
            project.ProjectItems.AddFromFile(destinationPath);
            TraceLog.WriteLine("Added file: \"{0}\"", destinationPath);
        }

        /// <summary>
        /// Creates token processor to update dynamically token markers within the template file.
        /// </summary>
        public static TokenProcessor CreateTokenProcessor(string projectName, string projectRoot, string destinationName)
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

            // project & file info:
            tokenProcessor.AddReplace("$projectname$", projectName);
            tokenProcessor.AddReplace("$ProjectName$", projectName);
            tokenProcessor.AddReplace("$projectroot$", projectRoot);
            tokenProcessor.AddReplace("$ProjectRoot$", projectRoot);
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
