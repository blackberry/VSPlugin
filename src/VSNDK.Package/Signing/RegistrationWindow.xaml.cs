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
using PkgResources = RIM.VSNDK_Package.Resources;
using System.IO;

namespace RIM.VSNDK_Package
{
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        /// <summary>
        /// RegistrationWindow Constructor
        /// </summary>
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Register Signing Keys on click of OK Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool registered = false;
            RegistrationData data = gridMain.DataContext as RegistrationData;
            if (data != null)
            {
                data.Author = this.tbAuthor.Text;
                data.CSJPassword = this.tbCSKPassword.Password;
                registered = data.Register();
                if (!registered)
                {
                    MessageBox.Show(data.Error, PkgResources.Errors);
                    data.Error = null;
                    e.Handled = true;
                    string certPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\author.p12";
                    if (File.Exists(certPath))
                        File.Delete(certPath);
                    return;
                }
                else if (!string.IsNullOrEmpty(data.Message))
                {
                    MessageBox.Show(data.Message, PkgResources.Info);
                    data.Message = null;
                }
            }
            DialogResult = registered;
        }
    }
}
