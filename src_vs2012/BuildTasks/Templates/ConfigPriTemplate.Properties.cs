using System;
using System.Collections.Generic;
using System.Reflection;
using BlackBerry.BuildTasks.Helpers;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks.Templates
{
    partial class ConfigPriTemplate : TemplateHelper.IWriter
    {
        private string[] _additionalLibraryDirs;
        private string[] _additionalIncludeDirs;

        private string _precompiledHeaderName;

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

        public ITaskItem LinkItem
        {
            get;
            set;
        }

        public ITaskItem[] CompileItems
        {
            get;
            set;
        }

        public ITaskItem[] IncludeItems
        {
            get;
            set;
        }

        public string[] CompileDirs
        {
            get;
            set;
        }

        public string[] IncludeDirs
        {
            get;
            set;
        }

        public string[] AdditionalLibraryDirs
        {
            get { return _additionalLibraryDirs; }
            set { _additionalLibraryDirs = TemplateHelper.NormalizePaths(value); }
        }

        public string[] AdditionalIncludeDirs
        {
            get { return _additionalIncludeDirs; }
            set { _additionalIncludeDirs = TemplateHelper.NormalizePaths(value); }
        }

        public string[] PreprocessorDefinitions
        {
            get;
            set;
        }

        public string[] UndefinePreprocessorDefinitions
        {
            get;
            set;
        }

        public string PrecompiledHeaderName
        {
            get { return _precompiledHeaderName; }
            set { _precompiledHeaderName = TemplateHelper.Normalize(value); }
        }

        public ITaskItem[] QmlItems
        {
            get;
            set;
        }

        public string[] QmlDirs
        {
            get;
            set;
        }

        #endregion

        private void WriteCollectionNewLine(IEnumerable<string> items, string prefix)
        {
            TemplateHelper.WriteCollection(this, items, prefix, Environment.NewLine);
        }

        private void WriteRelativePaths(ITaskItem[] items, string prefix, string suffix)
        {
            TemplateHelper.WriteRelativePaths(this, items, prefix, suffix);
        }

        private void WriteRelativePaths(string[] items, string prefix, string suffix)
        {
            TemplateHelper.WriteRelativePaths(this, items, prefix, suffix);
        }

        private void WriteRelativePathsTuple(string[] items1, string[] items2, string prefix, string suffix)
        {
            TemplateHelper.WriteRelativePathsTuple(this, items1, items2, prefix, suffix);
        }

        private bool HasDependencyLibrariesReferences()
        {
            return TemplateHelper.HasDependencyLibrariesReferences(LinkItem);
        }

        private void WriteDependencyLibrariesReferences()
        {
            TemplateHelper.WriteDependencyLibrariesReferences(this, LinkItem);
        }
    }
}
