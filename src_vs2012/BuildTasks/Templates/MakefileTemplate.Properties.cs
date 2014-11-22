using System;
using System.Collections.Generic;
using System.Reflection;
using BlackBerry.BuildTasks.Helpers;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks.Templates
{
    partial class MakefileTemplate : TemplateHelper.IWriter
    {
        private string[] _additionalIncludeDirectories;
        private string[] _additionalLibraryDirectories;

        #region Properties

        public string SolutionName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the current version of the current library.
        /// </summary>
        public Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public ITaskItem[] CompileItems
        {
            get;
            set;
        }

        public string TargetFile
        {
            get;
            set;
        }

        public string TargetCompiler
        {
            get;
            set;
        }

        public string TargetCompilerVersion
        {
            get;
            set;
        }

        public string CompilerFlags
        {
            get;
            set;
        }

        public ITaskItem[] CompileItemsAsC
        {
            get;
            set;
        }

        public ITaskItem[] CompileItemsAsCpp
        {
            get;
            set;
        }

        public ITaskItem LinkItem
        {
            get;
            set;
        }

        public string OutDir
        {
            get;
            set;
        }

        public string TargetBarFile
        {
            get;
            set;
        }

        public string ConfigurationType
        {
            get;
            set;
        }

        public string ProjectDir
        {
            get;
            set;
        }

        public string[] AdditionalIncludeDirectories
        {
            get { return _additionalIncludeDirectories; }
            set { _additionalIncludeDirectories = TemplateHelper.NormalizePaths(value); }
        }

        public string[] AdditionalLibraryDirectories
        {
            get { return _additionalLibraryDirectories; }
            set { _additionalLibraryDirectories = TemplateHelper.NormalizePaths(value); }
        }

        #endregion

        private static string[] GetRootedDirs(string projectDir, string[] items)
        {
            return TemplateHelper.GetRootedDirs(projectDir, items);
        }

        private void WriteCollection(IEnumerable<string> items, string prefix)
        {
            TemplateHelper.WriteCollection(this, items, prefix);
        }

        private string GetFullPath8dot3(ITaskItem item)
        {
            return TemplateHelper.GetFullPath8dot3(item);
        }

        private void Write8dot3Collection(ITaskItem[] items, string prefix, string suffix)
        {
            TemplateHelper.Write8dot3Collection(this, items, prefix, suffix);
        }

        private void WriteNameCollection(ITaskItem[] items, string prefix, string suffix)
        {
            TemplateHelper.WriteNameCollection(this, items, prefix, suffix);
        }

        private void WriteDependencyLibrariesReferences()
        {
            TemplateHelper.WriteDependencyLibrariesReferences(this, LinkItem);
        }
    }
}
