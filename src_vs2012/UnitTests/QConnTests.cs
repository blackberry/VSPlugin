using System;
using System.Threading;
using BlackBerry.NativeCore.QConn;
using BlackBerry.NativeCore.QConn.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class QConnTests
    {
        [Test]
        public void Connect()
        {
            var qdoor = new QConnDoor();
            qdoor.Authenticated += QConnDoorOnAuthenticated;

            // verify connection is not established:
            Assert.IsFalse(qdoor.IsAuthenticated);
            Assert.IsFalse(qdoor.IsConnected);

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);

            Assert.IsTrue(qdoor.IsAuthenticated);
            Assert.IsTrue(qdoor.IsConnected);

            // keep the connection alive for some time:
            qdoor.KeepAlive();      // send immediately
            qdoor.KeepAlive(200);   // first request will be sent after a delay
            Thread.Sleep(1000);     // can sleep here, as 'keep-alive' uses a thread-pool to work...

            // and close:
            qdoor.Close();

            Assert.IsFalse(qdoor.IsAuthenticated);
            Assert.IsFalse(qdoor.IsConnected);
        }

        private void QConnDoorOnAuthenticated(object sender, QConnAuthenticationEventArgs e)
        {
            Console.WriteLine("Is authenticated: {0}", e.IsAuthenticated);
        }

        [Test]
        public void LoadInfo()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            Assert.AreEqual(Endianess.Unknown, qclient.Endian);
            Assert.AreEqual(TargetSystemType.Unknown, qclient.System);
            Assert.IsNotNull(qclient.Services);
            Assert.AreEqual(0, qclient.Services.Length);
            Assert.IsNull(qclient.Version);
            Assert.IsNull(qclient.Name);
            Assert.IsNull(qclient.Locale);

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // verify data was read:
            Assert.AreEqual(Endianess.LittleEndian, qclient.Endian);
            Assert.AreEqual(TargetSystemType.Neutrino, qclient.System);
            Assert.IsNotNull(qclient.Services);
            Assert.AreEqual(3, qclient.Services.Length);
            Assert.IsNotNull(qclient.Version);
            Assert.IsNotNull(qclient.Name);
            Assert.IsNotNull(qclient.Locale);

            // and close
            qdoor.Close();
        }

        [Test]
        public void LoadProcessesList()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // load data:
            Assert.IsNotNull(qclient.SysInfoService);
            var processes = qclient.SysInfoService.LoadProcesses();

            // verify:
            Assert.IsNotNull(processes);
            Assert.IsTrue(processes.Length > 0, "Invalid processes list, QConn should be at least running");

            // and close
            qdoor.Close();
        }

        [Test]
        public void KillAnyProcess()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // load data:
            Assert.IsNotNull(qclient.SysInfoService);
            var processes = qclient.SysInfoService.LoadProcesses();

            // verify:
            Assert.IsNotNull(processes);
            Assert.IsTrue(processes.Length > 1, "Invalid processes list, QConn + 1 at least one other app should be still running");

            // find non-qconn application running:
            SystemInfoProcess toKill = null;
            foreach (var p in processes)
            {
                if (!p.Name.EndsWith("qconn"))
                {
                    toKill = p;
                    break;
                }
            }

            // kill the process:
            Assert.IsNotNull(qclient.ControlService);
            Assert.IsNotNull(toKill, "No application to terminate specified, please run anything on the device first");
            qclient.ControlService.Kill(toKill);

            // reload info:
            processes = qclient.SysInfoService.LoadProcesses();

            // verify:
            Assert.IsNotNull(processes);

            foreach (var p in processes)
            {
                // check that killed app doesn't belong to reloaded processes list:
                Assert.AreNotEqual(toKill.Name, p.Name);
            }

            // and close
            qdoor.Close();
        }

        [Test]
        public void ParseFileStatResponse()
        {
            var result = Token.Parse("a");
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("a", result[0].StringValue);

            result = Token.Parse(":");
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(string.Empty, result[0].StringValue);
            Assert.AreEqual(string.Empty, result[1].StringValue);

            result = Token.Parse("o+:0:FF:\"/abcdef\"");
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual("o+", result[0].StringValue);
            Assert.AreEqual(0u, result[1].UInt32Value);
            Assert.AreEqual(255u, result[2].UInt32Value);
            Assert.AreEqual("/abcdef", result[3].StringValue);
        }

        [Test]
        public void StatDirectory()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // list files within the folder:
            Assert.IsNotNull(qclient.FileService);

            // PH: FIXME: didn't find any symlink yet, could be valuable to add here... so this is just any folder for now
            var info = qclient.FileService.Stat("/accounts/1000/shared/");

            Assert.IsNotNull(info);

            // and close
            qdoor.Close();
        }

        [Test]
        public void ListDirectory()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // list files within the folder:
            Assert.IsNotNull(qclient.FileService);
            //var files = qclient.FileService.List("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/logs");
            var files = qclient.FileService.List("/accounts/1000/appdata/"); // place where all apps are installed
            //var files = qclient.FileService.List("/tmp/slogger2/"); // place where all apps are installed

            Assert.IsNotNull(files);
            Assert.IsTrue(files.Length > 2, "Invalid number of items loaded");

            // and close
            qdoor.Close();
        }

        [Test]
        public void CreateDirectory()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            // list files within the folder:
            Assert.IsNotNull(qclient.FileService);
            var info = qclient.FileService.CreateFolder("/accounts/1000/shared/misc/test", 0xFFF);
            //var info = qclient.FileService.CreateFolder("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/logs/test2", 0xFFF);

            Assert.IsNotNull(info);

            qclient.FileService.Remove(info.Path);

            // and close
            qdoor.Close();
        }
    }
}
