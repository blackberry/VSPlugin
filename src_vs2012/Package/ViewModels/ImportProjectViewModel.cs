using System;
using System.Collections.Generic;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Components;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Wizards;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.VCProjectEngine;

namespace BlackBerry.Package.ViewModels
{
    internal sealed class ImportProjectViewModel
    {
        private static readonly string[] ForbiddenFiles = { "\\.settings", "\\.device", "\\.cproject", "\\.project", "\\.gitignore", "\\.gitmodules", ".vcxproj", ".vcxproj.filters", ".suo", ".user" };
        private static readonly string[] ForbiddenDirs = { "\\arm", "\\x86", "\\.settings", "\\.git", "\\.svn", "\\.hg", "\\Device-Debug", "\\Debug", "\\Device-Release", "\\Release", "\\Simulator-Debug", "\\obj", "\\bin" };

        private const string TemplateNativeCoreApp = @"native-default.vcxproj";
        private const string TemplateCascadesApp = @"cascades-default.vcxproj";

        private ProjectFolderTree _folders;
        private VCProject _vcProject;

        /// <summary>
        /// Scans for all supported files recursively inside specified location.
        /// </summary>
        public IEnumerable<string> ScanForFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var result = new List<string>();
            ScanForFiles(result, path, true);

            return result;
        }

        private static void ScanForFiles(List<string> result, string path, bool root)
        {
            if (!Directory.Exists(path))
                return;

            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*.*");
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var file in files)
            {
                // filter a bit root level folder:
                if (root)
                {
                    if (IsForbidden(ForbiddenFiles, file))
                    {
                        continue;
                    }
                }

                result.Add(file);
            }

            string[] directories;
            try
            {
                directories = Directory.GetDirectories(path, "*.*");
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var subDirectory in directories)
            {
                // filter a bit root level folder:
                if (root)
                {
                    if (IsForbidden(ForbiddenDirs, subDirectory))
                    {
                        continue;
                    }
                }

                ScanForFiles(result, subDirectory, false);
            }
        }

        private static bool IsForbidden(IEnumerable<string> collection, string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            foreach (var pattern in collection)
            {
                if (path.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates new project and adds it into solution.
        /// </summary>
        public Project CreateProject(DTE2 dte, string projectName, bool createNativeCoreApp, string suggestedOutputPath)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            // evaluate, where to store new project:
            string outputPath;
            var solution = dte.Solution;
            bool saveSolution = false;
            bool hasSuggestedLocation = !string.IsNullOrEmpty(suggestedOutputPath);

            if (solution.IsOpen && File.Exists(solution.FullName))
            {
                outputPath = hasSuggestedLocation ? suggestedOutputPath : Path.GetDirectoryName(solution.FullName);
                if (string.IsNullOrEmpty(outputPath))
                {
                    UpdateProject(null);
                    return null;
                }
            }
            else
            {
                outputPath = hasSuggestedLocation ? suggestedOutputPath : ProjectHelper.GetDefaultProjectFolder(dte);
                if (string.IsNullOrEmpty(outputPath))
                {
                    UpdateProject(null);
                    return null;
                }
                saveSolution = true;
            }

            if (!hasSuggestedLocation)
            {
                outputPath = Path.Combine(outputPath, projectName);
            }

            try
            {
                Directory.CreateDirectory(outputPath);
            }
            catch
            {
                return null;
            }

            // create new NativeCore or Cascades project:
            var projectFullPath = Path.Combine(outputPath, projectName + ".vcxproj");
            File.Copy(Path.Combine(PuppetMasterWizardEngine.WizardDataFolder, createNativeCoreApp ? TemplateNativeCoreApp : TemplateCascadesApp), projectFullPath, true);

            // add project into solution:
            var project = solution.AddFromFile(projectFullPath);
            ProjectFolderTree.CreateFilters(ProjectFolderTree.DefaultFilters, project);
            project.Save();

            if (saveSolution)
            {
                solution.SaveAs(Path.Combine(outputPath, projectName + ".sln"));
            }

            // update state for adding items:
            UpdateProject(project);
            return project;
        }

        public void UpdateProject(Project project)
        {
            var newProject = project != null ? project.Object as VCProject : null;

            if (newProject == null)
            {
                _vcProject = null;
                _folders = null;
            }
            else
            {
                _vcProject = newProject;
                _folders = new ProjectFolderTree(_vcProject, true);
            }
        }

        /// <summary>
        /// Adds file to the project (copying it or not).
        /// </summary>
        public void AddFileToProject(string projectDir, string sourcePath, string relativePath, bool copyFile, bool addFile)
        {
            if (_vcProject == null)
                throw new InvalidOperationException("Project was not created before");

            // wrapped whole function into a huge try-catch to avoid problems, when single file import could fail the whole process:
            try
            {
                if (copyFile)
                {
                    var destinationPath = Path.Combine(projectDir, relativePath);
                    var destinationDir = Path.GetDirectoryName(destinationPath);

                    try
                    {
                        if (!string.IsNullOrEmpty(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }
                    }
                    catch
                    {
                    }

                    File.Copy(sourcePath, destinationPath, true);

                    if (addFile)
                    {
                        _folders.AddFile(relativePath, destinationPath);
                    }
                }
                else
                {
                    if (addFile)
                    {
                        _folders.AddFile(relativePath, sourcePath);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to import file: \"{0}\"", sourcePath);
            }
        }
    }
}
