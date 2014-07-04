using System;
using System.Globalization;
using System.Threading;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using NUnit.Framework;
using VSNDK.Parser;

namespace UnitTests
{
    [TestFixture]
    public sealed class DebuggerTests
    {
        #region Device Connection Establishing

        [TestFixtureSetUp]
        public void Setup()
        {
            Targets.Connect(Defaults.IP, Defaults.Password, DeviceDefinitionType.Device, Defaults.SshPublicKeyPath, OnDeviceConnectionStatusChanged);

            while (!Targets.IsConnectedOrFailed(Defaults.IP))
                Thread.Sleep(10);

            Assert.IsTrue(Targets.IsConnected(Defaults.IP), "Connection was not established properly");
        }

        private void OnDeviceConnectionStatusChanged(object sender, TargetConnectionEventArgs e)
        {
            if (e.Status == TargetStatus.Failed)
            {
                Targets.Disconnect(Defaults.IP);
            }
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            Targets.Disconnect(Defaults.IP);
        }

        #endregion

        #region Initialize Debugger

        public static GdbHostRunner CreateDebuggerRunner()
        {
            return CreateDebuggerRunner(null);
        }

        public static GdbHostRunner CreateDebuggerRunner(string runtimeFolder)
        {
            // get the NDK info:
            var ndk = NdkInfo.Scan(Defaults.InstalledNdkPath);
            Assert.IsNotNull(ndk);

            var runtime = string.IsNullOrEmpty(runtimeFolder) ? null : new RuntimeDefinition(runtimeFolder);

            // get the description of GDB for a device:
            var gdb = new GdbInfo(ndk, DeviceDefinitionType.Device, runtime, null);
            Assert.IsNotNull(gdb);

            // start the GDB:
            return new GdbHostRunner(Defaults.GdbHostPath, gdb);
        }

        #endregion

        [Test]
        public void LoadInstructions()
        {
            var instructions = InstructionCollection.Load();

            Assert.IsNotNull(instructions);
            Assert.IsTrue(instructions.Count > 0, "Too few elements loaded");
        }

        [Test]
        public void StartGDB()
        {
            var runner = CreateDebuggerRunner();

            // start the GDB:
            runner.ExecuteAsync();

            Response message;
            bool result;

            result = runner.Processor.Wait(out message);
            Assert.IsTrue(result, "Should receive GDB startup info");

            runner.Break();

            result = runner.Processor.Wait(3 * 1000, out message);
            Assert.IsFalse(result, "Did not expect any notification");

            runner.Abort();
        }

        [Test]
        public void SendExitToGdbAndNotCrash()
        {
            var runner = CreateDebuggerRunner();

            // start the GDB:
            runner.ExecuteAsync();

            Response message;
            bool result;

            result = runner.Processor.Wait(out message);
            Assert.IsTrue(result, "Should receive GDB startup info");

            var exitRequest = RequestsFactory.Exit();
            runner.Processor.Send(exitRequest);
            result = exitRequest.Wait();
            Assert.IsTrue(result, "Exit command should be confirmed by the GDB");

            DateTime now = DateTime.Now;
            runner.Abort();
            DateTime after = DateTime.Now;

            // since nothing is blocking here, to detect that something went wrong, just measure the time
            // if it's higher than few seconds, GDB channel was not closed correctly...
            Assert.IsTrue(after - now < new TimeSpan(0, 0, 2, 0), "GDB was closing faaar too long. Check it manually, if it's not hanging inside the Abort() waiting on secondary exit confirmation");
        }

        [Test]
        public void SendMultipleRequestAtOnce()
        {
            var runner = CreateDebuggerRunner();
            runner.ExecuteAsync();

            Response message;
            bool result;

            // ok, wait util GDB is initialized:
            result = runner.Processor.Wait(out message);
            Assert.IsTrue(result, "Should receive GDB startup info");

            // send invalid request and wait for error:
            var rubbish1 = new Request("gdb-exit-rubbish");
            var rubbish2 = new Request("xxx-yyy-zzz");
            runner.Processor.Send(rubbish1);
            runner.Processor.Send(rubbish2);

            rubbish1.Wait();
            message = runner.Processor.Read();
            Assert.IsNotNull(message, "Should already receive a message");

            // for rubbish 2:
            result = runner.Processor.Wait(out message);
            Assert.IsTrue(result, "Should already receive a message");
            Assert.AreEqual(rubbish2.ID, message.ID, "Request and respose should have identical ID");
        }

        [Test]
        public void LoadListOfProcesses()
        {
            var runner = CreateDebuggerRunner();
            runner.ExecuteAsync();

            Response message;
            bool result;

            // ok, wait util GDB is initialized:
            result = runner.Processor.Wait(out message);
            Assert.IsTrue(result, "Should receive GDB startup info");

            var selectDevice = RequestsFactory.SetTargetDevice(Defaults.IP);
            var listRequest = RequestsFactory.ListProcesses();
            runner.Processor.Send(selectDevice);
            runner.Processor.Send(listRequest);

            listRequest.Wait();
            Assert.IsNotNull(listRequest.Response);
        }

        [Test]
        public void LoadListOfProcessesByGroup()
        {
            var runner = CreateDebuggerRunner();
            runner.ExecuteAsync();

            var selectDevice = RequestsFactory.SetTargetDevice(Defaults.IP);
            var listRequest = RequestsFactory.ListProcesses();
            var group = RequestsFactory.Group(selectDevice, listRequest);
            runner.Processor.Send(group);

            group.Wait();
            Assert.IsNotNull(listRequest.Response);
            Assert.AreSame(group.Response, listRequest.Response);
        }

        [Test]
        public void ListAvailableFeaturesExtraGrouped()
        {
            var runner = CreateDebuggerRunner();
            runner.ExecuteAsync();

            var listFeatures = RequestsFactory.ListFeatures();
            // yes, it's not necessary to wrap it into any group, but just for testing purposes:
            var group = RequestsFactory.Group(RequestsFactory.Group(listFeatures));

            runner.Processor.Send(group);
            group.Wait();

            Assert.IsNotNull(listFeatures.Response);
            Assert.IsNotNull(listFeatures.Response.Content); // the list of features
            Assert.AreSame(group.Response, listFeatures.Response);
        }

        [Test]
        public void ListAvailableTargetFeatures()
        {
            var runner = CreateDebuggerRunner();
            runner.ExecuteAsync();

            var selectTarget = RequestsFactory.SetTargetDevice(Defaults.IP);
            var listTargetFeatures = RequestsFactory.ListTargetFeatures();

            runner.Processor.Send(selectTarget);
            runner.Processor.Send(listTargetFeatures);
            listTargetFeatures.Wait();

            Assert.IsNotNull(listTargetFeatures.Response);
        }

        [Test]
        public void AttachToProcess()
        {
            var runner = CreateDebuggerRunner(Defaults.InstalledRuntimePath);
            runner.ExecuteAsync();

            var enablePending = RequestsFactory.SetPendingBreakpoints(true);
            var selectDevice = RequestsFactory.SetTargetDevice(Defaults.IP);
            var listProcesses = RequestsFactory.ListProcesses();

            var procGroup = RequestsFactory.Group(enablePending, selectDevice, listProcesses);
            runner.Processor.Send(procGroup);
            procGroup.Wait(); // at this point, we should receive a list of running processes

            // find the PID of FallingBlocks sample:
            uint pid = GetPID("FallingBlocks", listProcesses.Response.Comments);

            // attach to this process:
            var setLibSearchPath = RequestsFactory.SetLibrarySearchPath(runner.GDB.LibraryPaths);
            var setExecutable = RequestsFactory.SetExecutable(@"S:\test\FallingBlocks\Device-Debug\FallingBlocks", true);
            var attachProcess = RequestsFactory.AttachTargetProcess(pid);
            var stackDepth = RequestsFactory.SetStackTraceDepth(1, 10);
            var infoThreads = RequestsFactory.InfoThreads();
            var stackTrace = RequestsFactory.StackTraceListFrames();

            var attachGroup = RequestsFactory.Group(setLibSearchPath, setExecutable, attachProcess, stackDepth, stackTrace, infoThreads);
            runner.Processor.Send(attachGroup);
            attachGroup.Wait();

            // insert a breakpoint
            var bp = RequestsFactory.InsertBreakPoint("main.c", 151);
            //var bp = RequestsFactory.InsertBreakpoint("update");

            var breakGroup = RequestsFactory.Group(bp);
            runner.Processor.Send(breakGroup);
            breakGroup.Wait();

            // resume execution:
            var continueExec = RequestsFactory.Continue();
            runner.Processor.Send(continueExec);
            continueExec.Wait();

            for (int i = 0; i < 1000; i++)
                Thread.Sleep(1);

            // delete the breakpoint:
            var delBp = RequestsFactory.DeleteBreakpoint(1); // PH: FIXME: assuming this BP-id, should be updated, when parsing responses is fully implemented
            runner.Processor.Send(delBp);
            delBp.Wait();

            // resume after breakpoint:
            var contGroup = RequestsFactory.Group(stackTrace, continueExec);
            runner.Processor.Send(contGroup);
            contGroup.Wait();

            for (int i = 0; i < 1000; i++)
                Thread.Sleep(5);

            // detach:
            runner.Break();
            var detachProcess = RequestsFactory.DetachTargetProcess();
            runner.Processor.Send(detachProcess);
            detachProcess.Wait();
        }

        private static uint GetPID(string exeName, string[] info)
        {
            if (string.IsNullOrEmpty(exeName))
                throw new ArgumentNullException("exeName");
            if (info == null || info.Length == 0)
                throw new ArgumentNullException("info");

            foreach (var process in info)
            {
                int startAt = process.IndexOf(exeName, StringComparison.OrdinalIgnoreCase);
                if (startAt >= 0)
                {
                    int pidStartAt = process.IndexOf('-', startAt);
                    if (pidStartAt > 0)
                    {
                        string pidString;
                        uint pid;
                        int pidEndAt = process.IndexOf('/', pidStartAt);
                        if (pidEndAt < 0)
                            pidString = process.Substring(pidStartAt + 1).Trim();
                        else
                            pidString = process.Substring(pidStartAt + 1, pidEndAt - pidStartAt - 1).Trim();

                        if (uint.TryParse(pidString, NumberStyles.Number, null, out pid))
                            return pid;
                    }
                }
            }

            throw new InvalidOperationException("Specified process is not running");
        }

        [Test]
        [Ignore]
        public void LoadListOfProcessesOld()
        {
            string response = GDBParser.GetPIDsThroughGDB(Defaults.IP, Defaults.Password, false, Defaults.ToolsDirectory, Defaults.SshPublicKeyPath, 12);

            Assert.IsNotNull(response);
        }
    }
}
