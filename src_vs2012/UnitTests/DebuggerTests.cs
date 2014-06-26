using System.Threading;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Tools;
using NUnit.Framework;
using VSNDK.Parser;

namespace UnitTests
{
    [TestFixture]
    public sealed class DebuggerTests
    {
        private const string IP = ToolRunnerTests.IP;
        private const string Password = ToolRunnerTests.Password;

        #region Device Connection Establishing

        private DeviceConnectRunner _connection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _connection = new DeviceConnectRunner(ConfigDefaults.TestToolsDirectory, IP, Password, ConfigDefaults.SshPublicKeyPath);
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

        [Test]
        public void LoadInstructions()
        {
            var instructions = InstructionCollection.Load();

            Assert.IsNotNull(instructions);
            Assert.IsTrue(instructions.Count > 0, "Too few elements loaded");
        }

        [Test]
        public void LoadListOfProcesses()
        {
            string response = GDBParser.GetPIDsThroughGDB(IP, Password, false, ConfigDefaults.ToolsDirectory, ConfigDefaults.SshPublicKeyPath, 12);

            Assert.IsNotNull(response);
        }
    }
}
