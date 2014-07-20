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
            return new MiRequest("gdb-exit");
        }

        public static Request Break()
        {
            return new BreakRequest();
        }

        public static Request ListFeatures()
        {
            return new MiRequest("list-features");
        }

        public static Request ListTargetFeatures()
        {
            return new MiRequest("list-target-features");
        }

        public static Request SetTargetDevice(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            return new MiRequest(string.Concat("target-select qnx ", ip, ":8000"));
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
            return new MiRequest("gdb-set breakpoint pending " + (on ? "on" : "off"));
        }

        public static Request SetExecutable(string exeFileName, bool hasSymbols)
        {
            if (string.IsNullOrEmpty(exeFileName))
                throw new ArgumentNullException("exeFileName");
            if (!File.Exists(exeFileName))
                throw new FileNotFoundException("Specified executable doesn't exist", exeFileName);

            if (hasSymbols)
                return new MiRequest("file-exec-and-symbols " + exeFileName.Replace("\\", "\\\\"));
            return new MiRequest("file-exec-file " + exeFileName.Replace("\\", "\\\\"));
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
            return new MiRequest("target-attach " + pid);
        }

        public static Request InfoThreads()
        {
            return new CliRequest("info threads");
        }

        public static Request DetachTargetProcess()
        {
            return new MiRequest("target-detach");
        }

        public static Request SetStackTraceDepth(int threadID, int depth)
        {
            if (depth < 0)
                return new MiRequest("stack-info-depth --thread " + threadID + " --frame 0");
            return new MiRequest("stack-info-depth " + depth + " --thread " + threadID + " --frame 0");
        }

        public static Request StackTraceListFrames()
        {
            return new MiRequest("stack-list-frames");
        }

        public static Request InsertBreakPoint(string fileName, uint line)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            return new MiRequest("break-insert --thread-group i1 -f " + fileName + ":" + line);
        }

        public static Request InsertBreakpoint(string functionName)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException("functionName");

            return new MiRequest("break-insert --thread-group i1 -f " + functionName);
        }

        public static Request DeleteBreakpoint(params uint[] breakpointIDs)
        {
            if (breakpointIDs == null || breakpointIDs.Length == 0)
                throw new ArgumentNullException("breakpointIDs");

            if (breakpointIDs.Length == 1)
                return new MiRequest("break-delete " + breakpointIDs[0]);

            var identifiers = new StringBuilder();
            for (int i = 0; i < identifiers.Length; i++)
            {
                identifiers.Append(i).Append(' ');
            }

            return new MiRequest("break-delete " + identifiers);
        }

        public static Request Continue()
        {
            return new MiRequest("exec-continue");
        }
        
        public static Request Run()
        {
            return new MiRequest("exec-run");
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
