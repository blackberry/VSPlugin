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
    public partial class SimulatorManager : Window
    {
        private UpdateManagerData umData;

        /// <summary>
        /// Constructor
        /// </summary>
        public SimulatorManager()
        {
            InitializeComponent();

            umData = new UpdateManagerData();
            gridMain.DataContext = umData;
            this.Close.IsEnabled = true;
        }

        /// <summary>
        /// Close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = umData.installed;
        }

        /// <summary>
        /// Install button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Install_Click(object sender, RoutedEventArgs e)
        {
            if (umData.IsInstalling)
            {
                MessageBox.Show("Visual Studio is currently already installing/uninstalling a Simulator. Please wait until completion before proceeding.", "Simulators", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                umData.InstallAPI(((SimulatorsClass)((StackPanel)((Button)sender).Parent).DataContext).TargetVersion, false, true);
            }
        }

        /// <summary>
        /// Uninstall button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            if (umData.IsInstalling)
            {
                MessageBox.Show("Visual Studio is currently already installing/uninstalling a Simulator. Please wait until completion before proceeding.", "Simulators", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
            else
            {
                umData.UninstallAPI(((SimulatorsClass)((StackPanel)((Button)sender).Parent).DataContext).TargetVersion, true);
            }
        }

        /// <summary>
        /// Prevent the user from closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((!umData.installed) && (umData.IsInstalling))
            {
                if (umData.isConfiguring)
                {
                    umData.waitTerminateInstallation();
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
                        if (umData.isConfiguring)
                        {
                            umData.waitTerminateInstallation();
                        }
                        else
                        {
                            umData.cancelInstallation();
                            umData.installed = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Expansion Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grpAPILevel120_Expanded(object sender, RoutedEventArgs e)
        {
            // Init variables
            Expander ex = ((Expander)sender);
            string APIText = ((TextBlock)((StackPanel)ex.Header).Children[0]).Text;

            // Filter sub list and set items source
            umData.FilterSubList(APIText.Substring(APIText.LastIndexOf(' ') + 1));
            ((ItemsControl)ex.Content).ItemsSource = umData.Simulators2;
        }
    }
}
