﻿//* Copyright 2010-2011 Research In Motion Limited.
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
using BlackBerry.Package.Helpers;
using Microsoft.VisualStudio.Shell;
using System.IO;
using Microsoft.VisualStudio;

namespace BlackBerry.Package.Registration
{
    /// <summary>
    /// Register our bardescriptor.xml custom editor
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class XmlEditorDesignerViewRegistrationAttribute : RegistrationAttribute
    {
        /// Declare Constants
        const string XmlChooserFactory = "XmlChooserFactory";
        const string XmlChooserEditorExtensionsKeyPath = @"Editors\{32CC8DFA-2D70-49b2-94CD-22D57349B778}\Extensions";
        const string XmlEditorFactoryGuid = "{FA3CD31E-987B-443A-9B81-186104E8DAC1}";

        /// Declare Private Member Variables
        private readonly string _keyName;
        private readonly string _defaultExtension;
        private readonly Guid _defaultLogicalView;
        private readonly int _xmlChooserPriority;

        /// <summary>
        /// Init constructor
        /// </summary>
        /// <param name="keyName">Registry key name</param>
        /// <param name="defaultExtension">Default extension for editor</param>
        /// <param name="defaultLogicalViewEditorFactory">Default Editor Factory</param>
        /// <param name="xmlChooserPriority">XML Priority</param>
        public XmlEditorDesignerViewRegistrationAttribute(string keyName, string defaultExtension, object defaultLogicalViewEditorFactory, int xmlChooserPriority)
        {
            // Validate parameter input 
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Editor description cannot be null or empty.", "editorDescription");
            if (string.IsNullOrWhiteSpace(defaultExtension))
                throw new ArgumentException("Extension cannot be null or empty.", "extension");
            if (defaultLogicalViewEditorFactory == null)
                throw new ArgumentNullException("defaultLogicalViewEditorFactory");

            // Set Member Variables 
            _keyName = keyName;
            _defaultExtension = defaultExtension.StartsWith(".") ? defaultExtension.Substring(1) : defaultExtension;
            _defaultLogicalView = AttributeHelper.GetGuidFrom(defaultLogicalViewEditorFactory);
            _xmlChooserPriority = xmlChooserPriority;

            CodeLogicalViewEditor = XmlEditorFactoryGuid;
            DebuggingLogicalViewEditor = XmlEditorFactoryGuid;
            DesignerLogicalViewEditor = XmlEditorFactoryGuid;
            TextLogicalViewEditor = XmlEditorFactoryGuid;
        }

        /// <summary>
        /// Register the custom editor
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            // Validate parameter input
            if (context == null)
                throw new ArgumentNullException("context");

            // Set extension key
            Key extensionKey = context.CreateKey(XmlChooserEditorExtensionsKeyPath);
            extensionKey.SetValue(_defaultExtension, _xmlChooserPriority);
            extensionKey.Close();

            // Set editor key
            Key editorKey = context.CreateKey(Path.Combine(XmlChooserFactory, _keyName));
            editorKey.SetValue("DefaultLogicalView", AttributeHelper.Format(_defaultLogicalView));
            editorKey.SetValue("Extension", _defaultExtension);
            if (!string.IsNullOrWhiteSpace(Namespace))
            {
                editorKey.SetValue("Namespace", Namespace);
            }
            if (MatchExtensionAndNamespace)
            {
                editorKey.SetValue("Match", "both");
            }
            if (IsDataSet.HasValue)
            {
                editorKey.SetValue("IsDataSet", Convert.ToInt32(IsDataSet.Value));
            }

            // Set DebuggingLogicalViewEditor Mapping
            if (DebuggingLogicalViewEditor != null)
            {
                editorKey.SetValue(AttributeHelper.Format(VSConstants.LOGVIEWID_Debugging), AttributeHelper.Format(DebuggingLogicalViewEditor));
            }

            // Set CodeLogicalViewEditor Mapping
            if (CodeLogicalViewEditor != null)
            {
                editorKey.SetValue(AttributeHelper.Format(VSConstants.LOGVIEWID_Code), AttributeHelper.Format(CodeLogicalViewEditor));
            }

            // Set DesignerLogicalViewEditor Mapping
            if (DesignerLogicalViewEditor != null)
            {
                editorKey.SetValue(AttributeHelper.Format(VSConstants.LOGVIEWID_Designer), AttributeHelper.Format(DesignerLogicalViewEditor));
            }

            // Set TextLogicalViewEditor Mapping
            if (TextLogicalViewEditor != null)
            {
                editorKey.SetValue(AttributeHelper.Format(VSConstants.LOGVIEWID_TextView), AttributeHelper.Format(TextLogicalViewEditor));
            }
            editorKey.Close();
        }

        /// <summary>
        /// Unregister the custom editor
        /// </summary>
        public override void Unregister(RegistrationContext context)
        {
            // Validate parameter input
            if (context == null)
                throw new ArgumentNullException("context");

            // Remove Key
            context.RemoveKey(Path.Combine(XmlChooserFactory, _keyName));
            context.RemoveValue(XmlChooserEditorExtensionsKeyPath, _defaultExtension);
            context.RemoveKeyIfEmpty(XmlChooserEditorExtensionsKeyPath);
        }

        /// <summary>
        /// The editor factor for the Code View Editor
        /// </summary>
        public object CodeLogicalViewEditor { get; set; }

        /// <summary>
        ///  The editor factory for the Debugging View Editor
        /// </summary>
        public object DebuggingLogicalViewEditor { get; set; }

        /// <summary>
        /// The editor factor for the Designer View Editor
        /// </summary>
        public object DesignerLogicalViewEditor { get; set; }

        /// <summary>
        /// The edtior factgory for the Text View Editor
        /// </summary>
        public object TextLogicalViewEditor { get; set; }

        /// <summary>
        /// Namespace property
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Property
        /// </summary>
        public bool MatchExtensionAndNamespace { get; set; }

        /// <summary>
        /// Special value used only by the DataSet designer.
        /// </summary>
        public bool? IsDataSet { get; set; }
    }
}
