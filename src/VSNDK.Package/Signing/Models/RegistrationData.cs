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
using System.ComponentModel;
using System.Collections;
using Microsoft.Win32;
using PkgResources = RIM.VSNDK_Package.Resources;
using System.Xml;
using System.Security.Cryptography;
using System.IO;

namespace RIM.VSNDK_Package.Signing.Models
{
    /// <summary>
    /// Data Model for the Registration Dialog
    /// </summary>
    class RegistrationData : NotifyPropertyChanged, IDataErrorInfo
    {
        private string _info;
        private string _deregInfo;
        private string _rdkCSJPath;
        private string _pbdbCSJPath;
        private string _csjPassword;
        private string _csjConfirmPassword;
        private string _csjPin;

        private string _errors;
        private string _message;

        private const string _colRDKCSJPath = "RDKCSJPath";
        private const string _colPBDKCSJPath = "PBDKCSJPath";
        private const string _colCSJPin = "CSJPin";
        private const string _colCSJPW = "CSJPassword";
        private const string _colCSJCPW = "CSJConfirmPassword";

        private static string _ndkTargetPath;
        private static string _ndkHostPath;

        /// <summary>
        /// Constructor for the RegistrationData Model
        /// </summary>
        static RegistrationData()
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkNDKPath = null;

            try
            {

                rkNDKPath = rkHKCU.OpenSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                _ndkHostPath = rkNDKPath.GetValue("NDKHostPath").ToString();
                _ndkTargetPath = rkNDKPath.GetValue("NDKTargetPath").ToString();
            }
            catch
            {
                _ndkHostPath = null;
                _ndkTargetPath = null;
            }

            rkNDKPath.Close();
            rkHKCU.Close();
        }

        /// <summary>
        /// Constructor for the RegistrationData Model
        /// </summary>
        public RegistrationData()
        {
            _info = PkgResources.RegistrationInfo;
            _deregInfo = PkgResources.UnRegistrationInfo;
        }

        #region Properties

        /// <summary>
        /// Getter/Setter for the Info property.
        /// </summary>
        public string Info { get { return _info; } }

        /// <summary>
        /// Getter/Setter for the DeRegInfo property.
        /// </summary>
        public string DeRegInfo { get { return _deregInfo; } }

        /// <summary>
        /// Getter/Setter for the RDKCSJPath variable
        /// </summary>
        public string RDKCSJPath
        {
            get { return _rdkCSJPath; }
            set { _rdkCSJPath = value; OnPropertyChanged(_colRDKCSJPath); }
        }

        /// <summary>
        /// Getter/Setter for the PBDKCSJPath property
        /// </summary>
        public string PBDKCSJPath
        {
            get { return _pbdbCSJPath; }
            set { _pbdbCSJPath = value; OnPropertyChanged(_colPBDKCSJPath); }
        }

        /// <summary>
        /// Getter/Setter for the CSJPin property
        /// </summary>
        public string CSJPin
        {
            get { return _csjPin; }
            set { _csjPin = value; OnPropertyChanged(_colCSJPin); }
        }

        /// <summary>
        /// Getter/Setter for the CSJPassword property
        /// </summary>
        public string CSJPassword
        {
            get { return _csjPassword; }
            set { _csjPassword = value; OnPropertyChanged(_colCSJPW); }
        }

        /// <summary>
        /// Getter/Setter for the CSJConfirmPassword property
        /// </summary>
        public string CSJConfirmPassword
        {
            get { return _csjConfirmPassword; }
            set { _csjConfirmPassword = value; OnPropertyChanged(_colCSJCPW); }
        }
        #endregion

        /// <summary>
        /// Run the blackberry-signer tool with parameters passed in
        /// </summary>
        /// <returns></returns>
        public bool Register()
        {
            if (!ValidateInput())
                return false;
            if (string.IsNullOrEmpty(_ndkHostPath))
            {
                _errors = PkgResources.NativSDKNotInstalled;
                return false;
            }
            bool success = false;
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            //run register tool
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format("/C blackberry-signer.bat -register -storepass {0} -csjpin {1} {2} {3}",
                _csjPassword, _csjPin, "\"" + _rdkCSJPath + "\"", "\"" + _pbdbCSJPath + "\"");

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    success = false;
                else
                    success = true;
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            setCSKPassword(CSJPassword);

            return success && string.IsNullOrEmpty(_errors);
        }

        /// <summary>
        /// Run the blackberry-signer tool with parameters passed in
        /// </summary>
        /// <returns></returns>
        public bool UnRegister()
        {
            if (string.IsNullOrEmpty(_ndkHostPath))
            {
                _errors = PkgResources.NativSDKNotInstalled;
                return false;
            }
            bool success = false;
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_ErrorDataReceived);
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);

            //run register tool
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format("/C blackberry-signer.bat -cskdelete");

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    success = false;
                p.Close();

                FileInfo fi = new FileInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +  @"\Research In Motion\author.p12");

                try
                {
                    fi.Delete();
                }
                catch (System.IO.IOException e)
                {
                    success = false;
                }
                success = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
                success = false;
            }

            setCSKPassword("");

            return success && string.IsNullOrEmpty(_errors);
        }

        /// <summary>
        /// Reset password to user input
        /// </summary>
        /// <returns></returns>
        public void resetPassword()
        {
            setCSKPassword(CSJPassword);
            Message = "Password successfully reset.";
        }

        /// <summary>
        /// Set CSK Password into the registry.
        /// </summary>
        private void setCSKPassword(string password)
        {
            RegistryKey rkHKCU = Registry.CurrentUser;
            RegistryKey rkCDKPass = null;

            try
            {
                rkCDKPass = rkHKCU.CreateSubKey("Software\\BlackBerry\\BlackBerryVSPlugin");
                rkCDKPass.SetValue("CSKPass", Encrypt(password));
            }
            catch
            {

            }
            rkCDKPass.Close();
            rkHKCU.Close();
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
        /// Validate the input from the dialog
        /// </summary>
        /// <returns></returns>
        private bool ValidateInput()
        {
            string err = this[_colRDKCSJPath];
            if ( !string.IsNullOrEmpty(err))
                _errors += err + "\n";
            err = this[_colPBDKCSJPath];
            if ( !string.IsNullOrEmpty(err))
                _errors += err + "\n";
            err = this[_colCSJPin];
            if (!string.IsNullOrEmpty(err))
                _errors += err + "\n";
            err = this[_colCSJPW];
            if (!string.IsNullOrEmpty(err))
               _errors += err + "\n";
            err = this[_colCSJCPW];
            if (!string.IsNullOrEmpty(err))
               _errors += err + "\n";
            return string.IsNullOrEmpty(_errors);
        }

        /// <summary>
        /// Event Handler for output received from the Register Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void p_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
                if (e.Data.Contains("Error:"))
                    _errors += e.Data + "\n";
                else
                    _message += e.Data + "\n";
            }
        }

        /// <summary>
        /// Event Handler for the error data received from the Registger Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void p_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                System.Diagnostics.Debug.WriteLine(e.Data);
                _errors += e.Data + "\n";
            }
        }

        /// <summary>
        /// Getter/Setter for the Message property
        /// </summary>
        public string Message { get { return _message; } set { _message = value; } }

        /// <summary>
        /// Getter/Setter for the Error property
        /// </summary>
        public string Error
        {
            get { return _errors; }
            set { _errors = value; }
        }

        /// <summary>
        /// Validation error reported in UI
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public string this[string columnName]
        {
            get 
            {
                string error = string.Empty;
                switch ( columnName )
                {
                    case _colRDKCSJPath:
                        if (string.IsNullOrEmpty(this._rdkCSJPath))
                            error = PkgResources.RDKCSJFileMissing;
                        break;
                    case _colCSJPin:
                        if ( string.IsNullOrEmpty(_csjPin) )
                            error = PkgResources.CSJPinMissing;
                        break;
                    case _colCSJPW:
                        if (string.IsNullOrEmpty(_csjPassword) || _csjPassword.Length < 6)
                            error = PkgResources.CSJPasswordMissing;
                        break;
                    case _colCSJCPW:
                        if (string.IsNullOrEmpty(_csjConfirmPassword))
                            error = PkgResources.CSJConfirmPasswordMissing;
                        else if (_csjPassword != _csjConfirmPassword)
                            error = PkgResources.PasswordNotmatch;
                        break;
                    default:
                        break;
                }
                return error;
            }
        }
    }
}
