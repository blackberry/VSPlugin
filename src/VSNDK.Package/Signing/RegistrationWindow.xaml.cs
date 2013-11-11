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
using System.ComponentModel;

namespace RIM.VSNDK_Package.Signing
{
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        private SigningData signingData = null;

        /// <summary>
        /// RegistrationWindow Constructor
        /// </summary>
        public RegistrationWindow()
        {
            InitializeComponent();

            signingData = new SigningData();
            gridMain.DataContext = signingData; 
        }

        /// <summary>
        /// Perform actions on the OK button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!signingData.Register(tbAuthor.Text, tbPassword.Password))
            {
                MessageBox.Show(signingData.Errors, "Registration Window", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                signingData.Errors = null;
                e.Handled = true;
                return;
            }
            else if (!string.IsNullOrEmpty(signingData.Messages))
            {
                signingData.Messages = signingData.Messages.Replace("CSK", "BB ID Token");
                MessageBox.Show(signingData.Messages, "Registration Window", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                signingData.Messages = null;
            }

            signingData.Register("", "");
            
            DialogResult = true;
        }

        /// <summary>
        /// Perform actions on close of dialog.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            signingData.CleanUp();
        }
    }
}