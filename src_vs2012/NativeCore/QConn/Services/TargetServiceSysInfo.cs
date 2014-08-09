using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Service providing access to system information about the target.
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
            public string name;
        }
         */

        #endregion

        public TargetServiceSysInfo(Version version, IQConnReader source)
            : base("sinfo", version, source)
        {
        }

        public override string ToString()
        {
            return "SysInfoService";
        }

        public SystemInfoProcess[] LoadProcesses()
        {
            Select();
            var reader = Send("get pids");

            // read info about payload:
            var header = new Header();
            header.Read(reader);

            // read payload:
            var result = new List<SystemInfoProcess>();
            for (int i = 0; i < header.nelems; i++)
            {
                uint id = reader.ReadUInt32();
                reader.Skip(41 * 4); // some other non-interesting fields
                string name = reader.ReadString();

                result.Add(new SystemInfoProcess(id, name));
            }

            return result.ToArray();
        }
    }
}
