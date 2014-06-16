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
using Microsoft.VisualStudio.Shell;
using System.IO;
using Microsoft.VisualStudio;

namespace BlackBerry.Package
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
        private string keyName;
        private string defaultExtension;
        private Guid defaultLogicalView;
        private int xmlChooserPriority;

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="keyName">Registry key name</param>
        /// <param name="defaultExtension">Default extension for editor</param>
        /// <param name="defaultLogicalViewEditorFactory">Default Editor Factory</param>
        /// <param name="xmlChooserPriority">XML Priority</param>
        public XmlEditorDesignerViewRegistrationAttribute(string keyName, string defaultExtension, object defaultLogicalViewEditorFactory, int xmlChooserPriority)
        {
            /// Validate parameter input 
            if (string.IsNullOrWhiteSpace(keyName))
            {
                throw new ArgumentException("Editor description cannot be null or empty.", "editorDescription");
            }
            if (string.IsNullOrWhiteSpace(defaultExtension))
            {
                throw new ArgumentException("Extension cannot be null or empty.", "extension");
            }
            if (defaultLogicalViewEditorFactory == null)
            {
                throw new ArgumentNullException("defaultLogicalViewEditorFactory");
            }

            /// Set Member Variables 
            this.keyName = keyName;
            this.defaultExtension = defaultExtension;
            this.defaultLogicalView = TryGetGuidFromObject(defaultLogicalViewEditorFactory);
            this.xmlChooserPriority = xmlChooserPriority;

            this.CodeLogicalViewEditor = XmlEditorFactoryGuid;
            this.DebuggingLogicalViewEditor = XmlEditorFactoryGuid;
            this.DesignerLogicalViewEditor = XmlEditorFactoryGuid;
            this.TextLogicalViewEditor = XmlEditorFactoryGuid;            
        }

        /// <summary>
        /// Register the custom editor
        /// </summary>
        /// <param name="context"></param>
        public override void Register(RegistrationAttribute.RegistrationContext context)
        {
            /// Validate parameter input
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            /// Set extension key
            Key extensionKey = context.CreateKey(XmlChooserEditorExtensionsKeyPath);
            extensionKey.SetValue(defaultExtension, xmlChooserPriority);
            extensionKey.Close();

            /// set editor key
            Key editorKey = context.CreateKey(Path.Combine(XmlChooserFactory, keyName));
            editorKey.SetValue("DefaultLogicalView", defaultLogicalView.ToString("B").ToUpperInvariant());
            editorKey.SetValue("Extension", defaultExtension);
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
            /// Set DebuggingLogicalViewEditor Mapping
            if (DebuggingLogicalViewEditor != null)
            {
                editorKey.SetValue(VSConstants.LOGVIEWID_Debugging.ToString("B").ToUpperInvariant(), TryGetGuidFromObject(DebuggingLogicalViewEditor).ToString("B").ToUpperInvariant());
            }
            /// Set CodeLogicalViewEditor Mapping
            if (CodeLogicalViewEditor != null)
            {
                editorKey.SetValue(VSConstants.LOGVIEWID_Code.ToString("B").ToUpperInvariant(), TryGetGuidFromObject(CodeLogicalViewEditor).ToString("B").ToUpperInvariant());
            }
            /// Set DesignerLogicalViewEditor Mapping
            if (DesignerLogicalViewEditor != null)
            {
                editorKey.SetValue(VSConstants.LOGVIEWID_Designer.ToString("B").ToUpperInvariant(), TryGetGuidFromObject(DesignerLogicalViewEditor).ToString("B").ToUpperInvariant());
            }
            /// Set TextLogicalViewEditor Mapping
            if (TextLogicalViewEditor != null)
            {
                editorKey.SetValue(VSConstants.LOGVIEWID_TextView.ToString("B").ToUpperInvariant(), TryGetGuidFromObject(TextLogicalViewEditor).ToString("B").ToUpperInvariant());
            }
            editorKey.Close();
        }

        /// <summary>
        /// Unregister the custom editor
        /// </summary>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context)
        {
            /// Validate parameter input
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            /// Remove Key
            context.RemoveKey(Path.Combine(XmlChooserFactory, keyName));
            context.RemoveValue(XmlChooserEditorExtensionsKeyPath, defaultExtension);
            context.RemoveKeyIfEmpty(XmlChooserEditorExtensionsKeyPath);
        }


        /// <summary>
        /// Private member function to return the GUID of an object.
        /// </summary>
        /// <param name="guidObject"></param>
        /// <returns></returns>
        private Guid TryGetGuidFromObject(object guidObject)
        {
            // figure out what type of object they passed in and get the GUID from it
            if (guidObject is string)
                return new Guid((string)guidObject);
            else if (guidObject is Type)
                return ((Type)guidObject).GUID;
            else if (guidObject is Guid)
                return (Guid)guidObject;
            else
                throw new ArgumentException("Could not determine Guid from supplied object.", "guidObject");
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
