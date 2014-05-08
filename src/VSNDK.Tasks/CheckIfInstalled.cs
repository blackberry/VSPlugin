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
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VSNDK.Tasks
{
    /// <summary>
    /// MSBuild Task to check to see if the application was previously installed.
    /// </summary>
    public class CheckIfInstalled : Task
    {
        #region Member Variables and Constants
        private string _listFile;
        private string _appName;
        private bool _isAppInstalled;
        #endregion

        /// <summary>
        /// Execute the MSBuild task
        /// </summary>
        /// <returns>True on successful execution</returns>
        public override bool Execute()
        {

            try
            {
                _isAppInstalled = false;

                string[] installedApps = File.ReadAllLines(_listFile);
                foreach (string app in installedApps)
                {
                    if (app.Contains(_appName))
                    {
                        _isAppInstalled = true;
                        break;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }


        }

        /// <summary>
        /// Setter for the ListFile property
        /// </summary>
        [Required]
        public string ListFile
        {
            set
            {
                _listFile = value;
            }
        }

        /// <summary>
        /// Setter for the AppName property
        /// </summary>
        [Required]
        public string AppName
        {
            set
            {
                _appName = value;
            }
        }

        /// <summary>
        /// Getter for the IsAppInstalled property
        /// </summary>
        [Output]
        public bool IsAppInstalled
        {
            get
            {
                return _isAppInstalled;
            }
        }
    }
}
