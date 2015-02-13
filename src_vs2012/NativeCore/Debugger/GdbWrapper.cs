using System;
using System.Collections.Generic;
using System.IO;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger.Model;
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
        #region Internal Classes

        /// <summary>
        /// Class to store context for specified requests, that expect async responses.
        /// </summary>
        sealed class AsyncContext
        {
            public AsyncContext(Request request, Instruction instruction)
            {
                if (request == null)
                    throw new ArgumentNullException("request");
                if (instruction == null)
                    throw new ArgumentNullException("instruction");

                Request = request;
                Instruction = instruction;
            }

            #region Properties

            public Request Request
            {
                get;
                private set;
            }

            public Instruction Instruction
            {
                get;
                private set;
            }

            #endregion

            public override string ToString()
            {
                return Request.ToString();
            }
        }

        #endregion

        private static GdbRunner _gdbRunner;
        private static readonly InstructionCollection Instructions = InstructionCollection.Load();
        private static readonly Dictionary<uint, AsyncContext> Context = new Dictionary<uint, AsyncContext>(); 

        private const uint LastSyncInstructionID = 50;

        /// <summary>
        /// Event send each time a response is received by GDB processor created during attaching to process.
        /// </summary>
        public static event EventHandler<ResponseParsedEventArgs> Received;

        /// <summary>
        /// Event send, when GDB unexpectedly crashed or was killed by developer.
        /// </summary>
        public static event EventHandler UnexpectedlyClosed;

        /// <summary>
        /// Attaches to the process with given PID or binary name on a target device.
        /// </summary>
        /// <param name="pidOrBinaryName">PID of the process or name of the binary.</param>
        /// <param name="localBinaryPath">Local path to the executable being debugged. It is required by GDB to load debug symbols.</param>
        /// <param name="existingProcessOrNull">Info about existing process running on the device or null, if not known.</param>
        /// <param name="ndk">Location of the NDK used to compile the binary.</param>
        /// <param name="device">Target device to connect to.</param>
        /// <param name="runtime">Path to the runtime with matching device libraries. Used to decipher OS callstacks.</param>
        /// <param name="pidNumber">Numerical PID of the process the GDB attached to.</param>
        /// <returns>Returns 'true', when attaching succeeded, otherwise 'false'. It will automatically close GDB, if anything failed.</returns>
        public static bool AttachToProcess(string pidOrBinaryName, string localBinaryPath, ProcessInfo existingProcessOrNull, NdkDefinition ndk, DeviceDefinition device, RuntimeDefinition runtime, out uint pidNumber)
        {
            if (ndk == null)
                throw new ArgumentNullException("device");
            if (device == null)
                throw new ArgumentNullException("device");

            pidNumber = 0;
            Targets.Connect(device, ConfigDefaults.SshPublicKeyPath, ConfigDefaults.SshPrivateKeyPath, null);

            // establish GDB connection:
            var gdbInfo = new GdbInfo(ndk, device, runtime, null);
            if (_gdbRunner != null)
            {
                _gdbRunner.Dispose();
            }

            // PH: INFO:
            // Here create a dedicated runner to let us communicate with GDB. We use a 'hosted' version
            // as communication is done via BlackBerry.GDBHost.exe (not directly as it would be done using GdbRunner).
            // The reason is simple - this is the ONLY way to send BreakRequest (aka Ctrl+C) to the GDB, while
            // it's running program. This allows to pause current binary execution at any time and inject other commands.
            // Second trick we do here is using a dedicated 'event dispatcher'. This async dispatcher simply instantiates
            // single background-thread and all event notifications from GDB are sent from that thread with order kept.
            // It is required since the current DE architecture assumes that blocking calls of SendCommand() can be
            // send immediately when processing asynchronous GDB notification. That, without an extra thread,
            // would lead to a total blockage of whole communication, as we would block in the reading thread
            // and wait for data it was supposed to produce.
            _gdbRunner = new GdbHostRunner(ConfigDefaults.GdbHostPath, gdbInfo);
            _gdbRunner.Finished += GdbRunnerUnexpectedlyFinished;
            _gdbRunner.Processor.Dispatcher = EventDispatcher.NewAsync("Default GDB events dispatcher");
            _gdbRunner.Processor.Received += GdbRunnerResponseReceived;
            _gdbRunner.ExecuteAsync();

            // setup environment:
            var parsed = _gdbRunner.Send(RequestsFactory.SetPendingBreakpoints(true), Instructions[8]);
            if (parsed == string.Empty || parsed[0] == '!')
            {
                _gdbRunner.Send(RequestsFactory.Exit());
                return false;
            }

            // select device:
            // and select current target device:
            parsed = _gdbRunner.Send(RequestsFactory.SetTargetDevice(_gdbRunner.Device), Instructions[3]);
            if (parsed == string.Empty || parsed[0] == '!')
            {
                _gdbRunner.Send(RequestsFactory.Exit());
                return false;
            }

            // load PID of the process to attach:
            if (!uint.TryParse(pidOrBinaryName, out pidNumber))
            {
                ///////////////////////////////////////////////////////
                // "the old way" - which seems not to work on PlayBook:
                /*
                var listRequest = RequestsFactory.ListProcesses();
                _gdbRunner.Send(listRequest);

                // wait for response:
                listRequest.Wait();
                var existingProcess = listRequest.Find(pidOrBinaryName);
                 */
                ///////////////////////////////////////////////////////

                // this should succeed as we already used GDB successfully, so secure communication is working...
                var qClient = Targets.Get(device);
                if (qClient == null || qClient.FileService == null)
                {
                    throw new InvalidOperationException("Missing the client connected to target - this should never happen");
                }

                var existingProcess = qClient.SysInfoService.FindProcess(pidOrBinaryName);
                if (existingProcess != null)
                {
                    // doesn't run
                    pidNumber = existingProcess.ID;

                    // start monitoring for console logs:
                    Targets.Trace(device, existingProcess, true);
                }
                else
                {
                    _gdbRunner.Send(RequestsFactory.Exit());
                    return false;
                }
            }
            else
            {
                if (existingProcessOrNull != null)
                {
                    // start monitoring for console logs:
                    Targets.Trace(device, existingProcessOrNull, true);
                }
            }

            // symbols paths:
            parsed = _gdbRunner.Send(RequestsFactory.SetLibrarySearchPath(_gdbRunner), Instructions[7]);
            if (parsed == string.Empty || parsed[0] == '!')
            {
                _gdbRunner.Send(RequestsFactory.Exit());
                return false;
            }

            if (!string.IsNullOrEmpty(localBinaryPath) && File.Exists(localBinaryPath))
            {
                parsed = _gdbRunner.Send(RequestsFactory.SetExecutable(localBinaryPath, true), Instructions[8]);

                // PH: TODO: HINT: potentially we could ignore the error here
                if (parsed == string.Empty || parsed[0] == '!')
                {
                    _gdbRunner.Send(RequestsFactory.Exit());
                    return false;
                }
            }

            // and attach:
            parsed = _gdbRunner.Send(RequestsFactory.AttachTargetProcess(pidNumber), Instructions[6]);
            if (parsed == string.Empty || parsed[0] == '!')
            {
                _gdbRunner.Send(RequestsFactory.Exit());
                return false;
            }

            return true;
        }

        private static void GdbRunnerResponseReceived(object sender, ResponseReceivedEventArgs e)
        {
            // process the data only of asynchronous responses:
            if (IsAsync(e.Response))
            {
                Instruction instruction;
                string parsedResponse;

                // process notifications:
                foreach (var notification in e.Response.Notifications)
                {
                    var name = GetNotificationName(notification);

                    if (!string.IsNullOrEmpty(name))
                    {
                        string param;
                        instruction = Instructions.Find(name, out param);
                        if (instruction != null)
                        {
                            parsedResponse = instruction.Parse(notification + "\r\n");
                            NotifyParsedResponse(e, parsedResponse);
                        }
                    }
                }

                // process result records and status changes:
                var context = GetContext(e.Response);
                instruction = context != null ? context.Instruction : Instructions[0];

                parsedResponse = instruction.Parse(e.Response);
                NotifyParsedResponse(e, parsedResponse);
            }

            // schedule all responses to be removed from the source queue:
            e.Handled = true;
        }

        private static string GetNotificationName(string notification)
        {
            if (string.IsNullOrEmpty(notification))
                return null;

            int endAt = notification.IndexOf(',');
            return endAt < 0 ? notification : notification.Substring(0, endAt);
        }

        private static void NotifyParsedResponse(ResponseReceivedEventArgs e, string parsedResponse)
        {
            if (!string.IsNullOrEmpty(parsedResponse) && parsedResponse != ";")
            {
                // notify about the response:
                var handler = Received;
                if (handler != null)
                {
                    handler(null, new ResponseParsedEventArgs(e, parsedResponse));
                }
            }
        }

        private static void GdbRunnerUnexpectedlyFinished(object sender, ToolRunnerEventArgs e)
        {
            _gdbRunner = null;

            // notify, that it unexpectedly closed:
            var handler = UnexpectedlyClosed;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Closes communication channel with GDB.
        /// </summary>
        public static void Exit()
        {
            if (_gdbRunner != null)
            {
                // stop all tracing for specified device:
                Targets.TraceStop(_gdbRunner.GDB.Device);

                _gdbRunner.Finished -= GdbRunnerUnexpectedlyFinished;
                _gdbRunner.Dispose();
                _gdbRunner = null;
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
        /// Gets an indication, if specified response is asynchronous.
        /// </summary>
        private static bool IsAsync(Response response)
        {
            if (response == null)
                return false;

            return response.ID == 0 || response.ID >= LastSyncInstructionID;
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

            Targets.Connect(device, ConfigDefaults.SshPublicKeyPath, ConfigDefaults.SshPrivateKeyPath, null);

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
        /// Sends a synchronous GDB command and waiting for the respective GDB response. This method is called by the Debug Engine whenever it needs a GDB response for a given GDB command.
        /// </summary>
        /// <param name="command">Command to be sent to GDB.</param>
        /// <param name="instructionID">Instruction ID.</param>
        public static string SendCommand(string command, uint instructionID)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (instructionID >= LastSyncInstructionID)
                throw new ArgumentOutOfRangeException("instructionID");
            if (_gdbRunner == null)
                throw new InvalidOperationException("Unable to send the command");

            string parsingParam;
            var instruction = Instructions.Find(command, out parsingParam);
            if (instruction == null)
                throw new ArgumentOutOfRangeException("command", "Specified command has no matching parsing instruction");

            var request = new Request(instructionID, command);

            _gdbRunner.Send(request);
            var hasResponse = request.Wait();

            // check if data was received:
            if (!hasResponse || request.Response == null || request.Response.RawData == null)
            {
                return "TIMEOUT!";
            }

            // parse response:
            var parsedResponse = instruction.Parse(request.Response);
            return parsedResponse;
        }

        /// <summary>
        /// Sends an asynchronous GDB command. This method is called by the Debug Engine whenever it needs to send a GDB command without having to wait for the respective GDB response.
        /// </summary>
        /// <param name="command">Command to be sent to GDB.</param>
        public static void PostCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");
            if (_gdbRunner == null)
                throw new InvalidOperationException("Unable to send the command");

            string parsingParam;
            var instruction = Instructions.Find(command, out parsingParam);
            if (instruction == null)
                throw new ArgumentOutOfRangeException("command", "Specified command has no matching parsing instruction");

            Request request;

            // schedule execution of a request, but don't wait for a response:
            if (string.Compare("-exec-interrupt", command, StringComparison.Ordinal) == 0)
            {
                request = RequestsFactory.Break();
            }
            else
            {
                request = new Request(command);
            }

            SetContext(request, instruction);
            _gdbRunner.Send(request);
        }

        private static void SetContext(Request request, Instruction instruction)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (instruction == null)
                throw new ArgumentNullException("instruction");

            lock (Context)
            {
                Context[request.ID] = new AsyncContext(request, instruction);
            }
        }

        private static AsyncContext GetContext(uint id)
        {
            AsyncContext result;
                
            lock (Context)
            {
                Context.TryGetValue(id, out result);
            }

            return result;
        }

        private static AsyncContext GetContext(Response response)
        {
            return response != null ? GetContext(response.ID) : null;
        }
    }
}
