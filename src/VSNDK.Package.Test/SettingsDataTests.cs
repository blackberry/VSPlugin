using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RIM.VSNDK_Package.Settings.Models;
using RIM.VSNDK_Package;
using Microsoft.Win32;
using System.Collections;
using System.IO;
using System.Xml;
using System.Windows.Data;

namespace VSNDK.Package.Test
{
    [TestFixture]
    class SettingsDataTests
    {
        private SettingsData settingsDataObject;

        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureSetUp]
        public void TFSetup()
        {
            /// Create Object
            settingsDataObject = new SettingsData();

            /// Backup NDK Entries
            string[] dirPaths = new string[2];
            dirPaths[0] = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\";
            dirPaths[1] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";

            for (int i = 0; i < 2; i++)
            {
                if (!Directory.Exists(dirPaths[i]))
                    continue;

                string[] filePaths = Directory.GetFiles(dirPaths[i], "*.xml");

                foreach (string file in filePaths)
                {
                    try
                    {
                        File.Move(file, file.Replace(".xml", ".tmp"));
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Setup for testing
        /// </summary>
        [TestFixtureTearDown]
        public void TFTearDown()
        {
            /// Backup NDK Entries
            string[] dirPaths = new string[2];
            dirPaths[0] = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\";
            dirPaths[1] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";

            for (int i = 0; i < 2; i++)
            {
                if (!Directory.Exists(dirPaths[i]))
                    continue;

                string[] filePaths = Directory.GetFiles(dirPaths[i], "*.tmp");

                foreach (string file in filePaths)
                {
                    try
                    {
                        File.Move(file, file.Replace(".tmp", ".xml"));
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Test function with key values not set.
        /// </summary>
        [TestCase]
        public void GetDeviceInfoTest_KeyValuesNotSet()
        {
            /// Setup 
            settingsDataObject.DeviceIP = "";
            settingsDataObject.DevicePassword = "";

            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {

                    key.DeleteValue("device_password", false);
                    key.DeleteValue("device_IP", false);
                }
            }

            settingsDataObject.GetDeviceInfo();

            Assert.IsNullOrEmpty(settingsDataObject.DeviceIP, "Device IP is not empty or null, " + settingsDataObject.DeviceIP);
            Assert.IsNullOrEmpty(settingsDataObject.DevicePassword, "Device Password is not empty or null, " + settingsDataObject.DevicePassword);
        }

        /// <summary>
        /// Test function with keys set.
        /// </summary>
        [TestCase]
        public void GetDeviceInfoTest_KeyValuesSet()
        {
            string ipAddr = "127.0.0.1";
            string password = "password";

            /// Setup 
            settingsDataObject.DeviceIP = "";
            settingsDataObject.DevicePassword = "";


            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    key.SetValue("device_password", GlobalFunctions.Encrypt(password), RegistryValueKind.String);
                    key.SetValue("device_IP", ipAddr, RegistryValueKind.String);
                }
            }

            settingsDataObject.GetDeviceInfo();

            Assert.IsTrue(settingsDataObject.DeviceIP == ipAddr , "Device IP value is not equal to test value, " + ipAddr + "!= " + settingsDataObject.DeviceIP);
            Assert.IsTrue(settingsDataObject.DevicePassword == password, "Device Password value is not equal to test value, " + password + " != " + settingsDataObject.DevicePassword);
        }

        /// <summary>
        /// Test function to set device info
        /// </summary>
        [TestCase]
        public void SetDeviceInfoTest()
        {
            string deviceIP = "127.0.0.1";
            string devicePassword = "password";

            /// Setup 
            settingsDataObject.DeviceIP = deviceIP;
            settingsDataObject.DevicePassword = devicePassword;

            settingsDataObject.SetDeviceInfo();


            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    Assert.IsTrue(deviceIP == key.GetValue("device_IP").ToString(), "Device IP value is not equal to test value, " + deviceIP + "!= " + key.GetValue("device_IP").ToString());
                    Assert.IsTrue(devicePassword == GlobalFunctions.Decrypt(key.GetValue("device_password").ToString()), "Device Password value is not equal to test value, " + devicePassword + " != " + GlobalFunctions.Decrypt(key.GetValue("device_password").ToString()));
                }
            }

        }
        
        /// <summary>
        /// Test function with key value not set
        /// </summary>
        [TestCase]
        public void GetSimulatorInfoTest_KeyValuesNotSet()
        {
            /// Setup 
            settingsDataObject.SimulatorIP = "";
            settingsDataObject.SimulatorPassword = "";

            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                   
                    key.DeleteValue("simulator_password", false);
                    key.DeleteValue("simulator_IP", false);
                }
            }

            settingsDataObject.GetSimulatorInfo();

            Assert.IsNullOrEmpty(settingsDataObject.SimulatorIP, "Simulator IP is not empty or null, " + settingsDataObject.SimulatorIP);
            Assert.IsNullOrEmpty(settingsDataObject.SimulatorPassword, "Simulator Password is not empty or null, " + settingsDataObject.SimulatorPassword);
        }

        /// <summary>
        /// Test function with key values set
        /// </summary>
        [TestCase]
        public void GetSimulatorInfoTest_KeyValuesSet()
        {
            string ipAddr = "127.0.0.1";
            string password = "password";

            /// Setup 
            settingsDataObject.SimulatorIP = "";
            settingsDataObject.SimulatorPassword = "";


            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    key.SetValue("simulator_password", GlobalFunctions.Encrypt(password), RegistryValueKind.String);
                    key.SetValue("simulator_IP", ipAddr, RegistryValueKind.String);
                }
            }

            settingsDataObject.GetSimulatorInfo();

            Assert.IsTrue(settingsDataObject.SimulatorIP == ipAddr , "Simulator IP value is not equal to test value, " + ipAddr + "!= " + settingsDataObject.SimulatorIP);
            Assert.IsTrue(settingsDataObject.SimulatorPassword == password, "Simulator Password value is not equal to test value, " + password + " != " + settingsDataObject.SimulatorPassword);
        }

        /// <summary>
        /// Test function to set simulator info
        /// </summary>
        [TestCase]
        public void SetSimulatorInfoTest()
        {
            string simulatorIP = "127.0.0.1";
            string simulatorPassword = "password";

            /// Setup 
            settingsDataObject.SimulatorIP = simulatorIP;
            settingsDataObject.SimulatorPassword = simulatorPassword;

            settingsDataObject.SetSimulatorInfo();


            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    Assert.IsTrue(simulatorIP == key.GetValue("simulator_IP").ToString(), "Simulator IP value is not equal to test value, " + simulatorIP + "!= " + key.GetValue("simulator_IP").ToString());
                    Assert.IsTrue(simulatorPassword == GlobalFunctions.Decrypt(key.GetValue("simulator_password").ToString()), "Simulator Password value is not equal to test value, " + simulatorPassword + " != " + GlobalFunctions.Decrypt(key.GetValue("simulator_password").ToString()));
                }
            }

        }

        /// <summary>
        /// Test function with key value not set
        /// </summary>
        [TestCase]
        public void GetNDKPathTest_KeyValuesNotSet()
        {
            /// Setup 
            settingsDataObject.HostPath = "";
            settingsDataObject.TargetPath = "";

            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {

                    key.DeleteValue("NDKHostPath", false);
                    key.DeleteValue("NDKTargetPath", false);
                }
            }

            settingsDataObject.GetNDKPath();

            Assert.IsNullOrEmpty(settingsDataObject.HostPath, "Host Path is not empty or null, " + settingsDataObject.HostPath);
            Assert.IsNullOrEmpty(settingsDataObject.TargetPath, "Target Path is not empty or null, " + settingsDataObject.TargetPath);
        }

        /// <summary>
        /// Test function with key values set
        /// </summary>
        [TestCase]
        public void GetNDKPathTest_KeyValuesSet()
        {
            string hostPath = @"C:\bbndk_vs\host\";
            string targetPath = @"C:\bbndk_vs\target\";

            /// Setup 
            settingsDataObject.HostPath = "";
            settingsDataObject.TargetPath = "";


            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    key.SetValue("NDKHostPath", hostPath, RegistryValueKind.String);
                    key.SetValue("NDKTargetPath", targetPath, RegistryValueKind.String);
                }
            }

            settingsDataObject.GetNDKPath();

            Assert.IsTrue(settingsDataObject.HostPath == hostPath, "Host Path value is not equal to test value, " + hostPath + "!= " + settingsDataObject.HostPath);
            Assert.IsTrue(settingsDataObject.TargetPath == targetPath, "Target Path value is not equal to test value, " + targetPath + " != " + settingsDataObject.TargetPath);
        }

        /// <summary>
        /// Test function with valid version number
        /// </summary>
        [TestCase]
        public void GetAPINameTest_ValidVersion()
        {
            string version = "10.2.0.1155";
            string name = "BlackBerry Native SDK 10.2";

            string result = settingsDataObject.GetAPIName(version);

            Assert.IsTrue(result == name, "API Name does not matched expected result., " + result + "!= " + name);
        }

        /// <summary>
        /// Test function with invalid version number
        /// </summary>
        [TestCase]
        public void GetAPINameTest_InvalidVersion()
        {
            string version = "1.1.1.1";
            string name = "";

            string result = settingsDataObject.GetAPIName(version);

            Assert.IsTrue(result == name, "API Name does not matched expected result., " + result + "!= " + name);
        }

        /// <summary>
        /// Test function with no API's
        /// </summary>
        [TestCase]
        public void RefreshScreen_NoAPIs()
        {
            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntries.Count == 0, "NDK Entries should be empty. ");
        }

        /// <summary>
        /// Test function with a single API in standard folder
        /// </summary>
        [TestCase]
        public void RefreshScreen_APIs1()
        {
            string version = "10.2.0.1155";

            GenerateXMLFiles(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\", version);

            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntries.Count == 1, "Number of Entries is unexpected. ");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).HostPath == @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86", "Host Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).TargetPath == @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6", "Target Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).NDKName == "BlackBerry Native SDK 10.2", "NDK Name is not expected");

            RemoveXmlFiles();
        }

        /// <summary>
        /// Test function with a single API in momentics folder
        /// </summary>
        [TestCase]
        public void RefreshScreen_APIs2()
        {
            string version = "10.2.0.1155";

            GenerateXMLFiles(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\", version);

            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntries.Count == 1, "Number of Entries is unexpected. ");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).HostPath == @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86", "Host Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).TargetPath == @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6", "Target Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).NDKName == "BlackBerry Native SDK 10.2", "NDK Name is not expected");

            RemoveXmlFiles();
        }

        /// <summary>
        /// Test function with  multiple APIs 
        /// </summary>
        [TestCase]
        public void RefreshScreen_APIs3()
        {
            string version = "10.2.0.1155";
            string version2 = "10.2.0.1200";

            GenerateXMLFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", version2);
            GenerateXMLFiles(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\", version);

            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntries.Count == 2, "Number of Entries is unexpected. ");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).HostPath == @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86", "Host Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).TargetPath == @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6", "Target Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).NDKName == "BlackBerry Native SDK 10.2", "NDK Name is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(1)).HostPath == @"C:/bbndk_vs/host_" + version2.Replace('.', '_') + @"/win32/x86", "Host Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(1)).TargetPath == @"C:/bbndk_vs/target_" + version2.Replace('.', '_') + @"/win32/qnx6", "Target Path is not expected");
            Assert.IsTrue(((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(1)).NDKName == "BlackBerry Native SDK 10.2", "NDK Name is not expected");

            RemoveXmlFiles();
        }

        /// <summary>
        /// Test function for selecting no API
        /// </summary>
        [TestCase]
        public void RefreshScreen_SelectedAPI1()
        {
            string version = "10.2.0.1155";
            string version2 = "10.2.0.1200";

            GenerateXMLFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", version2);
            GenerateXMLFiles(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\", version);

            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    key.SetValue("NDKHostPath", "", RegistryValueKind.String);
                    key.SetValue("NDKTargetPath", "", RegistryValueKind.String);
                }
            }

            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntryClass.HostPath == "", "Host path is not blank");
            Assert.IsTrue(settingsDataObject.NDKEntryClass.TargetPath == "", "Target path is not blank");
            Assert.IsTrue(settingsDataObject.NDKEntryClass.NDKName == "", "NDK name is not blank");

            RemoveXmlFiles();
        }


        /// <summary>
        /// Test function for selecting a single api
        /// </summary>
        [TestCase]
        public void RefreshScreen_SelectedAPI2()
        {
            string version = "10.2.0.1155";
            string version2 = "10.2.0.1200";

            GenerateXMLFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", version2);
            GenerateXMLFiles(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\", version);

            string keyName = @"Software\BlackBerry\BlackBerryVSPlugin";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (key == null)
                {
                    // Do Nothing
                }
                else
                {
                    key.SetValue("NDKHostPath", @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86", RegistryValueKind.String);
                    key.SetValue("NDKTargetPath", @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6", RegistryValueKind.String);
                }
            }

            settingsDataObject.RefreshScreen();

            Assert.IsTrue(settingsDataObject.NDKEntryClass.HostPath == @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86", "Host path is not " + @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86");
            Assert.IsTrue(settingsDataObject.NDKEntryClass.TargetPath == @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6", "Target path is not " + @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6");
            Assert.IsTrue(settingsDataObject.NDKEntryClass.NDKName == "BlackBerry Native SDK 10.2", "BlackBerry Native SDK 10.2");

            RemoveXmlFiles();
        }

        /// <summary>
        /// Test the setting of the NDK Paths
        /// </summary>
        [TestCase]
        public void TC_setNDKPaths()
        {
            // Initialize Test
            string targetPath = ((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).TargetPath;
            string hostPath = ((NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0)).HostPath;
            string qnx_config = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK";

            settingsDataObject.NDKEntryClass = (NDKEntryClass)settingsDataObject.NDKEntries.GetItemAt(0);

            RegistryKey regKeyCurrentUser = Registry.CurrentUser;
            RegistryKey regKey = regKeyCurrentUser.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
            regKey.SetValue("NDKTargetPath", "");
            regKey.SetValue("NDKHostPath", "");

            // Run Test
            settingsDataObject.SetNDKPaths();

            // Validate Test
            Assert.IsTrue(regKey.GetValue("NDKHostPath").ToString() == hostPath, "Host path does not match expected result");
            Assert.IsTrue(regKey.GetValue("NDKTargetPath").ToString() == targetPath, "Target path does not match expected result");
            Assert.IsTrue(System.Environment.GetEnvironmentVariable("QNX_TARGET") == targetPath, "QNX_TARGET system variable is not match expected result");
            Assert.IsTrue(System.Environment.GetEnvironmentVariable("QNX_HOST") == hostPath, "QNX_HOST system variable is not match expected result");
            Assert.IsTrue(System.Environment.GetEnvironmentVariable("QNX_CONFIGURATION") == qnx_config, "QNX_CONFIGURATION system variable is not match expected result");
            Assert.IsTrue(System.Environment.GetEnvironmentVariable("PATH").Contains(hostPath), "Path system variable does not contain host path");
            Assert.IsTrue(System.Environment.GetEnvironmentVariable("PATH").Contains(qnx_config), "Path system variable does not contain config path");
        }

        /// <summary>
        /// Helper function to generate XML Files
        /// </summary>
        /// <param name="version"></param>
        private void GenerateXMLFiles(string path, string version)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            doc.AppendChild(docNode);

            XmlNode sysDefNode = doc.CreateElement("qnxSystemDefinition");
            doc.AppendChild(sysDefNode);

            XmlNode installationNode = doc.CreateElement("installation");
            sysDefNode.AppendChild(installationNode);

            XmlNode baseNode = doc.CreateElement("base");
            baseNode.InnerText = @"C:/bbndk_vs";
            installationNode.AppendChild(baseNode);

            XmlNode hostNode = doc.CreateElement("host");
            hostNode.InnerText = @"C:/bbndk_vs/host_" + version.Replace('.', '_') + @"/win32/x86";
            installationNode.AppendChild(hostNode);

            XmlNode nameNode = doc.CreateElement("name");
            nameNode.InnerText = "BlackBerry Native SDK 10.2";
            installationNode.AppendChild(nameNode);

            XmlNode targetNode = doc.CreateElement("target");
            targetNode.InnerText = @"C:/bbndk_vs/target_" + version.Replace('.', '_') + @"/win32/qnx6";
            installationNode.AppendChild(targetNode);

            XmlNode versionNode = doc.CreateElement("version");
            versionNode.InnerText = version;
            installationNode.AppendChild(versionNode);

            XmlNode annotationNode = doc.CreateElement("annotation");
            installationNode.AppendChild(annotationNode);

            XmlNode appInfoNode = doc.CreateElement("appInfo");
            XmlAttribute appInfoAttribute = doc.CreateAttribute("source");
            appInfoAttribute.Value = "p2install";
            appInfoNode.Attributes.Append(appInfoAttribute);
            annotationNode.AppendChild(appInfoNode);

            XmlNode dversionNode = doc.CreateElement("detailedVersion");
            dversionNode.InnerText = version;
            appInfoNode.AppendChild(dversionNode);

            Directory.CreateDirectory(path);
            doc.Save(path + version + ".xml");
        }

        /// <summary>
        /// Helper function to remove test xml files
        /// </summary>
        private void RemoveXmlFiles()
        {
            /// Remove NDK Entries
            string[] dirPaths = new string[2];
            dirPaths[0] = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) + @"bbndk_vs\..\qconfig\";
            dirPaths[1] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\";

            for (int i = 0; i < 2; i++)
            {
                if (!Directory.Exists(dirPaths[i]))
                    continue;

                string[] filePaths = Directory.GetFiles(dirPaths[i], "*.xml");

                foreach (string file in filePaths)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }


    }
}
