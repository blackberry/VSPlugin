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
using RIM.VSNDK_Package.UpdateManager.Model;
using Microsoft.VisualStudio.PlatformUI;
using System.Net.Sockets;
using Microsoft.VisualStudio.Shell;

namespace RIM.VSNDK_Package.UpdateManager
{
    /// <summary>
    /// Interaction logic for UpdateManager.xaml
    /// </summary>
    public partial class UpdateManagerDialog : Window
    {
        private string _version = "";
        private bool _isRuntime = false;
        private bool _isSimulator = false;
        private bool _installed = false;
        private UpdateManagerData data = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManagerDialog()
        {
            if (!GlobalFunctions.isOnline())
            {
                System.Windows.MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                this.Close();
            }

            InitializeComponent();

            data = new UpdateManagerData();
            gridMain.DataContext = data;  
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateManagerDialog(string message, string version, bool isRuntime, bool isSimulator)
        {
            if (!GlobalFunctions.isOnline())
            {
                System.Windows.MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                this.Close();
            }

            InitializeComponent();

            data = new UpdateManagerData();
            gridMain.DataContext = data;  

            _version = version;
            _isRuntime = isRuntime;
            _isSimulator = isSimulator;

            lblMessage.Text = message;
        }
         
        /// <summary>
        /// No button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _installed;
        }

        /// <summary>
        /// No button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;

            if (!data.IsInstalling)
            {
                MessageBox.Show("Visual Studio is currently already installing an API Level. Please wait until completion before proceeding.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                data.InstallAPI(_version, _isRuntime, _isSimulator);
            }

            _installed = true;
        }

        /// <summary>
        /// Prevent the user from closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!data.IsInstalling)
            {
                e.Cancel = true;
            }
        }
    }
}
