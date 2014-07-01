using System;
using System.IO;
using System.Text;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Factory class to produce GDB requests.
    /// </summary>
    public static class RequestsFactory
    {
        public static Request Exit()
        {
            return new Request("gdb-exit");
        }

        public static Request ListFeatures()
        {
            return new Request("list-features");
        }

        public static Request ListTargetFeatures()
        {
            return new Request("list-target-features");
        }

        public static Request SetTargetDevice(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            return new Request(string.Concat("target-select qnx ", ip, ":8000"));
        }

        public static Request ListProcesses()
        {
            return new CliRequest("info pidlist");
        }

        public static Request SetPendingBreakpoints(bool on)
        {
            return new Request("gdb-set breakpoint pending " + (on ? "on" : "off"));
        }

        public static Request SetExecutable(string exeFileName, bool hasSymbols)
        {
            if (string.IsNullOrEmpty(exeFileName))
                throw new ArgumentNullException("exeFileName");
            if (!File.Exists(exeFileName))
                throw new FileNotFoundException("Specified executable doesn't exist", exeFileName);

            if (hasSymbols)
                return new Request("file-exec-and-symbols " + exeFileName.Replace("\\", "\\\\"));
            return new Request("file-exec-file " + exeFileName.Replace("\\", "\\\\"));
        }

        public static Request SetLibrarySearchPath(string[] searchPaths)
        {
            if (searchPaths == null || searchPaths.Length == 0)
                throw new ArgumentNullException("searchPaths");

            // serialize paths:
            var paths = new StringBuilder();
            foreach (var p in searchPaths)
            {
                paths.Append(p.Replace("\\", "\\\\")).Append(';');
            }

            // and return the prepared request:
            return new CliRequest("set solib-search-path " + paths);
        }

        public static Request AttachTargetProcess(uint pid)
        {
            return new Request("target-attach " + pid);
        }

        public static Request DetachTargetProcess()
        {
            return new Request("target-detach");
        }

        public static Request StackTraceListFrames()
        {
            return new Request("stack-list-frames");
        }

        /// <summary>
        /// Creates a group request.
        /// </summary>
        public static RequestGroup Group(params Request[] requests)
        {
            if (requests != null)
            {
                var group = new RequestGroup();
                foreach (var r in requests)
                {
                    if (r != null)
                        group.Add(r);
                }

                return group;
            }

            return null;
        }
    }
}
