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
using System.Diagnostics;
using System.Threading;

namespace RIM.VSNDK_Package.Signing
{
    /// <summary>
    /// Interaction logic for SigningDialog.xaml
    /// </summary>
    public partial class SigningDialog : DialogWindow
    {
        private string certPath;
        private string bbidtokenPath;
        private RIMSiginingAuthorityData data;

        /// <summary>
        /// Thread responsible for moving the bbidtoken.csk file from the default downloads folder to the correct one.
        /// </summary>
        private Thread m_moveCSKFileThread = null;

        public SigningDialog()
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(SigningDialog_Closing);

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            certPath = folder +  @"\Research In Motion\author.p12";
            bbidtokenPath = folder + @"\Research In Motion\bbidtoken.csk";
            data = gbRIMSigningAuthority.DataContext as RIMSiginingAuthorityData;
            UpdateUI(File.Exists(certPath));
        }

        public class MoveCSKFile
        {
            private SigningDialog _dialog;
            private string _downloadPath;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dialog"></param>
            public MoveCSKFile(SigningDialog dialog, string downloadPath)
            {
                _dialog = dialog;
                _downloadPath = downloadPath;
            }

            /// <summary>
            /// Thread responsible for copying the bbidtoken.csk file from the default downloads folder to the correct one.
            /// </summary>
            public void movingCSKFile()
            {
                // wait the file to be downloaded.
                while (!File.Exists(_downloadPath))
                {
                    Thread.Sleep(200);
                }

                // delete an already existing bbidtoken.csk file.
                if (File.Exists(_dialog.bbidtokenPath))
                    File.Delete(_dialog.bbidtokenPath);

                // moving the downloaded bbidtoken.csk file to the correct folder
                File.Copy(_downloadPath, _dialog.bbidtokenPath, true);

                // update the user interface.
                if (File.Exists(_dialog.bbidtokenPath))
                {
                    _dialog.Dispatcher.Invoke((Action)(() =>
                    {
                        RegistrationWindow win = new RegistrationWindow();
                        bool? res = win.ShowDialog();
                        _dialog.UpdateUI(File.Exists(_dialog.certPath));
                    }));
                }
            }
        }

        /// <summary>
        /// Private method to update the screen UI
        /// </summary>
        /// <param name="registered"></param>
        private void UpdateUI(bool registered)
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
            // if user clicks Setup button twice, the previous thread is closed.
            if ((m_moveCSKFileThread != null) && (m_moveCSKFileThread.IsAlive))
                m_moveCSKFileThread.Abort();

            // identifying the name of the file to be downloaded. If it already exists, Windows will add a " (x)" at the end.
            string downloadPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Downloads\bbidtoken");
            string tail = ".csk";
            int i = 0;
            while (File.Exists(downloadPath + tail))
            {
                i++;
                tail = " (" + i + ").csk";
            }
            downloadPath += tail;

            // start the thread
            MoveCSKFile moveFile;
            moveFile = new MoveCSKFile(this, downloadPath);
            m_moveCSKFileThread = new Thread(moveFile.movingCSKFile);
            m_moveCSKFileThread.Start();

            // open link in the default browser.
            Process.Start("https://bdsc01cnc.rim.net:8443/bdsc/Developer/csk.html");

//            RegistrationWindow win = new RegistrationWindow();
//            bool? res = win.ShowDialog();
//            UpdateUI(File.Exists(certPath), File.Exists(bbidtokenPath));
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

        /// <summary>
        /// Terminate m_moveCSKFileThread thread and close this window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SigningDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // if there is a running thread (i.e., the user clicked in Setup button but didn't download any file), close it.
            if ((m_moveCSKFileThread != null) && (m_moveCSKFileThread.IsAlive))
                m_moveCSKFileThread.Abort();
        }
    }
}
