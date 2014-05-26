#if DEBUG

using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace VSNDK.Package.Test
{
    [TestFixture]
    public class ToolRunnerTests
    {
        const string IP = "10.0.0.127";
        const string Password = "test";
        readonly static string DebugTokenPath = RunnerDefaults.DataFileName("debugtoken.bar");

        [Test]
        public void LoadDebugTokenInfo()
        {
            var runner = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, DebugTokenPath);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.DebugToken);
        }

        [Test]
        public void LoadDebugTokenAsync()
        {
            var runner = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, DebugTokenPath);

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
            var runner = new DeviceInfoRunner(RunnerDefaults.TestToolsDirectory, IP, Password);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void UploadDebugTokenInfo()
        {
            var runner = new DebugTokenUploadRunner(RunnerDefaults.TestToolsDirectory, DebugTokenPath, IP, Password);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsTrue(runner.UploadedSuccessfully);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void RemoveDebugTokenInfo()
        {
            // upload:
            var uploader = new DebugTokenUploadRunner(RunnerDefaults.TestToolsDirectory, DebugTokenPath, IP, Password);
            var result = uploader.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsTrue(uploader.UploadedSuccessfully);

            // get info about the debug-token:
            var informer = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, DebugTokenPath);
            result = informer.Execute();
            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(informer.DebugToken);
            Assert.IsNotNull(informer.DebugToken.ID);

            // remove:
            var cleaner = new ApplicationRemoveRunner(RunnerDefaults.TestToolsDirectory, informer.DebugToken.ID, IP, Password);
            result = cleaner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(cleaner.LastOutput);
            Assert.IsNull(cleaner.LastError);
            Assert.IsTrue(cleaner.RemovedSuccessfully);
        }

        [Test]
        [Ignore("Keystore password and device PINs must be fixed, otherwise Signing Authority will cause it to fail")]
        public void CreateDebugTokenInfo()
        {
            string debugToken = RunnerDefaults.DataFileName("debugtoken-new.bar");
            var runner = new DebugTokenCreateRunner(RunnerDefaults.TestToolsDirectory, debugToken, "test", new[] { 0x1ul, 0x2ul }, null);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsTrue(runner.CreatedSuccessfully);

            var informer = new DebugTokenInfoRunner(RunnerDefaults.TestToolsDirectory, debugToken);
            result = informer.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(informer.DebugToken);
            Assert.IsNotNull(informer.DebugToken.Devices);
            Assert.AreEqual(2, informer.DebugToken.Devices.Length);
        }

        [Test]
        public void LoadDefaultApiLevelList()
        {
            var runner = new ApiLevelListLoadRunner(RunnerDefaults.TestNdkDirectory, ApiLevelListTypes.Default);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.APIs);
            Assert.IsTrue(runner.APIs.Length > 0);
        }

        [Test]
        public void LoadFullApiLevelList()
        {
            var runner = new ApiLevelListLoadRunner(RunnerDefaults.TestNdkDirectory, ApiLevelListTypes.Full);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.APIs);
            Assert.IsTrue(runner.APIs.Length > 0);
        }

        [Test]
        public void LoadSimulatorApiLevelList()
        {
            var runner = new ApiLevelListLoadRunner(RunnerDefaults.TestNdkDirectory, ApiLevelListTypes.Simulators);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.APIs);
            Assert.IsTrue(runner.APIs.Length > 0);
        }

        [Test]
        public void LoadInstalledNdkInfo()
        {
            var info = NdkInfo.Load(RunnerDefaults.InstallationConfigDirectory);

            Assert.IsNotNull(info);
            Assert.IsTrue(info.Length > 0);
        }

        [Test]
        public void LoadInfoAboutCertificate()
        {
            var runner = new KeyToolInfoRunner(RunnerDefaults.TestToolsDirectory, Path.Combine(RunnerDefaults.DataDirectory, DeveloperDefinition.DefaultCertificateName), "abcdef");
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.Issuer);
        }
    }
}

#endif
