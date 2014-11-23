using System;
using System.Collections.Generic;
using System.IO;
using BlackBerry.NativeCore;
using Microsoft.Build.Framework;

namespace BlackBerry.BuildTasks.Helpers
{
    /// <summary>
    /// Helper methods for template-generator.
    /// </summary>
    static class TemplateHelper
    {
        /// <summary>
        /// Interface implemented by template-generator to let the helper class perform more complex actions.
        /// </summary>
        public interface IWriter
        {
            void Write(string text);
            void WriteLine(string text);
        }

        /// <summary>
        /// Checks, if path should be escaped.
        /// </summary>
        public static bool ShouldEscape(string text)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf(' ') > 0;
        }

        public static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? null : path.Replace('\\', '/');
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
                    result[i] = Normalize(projectDir + items[i]);
                else
                    result[i] = Normalize(items[i]);
            }

            return result;
        }

        /// <summary>
        /// Writes a collection of space-separated items, where each has given prefix.
        /// </summary>
        public static void WriteCollection(IWriter output, IEnumerable<string> items, string prefix, string separator)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        output.Write(prefix);

                        if (ShouldEscape(item))
                        {
                            output.Write("\"");
                            output.Write(item);
                            output.Write("\"");
                        }
                        else
                        {
                            output.Write(item);
                        }

                        if (!string.IsNullOrEmpty(separator))
                        {
                            output.Write(separator);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes the paths, so they are correctly processed by makefile.
        /// </summary>
        public static string[] NormalizePaths(ICollection<string> collection)
        {
            if (collection == null)
                return null;

            var result = new string[collection.Count];
            int i = 0;

            foreach (var item in collection)
            {
                result[i++] = NormalizePath(item);
            }

            return result;
        }

        /// <summary>
        /// Normalizes the single path.
        /// </summary>
        public static string NormalizePath(string path)
        {
            return Normalize(Path.GetFullPath(path));
        }

        /// <summary>
        /// Gets the full path to the item.
        /// </summary>
        public static string GetFullPath(ITaskItem item)
        {
            return Normalize(item.GetMetadata("FullPath"));
        }

        /// <summary>
        /// Gets the short name of the file.
        /// </summary>
        public static string GetFullPath8Dot3(ITaskItem item)
        {
            var path = item.GetMetadata("FullPath");
            return Normalize(NativeMethods.GetShortPathName(path));
        }

        /// <summary>
        /// Writes a collection of specified ITaskItems, using their 8.3-style full paths, where each item has specified prefix and suffix.
        /// </summary>
        public static void Write8Dot3Collection(IWriter output, ITaskItem[] items, string prefix, string suffix)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    output.Write(prefix);
                    output.Write(GetFullPath8Dot3(items[i]));
                    output.Write(suffix);
                    output.WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                output.Write(prefix);
                output.Write(GetFullPath8Dot3(items[lastIndex]));
                output.WriteLine(suffix);
            }
        }

        /// <summary>
        /// Writes a collection of specified ITaskItem names, where each item has specified prefix and suffix.
        /// </summary>
        public static void WriteNameCollection(IWriter output, ITaskItem[] items, string prefix, string suffix)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    output.Write(prefix);
                    output.Write(items[i].GetMetadata("Filename"));
                    output.Write(suffix);
                    output.WriteLine(" \\");
                }

                // print last item without suffix:
                output.Write(prefix);
                output.Write(items[lastIndex].GetMetadata("Filename"));
                output.WriteLine(suffix);
            }
        }

        private static bool IsDirSeparator(char c)
        {
            return c == '\\' || c == '/';
        }

        /// <summary>
        /// Writes path made of segments into the output. Segment can be several folder names.
        /// </summary>
        public static void WritePath(IWriter output, params string[] segments)
        {
            if (output == null)
                throw new ArgumentNullException("output");

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
                            output.Write(s.Substring(1));
                            if (s.Length > 1)
                                trailing = IsDirSeparator(s[s.Length - 1]);
                            continue;
                        }
                    }
                    output.Write(s);
                    trailing = IsDirSeparator(s[s.Length - 1]);
                }
            }
        }

        /// <summary>
        /// Writes a collection of relative paths from specified task-items. Each can have prefix and suffix.
        /// </summary>
        public static void WriteRelativePaths(IWriter output, ITaskItem[] items, string prefix, string suffix)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    WritePath(output, prefix, Normalize(items[i].ItemSpec), suffix);
                    output.WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                WritePath(output, prefix, Normalize(items[lastIndex].ItemSpec), suffix);
                output.WriteLine(string.Empty);
            }
        }

        public static void WriteRelativePaths(IWriter output, string[] items, string prefix, string suffix)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items != null && items.Length > 0)
            {
                int lastIndex = items.Length - 1;

                // print items:
                for (int i = 0; i < lastIndex; i++)
                {
                    WritePath(output, prefix, Normalize(items[i]), suffix);
                    output.WriteLine(" \\");
                }

                // print last item without 'move to next line char':
                WritePath(output, prefix, Normalize(items[lastIndex]), suffix);
                output.WriteLine(string.Empty);
            }
        }

        public static void WriteRelativePathsTuple(IWriter output, string[] items1, string[] items2, string prefix, string suffix)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (items1 != null && items1.Length > 0)
            {
                if (items2 == null || items2.Length == 0)
                {
                    WriteRelativePaths(output, items1, prefix, suffix);
                    return;
                }

                int lastIndex1 = items1.Length - 1;
                int lastIndex2 = items2.Length - 1;

                // print items:
                for (int i = 0; i <= lastIndex1; i++)
                {
                    for (int j = 0; j <= lastIndex2; j++)
                    {
                        WritePath(output, prefix, Normalize(items1[i]), "/", items2[j], suffix);

                        if (i != lastIndex1 || j != lastIndex2)
                        {
                            output.WriteLine(" \\");
                        }
                        else
                        {
                            output.WriteLine(string.Empty);
                        }
                    }
                }
            }
        }

        public static bool HasDependencyLibrariesReferences(ITaskItem linkItem)
        {
            return linkItem != null && !string.IsNullOrEmpty(linkItem.GetMetadata("AdditionalDependencies"));
        }

        public static void WriteDependencyLibrariesReferences(IWriter output, ITaskItem linkItem)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            if (linkItem != null)
            {
                // Visual Studio doesn't have separate lists for static vs. dynamic dependencies.
                // To differentiate between the two, include the extension in the list - `.a for static, `.so for dynamic.
                // If no extension is included in the name it will be treated as the same type
                // as the last declared type, and the default type is static. This is the way qcc would work
                // if you were using it from the command line with the "-Bstatic" and "-Bdynamic" flags.

                var libs = linkItem.GetMetadata("AdditionalDependencies").Split(';');
                for (int i = 0; i < libs.Length; i++)
                {
                    if (libs[i].EndsWith(".so"))
                    {
                        output.Write("-Bdynamic -l");
                        output.Write(libs[i].Substring(0, libs[i].Length - 3));
                    }
                    else if (libs[i].EndsWith(".a"))
                    {
                        output.Write("-Bstatic -l");
                        output.Write(libs[i].Substring(0, libs[i].Length - 2));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(libs[i]))
                        {
                            output.Write("-l");
                            output.Write(libs[i]);
                        }
                    }

                    output.Write(" ");
                }
            }
        }
    }
}
