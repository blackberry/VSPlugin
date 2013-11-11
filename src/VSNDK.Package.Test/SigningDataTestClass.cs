using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RIM.VSNDK_Package.Signing.Models;
using System.IO;
using Microsoft.Win32;

namespace VSNDK.Package.Test
{
    [TestFixture]
    public class SigningData_BackupRestore_TestClass
    {
        private SigningData _signingData = null;
        private string _certName = "";
        private string _certTmpPath = "";

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureSetUp]
        public void TFSetup()
        {
            /// Create Object
            _signingData = new SigningData();
            _certName = "BackupTest.zip";
            _certTmpPath = @"\temp";
        }

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureTearDown]
        public void TFTearDown()
        {
            /// Remove Zip file and unzipped files
            File.Delete(_signingData.CertPath + _certName);
            Directory.Delete(_signingData.CertPath + _certTmpPath);
        }

        /// <summary>
        /// Test the backup functionality
        /// </summary>
        [TestCase]
        public void SigningDataBackupTest()
        {
            _signingData.Backup(_certName);
            Assert.True(File.Exists(_signingData.CertPath + _certName));
        }


        /// <summary>
        /// Test the restore functionality
        /// </summary>
        [TestCase]
        public void SigningDataRestoreTest()
        {
            string certName = "BackupTest.zip";
            _signingData.CertPath = _signingData.CertPath + @"temp\";
            _signingData.Restore(certName);
            Assert.True(Directory.Exists(_signingData.CertPath), "Restore directory not created");

            DirectoryInfo di = new DirectoryInfo(_signingData.CertPath);
            FileInfo[] fileList = di.GetFiles();

            foreach (FileInfo file in fileList)
            {
                FileInfo file2 = new FileInfo(_signingData.CertPath + @"..\" + file.Name);
                FileAssert.AreEqual(file, file2, "File isn't the same: " + file.Name);
            }
        }
    }

    [TestFixture]
    public class SigningData_Register_TestClass
    {
        private SigningData _signingData = null;
        private string _cskPassword = "";
        private string _certName = "";
        private string _certTmpName = "";

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureSetUp]
        public void TFSetup()
        {
            /// Create Object
            _signingData = new SigningData();

            _certName = @"/bbidtoken.csk";
            _certTmpName = @"/bbidtoken.csk.bak";

            if (_signingData.Registered)
            {
                /// Backup CSK Password
                RegistryKey rkHKCU = Registry.CurrentUser;
                RegistryKey rkCDKPass = null;
                rkCDKPass = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                _cskPassword = GlobalFunctions.Decrypt(rkCDKPass.GetValue("CSKPass").ToString());
                rkCDKPass.Close();
                rkHKCU.Close();

                /// Backup CSK File..
                File.Copy(_signingData.CertPath + _certName, _signingData.CertPath + _certTmpName);

                /// Remove Key so we can register again.
                _signingData.UnRegister();

                /// Backup CSK File..
                File.Copy(_signingData.CertPath + _certTmpName, _signingData.CertPath + _certName);
            }
            else
            {
                Assert.Ignore();
            }
        }

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureTearDown]
        public void TFTearDown()
        {
            /// Remove backup file
            File.Delete(_signingData.CertPath + _certTmpName);
        }

        /// <summary>
        /// Test the unregister function - All Registered
        /// </summary>
        [TestCase]
        public void SigningDataRegisterTest()
        {
            string password = "";

            /// Check success Fail 
            _signingData.Register("Test", _cskPassword);

            /// Check return value from remote call
            StringAssert.IsMatch("CSK file deleted.\n", _signingData.Messages, "Return value from command is not as expected. " + _signingData.Messages);

            /// Check return value for errors
            StringAssert.DoesNotMatch("", _signingData.Errors, "Errors have been regturned from remote call. " + _signingData.Errors);

            /// Check to see that the cert file was removed 
            Assert.True(File.Exists(_signingData.CertPath + @"/author.p12"), "Certificate File is not present");

            /// Check to see that the csk file was removed 
            Assert.True(File.Exists(_signingData.CertPath + @"/bbidtoken.csk"), "CSK File is not present.");

            /// Get CSK Password
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkCDKPass = null;
            rkCDKPass = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            password = GlobalFunctions.Decrypt(rkCDKPass.GetValue("CSKPass").ToString());
            StringAssert.IsMatch(_cskPassword, password, "Password is not a match " + _cskPassword + " != " + password);
            rkCDKPass.Close();
            rkHKCU.Close();
        }

    }

    [TestFixture]
    public class SigningData_UnRegister_TestClass
    {
        private SigningData _signingData = null;
        private string _cskPassword = "";
        private string _certName = "";
        private string _certTmpName = "";

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureSetUp]
        public void TFSetup()
        {
            /// Create Object
            _signingData = new SigningData();

            _certName = @"/bbidtoken.csk";
            _certTmpName = @"/bbidtoken.csk.bak";

            if (_signingData.Registered)
            {
                /// Backup CSK Password
                RegistryKey rkHKCU = Registry.CurrentUser;
                RegistryKey rkCDKPass = null;
                rkCDKPass = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                _cskPassword = rkCDKPass.GetValue("CSKPass").ToString();
                rkCDKPass.Close();
                rkHKCU.Close();

                /// Backup CSK File..
                File.Copy(_signingData.CertPath + _certName, _signingData.CertPath + _certTmpName);
            }
            else
            {
                Assert.Ignore();
            }
        }

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureTearDown]
        public void TFTearDown()
        {
            /// Backup CSK File..
            File.Copy(_signingData.CertPath + _certTmpName, _signingData.CertPath + _certName);

            /// Check success Fail 
            _signingData.Register("Test Company", _cskPassword);

            /// Remove backup file
            File.Delete(_signingData.CertPath + _certTmpName);
        }

        /// <summary>
        /// Test the unregister function - All Registered
        /// </summary>
        [TestCase]
        public void SigningDataUnregisterTest()
        {
            /// Check success Fail 
            _signingData.UnRegister();

            /// Check return value from remote call
            StringAssert.IsMatch("CSK file deleted.\n", _signingData.Messages, "Return value from command is not as expected. " + _signingData.Messages);

            /// Check return value for errors
            StringAssert.DoesNotMatch("", _signingData.Errors, "Errors have been regturned from remote call. " + _signingData.Errors);

            /// Check to see that the cert file was removed 
            Assert.False(File.Exists(_signingData.CertPath + @"/author.p12"), "Certificate File is still present");

            /// Check to see that the csk file was removed 
            Assert.False(File.Exists(_signingData.CertPath + @"/bbidtoken.csk"), "CSK File is still present.");

            /// Get CSK Password
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkCDKPass = null;
            rkCDKPass = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            StringAssert.IsMatch("", GlobalFunctions.Decrypt(rkCDKPass.GetValue("CSKPass").ToString()), "Password is not set to blank");
            rkCDKPass.Close();
            rkHKCU.Close();
        }

    }
}
