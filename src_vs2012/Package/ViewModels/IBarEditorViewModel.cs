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
using BlackBerry.Package.Model;
using Microsoft.VisualStudio.XmlEditor;
using System.IO;

namespace BlackBerry.Package.ViewModels
{
    /// <summary>
    /// Implementation of the IViewModel interface
    /// </summary>
    public interface IBarEditorViewModel
    {
        /// <summary>
        /// Form Properties
        /// </summary>
        string Name { get; set; }
        string AppName { get; set; }
        string Description { get; set; }
        string Version { get; set; }
        string BuildID { get; set; }
        string Author { get; set; }
        string AuthorID { get; set; }
        List<asset> AssetList { get; }

        XmlModel Model { get; }

        string Chrome { get; set; }
        bool Transparent { get; set; }

        event EventHandler ViewModelChanged;

        void DoIdle();
        void AddIcon(FileInfo icon);
        void DeleteIcon(object iconName);
        void AddSplashScreen(FileInfo iconName);
        void DeleteSplashScreen(object iconName);
        void AddLocalAsset(string newAsset);
        void DeleteLocalAsset(object removeAsset);
        void EditLocalAsset(string identifier, bool? isPublic, string assetType);

        void Close();

        void CheckPermission(string identifier);
        void UnCheckPermission(string identifier);
        void setAuthorInfo();

        void OnSelectChanged(object p);
    }
}
