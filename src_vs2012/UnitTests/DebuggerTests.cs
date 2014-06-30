using System;
using System.Threading;
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

        private DeviceConnectRunner _connection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _connection = new DeviceConnectRunner(Defaults.ToolsDirectory, Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            Assert.IsNotNull(_connection);

            _connection.ExecuteAsync();

            // wait until connection with a device is established:
            while (!_connection.IsConnected && !_connection.IsConnectionFailed)
                Thread.Sleep(10);
            Assert.IsTrue(_connection.IsConnected, "Connection was not established properly");
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            if (_connection != null)
            {
                _connection.Abort();
                _connection = null;
            }
        }

        #endregion

        #region Initialize Debugger

        public GdbHostRunner CreateDebuggerRunner()
        {
            // get the NDK info:
            var ndk = NdkInfo.Scan(Defaults.InstalledNdkPath);
            Assert.IsNotNull(ndk);

            // get the description of GDB for a device:
            var gdb = new GdbInfo(ndk, DeviceDefinitionType.Device, null, null);
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

            var listRequest = RequestsFactory.ListProcesses();
            runner.Processor.Send(RequestsFactory.SelectTargetDevice(Defaults.IP));
            runner.Processor.Send(listRequest);

            listRequest.Wait();
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
