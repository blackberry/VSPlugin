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
using BlackBerry.BuildTasks.Helpers;
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
        /// Gets or sets items supposed to compile.
        /// </summary>
        public ITaskItem[] CompileItems
        {
            set;
            get;
        }

        public ITaskItem[] IncludeItems
        {
            get;
            set;
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

            if (ConfigurationAppType == "Regular")
            {
                GenerateRegularMakefile();
                return true;
            }

            if (ConfigurationAppType == "Cascades")
            {
                GenerateConfigPri();
                return true;
            }

            return false;
        }

        private void GenerateConfigPri()
        {
            // generate list of items to compile:
            ITaskItem[] toCompile;
            ITaskItem[] toCompileAsC;
            ITaskItem[] toCompileAsCpp;
            ITaskItem additionalInfo;

            SplitCompileItems(CompileItems, ExcludeDirectories, out toCompile, out toCompileAsC, out toCompileAsCpp);
            additionalInfo = toCompile != null && toCompile.Length > 0 ? toCompile[0] : null;

            using (var outputFile = new StreamWriter(ProjectDir + "config.pri"))
            {
                ITaskItem excludedHeader;
                ITaskItem excludedItem;

                var template = new ConfigPriTemplate();
                template.SolutionName = SolutionName;
                template.CompileItems = toCompile;
                template.CompileDirs = ExtractDirectories(template.CompileItems);
                template.IncludeItems = FilterByExtension(IncludeItems, new[] { ".h", ".hpp", ".hxx", ".h++", ".hh" }, new[] { "precompiled.h", "precompiled.hpp", "stdafx.h", "src\\precompiled.h", "src\\precompiled.hpp", "src\\stdafx.h" }, out excludedHeader);
                template.IncludeDirs = ExtractDirectories(template.IncludeItems);
                template.PrecompiledHeaderName = excludedHeader != null ? excludedHeader.ItemSpec : null;
                template.QmlItems = FilterByExtension(IncludeItems, new[] {".qml", ".js", ".qs"}, null, out excludedItem);
                template.QmlDirs = ExtractDirectories(template.QmlItems);
                template.LinkItem = LinkItems != null && LinkItems.Length > 0 ? LinkItems[0] : null;
                template.AdditionalIncludeDirs = TemplateHelper.GetRootedDirs(ProjectDir, AdditionalIncludeDirectories);
                template.AdditionalLibraryDirs = TemplateHelper.GetRootedDirs(ProjectDir, AdditionalLibraryDirectories);
                template.PreprocessorDefinitions = additionalInfo != null ? additionalInfo.GetMetadata("PreprocessorDefinitions").Split(';') : null;
                template.UndefinePreprocessorDefinitions = additionalInfo != null ? additionalInfo.GetMetadata("UndefinePreprocessorDefinitions").Split(';') : null;

                outputFile.Write(template.TransformText());
            }
        }

        private void GenerateRegularMakefile()
        {
            // generate list of items to compile:
            ITaskItem[] toCompile;
            ITaskItem[] toCompileAsC;
            ITaskItem[] toCompileAsCpp;
            SplitCompileItems(CompileItems, ExcludeDirectories, out toCompile, out toCompileAsC, out toCompileAsCpp);

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
                template.CompileItems = toCompile;
                template.CompileItemsAsC = toCompileAsC;
                template.CompileItemsAsCpp = toCompileAsCpp;
                template.LinkItem = LinkItems != null && LinkItems.Length > 0 ? LinkItems[0] : null;
                template.AdditionalIncludeDirectories = TemplateHelper.GetRootedDirs(ProjectDir, AdditionalIncludeDirectories);
                template.AdditionalLibraryDirectories = TemplateHelper.GetRootedDirs(ProjectDir, AdditionalLibraryDirectories);

                outputFile.Write(template.TransformText());
            }
        }

        private static void SplitCompileItems(ITaskItem[] compileItems, string[] excludedDirectories, out ITaskItem[] toCompile, out ITaskItem[] toCompileAsC, out ITaskItem[] toCompileAsCpp)
        {
            if (compileItems == null)
                throw new ArgumentNullException("compileItems");

            var listCompile = new List<ITaskItem>();
            var listCompileAsC = new List<ITaskItem>();
            var listCompileAsCpp = new List<ITaskItem>();

            excludedDirectories = TemplateHelper.NormalizePaths(excludedDirectories);
            foreach (var item in compileItems)
            {
                var fullPath = TemplateHelper.GetFullPath(item);

                // skip the ones, that are explicitly excluded:
                if (!IsExcludedPath(excludedDirectories, fullPath))
                {
                    listCompile.Add(item);

                    // and devide into C vs C++ sets:
                    if (IsCompileAsC(item))
                        listCompileAsC.Add(item);
                    else
                        listCompileAsCpp.Add(item);
                }
            }

            toCompile = listCompile.ToArray();
            toCompileAsC = listCompileAsC.ToArray();
            toCompileAsCpp = listCompileAsCpp.ToArray();
        }

        private static bool IsCompileAsC(ITaskItem item)
        {
            return item.GetMetadata("CompileAs") == "CompileAsC";
        }

        private static ITaskItem[] FilterByExtension(IEnumerable<ITaskItem> items, string[] extensions, string[] excludedItemSpecs, out ITaskItem excludedItem)
        {
            var result = new List<ITaskItem>();

            excludedItem = null;
            if (items != null)
            {
                if (extensions != null && extensions.Length > 0)
                {
                    foreach (var item in items)
                    {
                        var itemExtension = item.GetMetadata("Extension");
                        for (int i = 0; i < extensions.Length; i++)
                        {
                            // is the extension matching?
                            if (string.Compare(itemExtension, extensions[i], StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                // is the file excluded?
                                var index = IndexOf(excludedItemSpecs, item);
                                if (index >= 0)
                                {
                                    if (excludedItem == null)
                                    {
                                        excludedItem = item;
                                    }
                                }
                                else
                                {
                                    result.Add(item);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    result.AddRange(items);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds index of the matching by ItemSpec item.
        /// </summary>
        private static int IndexOf(string[] itemSpecs, ITaskItem item)
        {
            if (item == null || itemSpecs == null || itemSpecs.Length == 0)
                return -1;

            for(int i = 0; i < itemSpecs.Length; i++)
            {
                if (string.Compare(itemSpecs[i], item.ItemSpec, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] ExtractDirectories(IEnumerable<ITaskItem> items)
        {
            var result = new List<string>();

            if (items != null)
            {
                foreach (var item in items)
                {
                    var path = Path.GetDirectoryName(item.ItemSpec);
                    if (!result.Contains(path))
                    {
                        result.Add(path);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Check to see if path is in the excluded list.
        /// </summary>
        private static bool IsExcludedPath(IEnumerable<string> excludedDirectories, string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (excludedDirectories != null)
            {
                var fullPath = TemplateHelper.NormalizePath(path);

                foreach (string excludedDir in excludedDirectories)
                {
                    if (fullPath.StartsWith(excludedDir, StringComparison.OrdinalIgnoreCase))
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
