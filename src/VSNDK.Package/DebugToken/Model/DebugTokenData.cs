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

namespace RIM.VSNDK_Package.DebugToken.Model
{

    /// <summary>
    /// The DataModel for the DebugToken dialog
    /// </summary>
    class DebugTokenData : NotifyPropertyChanged
    {

        #region Member Variables and Constants
        private static string _deviceIP;
        private static string _devicePassword;
        private static string _errors;
        private static string _devicePin = "Not Attached";
        private static string _companyName = "";
        private static string _authorID = "";
        private static string _tmpAuthorID = "";
        private static string _ndkTargetPath;
        private static string _ndkHostPath;
        private static string _certPath;
        private static string _storepass;
        private static string _localRIMFolder;
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
        #endregion

        /// <summary>
        /// Company Name Property
        /// </summary>
        public bool AlreadyRegistered
        {
            get { return _alreadyRegistered; }
            set { _alreadyRegistered = value; }
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
                if (_devicePin != "Not Attached")
                {
                    return _devicePin.Substring(0, 2) + _devicePin.Substring(2, 8).ToUpper();
                }
                else
                {
                    return _devicePin;
                }
            }
            set 
            { 
                _devicePin = value;
                OnPropertyChanged(_colAttachedDevice);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DebugTokenData()
        {
            refreshScreen();
        }

        public bool resetPassword()
        {
            _errors = "";
            ResetPasswordWindow win = new ResetPasswordWindow();
            bool? res = win.ShowDialog();
            if (res == true)
                _storepass = win.tbCSKPassword.Password;

            return res == true;
        }

        /// <summary>
        /// Set all the screen info
        /// </summary>
        private void refreshScreen()
        {
            string certPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\author.p12";

            if (!File.Exists(certPath))
            {
                System.Windows.Forms.MessageBox.Show("Missing Signing Keys. Use menu \"BlackBerry\" -> \"Signing\" to register your signing keys.", "Signing keys not registered", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _initializedCorrectly = false;
                _tmpTokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tokenExpiryDate = "";
                _devicePin = "Not Attached";
                return;
            }

            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkPluginRegKey = null;

            try
            {
                rkPluginRegKey = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                _deviceIP = rkPluginRegKey.GetValue("device_IP").ToString();
                _devicePassword = rkPluginRegKey.GetValue("device_password").ToString();
                _ndkHostPath = rkPluginRegKey.GetValue("NDKHostPath").ToString();
                _ndkTargetPath = rkPluginRegKey.GetValue("NDKTargetPath").ToString();
                _storepass = (rkPluginRegKey.GetValue("CSKPass") != null) ? rkPluginRegKey.GetValue("CSKPass").ToString() : "";
                _localRIMFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\";
                _certPath = _localRIMFolder + "author.p12";
                _alreadyRegistered = false;
                _tokenExpiryDate = "";
                _tmpTokenExpiryDate = "";
                _tokenAuthor = "";
                _tmpAuthorID = "";
                _authorID = "";
                _companyName = "";
                _tmpTokenAuthor = "";
                if (_errors == null)
                    _errors = "";

                if ((_deviceIP == "") || (_deviceIP == null))
                {
                    MessageBox.Show("Missing Device IP", "Missing Device IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    rkPluginRegKey.Close();
                    rkHKCU.Close();
                    _initializedCorrectly = false;
                    _tmpTokenAuthor = "";
                    _tmpAuthorID = "";
                    _authorID = "";
                    _companyName = "";
                    _tokenExpiryDate = "";
                    _devicePin = "Not Attached";
                    return;
                }

                if (_devicePassword != null)
                {
                    try
                    {
                        _devicePassword = Decrypt(_devicePassword);
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
                        _devicePin = "Not Attached";
                        return;
                    }
                }

                if (_storepass != "")
                {
                    _storepass = Decrypt(_storepass);
                }

                if (getDevicePin())
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
        public bool addDevice()
        {
            if (_devicePin == "Not Attached")
            {
                _errors = "No device attached.\n";
                return false;
            }

            if (_storepass == "")
            {
                if (!resetPassword())
                    return false;
            }

            if (createDebugToken()) uploadDebugToken();

            while (_errors.Contains("invalid password"))
            {
                MessageBox.Show(_errors, "Invalid Password");

                if (!resetPassword())
                    return false;

                if (createDebugToken()) uploadDebugToken();
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
                _devicePin = "Not Attached";
                return false;
            }

            refreshScreen();
            return true;
        }

        /// <summary>
        /// Refresh current device to device list.
        /// </summary>
        public bool refreshDevice()
        {
            if (_devicePin == "Not Attached")
            {
                _errors = "No device attached.\n";
                return false;
            }

            if (_storepass == "")
            {
                if (!resetPassword())
                    return false;
            }

            if (createDebugToken()) uploadDebugToken();

            while (_errors.Contains("invalid password"))
            {
                MessageBox.Show(_errors, "Invalid Password");

                if (!resetPassword())
                    return false;

                if (createDebugToken()) uploadDebugToken();
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
                _devicePin = "Not Attached";
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

            if (!File.Exists(_localRIMFolder  + "DebugToken.bar"))
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
            startInfo.Arguments = string.Format(@"/C blackberry-airpackager.bat -listManifest ""{0}""", _localRIMFolder + "DebugToken.bar");

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
        /// Get the device PIN of the connected device
        /// </summary>
        /// <returns>True if successful</returns>
        private bool getDevicePin()
        {
            bool success = false;

            if (string.IsNullOrEmpty(_deviceIP))
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
            startInfo.Arguments = string.Format("/C blackberry-deploy.bat -listDeviceInfo {0} -password {1}", _deviceIP, _devicePassword);

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

            if (string.IsNullOrEmpty(_deviceIP))
            {
                return success;
            }
            else if (!File.Exists(_localRIMFolder + "DebugToken.bar"))
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
            startInfo.Arguments = string.Format(@"/C blackberry-deploy.bat -installDebugToken ""{0}"" -device {1} -password {2}", _localRIMFolder + "DebugToken.bar", _deviceIP, _devicePassword);

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

            if (string.IsNullOrEmpty(_deviceIP))
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
            startInfo.Arguments = string.Format(@"/C blackberry-deploy.bat -uninstallApp -device {0} -password {1} -package-id {2}", _deviceIP, _devicePassword, _authorID);

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
        private bool createDebugToken()
        {
            bool success = false;

            if (string.IsNullOrEmpty(_certPath))
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
            startInfo.Arguments = string.Format(@"/C blackberry-debugtokenrequest.bat -cskpass {0} -keystore ""{1}"" -storepass {2} -deviceid ""{3}"" ""{4}""", _storepass, _certPath, _storepass, _devicePin, _localRIMFolder + "DebugToken.bar");

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
        private string Encrypt(string plainText)
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
        private string Decrypt(string cipher)
        {
            if (cipher == null) throw new ArgumentNullException("cipher");

            //parse base64 string
            byte[] data = Convert.FromBase64String(cipher);

            //decrypt data
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }

    }
}
