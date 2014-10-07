//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BlackBerry.BuildTasks.Templates;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// MSBuild Task for generating a make file for packaging the bar file. 
    /// </summary>
    public sealed class GenerateMakefile : Task
    {
        #region Member Variables and Constants

        private string _projectDirectory;
        private string _intDir;
        private string _outDir;

        #endregion

        #region Properties

        /// <summary>
        /// Getter/Setter for CompileItems property
        /// </summary>
        public ITaskItem[] CompileItems
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for LinkItems
        /// </summary>
        public ITaskItem[] LinkItems
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for ProjectDir property
        /// </summary>
        public string ProjectDir
        {
            set { _projectDirectory = value != null ? value.Replace('\\', '/') : string.Empty; }
            get { return _projectDirectory; }
        }

        /// <summary>
        /// Getter/Setter for IntDir property
        /// </summary>
        public string IntDir
        {
            set { _intDir = value != null ? value.Replace('\\', '/') : string.Empty; }
            get { return _intDir; }
        }

        /// <summary>
        /// Getter/Setter for OutDir property
        /// </summary>
        public string OutDir
        {
            set { _outDir = value != null ? value.Replace('\\', '/') : string.Empty; }
            get { return _outDir; }
        }

        /// <summary>
        /// Getter/Setter for AdditionalIncludeDirectories property 
        /// </summary>
        public string[] AdditionalIncludeDirectories
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for AdditionLibraryDirectories
        /// </summary>
        public string[] AdditionalLibraryDirectories
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for ExcludeDirectories
        /// </summary>
        public string[] ExcludeDirectories
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for TargetName property
        /// </summary>
        public string TargetName
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for TargetExtension property
        /// </summary>
        public string TargetExtension
        {
            set;
            get;
        }

        public string SolutionName
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for ConfigurationType property
        /// </summary>
        public string ConfigurationType
        {
            set;
            get;
        }

        public string ConfigurationAppType
        {
            get;
            set;
        }

        public string TargetCompiler
        {
            get;
            set;
        }

        public string TargetArch
        {
            get;
            set;
        }

        /// <summary>
        /// Getter/Setter for CompilerVersion property
        /// </summary>
        public string TargetCompilerVersion
        {
            set;
            get;
        }

        /// <summary>
        /// Getter/Setter for Platform property
        /// </summary>
        public string Platform
        {
            set;
            get;
        }

        #endregion

        /// <summary>
        /// Execute MSBuild Task
        /// </summary>
        public override bool Execute()
        {
            // are we asked not to generate makefile as developer supplied custom one:
            if (ConfigurationAppType == "Custom")
            {
                NotifyMessage(new BuildMessageEventArgs("Skipped makefile creation", null, "GenerateMakefile", MessageImportance.High));
                return true;
            }

            // normalize excluded list:
            if (ExcludeDirectories != null)
            {
                for (int i = 0; i < ExcludeDirectories.Length; i++)
                    ExcludeDirectories[i] = ExcludeDirectories[i].Replace('\\', '/');
            }

            // normalize additional included list:
            if (AdditionalIncludeDirectories != null)
            {
                for (int i = 0; i < AdditionalIncludeDirectories.Length; i++)
                    AdditionalIncludeDirectories[i] = AdditionalIncludeDirectories[i].Replace('\\', '/');
            }

            // normalize additional library paths list:
            if (AdditionalLibraryDirectories != null)
            {
                for (int i = 0; i < AdditionalLibraryDirectories.Length; i++)
                    AdditionalLibraryDirectories[i] = AdditionalLibraryDirectories[i].Replace('\\', '/');
            }

            // generate list of items to compile:
            var toCompile = new List<ITaskItem>();
            var toCompileAsC = new List<ITaskItem>();
            var toCompileAsCpp = new List<ITaskItem>();

            foreach (var item in CompileItems)
            {
                var fullPath = GetFullPath(item);

                // skip the ones, that are explicitly excluded:
                if (!IsExcludedPath(fullPath))
                {
                    toCompile.Add(item);

                    // and devide into C vs C++ sets:
                    if (IsCompileAsC(item))
                        toCompileAsC.Add(item);
                    else
                        toCompileAsCpp.Add(item);
                }
            }

            using (var outputFile = new StreamWriter(IntDir + "makefile"))
            {
                var template = new MakefileTemplate();
                template.ConfigurationType = ConfigurationType;
                template.SolutionName = SolutionName;
                template.OutDir = Path.IsPathRooted(OutDir) ? OutDir : ProjectDir + OutDir;
                template.ProjectDir = ProjectDir;
                template.TargetFile = TargetName + (TargetExtension != ".exe" ? TargetExtension : string.Empty);
                template.TargetBarFile = ConfigurationType == "Application" ? TargetName + ".bar" : string.Empty;
                template.TargetCompiler = TargetCompiler;
                template.TargetCompilerVersion = TargetCompilerVersion;
                template.CompilerFlags = string.Concat("-V\"", TargetCompilerVersion,
                                                       string.IsNullOrEmpty(TargetCompilerVersion) || string.IsNullOrEmpty(TargetCompiler) ? string.Empty : ",",
                                                       TargetCompiler, "\"");
                template.CompileItems = toCompile.ToArray();
                template.CompileItemsAsC = toCompileAsC.ToArray();
                template.CompileItemsAsCpp = toCompileAsCpp.ToArray();
                template.LinkItem = LinkItems != null && LinkItems.Length > 0 ? LinkItems[0] : null;
                template.AdditionalIncludeDirectories = MakefileTemplate.GetRootedDirs(ProjectDir, AdditionalIncludeDirectories);
                template.AdditionalLibraryDirectories = MakefileTemplate.GetRootedDirs(ProjectDir, AdditionalLibraryDirectories);

                outputFile.Write(template.TransformText());
            }

            return true;
        }

        private static bool IsCompileAsC(ITaskItem item)
        {
            return item.GetMetadata("CompileAs") == "CompileAsC";
        }

        private static string GetFullPath(ITaskItem item)
        {
            return item.GetMetadata("FullPath").Replace('\\', '/');
        }

        /// <summary>
        /// Check to see if path is in the excluded list.
        /// </summary>
        private bool IsExcludedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (ExcludeDirectories != null)
            {
                var fullPath = Path.GetFullPath(path);

                foreach (string excludedDir in ExcludeDirectories)
                {
                    if (fullPath.StartsWith(Path.GetFullPath(excludedDir), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void NotifyMessage(BuildMessageEventArgs e)
        {
            if (e != null)
            {
                try
                {
                    if (BuildEngine != null)
                    {
                        BuildEngine.LogMessageEvent(e);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "BlackBerry.Build.Log");
                }
            }
        }
    }
}
