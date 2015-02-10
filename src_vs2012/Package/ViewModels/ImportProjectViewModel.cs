using System;
using System.IO;
using System.Xml;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
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
        private const string TemplateNativeCoreApp = @"native-default.vcxproj";
        private const string TemplateCascadesApp = @"cascades-default.vcxproj";

        private ProjectFolderTree _folders;
        private VCProject _vcProject;
        private bool _updateAuthorInfo;

        #region Properties

        /// <summary>
        /// Gets an info about the developer creating the project.
        /// </summary>
        private DeveloperDefinition Developer
        {
            get { return PackageViewModel.Instance.Developer; }
        }

        /// <summary>
        /// Gets info about the author (publisher).
        /// </summary>
        public AuthorInfo Author
        {
            get { return Developer != null && Developer.IsRegistered && Developer.CachedAuthor != null && !string.IsNullOrEmpty(Developer.CachedAuthor.Name) ? Developer.CachedAuthor : null; }
        }

        #endregion

        /// <summary>
        /// Creates new project and adds it into solution.
        /// </summary>
        public Project CreateProject(DTE2 dte, string projectName, bool createNativeCoreApp, string suggestedProjectOutputPath, string[] defines, string[] dependencies, string[] includeDirectories, bool buildOutputsDependsOnTargetArch)
        {
            if (dte == null)
                throw new ArgumentNullException("dte");

            // evaluate, where to store new project:
            string outputPath;
            var solution = dte.Solution;
            bool saveSolution = false;
            bool hasSuggestedLocation = !string.IsNullOrEmpty(suggestedProjectOutputPath);

            if (solution.IsOpen && File.Exists(solution.FullName))
            {
                outputPath = hasSuggestedLocation ? suggestedProjectOutputPath : Path.GetDirectoryName(solution.FullName);
                if (string.IsNullOrEmpty(outputPath))
                {
                    UpdateProject(null, null, null, null, false, false);
                    return null;
                }
            }
            else
            {
                outputPath = hasSuggestedLocation ? suggestedProjectOutputPath : ProjectHelper.GetDefaultProjectFolder(dte);
                if (string.IsNullOrEmpty(outputPath))
                {
                    UpdateProject(null, null, null, null, false, false);
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
                try
                {
                    solution.SaveAs(Path.Combine(outputPath, projectName + ".sln"));
                }
                catch (Exception)
                {
                    // user cancelled save...
                }
            }

            // update state for adding items:
            UpdateProject(project, defines, dependencies, includeDirectories, buildOutputsDependsOnTargetArch, true);
            return project;
        }

        /// <summary>
        /// Sets a project to be used by add-file functionality and updates few of its properties.
        /// </summary>
        public void UpdateProject(Project project, string[] defines, string[] dependencies, string[] includeDirectories, bool buildOutputsDependsOnTargetArch, bool updateAuthorInfo)
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
                _updateAuthorInfo = updateAuthorInfo && Author != null;

                ProjectHelper.AddPreprocessorDefines(_vcProject, "BlackBerry", null, defines);
                ProjectHelper.AddAdditionalDependencies(_vcProject, "BlackBerry", null, dependencies);
                ProjectHelper.AddAdditionalIncludeDirectories(_vcProject, "BlackBerry", null, includeDirectories);

                if (buildOutputsDependsOnTargetArch)
                {
                    ProjectHelper.SetBuildOutputDirectory(_vcProject, "BlackBerry", "Release", "$(TargetArchPre)\\o$(TargetArchPost)\\");
                    ProjectHelper.SetBuildOutputDirectory(_vcProject, "BlackBerry", "Debug", "$(TargetArchPre)\\o$(TargetArchPost)-g\\");
                }
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

                    try
                    {
                        // copy and add file into the project at proper part of the hierarchy:
                        File.Copy(sourcePath, destinationPath, true);

                        if (addFile)
                        {
                            _folders.AddFile(destinationPath, relativePath);
                        }

                        // update author info, if requested:
                        if (_updateAuthorInfo && ImportProjectInfo.IsBarDescriptorFile(destinationPath))
                        {
                            UpdateBarDescriptor(destinationPath, Author);
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Issue, when importing from: \"{0}\" to: \"{1}\"", sourcePath, destinationPath);
                    }
                }
                else
                {
                    // add file into the project at proper part of the hierarchy:
                    if (addFile)
                    {
                        _folders.AddFile(sourcePath, relativePath);
                    }

                    // update author info, if requested:
                    if (_updateAuthorInfo && ImportProjectInfo.IsBarDescriptorFile(sourcePath))
                    {
                        UpdateBarDescriptor(sourcePath, Author);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to import file: \"{0}\"", sourcePath);
            }
        }

        /// <summary>
        /// Updates an author info inside the bar-descriptor-like-format file.
        /// </summary>
        private static bool UpdateBarDescriptor(string filePath, AuthorInfo author)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            if (author == null)
                return false;

            try
            {
                var xml = new XmlDocument();
                xml.PreserveWhitespace = true;

                // load:
                xml.Load(filePath);

                // update:
                if (xml.DocumentElement != null && xml.DocumentElement.Name == "qnx")
                {
                    UpdateTag(xml.DocumentElement, "author", author.Name, "description");
                    UpdateTag(xml.DocumentElement, "authorId", author.ID, "author");

                    // overwrite original XML:
                    xml.Save(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to update author info in: \"{0}\"", filePath);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Updates the value of the specified tag.
        /// If this tag doesn't exist, it will try to insert it after the one specified.
        /// If this tag doesn't exist neither, it will just append it at the end.
        /// </summary>
        private static void UpdateTag(XmlElement root, string tagName, string value, string afterTag)
        {
            if (root == null)
                throw new ArgumentNullException("root");
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentNullException("tagName");
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            // check, if exists:
            XmlNode existing = root[tagName];
            if (existing != null)
            {
                existing.InnerText = value;
                return;
            }

            // create new node:
            var newLine = root.OwnerDocument != null ? root.OwnerDocument.CreateSignificantWhitespace("\r\n    ") : null;
            var node = root.OwnerDocument != null ? root.OwnerDocument.CreateElement(tagName, root.NamespaceURI) : null;
            if (newLine == null || node == null)
            {
                return;
            }
            node.InnerText = value;

            // find, where to place new node:
            existing = string.IsNullOrEmpty(afterTag) ? root.LastChild : root[afterTag];

            // try to add it after:
            root.InsertAfter(newLine, existing ?? root.LastChild);
            root.InsertAfter(node, newLine);
        }
    }
}
