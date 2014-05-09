using System;
using System.Threading;
using NUnit.Framework;
using RIM.VSNDK_Package.Tools;

namespace VSNDK.Package.Test
{
    [TestFixture]
    public class ToolRunnerTests
    {
        [Test]
        public void LoadDebugTokenInfo()
        {
            var runner = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, RunnerDefaults.ConfigFileName("debugtoken.bar"));
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.DebugToken);
        }

        [Test]
        public void LoadDebugTokenAsync()
        {
            var runner = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, RunnerDefaults.ConfigFileName("debugtoken.bar"));

            Assert.IsFalse(runner.IsProcessing);
            runner.ExecuteAsync();
            Assert.IsTrue(runner.IsProcessing);

            // wait for completion:
            DateTime startTime = DateTime.Now;
            TimeSpan timeout = new TimeSpan(0, 0, 5); // 5 sec
            while (runner.IsProcessing)
            {
                Thread.Sleep(50);
                if (DateTime.Now - startTime > timeout)
                    break;
            }

            Assert.IsFalse(runner.IsProcessing, "Tool execution got too much time!");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.DebugToken);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void LoadDeviceInfo()
        {
            var runner = new DeviceInfoRunner(RunnerDefaults.TestToolsDirectory, "10.0.0.127", "test");
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void UploadDebugTokenInfo()
        {
            var runner = new DebugTokenUploadRunner(RunnerDefaults.TestToolsDirectory, RunnerDefaults.ConfigFileName("debugtoken.bar"), "10.0.0.127", "test");
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsTrue(runner.UploadedSuccessfully);
        }
    }
}
