using System.Runtime.InteropServices;
using BlackBerry.NativeCore.Debugger.Model;

namespace BlackBerry.NativeCore.Services
{
    /// <summary>
    /// Service dedicated to attaching to a process running on target.
    /// It allows discovering matching local executable (binary) from currently active projects,
    /// so that the debugger can issue breakpoints and step through the code.
    /// </summary>
    [Guid("71465b7f-1730-4d55-8e5d-65ddb14a0ce1")]
    public interface IAttachDiscoveryService
    {
        /// <summary>
        /// Find path to local executable matching running process.
        /// </summary>
        string FindExecutable(ProcessInfo process);
    }
}
