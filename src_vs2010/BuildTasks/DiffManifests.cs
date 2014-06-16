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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlackBerry.BuildTasks
{
    /// <summary>
    /// MSBuild task to compare the manifest file from the bar file being uploaded against the one already installed on the device.
    /// </summary>
    public class DiffManifests : Task
    {
        #region Member Variables and Constants
        private string _localManifestFile;
        private string _targetManifestFile;
        private string _targetFileMap;
        private ArrayList _modifiedFiles;
        private ArrayList _deletedFiles;
        private int _modifiedFilesCount;
        private int _deletedFilesCount;
        #endregion

        /// <summary>
        /// Execute the MSBuild Task.
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            _modifiedFiles = new ArrayList();
            _deletedFiles = new ArrayList();

            // Parse local manifest to retrieve list of files and their hashes.
            string[] localManifest = File.ReadAllLines(_localManifestFile);
            Dictionary<string, string> localFiles = new Dictionary<string,string>();
            for (int i = 0; i < localManifest.Length; i++)
            {
                if (localManifest[i].StartsWith("Archive-Asset-Name: "))
                {
                    string assetName = localManifest[i].Substring(20);
                    i++;
                    if (localManifest[i].StartsWith("Archive-Asset-SHA-512-Digest: "))
                    {
                        string assetHash = localManifest[i].Substring(30);
                        localFiles.Add(assetName, assetHash);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Do the same for the target manifest.
            string[] targetManifest = File.ReadAllLines(_targetManifestFile);
            Dictionary<string, string> targetFiles = new Dictionary<string,string>();
            for (int i = 0; i < targetManifest.Length; i++)
            {
                if (targetManifest[i].StartsWith("Archive-Asset-Name: "))
                {
                    string assetName = targetManifest[i].Substring(20);
                    i++;
                    if (targetManifest[i].StartsWith("Archive-Asset-SHA-512-Digest: "))
                    {
                        string assetHash = targetManifest[i].Substring(30);
                        targetFiles.Add(assetName, assetHash);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // Compare hashes and populate the lists of modified and deleted files.
            string[] targetFileMap = File.ReadAllLines(_targetFileMap);
            foreach (KeyValuePair<string, string> file in localFiles)
            {
                // For some reason this MANIFEST.bbr file appears in the manifest, even though
                // it doesn't actually exist anywhere...?  It doesn't seem to correspond to
                // MANIFEST.MF either.
                if (file.Key == "META-INF/MANIFEST.bbr")
                {
                    continue;
                }

                // If the target manifest doesn't contain the same key/value pair,
                // that means the local file has been either added or modified.
                if (!targetFiles.Contains(file))
                {
                    TaskItem item = new TaskItem(file.Key);
                    item.SetMetadata("SourcePath", getSourcePath(file.Key, targetFileMap));
                    _modifiedFiles.Add(item);
                }
            }

            IDictionaryEnumerator targetEnum = targetFiles.GetEnumerator();
            while (targetEnum.MoveNext())
            {
                // If the local manifest doesn't contain the same key,
                // that means the target file has been deleted from the project.
                if (!localFiles.ContainsKey((string)targetEnum.Key))
                {
                    TaskItem item = new TaskItem((string)targetEnum.Key);
                    _deletedFiles.Add(item);
                }
            }

            // For some reason the manifest file doesn't show up in the target file map
            // or the manifest itself, so we add it manually here and always upload it.
            TaskItem manifestItem = new TaskItem("META-INF/MANIFEST.MF");
            manifestItem.SetMetadata("SourcePath", "localManifest.mf");
            _modifiedFiles.Add(manifestItem);

            return true;
        }

        /// <summary>
        /// Helper function to parse the target file map to add the source path of each modified file as metadata.
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="targetFileMap">Target file map</param>
        /// <returns></returns>
        private string getSourcePath(string sourcePath, string[] targetFileMap)
        {
            foreach (string line in targetFileMap)
            {
                if (line.Contains(sourcePath))
                {
                    int startIndex = line.IndexOf('=');
                    return line.Substring(startIndex + 1);
                }
            }

            return "";
        }

        /// <summary>
        /// Getter for the ModifiedFiles property.
        /// </summary>
        [Output]
        public ITaskItem[] ModifiedFiles
        {
            get
            {
                ITaskItem[] items = (ITaskItem[])_modifiedFiles.ToArray(typeof(ITaskItem));
                _modifiedFilesCount = items.Length;
                return items;
            }
        }

        /// <summary>
        /// Getter for the ModifiedFilesCount property
        /// </summary>
        [Output]
        public int ModifiedFilesCount
        {
            get { return _modifiedFilesCount; }
            set { _modifiedFilesCount = value; }
        }

        /// <summary>
        /// Getter for the DeletedFiles property
        /// </summary>
        [Output]
        public ITaskItem[] DeletedFiles
        {
            get
            {
                ITaskItem[] items = (ITaskItem[])_deletedFiles.ToArray(typeof(ITaskItem));
                _deletedFilesCount = items.Length;
                return items;
            }
        }

        /// <summary>
        /// Getter/Setter for the DeletedFilesCount property
        /// </summary>
        [Output]
        public int DeletedFilesCount
        {
            get { return _deletedFilesCount; }
            set { _deletedFilesCount = value; }
        }

        /// <summary>
        /// Setter for the LocalManifestFile property
        /// </summary>
        [Required]
        public string LocalManifestFile
        {
            set
            {
                _localManifestFile = value;
            }
        }

        /// <summary>
        /// Setter for the TargetManifestFile property
        /// </summary>
        [Required]
        public string TargetManifestFile
        {
            set
            {
                _targetManifestFile = value;
            }
        }

        /// <summary>
        /// Setter for the targetFileMap property
        /// </summary>
        [Required]
        public string targetFileMap
        {
            set
            {
                _targetFileMap = value;
            }
        }
    }
}
