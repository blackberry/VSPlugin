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
using Microsoft.VisualStudio.Shell;

namespace RIM.VSNDK_Package.UpdateManager
{
    /// <summary>
    /// Interaction logic for UpdateManager.xaml
    /// </summary>
    public partial class UpdateManager : Window
    {
        private UpdateManagerData data = null;

        /// <summary>
        /// Constructor
        /// </summary>
        private UpdateManager()
        {

            InitializeComponent();

            data = new UpdateManagerData();

            gridMain.DataContext = data;
            this.Close.IsEnabled = true;
            this.Simulators.IsEnabled = true;
        }

        public static UpdateManager create()
        {
            if (!GlobalFunctions.isOnline())
            {
                System.Windows.MessageBox.Show("You are currently experiencing internet connection issues and cannot access the Update Manager server.  Please check your connection or try again later.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                return null;
            }
            else
            {
                return new UpdateManager();
            }
        }


        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = data.installed;
        }

        /// <summary>
        /// Install button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;

            if (data != null)
            {
                if (data.IsInstalling)
                {
                    MessageBox.Show("Visual Studio is currently already installing/uninstalling an API Level. Please wait until completion before proceeding.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                else
                {
                    this.Simulators.IsEnabled = false;
                    data.InstallAPI(((APITargetClass)((StackPanel)((Button)sender).Parent).DataContext).TargetVersion, false, false);
                }
            }
        }

        /// <summary>
        /// Uninstall button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;
            if (data != null)
            {
                if (data.IsInstalling)
                {
                    MessageBox.Show("Visual Studio is currently already installing/uninstalling an API Level. Please wait until completion before proceeding.", "Update Manager", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                else
                {

                    List<APITargetClass> apiList = APITargetListSingleton.Instance._tempAPITargetList.FindAll(i => i.IsInstalled > 0);
                    if (apiList.Count <= 1)
                    {
                        MessageBox.Show("The BlackBerry Plug-in for Microsoft Visual Studio requires at least one API Target to function correctly.  Removal of the last API Target is prohibited.", "Update Manager", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                    }
                    else
                    {
                        this.Simulators.IsEnabled = false;
                        data.UninstallAPI(((APITargetClass)((StackPanel)((Button)sender).Parent).DataContext).LatestVersion, false);
                    }
                }
            }
        }

        /// <summary>
        /// Update 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateManagerData data = gridMain.DataContext as UpdateManagerData;
            if (data != null)
            {
                if (data.IsInstalling)
                {
                    MessageBox.Show("Visual Studio is currently already installing/uninstalling an API Level. Please wait until completion before proceeding.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
                else
                {
                    this.Simulators.IsEnabled = false;
                    data.UpdateAPI(((APITargetClass)((StackPanel)((Button)sender).Parent).DataContext).TargetVersion, ((APITargetClass)((StackPanel)((Button)sender).Parent).DataContext).LatestVersion);
                }

            }
        }

        /// <summary>
        /// Prevent the user from closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((!data.installed) && (data.IsInstalling))
            {
                if (data.isConfiguring)
                {
                    data.waitTerminateInstallation();
                }
                else
                {
                    var result = MessageBox.Show("Are you sure that you want to cancel the installation?", "Cancel installation?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        if (data.isConfiguring)
                        {
                            data.waitTerminateInstallation();
                        }
                        else
                        {
                            data.cancelInstallation();
                            data.installed = false;
                            data.Error = "Download cancelled by the user. You must be able to debug only after completing the download.";
                        }
                    }
                }
            }
        }

        private void Simulators_Click(object sender, RoutedEventArgs e)
        {
            SimulatorManager sm = new SimulatorManager();
            sm.ShowDialog();
        }
    }
}
