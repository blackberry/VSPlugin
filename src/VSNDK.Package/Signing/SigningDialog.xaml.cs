//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RIM.VSNDK_Package.Signing.Models;
using System.IO;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Win32;
using System.Xml;

namespace RIM.VSNDK_Package.Signing
{
    /// <summary>
    /// Interaction logic for SigningDialog.xaml
    /// </summary>
    public partial class SigningDialog : DialogWindow
    {
        public string certPath;
        public string bbidtokenPath;
        private RIMSiginingAuthorityData data;

        public static bool ndk10_2_orNewer = true;
        public static bool useNewSigningMethodology = true;
        public static string latestNDK_Hostpath = "";

        public static void updateNDKHostpath()
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;
            try
            {
                rkNDKPath = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                string ndkHostPath = rkNDKPath.GetValue("NDKHostPath").ToString();
                if (ndkHostPath.Contains("host_10_0") || ndkHostPath.Contains("host_10_1") || ndkHostPath.Contains("bbndk-2.1.0") || ndkHostPath.Contains("host/"))
                    ndk10_2_orNewer = false;
            }
            catch
            {
            }
            rkNDKPath.Close();
            rkHKCU.Close();

            if (!ndk10_2_orNewer)
            {
                string[] filePaths = Directory.GetFiles(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\BlackBerry Native SDK\qconfig\", "*.xml");
                int latestV1, latestV2, latestV3, latestV4;
                latestV1 = latestV2 = latestV3 = latestV4 = 0;

                foreach (string file in filePaths)
                {
                    try
                    {
                        int p1, p2, v1, v2, v3, v4;
                        v1 = v2 = v3 = v4 = 0;
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(file);
                        string hostpath = xmlDoc.GetElementsByTagName("host")[0].InnerText;

                        if (hostpath == latestNDK_Hostpath)
                            continue;

                        try
                        {
                            p1 = hostpath.IndexOf('_') + 1;
                            p2 = hostpath.IndexOf('_', p1);
                            v1 = Convert.ToInt32(hostpath.Substring(p1, p2 - p1));

                            p1 = p2 + 1;
                            p2 = hostpath.IndexOf('_', p1);
                            v2 = Convert.ToInt32(hostpath.Substring(p1, p2 - p1));

                            p1 = p2 + 1;
                            p2 = hostpath.IndexOf('_', p1);
                            v3 = Convert.ToInt32(hostpath.Substring(p1, p2 - p1));

                            p1 = p2 + 1;
                            p2 = hostpath.IndexOf('/', p1);
                            v4 = Convert.ToInt32(hostpath.Substring(p1, p2 - p1));
                        }
                        catch
                        {
                        }

                        bool latest = false;

                        if (v1 > latestV1)
                        {
                            latest = true;
                        }
                        else if (v1 == latestV1)
                        {
                            if (v2 > latestV2)
                            {
                                latest = true;
                            }
                            else if (v2 == latestV2)
                            {
                                if (v3 > latestV3)
                                {
                                    latest = true;
                                }
                                else if (v3 == latestV3)
                                {
                                    if (v4 > latestV4)
                                    {
                                        latest = true;
                                    }
                                }
                            }
                        }

                        if (latest)
                        {
                            latestNDK_Hostpath = hostpath;
                            latestV1 = v1;
                            latestV2 = v2;
                            latestV3 = v3;
                            latestV4 = v4;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                }
                if ((latestV1 >= 10) && (latestV2 >= 2))
                {
                    latestNDK_Hostpath = latestNDK_Hostpath + "/usr/bin/";
                    useNewSigningMethodology = true;
                }
                else
                    useNewSigningMethodology = false;

            }
        }

        public SigningDialog()
        {
            InitializeComponent();

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            certPath = folder +  @"\Research In Motion\author.p12";
            bbidtokenPath = folder + @"\Research In Motion\bbidtoken.csk";
            data = gbRIMSigningAuthority.DataContext as RIMSiginingAuthorityData;
            UpdateUI(File.Exists(certPath));
        }


        /// <summary>
        /// Private method to update the screen UI
        /// </summary>
        /// <param name="registered"></param>
        public void UpdateUI(bool registered)
        {
            if (data != null)
            {
                data.Registered = registered;

                btnUnregister.IsEnabled = registered;
                btnRegister.IsEnabled = !registered;
                btnBackup.IsEnabled = registered;
            }
        }

        /// <summary>
        /// Open BlackBerry Signing in the default browser and start a thread that will move the downloaded 
        /// bbidtoken.csk file to the right folder. Then, it is presented the Regisration Dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            updateNDKHostpath();
            if ((ndk10_2_orNewer) || (useNewSigningMethodology))
            {
                Browser wb = new Browser(this);
                wb.Show();
            }
            else
            {
                MessageBox.Show("You have to download the NDK 10.2 or higher to be able to sign and register your app.", "Missing NDK 10.2", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Unregister your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUnregister_Click(object sender, RoutedEventArgs e)
        {
            DeRegisterWindow win = new DeRegisterWindow();
            bool? res = win.ShowDialog();
            UpdateUI(File.Exists(certPath));    
        }

        /// <summary>
        /// Backup your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            BackupRestoreData brData = gbBackupRestore.DataContext as BackupRestoreData;
            string zipfile = string.Empty;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "signingkey";
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "zip files (.zip)|*.zip"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                zipfile = dlg.FileName;
                brData.Backup(System.IO.Path.GetDirectoryName(certPath), zipfile);
            }
        }

        /// <summary>
        /// Restore your signing keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            BackupRestoreData brData = gbBackupRestore.DataContext as BackupRestoreData;
            string zipfile = string.Empty;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "zip files (.zip)|*.zip"; // Filter files by extension
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                zipfile = dlg.FileName;
                brData.Restore(zipfile, System.IO.Path.GetDirectoryName(certPath));
                UpdateUI(File.Exists(certPath));
            }
        }
    }
}
