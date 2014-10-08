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

        private void WriteRelativePaths(ITaskItem[] items, string prefix, string suffix)
        {
            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                        Write(prefix);
                        Write(items[i].ItemSpec.Replace('\\', '/'));
                        Write(suffix);
                        WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                Write(prefix);
                Write(items[lastIndex].ItemSpec.Replace('\\', '/'));
                WriteLine(suffix);
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
                    Write(prefix);
                    Write(items[i].Replace('\\', '/'));
                    Write(suffix);
                    WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                Write(prefix);
                Write(items[lastIndex].Replace('\\', '/'));
                WriteLine(suffix);
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
                        Write(prefix);
                        Write(items1[i].Replace('\\', '/'));
                        Write("/");
                        Write(items2[j]);
                        Write(suffix);

                        if (i != lastIndex1 && j != lastIndex2)
                        {
                            WriteLine(" \\");
                        }
                        else
                        {
                            WriteLine("");
                        }
                    }
                }
            }
        }
    }
}
