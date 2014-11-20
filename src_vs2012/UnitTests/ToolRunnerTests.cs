using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using System;
using System.Threading;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ToolRunnerTests
    {
        [Test]
        public void LoadDebugTokenInfo()
        {
            var runner = new DebugTokenInfoRunner(Defaults.DebugTokenPath);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.DebugToken);
        }

        [Test]
        public void AbortAfterSuccessfulLoadDebugTokenInfo()
        {
            var runner = new DebugTokenInfoRunner(Defaults.DebugTokenPath);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.DebugToken);

            result = runner.Abort();
            Assert.IsFalse(result, "There should be nothing to abort!");
            Assert.IsFalse(runner.IsProcessing);
        }

        [Test]
        public void LoadDebugTokenAsync()
        {
            var runner = new DebugTokenInfoRunner(Defaults.DebugTokenPath);

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
        public void AbortDuringLoadDebugTokenAsync()
        {
            var runner = new DebugTokenInfoRunner(Defaults.DebugTokenPath);
            bool result;

            Assert.IsFalse(runner.IsProcessing);
            runner.ExecuteAsync();
            Assert.IsTrue(runner.IsProcessing);

            result = runner.Abort();
            Assert.IsTrue(result, "Impossible to abort the tool execution");
            Assert.IsFalse(runner.IsProcessing);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void LoadDeviceInfo()
        {
            var runner = new DeviceInfoRunner(Defaults.IP, Defaults.Password);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
        }

        [Test]
        [Ignore("Device-IP dependant test will only run somewhere correctly")]
        public void UploadDebugTokenInfo()
        {
            var runner = new DebugTokenUploadRunner(Defaults.DebugTokenPath, Defaults.IP, Defaults.Password);
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
            var uploader = new DebugTokenUploadRunner(Defaults.DebugTokenPath, Defaults.IP, Defaults.Password);
            var result = uploader.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsTrue(uploader.UploadedSuccessfully);

            // get info about the debug-token:
            var informer = new DebugTokenInfoRunner(Defaults.DebugTokenPath);
            result = informer.Execute();
            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(informer.DebugToken);
            Assert.IsNotNull(informer.DebugToken.ID);

            // remove:
            var cleaner = new ApplicationRemoveRunner(informer.DebugToken.ID, Defaults.IP, Defaults.Password);
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
            string debugToken = ConfigDefaults.DataFileName("debugtoken-new.bar");
            var runner = new DebugTokenCreateRunner(debugToken, "test", new[] { 0x1ul, 0x2ul }, null);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsTrue(runner.CreatedSuccessfully);

            var informer = new DebugTokenInfoRunner(debugToken);
            result = informer.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(informer.DebugToken);
            Assert.IsNotNull(informer.DebugToken.Devices);
            Assert.AreEqual(2, informer.DebugToken.Devices.Length);
        }

        [Test]
        public void LoadDefaultApiLevelList()
        {
            var runner = new ApiLevelListLoadRunner(Defaults.NdkDirectory, ApiLevelListTypes.Default);
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
            var runner = new ApiLevelListLoadRunner(Defaults.NdkDirectory, ApiLevelListTypes.Full);
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
            var runner = new ApiLevelListLoadRunner(Defaults.NdkDirectory, ApiLevelListTypes.Simulators);
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
            var info = NdkInfo.Load(ConfigDefaults.InstallationConfigDirectory);

            Assert.IsNotNull(info);
            Assert.IsTrue(info.Length > 0);
        }

        [Test]
        public void LoadInfoAboutCertificate()
        {
            var runner = new KeyToolInfoRunner(Defaults.CertificatePath, Defaults.CertificatePassword);
            var result = runner.Execute();

            Assert.IsTrue(result, "Unable to start the tool");
            Assert.IsNotNull(runner.LastOutput);
            Assert.IsNull(runner.LastError);
            Assert.IsNotNull(runner.Info);
            Assert.IsNotNull(runner.Info.Issuer);
        }

        [Test]
        [Ignore]
        public void InstallSimulator()
        {
            var runner = new ApiLevelUpdateRunner(ConfigDefaults.NdkDirectory, ApiLevelAction.Install, ApiLevelTarget.Simulator, new Version(10, 1, 0, 2354));
            var result = runner.Execute();

            runner.Wait();
            Assert.IsTrue(result);
        }

        [Test]
        public void EstablishConnection()
        {
            var runner = new DeviceConnectRunner(Defaults.IP, Defaults.Password, ConfigDefaults.SshPublicKeyPath);
            runner.ExecuteAsync();

            // monitor for max 30sec, if it successfully connected or failed:
            for (int i = 0; i < 10000 && !runner.IsConnected; i++)
            {
                Thread.Sleep(3);
                Assert.IsFalse(runner.IsConnectionFailed, "Failed to connect to the device");
            }

            Assert.IsTrue(runner.IsConnected, "Unable to connect to the device");
        }
    }
}
