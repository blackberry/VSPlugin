using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RIM.VSNDK_Package.DebugToken.Model;
using Microsoft.Win32;

namespace VSNDK.Package.Test
{
    [TestFixture]
    public class DebugTokenDataTestClass
    {
        private DebugTokenData DebugTokenObject;

        /// <summary>
        /// Setup for testing
        /// </summary>
        [SetUp]
        public void Setup()
        {
            /// Create Object
            DebugTokenObject = new RIM.VSNDK_Package.DebugToken.Model.DebugTokenData();
 
            /// Set Paths - This would normally be done by the creation of the package.
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;
            string qnx_target = "";
            string qnx_host = "";

            rkNDKPath = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            qnx_host = rkNDKPath.GetValue("NDKHostPath").ToString();
            qnx_target = rkNDKPath.GetValue("NDKHostPath").ToString();

            string qnx_config = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK";

            System.Environment.SetEnvironmentVariable("QNX_TARGET", qnx_target);
            System.Environment.SetEnvironmentVariable("QNX_HOST", qnx_host);
            System.Environment.SetEnvironmentVariable("QNX_CONFIGURATION", qnx_config);

            string ndkpath = string.Format(@"{0}/usr/bin;{1}\bin;{0}/usr/qde/eclipse/jre/bin;", qnx_host, qnx_config) + System.Environment.GetEnvironmentVariable("PATH");
            System.Environment.SetEnvironmentVariable("PATH", ndkpath);
        }

        /// <summary>
        /// Test for constructor
        /// </summary>
        [TestCase]
        public void DebugTokenDataConstructorTest()
        {
            /// Verify Test Case
            Assert.IsNotNull(DebugTokenObject);
        }

        /// <summary>
        /// Test what happens when Signing Certificate is unset.
        /// </summary>
        [TestCase]
        public void CreateDebugToken_NoCert_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.CertPath = "";
            bool result = DebugTokenObject.createDebugToken();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test what happens when an invalid Key Store Password is sent
        /// </summary>
        [TestCase]
        public void CreateDebugToken_InvalidKeyStore_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.KeyStorePassword = "invalid";
            bool result = DebugTokenObject.createDebugToken();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test what happens when an invalid Signing certificate path is sent
        /// </summary>
        [TestCase]
        public void CreateDebugToken_InvalidCertPath_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.CertPath = @"C:\";
            bool result = DebugTokenObject.createDebugToken();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test what happens when an invalid Device PIN is sent
        /// </summary>
        [TestCase]
        public void CreateDebugToken_InvalidDevicePIN_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.DevicePIN = "error";
            bool result = DebugTokenObject.createDebugToken();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test success test
        /// </summary>
        [TestCase]
        public void CreateDebugToken_Success_Test()
        {
            
            bool result = DebugTokenObject.createDebugToken();

            /// Verify Test Case
            Assert.IsTrue(result);

        }

        /// <summary>
        /// Test what happens when DeviceID is empty
        /// </summary>
        [TestCase]
        public void getDevicePin_EmptyDeviceID_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.DeviceIP = "";
            bool result = DebugTokenObject.getDevicePin();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test what happens when a successful command is run
        /// </summary>
        [TestCase]
        public void getDevicePin_Success_Test()
        {
            /// Initialize Test Case
            bool result = DebugTokenObject.getDevicePin();

            /// Verify Test Case
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Test what happens when an invalid password is sent
        /// </summary>
        [TestCase]
        public void getDevicePin_InvalidPassword_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.DevicePassword = "error";
            bool result = DebugTokenObject.getDevicePin();

            /// Verify Test Case
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Test what happens when an invalid Device IP is sent
        /// </summary>
        [TestCase]
        public void getDevicePin_InvalidDeviceIP_Test()
        {
            /// Initialize Test Case
            DebugTokenObject.DeviceIP = "1.1.1.1";
            bool result = DebugTokenObject.getDevicePin();

            /// Verify Test Case
            Assert.IsFalse(result);
        }
    }
}
