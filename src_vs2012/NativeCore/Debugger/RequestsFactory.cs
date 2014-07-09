using System;
using System.IO;
using System.Text;
using BlackBerry.NativeCore.Debugger.Requests;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;

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

        public static Request SetTargetDevice(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            return SetTargetDevice(device.IP);
        }

        public static ProcessListRequest ListProcesses()
        {
            return new ProcessListRequest();
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

        public static Request SetLibrarySearchPath(GdbInfo gdbInfo)
        {
            if (gdbInfo == null)
                throw new ArgumentNullException("gdbInfo");

            return SetLibrarySearchPath(gdbInfo.LibraryPaths);
        }

        public static Request SetLibrarySearchPath(GdbRunner runner)
        {
            if (runner == null)
                throw new ArgumentNullException("runner");

            return SetLibrarySearchPath(runner.GDB.LibraryPaths);
        }

        public static Request AttachTargetProcess(uint pid)
        {
            return new Request("target-attach " + pid);
        }

        public static Request InfoThreads()
        {
            return new CliRequest("info threads");
        }

        public static Request DetachTargetProcess()
        {
            return new Request("target-detach");
        }

        public static Request SetStackTraceDepth(int threadID, int depth)
        {
            if (depth < 0)
                return new Request("stack-info-depth --thread " + threadID + " --frame 0");
            return new Request("stack-info-depth " + depth + " --thread " + threadID + " --frame 0");
        }

        public static Request StackTraceListFrames()
        {
            return new Request("stack-list-frames");
        }

        public static Request InsertBreakPoint(string fileName, uint line)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            return new Request("break-insert --thread-group i1 -f " + fileName + ":" + line);
        }

        public static Request InsertBreakpoint(string functionName)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException("functionName");

            return new Request("break-insert --thread-group i1 -f " + functionName);
        }

        public static Request DeleteBreakpoint(params uint[] breakpointIDs)
        {
            if (breakpointIDs == null || breakpointIDs.Length == 0)
                throw new ArgumentNullException("breakpointIDs");

            if (breakpointIDs.Length == 1)
                return new Request("break-delete " + breakpointIDs[0]);

            var identifiers = new StringBuilder();
            for (int i = 0; i < identifiers.Length; i++)
            {
                identifiers.Append(i).Append(' ');
            }

            return new Request("break-delete " + identifiers);
        }

        public static Request Continue()
        {
            return new Request("exec-continue");
        }
        
        public static Request Run()
        {
            return new Request("exec-run");
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
