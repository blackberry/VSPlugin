using System;
using System.Reflection;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks.Templates
{
    partial class ConfigPriTemplate
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

        public string PrecompiledHeaderName
        {
            get;
            set;
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

        private static bool IsDirSeparator(char c)
        {
            return c == '\\' || c == '/';
        }

        private void WritePath(params string[] segments)
        {
            if (segments != null)
            {
                bool trailing = false;
                foreach (var s in segments)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        continue;
                    }

                    if (trailing)
                    {
                        if (IsDirSeparator(s[0]))
                        {
                            Write(s.Substring(1));
                            if (s.Length > 1)
                                trailing = IsDirSeparator(s[s.Length - 1]);
                            continue;
                        }
                    }
                    Write(s);
                    trailing = IsDirSeparator(s[s.Length - 1]);
                }
            }
        }

        private void WriteRelativePaths(ITaskItem[] items, string prefix, string suffix)
        {
            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    WritePath(prefix, items[i].ItemSpec.Replace('\\', '/'), suffix);
                    WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                WritePath(prefix, items[lastIndex].ItemSpec.Replace('\\', '/'), suffix);
                WriteLine(string.Empty);
            }
        }

        private void WriteRelativePaths(string[] items, string prefix, string suffix)
        {
            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    WritePath(prefix, items[i].Replace('\\', '/'), suffix);
                    WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                WritePath(prefix, items[lastIndex].Replace('\\', '/'), suffix);
                WriteLine(string.Empty);
            }
        }

        private void WriteRelativePathsTuple(string[] items1, string[] items2, string prefix, string suffix)
        {
            if (items1 != null && items1.Length > 0)
            {
                if (items2 == null || items2.Length == 0)
                {
                    WriteRelativePaths(items1, prefix, suffix);
                    return;
                }

                int lastIndex1 = items1.Length - 1;
                int lastIndex2 = items2.Length - 1;

                // print items:
                for (int i = 0; i <= lastIndex1; i++)
                {
                    for (int j = 0; j <= lastIndex2; j++)
                    {
                        WritePath(prefix, items1[i].Replace('\\', '/'), "/", items2[j], suffix);

                        if (i != lastIndex1 || j != lastIndex2)
                        {
                            WriteLine(" \\");
                        }
                        else
                        {
                            WriteLine(string.Empty);
                        }
                    }
                }
            }
        }
    }
}
