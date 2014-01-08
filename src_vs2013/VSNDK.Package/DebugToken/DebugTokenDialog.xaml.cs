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
using Microsoft.VisualStudio.PlatformUI;
using RIM.VSNDK_Package.Signing.Models;
using RIM.VSNDK_Package.DebugToken.Model;
using PkgResources = RIM.VSNDK_Package.Resources;
using RIM.VSNDK_Package.Signing;

namespace RIM.VSNDK_Package.DebugToken
{
    /// <summary>
    /// Interaction logic for DebugTokenDialog.xaml
    /// </summary>
    public partial class DebugTokenDialog : Window
    {
        #region Member Variables and Constants
        private DebugTokenData deployTokenData;
        private bool isRegistering = false;
        public bool IsClosing = false;
        #endregion

        /// <summary>
        /// DebugTokenDialog Constructor
        /// </summary>
        public DebugTokenDialog()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            deployTokenData = new DebugTokenData();

            InitializeComponent();

            gridMain.DataContext = deployTokenData;

            if (deployTokenData.Error != "")
            {
                MessageBox.Show(deployTokenData.Error, PkgResources.Errors);
                deployTokenData.Error = "";

                IsClosing = true;

                Close();

            }

            btnAdd.IsEnabled = !deployTokenData.AlreadyRegistered;
            btnRefresh.IsEnabled = deployTokenData.AlreadyRegistered;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        /// <summary>
        /// Add debug token to device and registration file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (!isRegistering)
                {
                    isRegistering = true;

                    if (!(deployTokenData.addDevice(this)))
                    {
                        deployTokenData.Error = "";
                        e.Handled = true;
                        btnAdd.IsEnabled = false;
                        btnRefresh.IsEnabled = false;
                    }
                    else
                    {
                        btnAdd.IsEnabled = false;
                        btnRefresh.IsEnabled = true;
                    }
                }
            }
            finally
            {
                isRegistering = false;
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }


        /// <summary>
        /// Refresh debug token to device and registration file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (!isRegistering)
            {
                isRegistering = true;

                if (!(deployTokenData.refreshDevice(this)))
                {
                    deployTokenData.Error = "";
                    e.Handled = true;
                    btnAdd.IsEnabled = false;
                    btnRefresh.IsEnabled = false;
                }
                else
                {
                    btnAdd.IsEnabled = false;
                    btnRefresh.IsEnabled = true;
                }
            }

            isRegistering = false;
            Mouse.OverrideCursor = Cursors.Arrow;

        }

    }
}
