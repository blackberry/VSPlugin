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
using RIM.VSNDK_Package.Import.Model;
using EnvDTE;

namespace RIM.VSNDK_Package.Import
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Import : DialogWindow
    {

        private Project _project;
        private string _sourceDir;
        private string _destDir;
        private ImportModel _data;

        /// <summary>
        /// Import Constructor
        /// </summary>
        public Import()
        {
            InitializeComponent();

            _data = new ImportModel();
            gridMain.DataContext = _data;
        }

        /// <summary>
        /// Import Constructor
        /// </summary>
        /// <param name="project">Reference to project.</param>
        /// <param name="sourceDir">Source Directory to import from.</param>
        /// <param name="destDir">Destination Directory to import to.</param>
        public Import(Project project, string sourceDir, string destDir)
        {
            InitializeComponent();

            _data = new ImportModel();
            gridMain.DataContext = _data;

            _project = project;
            _sourceDir = sourceDir;
            _destDir = destDir;
        }   

        /// <summary>
        /// Display the dialog in Model form and start import process.
        /// </summary>
        public void ShowModel2()
        {
            this.Show();

            FileInfo projectDir = new FileInfo(_destDir);
            DirectoryInfo source = new DirectoryInfo(_sourceDir);
            DirectoryInfo destination = new DirectoryInfo(projectDir.DirectoryName);

            ImportModel data = gridMain.DataContext as ImportModel;
            if (data != null)
            {
                data.AddSummaryString("Starting Conversion Process");
                if (data.WalkDirectoryTree(_project, source, destination, null))
                {
                    data.AddBlackBerryConfigurations(_project);
                }
                data.AddSummaryString("Conversion Process Completed");
            }
        }

        /// <summary>
        /// Close the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
