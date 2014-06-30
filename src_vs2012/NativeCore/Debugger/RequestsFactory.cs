using System;

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

        public static Request SelectTargetDevice(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            return new Request(string.Concat("target-select qnx ", ip, ":8000"));
        }

        public static Request ListProcesses()
        {
            return new CliRequest("info pidlist");
        }
    }
}
