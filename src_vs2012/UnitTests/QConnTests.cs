using System;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Threading;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Visitors;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class QConnTests
    {
        private static void SetupProgressMonitor(object source)
        {
            var monitor = source as IFileServiceVisitorMonitor;
            if (monitor != null)
            {
                monitor.Started += (sender, e) => QTraceLog.WriteLine("!!! Transfer started");
                monitor.Completed += (sender, e) => QTraceLog.WriteLine("!!! Transfer completed");
                monitor.Failed += (sender, e) => QTraceLog.WriteLine("!!! Failure: {0}", e.UltimateMassage);
                monitor.ProgressChanged += (sender, e) => QTraceLog.WriteLine("!!! {0} / {1} / {2}%", e.Name, e.Operation, e.Progress);
            }
        }

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

            foreach (var p in processes)
            {
                QTraceLog.WriteLine("Process found: 0x{0:X8} (parent: 0x{1:X8}) - {2}", p.ID, p.ParentID, p.Name);
            }

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
            qclient.ControlService.Terminate(toKill);

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
            //var files = qclient.FileService.List("/tmp/");
            //var files = qclient.FileService.List("/tmp/slogger2/"); // place where all apps are installed
            //var files = qclient.FileService.List("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/");

            Assert.IsNotNull(files);
            //Assert.IsTrue(files.Length > 2, "Invalid number of items loaded");

            // print all files:
            QTraceLog.WriteLine("--------------------------------------------------------");
            QTraceLog.WriteLine("Items: {0}", files.Length);

            foreach (var f in files)
            {
                QTraceLog.WriteLine("Item: {0}: {1,-10} - {2} ({3})", f.FormattedType, f.FormattedPermissions, f.Path, f.Name);
            }

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

        [Test]
        public void DownloadSampleFolderStats()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // calculate stats about all files from the folder:
            var visitor = new LoggingVisitor();

            SetupProgressMonitor(visitor);
            //qclient.FileService.DownloadAsync("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/", visitor);
            qclient.FileService.DownloadAsync("/tmp", visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            Assert.IsTrue(visitor.FilesCount > 0, "No files found in the folder");
            Assert.IsTrue(visitor.TotalSize > 0, "No data to download");

            // and close
            qdoor.Close();
        }

        [Test]
        public void ZipSampleFolder()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // download all files from the folder:
            var visitor = new ZipPackageVisitor(Path.Combine(Defaults.NdkDirectory, "test.zip"), CompressionOption.Maximum);

            SetupProgressMonitor(visitor);
            //qclient.FileService.DownloadAsync("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/", visitor);
            //qclient.FileService.DownloadAsync("/pps", visitor); // can take some time ~25sec
            //qclient.FileService.DownloadAsync("/pps/accounts", visitor);
            qclient.FileService.DownloadAsync("/tmp", visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            // and close
            qdoor.Close();
        }

        [Test]
        public void ZipSampleFolderAndListFilesInParallel()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // download all files from the folder:
            var visitor = new ZipPackageVisitor(Path.Combine(Defaults.NdkDirectory, "test-parallel.zip"), CompressionOption.NotCompressed);

            SetupProgressMonitor(visitor);
            qclient.FileService.DownloadAsync("/tmp", visitor);

            // this should be executed in parallel to the download:
            var listing = qclient.FileService.List("/accounts/1000/appdata");

            Assert.IsNotNull(visitor);
            Assert.IsNotNull(listing);
            Assert.IsTrue(listing.Length > 0, "No installed application found, what is not true");

            visitor.Wait();

            // and close
            qdoor.Close();
        }

        [Test]
        public void DownloadSampleFolderToLocalFileSystem()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // calculate stats about all files from the folder:
            var visitor = new LocalCopyVisitor(Path.Combine(Defaults.NdkDirectory, "tmp_copy.xxx"));

            SetupProgressMonitor(visitor);
            //qclient.FileService.DownloadAsync("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/", visitor);
            qclient.FileService.DownloadAsync("/tmp", visitor);
            //qclient.FileService.DownloadAsync("/tmp/pim.services.pimmain.pid", visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            // and close
            qdoor.Close();
        }

        [Test]
        public void LoadSampleFilePreview()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // calculate stats about all files from the folder:
            var visitor = new BufferVisitor();

            SetupProgressMonitor(visitor);
            qclient.FileService.DownloadAsync("/accounts/1000/appdata/com.example.FallingBlocks.testDev_llingBlocks37d009c_/app/native/", visitor);
            //qclient.FileService.DownloadAsync("/tmp", visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            var data = visitor.Find("bar-descriptor.xml");
            Assert.IsNotNull(visitor.Data, "There should be any file loaded");
            Assert.IsNotNull(data, "Missing data for bar-descriptor.xml");

            var descriptorContent = Encoding.UTF8.GetString(data);
            Assert.IsFalse(string.IsNullOrEmpty(descriptorContent));

            QTraceLog.WriteLine("bar-descriptor.xml:\r\n\r\n{0}", descriptorContent);

            // and close
            qdoor.Close();
        }

        [Test]
        [Ignore]
        public void ZipLocalFolder()
        {
            // this is purely for development purposes, let's just try to zip local folder to see all is working fine
            // normally it makes NO SENSE to have the connection to the target opened

            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // package local folder:
            var visitor = new ZipPackageVisitor(Path.Combine(Defaults.NdkDirectory, "tmp_tools.zip"), CompressionOption.Maximum);
            var enumerator = new LocalEnumerator(@"C:\Tools");

            SetupProgressMonitor(visitor);
            qclient.FileService.EnumerateAsync(enumerator, visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            // and close
            qdoor.Close();
        }

        [Test]
        [Ignore]
        public void CopySampleFolderToTargetFileSystem()
        {
            var qdoor = new QConnDoor();
            var qclient = new QConnClient();

            // connect:
            qdoor.Open(Defaults.IP, Defaults.Password, Defaults.SshPublicKeyPath);
            qclient.Load(Defaults.IP);

            Assert.IsNotNull(qclient.FileService);

            // upload local folder:
            var visitor = qclient.FileService.UploadAsync(@"C:\Tools\Putty", "/accounts/1000/shared/misc/titans/123");
            SetupProgressMonitor(visitor);

            Assert.IsNotNull(visitor);
            visitor.Wait();

            // and close
            qdoor.Close();
        }
    }
}
