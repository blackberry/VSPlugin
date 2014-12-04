using System;
using System.Collections.Generic;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Class to communicate with a service running on the device, responsible for launching any remote tools.
    /// </summary>
    public sealed class TargetServiceLauncher : TargetService
    {
        public const uint LaunchDefault = 0;
        public const uint LaunchSuspended = 0x8000;
        private const uint LaunchFlagMask = 0xFFFF;

        internal const int ConsoleOutputsChunkSize = 32 * 1024;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceLauncher(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return "LauncherService";
        }

        /// <summary>
        /// Executes specified command on a target device.
        /// </summary>
        public T Start<T>(string command, string[] arguments) where T : TargetProcess
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            return Start<T>(command, arguments, null, null, string.Empty, LaunchDefault);
        }

        /// <summary>
        /// Executes specified command on a target device.
        /// </summary>
        public TargetProcess Start(string command, string[] arguments)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            return Start<TargetProcess>(command, arguments, null, null, string.Empty, LaunchDefault);
        }

        /// <summary>
        /// Executes specified command on a target device.
        /// </summary>
        public T Start<T>(string command, string[] arguments, IEnumerable<KeyValuePair<string, string>> environmentVariables, string workingDirectory, string pty, uint flags)
            where T : TargetProcess
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            // first establish new connection, so that created process is able to perform all actions asynchronously
            // and not interfere with the current state of the service:
            var newConnection = (QConnConnection) Connection.Clone(ConsoleOutputsChunkSize);

            if (string.IsNullOrEmpty(workingDirectory))
            {
                var pathEndAt = command.LastIndexOf('/');
                if (pathEndAt >= 0)
                {
                    workingDirectory = command.Substring(0, pathEndAt + 1);
                }
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                newConnection.Send(string.Concat("chdir \"", workingDirectory, "\""));
            }

            // disable killing processes by Ctrl+C
            if (pty == null)
            {
                newConnection.Send("set nopty");
            }

            // prepare environment variables:
            if (environmentVariables != null)
            {
                foreach (var ev in environmentVariables)
                {
                    if (string.IsNullOrEmpty(ev.Value))
                    {
                        // remove variable:
                        newConnection.Send(string.Concat("setenv ", ev.Key, "="));
                    }
                    else
                    {
                        // update variable's value:
                        newConnection.Send(string.Concat("setenv ", ev.Key, "=\"", ev.Value, "\""));
                    }
                }
            }

            // initiate the run command to execute:
            var runner = new StringBuilder();
            flags &= LaunchFlagMask;

            runner.Append("start");
            if (flags != 0)
            {
                runner.Append("/flags ");
                runner.Append(flags.ToString("X"));
            }
            runner.Append(" ");
            runner.Append(command); // PH: TODO: some escaping?

            if (arguments != null && arguments.Length > 0)
            {
                foreach (var arg in arguments)
                {
                    runner.Append(" ");
                    runner.Append(arg); // PH: TODO: some escaping?
                }
            }

            // execute and grab the process' ID:
            var startupResult = newConnection.Send(runner.ToString());
            uint pid;

            if (startupResult != null && startupResult.StartsWith("ok ", StringComparison.OrdinalIgnoreCase)
                && uint.TryParse(startupResult.Substring(3), out pid))
            {
                // create new instance of the class wrapping process functionalities:
                return (T)Activator.CreateInstance(typeof(T), this, newConnection, pid, (flags & LaunchSuspended) == LaunchSuspended);
            }

            QTraceLog.WriteLine("Failed to startup the command, result: {0}", startupResult);
            return null;
        }

        /// <summary>
        /// Gets the exit code of the specified process.
        /// </summary>
        public uint GetExitCode(TargetProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            return GetExitCode(process.PID);
        }

        /// <summary>
        /// Gets the exit code for process with specified ID.
        /// </summary>
        public uint GetExitCode(uint pid)
        {
            var exitResult = Connection.Send("getexitcode " + pid);

            if (exitResult != null && exitResult.StartsWith("e:", StringComparison.OrdinalIgnoreCase))
            {
                return uint.Parse(exitResult.Substring(2));
            }

            throw new QConnException("Invalid response for exit-code request (\"" + exitResult + "\")");
        }
    }
}
