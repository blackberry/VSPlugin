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

        private const string DefaultBarDescriptorFileName = "bar-descriptor.xml";

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ImportProjectInfo(string path, string name, string[] files, string[] defines, string[] dependencies, string[] includeDirectories, bool buildOutputDependsOnTargetArch)
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
            IncludeDirectories = includeDirectories ?? new string[0];
            BuildOutputDependsOnTargetArch = buildOutputDependsOnTargetArch;
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

        /// <summary>
        /// Gets the collection of additional include directories.
        /// </summary>
        public string[] IncludeDirectories
        {
            get;
            private set;
        }

        public bool BuildOutputDependsOnTargetArch
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

            var projectFiles = new List<string>();
            var projectDependencies = new List<string>();
            var projectDefines = new List<string>();
            var projectIncludeDirectories = new List<string>();

            // is there redefining all manifest file?
            var manifestFileName = System.IO.Path.Combine(path, "manifest.properties");
            if (File.Exists(manifestFileName))
            {
                string name;
                string[] files;
                string[] defines;
                string[] dependencies;

                if (ReadProjectManifest(manifestFileName, path, out name, out files, out defines, out dependencies))
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        projectName = name;
                    }

                    AppendUniquely(projectFiles, files);
                    AppendUniquely(projectDefines, defines);
                    AppendUniquely(projectDependencies, dependencies);
                }
            }

            var cprojectFileName = System.IO.Path.Combine(path, ".cproject");
            if (File.Exists(cprojectFileName))
            {
                string[] defines;
                string[] dependencies;
                string[] includes;
                if (ReadCProjectFile(cprojectFileName, out defines, out dependencies, out includes))
                {
                    AppendUniquely(projectDefines, defines);
                    AppendUniquely(projectDependencies, dependencies);
                    AppendUniquely(projectIncludeDirectories, includes);
                }
            }

            // default list of files is everything from the folder:
            if (projectFiles.Count == 0)
            {
                AppendUniquely(projectFiles, ScanForFiles(path));
            }

            ///////////
            // Outputs

            bool projectBuildOutputDependsOnTargetArch = false;

            // is there any 'entry' definition in bar-descriptor.xml that looks like: arm/o.le-v7-g or x86/o-g or similar?
            var barDescriptorFileName = System.IO.Path.Combine(path, DefaultBarDescriptorFileName);

            // if it's not in standard location, try to find it among project files:
            if (!File.Exists(barDescriptorFileName))
            {
                foreach (var fileName in projectFiles)
                {
                    if (IsBarDescriptorFile(fileName))
                    {
                        barDescriptorFileName = fileName;
                        break;
                    }
                }
            }

            if (File.Exists(barDescriptorFileName))
            {
                bool outputsBasedOnTargetArch;
                if (ReadProjectBarDescriptor(barDescriptorFileName, out outputsBasedOnTargetArch))
                {
                    projectBuildOutputDependsOnTargetArch = outputsBasedOnTargetArch;
                }
            }

            return new ImportProjectInfo(path, projectName, projectFiles.ToArray(), projectDefines.ToArray(), projectDependencies.ToArray(), projectIncludeDirectories.ToArray(), projectBuildOutputDependsOnTargetArch);
        }

        /// <summary>
        /// Checks, if specified file refers to bar-descriptor.
        /// </summary>
        public static bool IsBarDescriptorFile(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && fileName.EndsWith(System.IO.Path.DirectorySeparatorChar + DefaultBarDescriptorFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendUniquely(List<string> list, IEnumerable<string> newItems)
        {
            if (newItems != null)
            {
                foreach (var value in newItems)
                {
                    if (!list.Contains(value))
                    {
                        list.Add(value);
                    }
                }
            }
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

        private static bool ReadCProjectFile(string cprojectFileName, out string[] defines, out string[] dependencies, out string[] includeDirectories)
        {
            if (string.IsNullOrEmpty(cprojectFileName))
                throw new ArgumentNullException("cprojectFileName");

            var defs = new List<string>();
            var libs = new List<string>();
            var includes = new List<string>();

            defines = null;
            dependencies = null;
            includeDirectories = null;

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(cprojectFileName);

                // process the 'option' items of any configuration:
                var options = xmlDoc.GetElementsByTagName("option");
                for (int i = 0; i < options.Count; i++)
                {
                    var node = options[i];
                    if (node != null && node.Attributes != null)
                    {
                        var attrSuperClass = node.Attributes["superClass"];
                        if (attrSuperClass != null)
                        {
                            if (string.Compare(attrSuperClass.Value, "com.qnx.qcc.option.linker.libraries", StringComparison.Ordinal) == 0)
                            {
                                AddFromNodeList(libs, node.ChildNodes);
                            }
                            else
                            {
                                if (string.Compare(attrSuperClass.Value, "com.qnx.qcc.option.compiler.defines", StringComparison.Ordinal) == 0)
                                {
                                    AddFromNodeList(defs, node.ChildNodes);
                                }
                                else
                                {
                                    if (string.Compare(attrSuperClass.Value, "com.qnx.qcc.option.compiler.includePath", StringComparison.Ordinal) == 0)
                                    {
                                        AddFromNodeList(includes, node.ChildNodes);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to read .cproject file: \"{0}\"", cprojectFileName);
                return false;
            }

            // fix the difference environment variables are named in Momentics:
            for (int i = 0; i < includes.Count; i++)
            {
                includes[i] = includes[i].Replace('{', '(').Replace('}', ')').Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            }


            defines = defs.ToArray();
            dependencies = libs.ToArray();
            includeDirectories = includes.ToArray();

            return true;
        }

        private static void AddFromNodeList(List<string> list, XmlNodeList nodeList)
        {
            if (nodeList != null)
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    var node = nodeList[i];
                    if (node != null && node.Attributes != null)
                    {
                        // for non-built-in options:
                        var attrBuiltIn = node.Attributes["builtIn"];
                        if (attrBuiltIn != null && string.Compare(attrBuiltIn.Value, "false", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            var attrValue = node.Attributes["value"];
                            if (attrValue != null)
                            {
                                if (!list.Contains(attrValue.Value))
                                {
                                    list.Add(attrValue.Value);
                                }
                            }
                        }
                    }
                }
            }
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

        private static bool ReadProjectManifest(string manifestFileName, string filesRoot, out string name, out string[] files, out string[] defines, out string[] dependencies)
        {
            if (string.IsNullOrEmpty(manifestFileName))
                throw new ArgumentNullException("manifestFileName");
            if (string.IsNullOrEmpty(filesRoot))
                throw new ArgumentNullException("filesRoot");

            char[] separators = { ' ', '\t' };
            var contents = new List<string>();
            var defs = new List<string>();
            var libs = new List<string>();
            var paths = new Dictionary<string, string>();

            contents.Add("bar-descriptor.xml");

            name = null;
            files = null;
            defines = null;
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
                                AppendUniquely(libs, ruleValue.Split(separators, StringSplitOptions.RemoveEmptyEntries));
                            }

                            // is it a define?
                            if (string.Compare(ruleName, "flags.compiler", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                foreach (var value in ruleValue.Split(separators, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (value.StartsWith("-D"))
                                    {
                                        var define = value.Substring(2);
                                        if (!defs.Contains(define))
                                        {
                                            defs.Add(define);
                                        }
                                    }
                                }
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
            defines = defs.ToArray();
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

        private static bool ReadProjectBarDescriptor(string barDescriptorFileName, out bool buildOutputsDependsOnTargetArch)
        {
            if (string.IsNullOrEmpty(barDescriptorFileName))
                throw new ArgumentNullException("barDescriptorFileName");

            buildOutputsDependsOnTargetArch = false;

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(barDescriptorFileName);

                // get all assets and verify if the 'entry' ones have special output path:
                var assets = xmlDoc.GetElementsByTagName("asset");
                for(int i = 0; i < assets.Count; i++)
                {
                    var node = assets[i];
                    if (node != null && node.Attributes != null)
                    {
                        var attrEntry = node.Attributes["entry"];
                        if (attrEntry != null && string.Compare(attrEntry.Value, "true", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            var attrPath = node.Attributes["path"];
                            if (attrPath != null)
                            {
                                buildOutputsDependsOnTargetArch = IsTargetArchBuildOutput(attrPath.Value);
                                break;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsTargetArchBuildOutput(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.StartsWith("arm/o.le-v7-g/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("arm/o.le-v7/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("x86/o-g/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
