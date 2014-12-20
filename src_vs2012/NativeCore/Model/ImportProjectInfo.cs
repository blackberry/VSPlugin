using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Description of the Momentics project to import.
    /// </summary>
    public sealed class ImportProjectInfo
    {
        private static readonly string[] ForbiddenFiles = { "\\.settings", "\\.device", "\\.cproject", "\\.project", "\\.gitignore", "\\.gitmodules", "\\manifest.properties", "\\installedApps.txt", "\\vsndk-compile-ran.flag", ".vcxproj", ".vcxproj.filters", ".suo", ".user" };
        private static readonly string[] ForbiddenDirs = { "\\arm", "\\x86", "\\.settings", "\\.git", "\\.svn", "\\.hg", "\\Device-Debug", "\\Debug", "\\Device-Release", "\\Release", "\\Simulator-Debug", "\\obj", "\\bin" };

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ImportProjectInfo(string path, string name, string[] files, string[] defines, string[] dependencies)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            Path = path;
            Name = name;
            Files = files ?? new string[0];
            Defines = defines ?? new string[0];
            Dependencies = dependencies ?? new string[0];
        }

        #region Properties

        public string Path
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string[] Files
        {
            get;
            private set;
        }

        public string[] Defines
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the collection of names of referenced shared libraries.
        /// </summary>
        public string[] Dependencies
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Loads info about the Momentics project to import.
        /// </summary>
        public static ImportProjectInfo Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            if (!Directory.Exists(path))
                return null;

            ///////////
            // Name

            // default name is the name of the directory:
            var projectName = System.IO.Path.GetFileName(path);

            var projectFileName = System.IO.Path.Combine(path, ".project");
            if (File.Exists(projectFileName))
            {
                string name;

                // if correctly loaded:
                if (ReadProjectFile(projectFileName, out name))
                {
                    projectName = name;
                }
            }

            ///////////
            // Files

            // default list of files is everything from the folder:
            var projectFiles = ScanForFiles(path);
            string[] projectDependencies = null;
            string[] projectDefines = null;

            // is there redefining all manifest file?
            var manifestFileName = System.IO.Path.Combine(path, "manifest.properties");
            if (File.Exists(manifestFileName))
            {
                string name;
                string[] files;
                string[] dependencies;

                if (ReadProjectManifest(manifestFileName, path, out name, out files, out dependencies))
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        projectName = name;
                    }

                    if (files != null && files.Length > 0)
                    {
                        projectFiles = files;
                    }

                    if (dependencies != null && dependencies.Length > 0)
                    {
                        projectDependencies = dependencies;
                    }
                }
            }

            return new ImportProjectInfo(path, projectName, projectFiles, projectDefines, projectDependencies);
        }

        private static bool ReadProjectFile(string projectFileName, out string name)
        {
            if (string.IsNullOrEmpty(projectFileName))
                throw new ArgumentNullException("projectFileName");

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(projectFileName);

                if (xmlDoc.DocumentElement != null && xmlDoc.DocumentElement.Name == "projectDescription")
                {
                    var nameElement = xmlDoc.DocumentElement["name"];
                    if (nameElement != null)
                    {
                        name = nameElement.InnerText;
                        return !string.IsNullOrEmpty(name);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load .project file: \"{0}\"", projectFileName);
            }

            name = null;
            return false;
        }

        /// <summary>
        /// Scans for all supported files recursively inside specified location.
        /// </summary>
        public static string[] ScanForFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var result = new List<string>();
            ScanForFiles(result, path, true);

            return result.ToArray();
        }

        private static void ScanForFiles(ICollection<string> result, string path, bool root)
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

        private static bool ReadProjectManifest(string manifestFileName, string filesRoot, out string name, out string[] files, out string[] dependencies)
        {
            if (string.IsNullOrEmpty(manifestFileName))
                throw new ArgumentNullException("manifestFileName");
            if (string.IsNullOrEmpty(filesRoot))
                throw new ArgumentNullException("filesRoot");

            char[] separators = { ' ', '\t' };
            var contents = new List<string>();
            var libs = new List<string>();
            var paths = new Dictionary<string, string>();

            contents.Add("bar-descriptor.xml");

            name = null;
            files = null;
            dependencies = null;
            try
            {
                using (var reader = new StreamReader(manifestFileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // comment?
                        if (line.Length == 0 || line[0] == '#')
                            continue;

                        // rule?
                        int separatorIndex = line.IndexOf(':');
                        if (separatorIndex > 0)
                        {
                            var ruleName = line.Substring(0, separatorIndex).Trim();
                            var ruleValue = line.Substring(separatorIndex + 1).Trim();

                            // is it a project name?
                            if (string.Compare(ruleName, "project.name", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                name = ruleValue;
                            }

                            // is it a referenced library?
                            if (string.Compare(ruleName, "libs", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                libs.AddRange(ruleValue.Split(separators, StringSplitOptions.RemoveEmptyEntries));
                            }

                            // is it a file, part of the project?
                            if (string.Compare(ruleName, "icon", StringComparison.OrdinalIgnoreCase) == 0
                                || string.Compare(ruleName, "sources", StringComparison.OrdinalIgnoreCase) == 0
                                || string.Compare(ruleName, "resources", StringComparison.OrdinalIgnoreCase) == 0
                                || string.Compare(ruleName, "readmes", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                contents.AddRange(ruleValue.Split(separators, StringSplitOptions.RemoveEmptyEntries));
                            }

                            // is it a path definition?
                            if (ruleName.StartsWith("path.", StringComparison.OrdinalIgnoreCase))
                            {
                                paths[ruleName.Substring(5)] = ruleValue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load manifest file: \"{0}\"", manifestFileName);
                return false;
            }

            // prepare final results:
            var fullPaths = new List<string>();

            foreach (var contentFileName in contents)
            {
                var fullFileName = MakeFullFilePath(contentFileName, filesRoot, paths);
                if (!string.IsNullOrEmpty(fullFileName) && !fullPaths.Contains(fullFileName))
                {
                    fullPaths.Add(fullFileName);
                }
            }
            fullPaths.Sort();

            files = fullPaths.ToArray();
            dependencies = libs.ToArray();
            return true;
        }

        private static string MakeFullFilePath(string fileName, string root, Dictionary<string, string> paths)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            string fullName;
            string pathMapping;

            if (paths != null && paths.TryGetValue(fileName, out pathMapping))
            {
                fullName = pathMapping;
            }
            else
            {
                fullName = fileName;
            }

            if (!System.IO.Path.IsPathRooted(fullName))
            {
                if (string.IsNullOrEmpty(root))
                    return null;

                fullName = System.IO.Path.Combine(root, fullName);
            }

            return File.Exists(fullName) ? fullName : null;
        }
    }
}
