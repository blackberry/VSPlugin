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
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using PkgResources = RIM.VSNDK_Package.Resources;
using RIM.VSNDK_Package.Settings.Models;

namespace RIM.VSNDK_Package.Settings
{

    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsDialog : DialogWindow
    {
        public SettingsDialog()
        {
            InitializeComponent();
            SettingsData data = gridMain.DataContext as SettingsData;
            if (data != null)
           {
             //  this.NDKPath.SelectedValue = "test2"; 
                data.getSimulatorInfo();
                data.getDeviceInfo();
            }
        }

        /// <summary>
        /// Persist Changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SettingsData data = gridMain.DataContext as SettingsData;
            if (data != null)
            {
                data.setDeviceInfo();
                data.setSimulatorInfo();
                data.setNDKPaths(); 
            }


            DialogResult = true; ;

        }
    }
}
