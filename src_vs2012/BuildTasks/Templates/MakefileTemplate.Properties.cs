using System;
using System.Collections.Generic;
using System.Reflection;
using BlackBerry.NativeCore;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks.Templates
{
    partial class MakefileTemplate
    {
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
            get;
            set;
        }

        public string[] AdditionalLibraryDirectories
        {
            get;
            set;
        }

        #endregion

        private static bool ShouldEscape(string text)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(' ') > 0;
        }

        /// <summary>
        /// Gets the list of rooted directories. All relative ones are expanded using specified project's location.
        /// </summary>
        public static string[] GetRootedDirs(string projectDir, string[] items)
        {
            if (string.IsNullOrEmpty(projectDir))
            {
                if (items == null)
                    return new string[0];
                return items;
            }

            if (items == null)
                return new string[0];

            var result = new string[items.Length];

            // prepend ProjectDir to relative paths:
            for (int i = 0; i < items.Length; i++)
            {
                if (!string.IsNullOrEmpty(items[i]) && !items[i].StartsWith("\\\\") && items[i].IndexOf(':') < 0)
                    result[i] = projectDir + items[i];
                else
                    result[i] = items[i];
            }

            return result;
        }

        private string GetFullPath8dot3(ITaskItem item)
        {
            var path = item.GetMetadata("FullPath");
            return NativeMethods.GetShortPathName(path).Replace('\\', '/');
        }

        private void Write8dot3Collection(ITaskItem[] items, string prefix, string suffix)
        {
            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print first item without prefix:
                Write(GetFullPath8dot3(items[0]));
                Write(suffix);
                WriteLine(" \\");

                // print middle items:
                for (int i = 1; i < lastIndex; i++)
                {
                    Write(prefix);
                    Write(GetFullPath8dot3(items[i]));
                    Write(suffix);
                    WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                Write(prefix);
                Write(GetFullPath8dot3(items[lastIndex]));
                WriteLine(suffix);
            }
        }

        private void WriteNameCollection(ITaskItem[] items, string prefix, string suffix)
        {
            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print first item without prefix:
                Write(items[0].GetMetadata("Filename"));
                Write(suffix);
                WriteLine(" \\");

                // print middle items:
                for (int i = 1; i < lastIndex; i++)
                {
                    Write(prefix);
                    Write(items[i].GetMetadata("Filename"));
                    Write(suffix);
                    WriteLine(" \\");
                }

                // print last item without suffix:
                Write(prefix);
                Write(items[lastIndex].GetMetadata("Filename"));
                WriteLine(suffix);
            }
        }

        private void WriteCollection(IEnumerable<string> items, string prefix)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        Write(prefix);

                        if (ShouldEscape(item))
                        {
                            Write("\"");
                            Write(item);
                            Write("\"");
                        }
                        else
                        {
                            Write(item);
                        }

                        Write(" ");
                    }
                }
            }
        }
    }
}
