using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RIM.VSNDK_Package.UpdateManager.Model;
using RIM.VSNDK_Package;
using System.IO;
using Microsoft.Win32;

namespace VSNDK.Package.Test
{
    [TestFixture]
    class UpdateManagerDataTestClass
    {
        private UpdateManagerData updateManagerDataObject;

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureSetUp]
        public void TF_Setup()
        {
            /// Create Object
            updateManagerDataObject = new UpdateManagerData();

        }

        [TestFixtureTearDown]
        public void TF_TearDown()
        {

        }

        /// <summary>
        /// Test GetAPILevel with a bad version expecting a no match
        /// </summary>
        [TestCase]
        public void TC_GetAPILevel_NoMatch()
        {
            /// Initialize Test
            string version = "1.0.0.0";
            string expectedResult = "";

            /// Run Test
            string result = updateManagerDataObject.GetAPILevel(version);

            /// Validate Test
            Assert.IsTrue(result == expectedResult, "Expected result is not empty, " + result); 
        }

        /// <summary>
        /// Test GetAPILevel with a valid version expecting the correct API Level
        /// </summary>
        [TestCase]
        public void TC_GetAPILevel_Match()
        {
            /// Initialize Test
            string version = "10.2.0";
            string expectedResult = "10.2.0.1155";

            /// Run Test
            string result = updateManagerDataObject.GetAPILevel(version);

            /// Validate Test
            Assert.IsTrue(result == expectedResult, "Expected result does not match, " + result + " != " + expectedResult);
        }

        /// <summary>
        /// Test IsRuntimeInstalled with a invalid version expecting blank
        /// </summary>
        [TestCase]
        public void TC_IsRuntimeInstalled_NoMatch()
        {
            /// Initialize Test
            string version = "10.2.0.1197";

            /// Run Test
            bool result = updateManagerDataObject.IsRuntimeInstalled(version);

            /// Validate Test
            Assert.IsFalse(result, "Result should be false.");
        }

        /// <summary>
        /// Test IsRuntimeInstalled with a invalid version expecting blank
        /// </summary>
        [TestCase]
        public void TC_IsRuntimeInstalled_Match()
        {
            /// Initialize Test
            string version = "10.2.0.1197";
            Directory.CreateDirectory(GlobalFunctions.bbndkPathConst + @"\runtime_" + version.Replace(".", "_"));

            /// Run Test
            bool result = updateManagerDataObject.IsRuntimeInstalled(version);

            /// Validate Test
            Assert.IsTrue(result, "Result should be true.");

            ///Clean Up Test
            Directory.Delete(GlobalFunctions.bbndkPathConst + @"\runtime_" + version.Replace(".", "_"));
        }

        /// <summary>
        /// Test getCurrentAPIVersion when an API Version is not set.
        /// </summary>
        [TestCase]
        public void TC_getCurrentAPIVersion_Unset()
        {
            /// Initialize Test 
            RegistryKey regKeyCurrentUser = Registry.CurrentUser;
            RegistryKey regKey = regKeyCurrentUser.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            regKey.SetValue("NDKTargetPath", "");
            regKey.Close();
            regKeyCurrentUser.Close();

            /// Run Test
            string result = updateManagerDataObject.getCurrentAPIVersion();

            /// Validate Test
            Assert.IsTrue(result == "", "Return value doesn't match expected result. " + result + " != " + "");

        }

        /// <summary>
        /// Test getCurrentAPIVersion when an API Version is set.
        /// </summary>
        [TestCase]
        public void TC_getCurrentAPIVersion_Set()
        {
            /// Initialize Test 
            string version = "10.2.0.1155";
            RegistryKey regKeyCurrentUser = Registry.CurrentUser;
            RegistryKey regKey = regKeyCurrentUser.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            regKey.SetValue("NDKTargetPath", @"C:/bbndk_vs/target_" + version.Replace('.', '_') + "/qnx6");

            /// Run Test
            string result = updateManagerDataObject.getCurrentAPIVersion();

            /// Validate Test
            Assert.IsTrue(result == version, "Return value doesn't match expected result. " + result + " != " + version);

            /// Clean Up 
            regKey.SetValue("NDKTargetPath", "");
            regKey.Close();
            regKeyCurrentUser.Close();
        }


    }
}
