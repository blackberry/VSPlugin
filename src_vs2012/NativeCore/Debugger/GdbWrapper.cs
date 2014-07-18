using System;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger.Requests;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Wrapper class for calls, that used to be exposed by C++ GDBWrapper project.
    /// </summary>
    public static class GdbWrapper
    {
        private static GdbRunner _gdbRunner;

        /// <summary>
        /// Event send each time a response is received by GDB processor created during attaching to process.
        /// </summary>
        public static event EventHandler<ResponseReceivedEventArgs> Received;

        /// <summary>
        /// Attaches to the process with given PID or binary name on a target device.
        /// </summary>
        /// <param name="pidOrBinaryName">PID of the process or name of the binary.</param>
        /// <param name="localBinaryPath">Local path to the executable being debugged. It is required by GDB to load debug symbols.</param>
        /// <param name="ndk">Location of the NDK used to compile the binary.</param>
        /// <param name="device">Target device to connect to.</param>
        /// <param name="runtime">Path to the runtime with matching device libraries. Used to decipher OS callstacks.</param>
        /// <returns>Returns 'true', when attaching succeeded, otherwise 'false'. It will automatically close GDB, if anything failed.</returns>
        public static bool AttachToProcess(string pidOrBinaryName, string localBinaryPath, NdkDefinition ndk, DeviceDefinition device, RuntimeDefinition runtime)
        {
            if (ndk == null)
                throw new ArgumentNullException("device");
            if (device == null)
                throw new ArgumentNullException("device");

            Targets.Connect(device, ConfigDefaults.SshPublicKeyPath, null);

            // establish GDB connection:
            var gdbInfo = new GdbInfo(ndk, device, runtime, null);
            if (_gdbRunner != null)
            {
                _gdbRunner.Dispose();
            }
            _gdbRunner = new GdbHostRunner(ConfigDefaults.GdbHostPath, gdbInfo);
            _gdbRunner.Finished += GdbRunnerFinished;
            _gdbRunner.Processor.Received += GdbRunnerResponseReceived;
            _gdbRunner.ExecuteAsync();

            // select device:
            // and select current target device:
            _gdbRunner.Send(RequestsFactory.SetTargetDevice(_gdbRunner.Device));

            uint pidNumber;

            // load PID of the process to attach:
            if (!uint.TryParse(pidOrBinaryName, out pidNumber))
            {
                var listRequest = RequestsFactory.ListProcesses();
                _gdbRunner.Send(listRequest);

                // wait for response:
                listRequest.Wait();
                var existingProcess = listRequest.Find(pidOrBinaryName);
                if (existingProcess != null)
                {
                    // doesn't run
                    pidNumber = existingProcess.ID;
                }
                else
                {
                    _gdbRunner.Send(RequestsFactory.Exit());
                    return false;
                }
            }

            // and attach:
            var setLibSearchPath = RequestsFactory.SetLibrarySearchPath(_gdbRunner);
            var setExecutable = string.IsNullOrEmpty(localBinaryPath) ? null : RequestsFactory.SetExecutable(localBinaryPath, true);
            var attachProcess = RequestsFactory.AttachTargetProcess(pidNumber);

            var attachGroup = RequestsFactory.Group(setLibSearchPath, setExecutable, attachProcess);
            _gdbRunner.Send(attachGroup);

            // wait till attached:
            if (!attachGroup.Wait())
            {
                _gdbRunner.Send(RequestsFactory.Exit());
                return false;
            }

            return true;
        }

        private static void GdbRunnerResponseReceived(object sender, ResponseReceivedEventArgs e)
        {
            var handler = Received;
            if (handler != null)
            {
                handler(null, e);
            }

            // schedule all responses to be removed from the source queue:
            e.Handled = true;
        }

        private static void GdbRunnerFinished(object sender, ToolRunnerEventArgs e)
        {
            _gdbRunner = null;
        }

        /// <summary>
        /// Closes communication channel with GDB.
        /// </summary>
        public static void Exit()
        {
            if (_gdbRunner != null)
            {
                _gdbRunner.Dispose();
            }
        }

        /// <summary>
        /// Gets an indication, if there is open GDB connection to target device.
        /// </summary>
        public static bool IsRunning
        {
            get { return _gdbRunner != null && _gdbRunner.IsProcessing; }
        }

        /// <summary>
        /// Lists all running processes from specified target device.
        /// </summary>
        public static ProcessListRequest ListProcesses(NdkDefinition ndk, DeviceDefinition device)
        {
            if (ndk == null)
                throw new ArgumentNullException("device");
            if (device == null)
                throw new ArgumentNullException("device");

            Targets.Connect(device, ConfigDefaults.SshPublicKeyPath, null);

            // start own GDB, if any specified:
            var info = new GdbInfo(ndk, device, null, null);
            var gdb = new GdbRunner(info);
            gdb.ExecuteAsync();

            var selectTarget = RequestsFactory.SetTargetDevice(device);
            gdb.Send(selectTarget);
            if (!selectTarget.Wait() || selectTarget.Response == null || selectTarget.Response.Name == "error")
            {
                // ask the GDB to exit, if created internally:
                gdb.Send(RequestsFactory.Exit());
                return null;
            }

            var listRequest = RequestsFactory.ListProcesses();
            gdb.Send(listRequest);

            // wait for response:
            bool hasResponse = listRequest.Wait();

            // ask GDB to exit, if created internally:
            gdb.Send(RequestsFactory.Exit());
            return hasResponse ? listRequest : null;
        }

        /// <summary>
        /// Sends a synchronous GDB command and waiting for the respective GDB response. This 
        /// method is called by the Debug Engine whenever it needs a GDB response for a given GDB command. 
        /// </summary>
        /// <param name="command">Command to be sent to GDB.</param>
        /// <param name="instructionID">Instruction ID.</param>
        public static string SendCommand(string command, uint instructionID)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (_gdbRunner == null)
                throw new InvalidOperationException("Unable to send the command");

            var request = new Request(string.Concat(instructionID.ToString("D2"), command));

            _gdbRunner.Send(request);
            var hasResponse = request.Wait();

            // check if data was received:
            if (!hasResponse || request.Response == null || request.Response.RawData == null)
            {
                return "TIMEOUT!";
            }

            // forward the data received from GDB:
            return string.Join("\r\n", request.Response.RawData);
        }

        /// <summary>
        /// Sends an asynchronous GDB command. This method is called by the Debug Engine whenever 
        /// it needs to send a GDB command without having to wait for the respective GDB response. 
        /// </summary>
        /// <param name="command">Command to be sent to GDB.</param>
        public static void PostCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (_gdbRunner == null)
                throw new InvalidOperationException("Unable to send the command");

            var request = new Request(command);
            _gdbRunner.Send(request);
        }
    }
}
