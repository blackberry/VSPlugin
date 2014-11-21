using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Class to communicate with a System-Info Service on target.
    /// It specifies information about the host.
    /// </summary>
    public sealed class TargetServiceSysInfo : TargetService
    {
        #region Private Classes

        struct Header
        {
            public int version;
            public int status;
            public int nbytes;
            public int bbytes;
            public int nelems;
            public int flags;
            public int sinfo_rate;

            public void Read(IDataReader reader)
            {
                if (reader == null)
                    throw new ArgumentNullException("reader");

                version = reader.ReadInt32();
                status = reader.ReadInt32();
                nbytes = reader.ReadInt32();
                bbytes = reader.ReadInt32();
                nelems = reader.ReadInt32();
                flags = reader.ReadInt32();
                sinfo_rate = reader.ReadInt32();
            }
        }

        /*
        sealed class SystemInfoPid
        {
            public int pid;
            public int parent;
            public int flags;
            public int umask;
            public int child;
            public int sibling;
            public int pgrp;
            public int sid;
            public long base_address;
            public long initial_stack;
            public int uid;
            public int gid;
            public int euid;
            public int egid;
            public int suid;
            public int sgid;
            public long sig_ignore;
            public long sig_queue;
            public long sig_pending;
            public long num_chancons;
            public long num_fdcons;
            public long num_threads;
            public long num_timers;
            public long start_time;
            public long utime;
            public long stime;
            public long cutime;
            public long cstime;
            public long codesize;
            public long datasize;
            public long stacksize;
            public long vstacksize;
            public char name[128];
        }
         */

        #endregion

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceSysInfo(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        public override string ToString()
        {
            return "SysInfoService";
        }

        /// <summary>
        /// Gets the list of running processes.
        /// </summary>
        public SystemInfoProcess[] LoadProcesses()
        {
            var reader = Connection.Request("get pids");

            // read info about payload:
            var header = new Header();
            header.Read(reader);

            // read payload:
            var result = new List<SystemInfoProcess>();
            for (int i = 0; i < header.nelems; i++)
            {
                uint id = reader.ReadUInt32();
                uint parentID = reader.ReadUInt32();
                reader.Skip(40 * 4); // some other non-interesting fields

                // read the process name, which is a NUL-terminated string with max 128 chars
                string name = reader.ReadString(128u, '\0');

                // skip remaining name buffer:
                if (name.Length + 1 < 128)
                {
                    reader.Skip(127 - name.Length);
                }

                if (id != 0 && parentID != 0 && !string.IsNullOrEmpty(name))
                {
                    result.Add(new SystemInfoProcess(id, parentID, name));
                }
                else
                {
#if DEBUG
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
#endif
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Searches for a running process with specified executable (full name or partial).
        /// It will return null, if not found.
        /// </summary>
        public SystemInfoProcess FindProcess(string executable)
        {
            if (string.IsNullOrEmpty(executable))
                throw new ArgumentNullException("executable");

            return (SystemInfoProcess) ProcessInfo.Find(LoadProcesses(), executable);
        }
    }
}
