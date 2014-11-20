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
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Diagnostics;

namespace BlackBerry.Package.Import.Model
{
    /// <summary>
    /// DataModel Class for the Import Dialog
    /// </summary>
    class ImportModel : INotifyPropertyChanged
    {
        #region Member Variables and Constants
        private const string _colSumaryString = "SummaryString";
        private const string BLACKBERRY = "BlackBerry";
        
        private IList<String> _summaryList;
        private CollectionView _summaryString;
        #endregion

        /// <summary>
        /// ImportModel Constructor
        /// </summary>
        public ImportModel()
        {
            _summaryList = new List<String>();
            _summaryList.Add("Conversion Summary");
        }


        #region Properties

        /// <summary>
        /// Getter/Setter for the SummaryString Property
        /// </summary>
        public CollectionView SummaryString
        {
            get { return _summaryString; }
            set
            {
                _summaryString = value;
                OnPropertyChanged(_colSumaryString);
            }
        }

        #endregion

        /// <summary>
        /// Helper function to add a summary string to the list
        /// </summary>
        /// <param name="entry"></param>
        public void AddSummaryString(string entry)
        {
            _summaryList.Add(entry);
            SummaryString = new CollectionView(_summaryList);
        }

        /// <summary>
        /// Recursive Function to walk a specified directory tree and add the files into a Visual Studio C project.
        /// </summary>
        /// <param name="proj">Destination Project</param>
        /// <param name="sourceDir">Source Directory to begin walking</param>
        /// <param name="destinationDir">Destination Directory of new project to copy files to</param>
        /// <param name="filter">VCFilter object to add files to</param>
        /// <returns></returns>
        public bool WalkDirectoryTree(Project proj, DirectoryInfo sourceDir, DirectoryInfo destinationDir, VCFilter filter)
        {
            VCFilter localFilter = filter;
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder 
            try
            {
                files = sourceDir.GetFiles("*.*");

                foreach (FileInfo file in files)
                {
                    if (file.Name.Contains("vcxproj"))
                    {
                        AddSummaryString("Selected project already converted.");
                        return false;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            if (files != null)
            {
                // Now find all the subdirectories under this directory.
                subDirs = sourceDir.GetDirectories();

                if (localFilter == null)
                {
                    foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                    {
                        if (dirInfo.Name == "src")
                        {
                            IVCCollection tmpCollection;
                            VCFilter tmpFilter;
                            tmpCollection = (proj.Object as VCProject).Filters;
                            tmpFilter = tmpCollection.Item("Source Files");
                            (proj.Object as VCProject).RemoveFilter(tmpFilter);
                        }
                        if (dirInfo.Name == "res")
                        {
                            IVCCollection tmpCollection;
                            VCFilter tmpFilter;
                            tmpCollection = (proj.Object as VCProject).Filters;
                            tmpFilter = tmpCollection.Item("Resource Files");
                            (proj.Object as VCProject).RemoveFilter(tmpFilter);
                        }
                    }
                }
               
                foreach (System.IO.FileInfo fi in files)
                {
                    AddFileToProject(proj, fi.FullName, destinationDir.FullName, localFilter);
                }

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    VCFilter newFilter = AddFolderToProject(proj, destinationDir.FullName, dirInfo.Name, localFilter);
                    // Resursive call for each subdirectory.
                    DirectoryInfo source = new DirectoryInfo(dirInfo.FullName);
                    DirectoryInfo destination = new DirectoryInfo(Path.Combine(destinationDir.FullName, dirInfo.Name));
                    if (!WalkDirectoryTree(proj, source, destination, newFilter))
                        return false;
                }

            }
            return true;
        }

        /// <summary>
        /// Add File to specified project
        /// </summary>
        /// <param name="path"></param>
        private void AddFileToProject(Project proj, string source, string destination, VCFilter filter)
        {
            VCFilter localFilter = filter;

            try
            {
                FileInfo fileInfo1 = new FileInfo(source);

                if (!File.Exists(System.IO.Path.Combine(destination, fileInfo1.Name)))
                {
                    File.Copy(source, System.IO.Path.Combine(destination, fileInfo1.Name));
                }

                if ((fileInfo1.Name != ".cproject") && (fileInfo1.Name != ".project"))
                {
                    if (localFilter == null)
                    {
                        if (proj.ProjectItems.Item(fileInfo1.Name) == null)
                        {
                            proj.ProjectItems.AddFromFileCopy(System.IO.Path.Combine(destination, fileInfo1.Name));
                            AddSummaryString("Project File Added: " + System.IO.Path.Combine(destination, fileInfo1.Name));
                        }
                        else
                        {
                            AddSummaryString("Duplicate File: " + System.IO.Path.Combine(destination, fileInfo1.Name));
                        }
                    }
                    else
                    {
                        IVCCollection tmpCollection;
                        tmpCollection = localFilter.Files;

                        if (tmpCollection.Item(fileInfo1.Name) == null)
                        {
                            localFilter.AddFile(System.IO.Path.Combine(destination, fileInfo1.Name));
                            AddSummaryString("Project File Added: " + System.IO.Path.Combine(destination, fileInfo1.Name));
                        }
                        else
                        {
                            AddSummaryString("Duplicate File: " + Path.Combine(destination, fileInfo1.Name));
                        }

                    }
                    
                }
                else
                {
                    AddSummaryString("File Skipped: " + Path.Combine(destination, fileInfo1.Name));
                }

            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Add Folder to Specified Project
        /// </summary>
        private VCFilter AddFolderToProject(Project proj, string dirInfo, string newDir, VCFilter filter)
        {
            VCFilter localFilter = filter;

            try
            {
                DirectoryInfo directoryInfo1 = new DirectoryInfo(dirInfo);
                directoryInfo1.CreateSubdirectory(newDir);
                if (localFilter == null)
                {
                    localFilter = (proj.Object as VCProject).AddFilter(newDir);
                }
                else
                {
                    localFilter = localFilter.AddFilter(newDir);
                }

                AddSummaryString("Folder Added: " + newDir); 
            }
            catch (Exception e)
            {
                string error = e.Message;
            }

            return localFilter;
        }

        /// <summary>
        /// add blackberry configurations
        /// </summary>
        public void AddBlackBerryConfigurations(Project project)
        {
            try
            {
                ConfigurationManager mgr = project.ConfigurationManager;
                Configurations cfgs = mgr.AddPlatform(BLACKBERRY, "Win32", true);
                AddSummaryString("Added BlackBerry Configuration");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire the PropertyChnaged event handler on change of property
        /// </summary>
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion

    }
}
