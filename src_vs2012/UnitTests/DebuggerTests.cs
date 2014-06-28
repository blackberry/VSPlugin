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
/*
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
        */
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
            // get the NDK info:
            var ndk = NdkInfo.Scan(Defaults.InstalledNdkPath);
            Assert.IsNotNull(ndk);

            // get the description of GDB for a device:
            var gdb = new GdbInfo(ndk, DeviceDefinitionType.Device, null, null);
            Assert.IsNotNull(gdb);

            // start the GDB:
            var runner = new GdbHostRunner(@"S:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe", gdb);
            runner.ShowConsole = true;
            runner.ExecuteAsync();

            for (int i = 0; i < 1000; i++)
                Thread.Sleep(1);

            runner.Break();

            for (int i = 0; i < 1000; i++)
                Thread.Sleep(3);
            runner.Abort();
        }

        [Test]
        public void LoadListOfProcesses()
        {
            string response = GDBParser.GetPIDsThroughGDB(Defaults.IP, Defaults.Password, false, Defaults.ToolsDirectory, Defaults.SshPublicKeyPath, 12);

            Assert.IsNotNull(response);
        }
    }
}
