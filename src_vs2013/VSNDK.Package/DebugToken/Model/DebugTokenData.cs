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
using RIM.VSNDK_Package.Signing.Models;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.IO;
using System.Windows.Data;
using RIM.VSNDK_Package.Signing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Input;

namespace RIM.VSNDK_Package.DebugToken.Model
{

    /// <summary>
    /// The DataModel for the DebugToken dialog
    /// </summary>
    public class DebugTokenData : INotifyPropertyChanged
    {

        #region Member Variables and Constants

        private static string _deviceIP;
        private static string _devicePassword;
        private static string _errors = "";
        private static string _devicePin = "Not Attached";
        private static string _companyName = "";
        private static string _authorID = "";
        private static string _tmpAuthorID = "";
        private static string _ndkTargetPath;
        private static string _ndkHostPath;
        private static string _certPath;
        private static string _deviceosversion = "";
        private static string _storepass;
        private static string _localFolder;
        private static bool _alreadyRegistered = false;
        private static string _tokenExpiryDate = "";
        private static string _tmpTokenExpiryDate = "";
        private static string _tokenAuthor = "";
        private static string _tmpTokenAuthor = "";
        public static bool _initializedCorrectly = true;
        private const string _colCompanyName = "CompanyName";
        private const string _colAuthorID = "AuthorID";
        private const string _colAttachedDevice = "AttachedDevice";
        private const string _colExpiryDate = "ExpiryDate";
        public static bool restart = false;

        #endregion

        #region Properties

        /// <summary>
        /// Company Name Property
        /// </summary>
        public bool AlreadyRegistered
        {
            get { return _alreadyRegistered; }
            set { _alreadyRegistered = value; }
        }

        /// <summary>
        /// KeyStore Password
        /// </summary>
        public string KeyStorePassword
        {
            get { return _storepass; }
            set { _storepass = value; }
        }

        /// <summary>
        /// Device PIN
        /// </summary>
        public string DevicePIN
        {
            get { return _devicePin; }
            set { _devicePin = value; }
        }

        /// <summary>
        /// Device Password
        /// </summary>
        public string DevicePassword
        {
            get { return _devicePassword; }
            set { _devicePassword = value; }
        }

        /// <summary>
        /// Device PIN
        /// </summary>
        public string DeviceIP 
        {
            get { return _deviceIP; }
            set { _deviceIP = value; }
        }

        /// <summary>
        /// Device OS Version
        /// </summary>
        public string DeviceOSVersion
        {
            get { return _deviceosversion; }
            set { _deviceosversion = value; }
        }

        /// <summary>
        /// Signing Certificate Path Property
        /// </summary>
        public string CertPath
        {
            get { return _certPath; }
            set { _certPath = value; }
        }

        /// <summary>
        /// Local Folder Property
        /// </summary>
        public string LocalFolder
        {
            get { return _localFolder; }
            set { _localFolder = value; }
        }

        /// <summary>
        /// Expiry Date Property
        /// </summary>
        public string ExpiryDate
        {
            get { return _tokenExpiryDate; }
            set { _tokenExpiryDate = value; OnPropertyChanged(_colExpiryDate); }
        }

        /// <summary>
        /// Contains any errors during the registration
        /// </summary>
        public string Error
        {
            get { return _errors; }
            set { _errors = value; }
        }

        /// <summary>
        /// Company Name Property
        /// </summary>
        public string CompanyName
        {
            get { return _companyName; }
            set { _companyName = value; OnPropertyChanged(_colCompanyName); }
        }

        /// <summary>
        /// Author ID Property
        /// </summary>
        public string AuthorID
        {
            get { return _authorID; }
            set { _authorID = value; OnPropertyChanged(_colAuthorID); }
        }

        /// <summary>
        /// Attached Device Property
        /// </summary>
        public string AttachedDevice
        {
            get
            {
              
                if (DevicePIN != "Not Attached")
                {
                    return DevicePIN.Substring(0, 2) + DevicePIN.Substring(2, 8).ToUpper();
                }
                else
                {
                    return DevicePIN;
                }
            }
            set 
            {
                DevicePIN = value;
                OnPropertyChanged(_colAttachedDevice);
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public DebugTokenData()
        {
            refreshScreen();
        }

        public bool resetPassword()
        {
            System.Windows.Forms.Cursor currentCursor = System.Windows.Forms.Cursor.Current;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;

            _errors = "";
            RegistrationWindow win = new RegistrationWindow();
            win.ResizeMode = System.Windows.ResizeMode.NoResize;
            bool? res = win.ShowDialog();
            if (res == true)
                KeyStorePassword = win.tbPassword.Password;

            if (currentCursor.ToString().Contains("Wait"))
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            return res == true;
        }

        /// <summary>
        /// Set all the screen info
        /// </summary>
        private void refreshScreen()
        {
            LocalFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\";
            CertPath = LocalFolder + "author.p12";

            if (!File.Exists(CertPath))
            {
                System.Windows.Forms.MessageBox.Show("Missing Signing Keys. Use menu \"BlackBerry\" -> \"Signing\" to register your signing keys.", "Signing keys not registered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _initializedCorrectly = false;
                _tmpTokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tokenExpiryDate = "";
                DevicePIN = "Not Attached";
                return;
            }

            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkPluginRegKey = null;

            try
            {
                rkPluginRegKey = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                DeviceIP = rkPluginRegKey.GetValue("device_IP").ToString();
                DevicePassword = rkPluginRegKey.GetValue("device_password").ToString();
                _ndkHostPath = rkPluginRegKey.GetValue("NDKHostPath").ToString();
                _ndkTargetPath = rkPluginRegKey.GetValue("NDKTargetPath").ToString();
                KeyStorePassword = (rkPluginRegKey.GetValue("CSKPass") != null) ? rkPluginRegKey.GetValue("CSKPass").ToString() : "";
                _alreadyRegistered = false;
                _tokenExpiryDate = "";
                _tmpTokenExpiryDate = "";
                _tokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tmpTokenAuthor = "";

                if ((DeviceIP == "") || (DeviceIP == null))
                {
                    this.Error = "You have a missing or incorrect Device IP.  Please check BlackBerry - Settings.";   
                    rkPluginRegKey.Close();
                    rkHKCU.Close();
                    _initializedCorrectly = false;
                    _tmpTokenAuthor = "";
                    _tmpAuthorID = "";
                    _authorID = "";
                    _companyName = "";
                    _tokenExpiryDate = "";
                    DevicePIN = "Not Attached";
                    return;
                }

                if (DevicePassword != null)
                {
                    try
                    {
                        DevicePassword = Decrypt(DevicePassword);
                    }
                    catch
                    {
                        MessageBox.Show("Missing Device password", "Missing Device Password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        rkPluginRegKey.Close();
                        rkHKCU.Close();
                        _initializedCorrectly = false;
                        _tmpTokenAuthor = "";
                        _tmpAuthorID = "";
                        _authorID = "";
                        _companyName = "";
                        _tokenExpiryDate = "";
                        DevicePIN = "Not Attached";
                        return;
                    }
                }

                if (KeyStorePassword != "")
                {
                    KeyStorePassword = Decrypt(KeyStorePassword);
                }

                if (getDeviceInfo())
                {
                    if (getDebugTokenInfo())
                    {
                        isRegistered();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Microsoft Visual Studio", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }

            rkPluginRegKey.Close();
            rkHKCU.Close();

        }

        /// <summary>
        /// Add current device to device list.
        /// </summary>
        public bool addDevice(DebugTokenDialog parent)
        {
           
            if (DevicePIN == "Not Attached")
            {
                _errors = "No device attached.\n";
                return false;
            }

            if (KeyStorePassword == "")
            {
                if (!resetPassword())
                    return false;
            }

            bool validatePW = true;

            while (validatePW)
            {
                if (createDebugToken()) uploadDebugToken();

                if ((_errors.Contains("invalid password")) || (_errors.Contains("password is not valid")) || (_errors.Contains("invalid store password")))
                {
                    if (MessageBox.Show("The specified password is invalid.\n\nPlease, enter the same one that you used to generate your BB ID Token.\n\nIf you don't remember it, you can unregister and register again using BlackBerry -> Signing menu.", "Debug Tokens", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                        return false;
                    validatePW = true;
                    resetPassword();
                }
                else
                {
                    validatePW = false;
                }
            }

            if (_errors != "")
            {
                MessageBox.Show(_errors, "Debug Tokens", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _initializedCorrectly = false;
                _tmpTokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tokenExpiryDate = "";
                return false;
            }

            refreshScreen();
            return true;
        }

        /// <summary>
        /// Refresh current device to device list.
        /// </summary>
        public bool refreshDevice(DebugTokenDialog parent)
        {
            if (DevicePIN == "Not Attached")
            {
                _errors = "No device attached.\n";
                return false;
            }

            if (KeyStorePassword == "")
            {
                if (!resetPassword())
                    return false;
            }

            bool validatePW = true;

            while (validatePW)
            {
                if (createDebugToken()) uploadDebugToken();

                if ((_errors.Contains("invalid password")) || (_errors.Contains("password is not valid")) || (_errors.Contains("invalid store password")))
                {
                    if (MessageBox.Show("The specified password is invalid.\n\nPlease, enter the same one that you used to generate your BB ID Token.\n\nIf you don't remember it, you can unregister and register again using BlackBerry -> Signing menu.", _errors, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                        return false;
                    validatePW = true;
                    resetPassword();
                }
                else
                {
                    validatePW = false;
                }
            }

            if (_errors.Contains("Cannot connect:"))
            {
                MessageBox.Show(_errors, "Cannot connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _initializedCorrectly = false;
                _tmpTokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tokenExpiryDate = "";
                DevicePIN = "Not Attached";
                return false;
            }

            refreshScreen();
            return true;
        }

        /// <summary>
        /// Retrieve debug token details
        /// </summary>
        /// <returns>True if successfully retrieved</returns>
        private bool getDebugTokenInfo()
        {
            bool success = false;

            if (!File.Exists(LocalFolder  + "DebugToken.bar"))
            {
                return success;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);


            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\";
            startInfo.Arguments = string.Format(@"/C blackberry-airpackager.bat -listManifest ""{0}""", LocalFolder + "DebugToken.bar");

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;

        }
        
        /// <summary>
        /// Get the device Info of the connected device
        /// </summary>
        /// <returns>True if successful</returns>
        public bool getDeviceInfo()
        {
            bool success = false;

            if (string.IsNullOrEmpty(DeviceIP))
            {
                return success;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\"; 
            startInfo.Arguments = string.Format("/C blackberry-deploy.bat -listDeviceInfo {0} -password {1}", DeviceIP, DevicePassword);

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }

                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

          return success;

        }

        /// <summary>
        /// Get the list of registered devices and display
        /// </summary>
        private bool isRegistered()
        {
            //Reset Screen
            CompanyName = "";
            AuthorID = "";
            ExpiryDate = "";

            if ((_tmpTokenExpiryDate != "") && (_tmpTokenAuthor == _tokenAuthor))
            {
                AlreadyRegistered = true;
                CompanyName = _tmpTokenAuthor;
                AuthorID = _tmpAuthorID;
                ExpiryDate = _tmpTokenExpiryDate;
            }
            else
            {
                AlreadyRegistered = false;
            }

            return AlreadyRegistered;
        }

        /// <summary>
        /// Upload Token to connected device
        /// </summary>
        /// <returns>True if successful</returns>
        private bool uploadDebugToken()
        {
            bool success = false;

            if (string.IsNullOrEmpty(DeviceIP))
            {
                return success;
            }
            else if (!File.Exists(LocalFolder + "DebugToken.bar"))
            {
                return success;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            /// Request Debug Token
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\";
            startInfo.Arguments = string.Format(@"/C blackberry-deploy.bat -installDebugToken ""{0}"" -device {1} -password {2}", LocalFolder + "DebugToken.bar", DeviceIP, DevicePassword);

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;

        }

        /// <summary>
        /// remove debug token from connected device
        /// </summary>
        /// <returns>True if successful</returns>
        private bool removeDebugToken()
        {
            bool success = false;

            if (string.IsNullOrEmpty(DeviceIP))
            {
                return success;
            }
            else if (string.IsNullOrEmpty(_authorID))
            {
                return success;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            /// Request Debug Token
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\";
            startInfo.Arguments = string.Format(@"/C blackberry-deploy.bat -uninstallApp -device {0} -password {1} -package-id {2}", DeviceIP, DevicePassword, _authorID);

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;

        }

        /// <summary>
        /// Create a new Debug Token for connected device
        /// </summary>
        /// <returns>True if successful</returns>
        public bool createDebugToken()
        {
            bool success = false;

            if (string.IsNullOrEmpty(CertPath))
            {
                return success;
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            /// Request Debug Token
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\BlackBerry\\VSPlugin-NDK\\qnxtools\\bin\\";
            startInfo.Arguments = string.Format(@"/C blackberry-debugtokenrequest.bat -storepass {0} -deviceid ""{1}"" ""{2}""",
                KeyStorePassword, DevicePIN, LocalFolder + "DebugToken.bar");


            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            return success;

        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
                if (e.Data.Contains("Error:"))
                    _errors += e.Data + "\n";
                else if (e.Data.Contains("devicepin::"))
                {
                    AttachedDevice = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
                    _alreadyRegistered = false;
                }
                else if (e.Data.Contains("Package-Author-Id:"))
                    _tmpAuthorID = e.Data.Substring(e.Data.LastIndexOf(": ") + 2);
                else if (e.Data.Contains("Package-Author:"))
                    _tmpTokenAuthor = e.Data.Substring(e.Data.LastIndexOf(": ") + 2);
                else if (e.Data.Contains("[n]debug_token_expiration::"))
                    _tmpTokenExpiryDate = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
                else if (e.Data.Contains("[n]debug_token_author::"))
                    _tokenAuthor = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
                else if (e.Data.Contains("scmbundle::"))
                    _deviceosversion = e.Data.Substring(e.Data.LastIndexOf("::") + 2);
            }
        }

        /// <summary>
        /// On Error received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);

                if (e.Data.Contains("-password"))
                {
                    _errors += "The password supplied for connecting to your device is not set or invalid\n";
                }
                else
                    _errors += e.Data + "\n";
            }
        }



        /// <summary>
        /// Encrypts a given password and returns the encrypted data
        /// as a base64 string.
        /// </summary>
        /// <param name="plainText">An unencrypted string that needs
        /// to be secured.</param>
        /// <returns>A base64 encoded string that represents the encrypted
        /// binary data.
        /// </returns>
        /// <remarks>This solution is not really secure as we are
        /// keeping strings in memory. If runtime protection is essential,
        /// <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="plainText"/>
        /// is a null reference.</exception>
        public string Encrypt(string plainText)
        {
            if (plainText == null) throw new ArgumentNullException("plainText");

            //encrypt data
            var data = Encoding.Unicode.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);

            //return as base64 string
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        /// <param name="cipher">A base64 encoded string that was created
        /// through the <see cref="Encrypt(string)"/> or
        /// <see cref="Encrypt(SecureString)"/> extension methods.</param>
        /// <returns>The decrypted string.</returns>
        /// <remarks>Keep in mind that the decrypted string remains in memory
        /// and makes your application vulnerable per se. If runtime protection
        /// is essential, <see cref="SecureString"/> should be used.</remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="cipher"/>
        /// is a null reference.</exception>
        public string Decrypt(string cipher)
        {
            if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire the PropertyChnaged event handler on change of property
        /// </summary>
        /// <param name="propName"></param>
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

    }
}
