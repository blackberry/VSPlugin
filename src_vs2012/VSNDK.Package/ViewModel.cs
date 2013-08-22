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
using System.ComponentModel;
using Microsoft.VisualStudio.Package;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Xml;
using Microsoft.VisualStudio.XmlEditor;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Collections;
using System.IO;
using System.Windows.Data;
using System.Collections.ObjectModel;
using EnvDTE;

namespace RIM.VSNDK_Package
{
    public class OrientationItemClass
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public OrientationItemClass(string name)
        {
            _name = name;
        }
    }

    public class ConfigurationItemClass
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ConfigurationItemClass(string name)
        {
            _name = name;
        }
    }

    public class AssetTypeItemClass
    {
        private string _type;

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public AssetTypeItemClass(string type)
        {
            _type = type;
        }
    }

    public class PermissionItemClass : INotifyPropertyChanged
    {
        private bool _isChecked;
        private string _permission;
        private string _identifier;
        private string _permisionImagePath;

        public bool IsChecked
        {
            get { return _isChecked; }
            set { 
                _isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public string Permission
        {
            get { return _permission; }
            set 
            { 
                _permission = value;
                OnPropertyChanged("Permission");
            }
        }

        public string PermissionImagePath
        {
            get { return _permisionImagePath; }
            set
            {
                _permisionImagePath = value;
                OnPropertyChanged("PermissionImagePath");
            }
        }

        public string Identifier
        {
            get { return _identifier; }
            set 
            { 
                _identifier = value;
                OnPropertyChanged("Identifier");
            }
        }

        public PermissionItemClass(bool isChecked, string permission, string identifier, string permissionImagePath)
        {
            _isChecked = isChecked;
            _permission = permission;
            _identifier = identifier;
            _permisionImagePath = permissionImagePath;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyname)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyname));
        }
    }

    public class ImageItemClass
    {
        private string _imageName;
        private string _imagePath;
        private string _imageSize;

        public string ImageName
        {
            get { return _imageName; }
            set { _imageName = value; }
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; }
        }

        public string ImageSize
        {
            get { return _imageSize; }
            set { _imageSize = value; }
        }

        public ImageItemClass(string imageName, string imagePath, string activeProjectDirectory)
        {
            _imageName = imageName;
            _imagePath = imagePath;
            try
            {
                System.Drawing.Image objImage = System.Drawing.Image.FromFile(imagePath);
                _imageSize = objImage.Width.ToString() + "X" + objImage.Height.ToString();
            }
            catch
            {
                try
                {
                    System.Drawing.Image objImage = System.Drawing.Image.FromFile(activeProjectDirectory + "\\" + imagePath);
                    _imageSize = objImage.Width.ToString() + "X" + objImage.Height.ToString();
                }
                catch
                {
                }
            }
        }

    }

    /// <summary>
    /// ViewModel is where the interesting portion of the VsTemplate Designer lives. The View binds to an instance of this class.
    /// 
    /// The View binds the various designer controls to the methods derived from IViewModel that get and set values in the XmlModel.
    /// The ViewModel and an underlying XmlModel manage how an IVsTextBuffer is shared between the designer and the XML editor (if opened).
    /// </summary>
    public class ViewModel : IViewModel, IDataErrorInfo, INotifyPropertyChanged
    {
        private static string _localRIMFolder;
        private static string _tmpAuthor = "";
        private static string _tmpAuthorID = "";
        private readonly CollectionView _orientationList;
        private OrientationItemClass _orientationItem;
        private CollectionView _iconImageList;
        private CollectionView _splashScreenImageList;
        private CollectionView _assetTypeList;
        private CollectionView _permissionList;
        private CollectionView _configurationList;
        private ConfigurationItemClass _config;
        private PermissionItemClass _permission;
        private DTE _dte;
        private string _activeProjectDirectory;

        long _dirtyTime;
        LanguageService _xmlLanguageService;
        IServiceProvider _serviceProvider;
        qnx _qnxSchema;
        bool _synchronizing;
        XmlModel _xmlModel;
        XmlStore _xmlStore;
        IVsTextLines _buffer;

        bool? _canEditFile;
        bool _gettingCheckoutStatus;

        EventHandler<XmlEditingScopeEventArgs> _editingScopeCompletedHandler;
        EventHandler<XmlEditingScopeEventArgs> _undoRedoCompletedHandler;
        EventHandler _bufferReloadedHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlStore"></param>
        /// <param name="xmlModel"></param>
        /// <param name="provider"></param>
        /// <param name="buffer"></param>
        public ViewModel(XmlStore xmlStore, XmlModel xmlModel, IServiceProvider provider, IVsTextLines buffer)
        {
            /// Initialize Asset Type List
            IList<AssetTypeItemClass> AssetTypeListItem = new List<AssetTypeItemClass>();
            AssetTypeItemClass assetType = new AssetTypeItemClass("Other");
            AssetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Entry-point");
            AssetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Library");
            AssetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Executable");
            AssetTypeListItem.Add(assetType);
            _assetTypeList = new CollectionView(AssetTypeListItem);

            if (xmlModel == null)
                throw new ArgumentNullException("xmlModel");
            if (xmlStore == null)
                throw new ArgumentNullException("xmlStore");
            if (provider == null)
                throw new ArgumentNullException("provider");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            this.BufferDirty = false;
            this.DesignerDirty = false;

            this._serviceProvider = provider;
            this._buffer = buffer;

            _dte = (DTE)_serviceProvider.GetService(typeof(DTE));
            Array activeProjects = (Array)_dte.ActiveSolutionProjects;
            Project activeProject = (Project)activeProjects.GetValue(0);
            FileInfo activeProjectFileInfo = new FileInfo(activeProject.FullName);
            _activeProjectDirectory = activeProjectFileInfo.DirectoryName;

            this._xmlStore = xmlStore;
            // OnUnderlyingEditCompleted
            _editingScopeCompletedHandler = new EventHandler<XmlEditingScopeEventArgs>(OnUnderlyingEditCompleted);
            this._xmlStore.EditingScopeCompleted += _editingScopeCompletedHandler;
            // OnUndoRedoCompleted
            _undoRedoCompletedHandler = new EventHandler<XmlEditingScopeEventArgs>(OnUndoRedoCompleted);
            this._xmlStore.UndoRedoCompleted += _undoRedoCompletedHandler;

            this._xmlModel = xmlModel;
            // BufferReloaded
            _bufferReloadedHandler += new EventHandler(BufferReloaded);
            this._xmlModel.BufferReloaded += _bufferReloadedHandler;

            _localRIMFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\";

            LoadModelFromXmlModel();

            IList<ImageItemClass> IconImageList = new List<ImageItemClass>();
            if ((_qnxSchema.icon != null) && (_qnxSchema.icon.image != null))
            {
                string iconPNG_Path = "";  //added to avoid duplication. That's because I didn't find the template to remove teh ICON.PNG.
                foreach (string iconImage in _qnxSchema.icon.image)
                {
                    ImageItemClass imageItem = new ImageItemClass(iconImage, getImagePath(iconImage), _activeProjectDirectory);
                    if (imageItem.ImageName != null) //added because I didn't find the template to remove teh ICON.PNG.
                        if (imageItem.ImageName == "icon.png")
                        {
                            if (iconPNG_Path != imageItem.ImagePath) //added because I didn't find the template to remove teh ICON.PNG.
                            {
                                IconImageList.Add(imageItem);
                                iconPNG_Path = imageItem.ImagePath;
                            }
                        }
                        else
                            IconImageList.Add(imageItem);

                }
            }
            _iconImageList = new CollectionView(IconImageList);

            LoadPermissions();

            IList<ImageItemClass> SplashScreenImageList = new List<ImageItemClass>();
            if ((_qnxSchema.splashScreens != null) && (_qnxSchema.splashScreens.image != null))
            {
                foreach (string splashScreenImage in _qnxSchema.splashScreens.image)
                {
                    ImageItemClass imageItem = new ImageItemClass(splashScreenImage, getImagePath(splashScreenImage), _activeProjectDirectory);
                    SplashScreenImageList.Add(imageItem);
                }
            }
            _splashScreenImageList = new CollectionView(SplashScreenImageList);

            IList<ConfigurationItemClass> ConfigurationList = new List<ConfigurationItemClass>();
            ConfigurationItemClass configItem = new ConfigurationItemClass("All Configurations");
            ConfigurationList.Add(configItem);
            foreach (qnxConfiguration config in _qnxSchema.configuration)
            {
                configItem = new ConfigurationItemClass(config.name);
                ConfigurationList.Add(configItem);
            }
            _configurationList = new CollectionView(ConfigurationList);

            IList<OrientationItemClass> OrientationList = new List<OrientationItemClass>();
            OrientationItemClass OrientationItem = new OrientationItemClass("Default");
            OrientationList.Add(OrientationItem);
            if (_qnxSchema.initialWindow.autoOrients == "") 
            {
                _orientationItem = OrientationItem;
            }            

            OrientationItem = new OrientationItemClass("Auto-orient");
            OrientationList.Add(OrientationItem);
            if (_qnxSchema.initialWindow.autoOrients == "true") 
            {
                _orientationItem = OrientationItem;
            }


            OrientationItem = new OrientationItemClass("Landscape");
            OrientationList.Add(OrientationItem);
            if (_qnxSchema.initialWindow.aspectRatio == "landscape") 
            {
                _orientationItem = OrientationItem;
            }

            OrientationItem = new OrientationItemClass("Portrait");
            OrientationList.Add(OrientationItem);
            if (_qnxSchema.initialWindow.aspectRatio == "portrait")
            {
                _orientationItem = OrientationItem;
            }

            _orientationList = new CollectionView(OrientationList);
        }

        private string getImagePath(string imgName)
        {
            string imagePath = "";

            foreach (asset assetItem in _qnxSchema.asset)
            {
                if (assetItem.Value == imgName)
                {
                    imagePath = assetItem.path; 
                }
            }

            return imagePath;
        }

        /// <summary>
        /// Close View Model
        /// </summary>
        public void Close()
        {
            //Unhook the events from the underlying XmlStore/XmlModel
            if (_xmlStore != null)
            {
                this._xmlStore.EditingScopeCompleted -= _editingScopeCompletedHandler;
                this._xmlStore.UndoRedoCompleted -= _undoRedoCompletedHandler;
            }
            if (this._xmlModel != null)
            {
                this._xmlModel.BufferReloaded -= _bufferReloadedHandler;
            }
        }

        private void LoadPermissions()
        {
            IList<PermissionItemClass> PermissionList = new List<PermissionItemClass>();
            PermissionItemClass permissionItem = new PermissionItemClass(isPermissionChecked("bbm_connect"), "BlackBerry Messenger", "bbm_connect", "/VSNDK.Package;component/Resources/BlackBerryMessager.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_pimdomain_calendars"), "Calendar", "access_pimdomain_calendars", "/VSNDK.Package;component/Resources/Calendar.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("use_camera"), "Camera", "use_camera", "/VSNDK.Package;component/Resources/Camera.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_pimdomain_contacts"), "Contacts", "access_pimdomain_contacts", "/VSNDK.Package;component/Resources/Contacts.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("read_device_identifying_information"), "Device Identifying Information", "read_device_identifying_information", "/VSNDK.Package;component/Resources/DeviceIdentifyingInfo.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_pimdomain_messages"), "Email and PIN Message", "access_pimdomain_messages", "/VSNDK.Package;component/Resources/EmailPINMessages.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_internet"), "Internet", "access_internet", "/VSNDK.Package;component/Resources/Internet.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("read_geolocation"), "GPS Location", "read_geolocation", "/VSNDK.Package;component/Resources/GPSLocation.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_location_services"), "Location", "access_location_services", "/VSNDK.Package;component/Resources/Location.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("record_audio"), "Microphone", "record_audio", "/VSNDK.Package;component/Resources/Mircrophone.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_pimdomain_notebooks"), "Notebooks", "access_pimdomain_notebooks", "/VSNDK.Package;component/Resources/Notebooks.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("post_notification"), "Post Notifications", "post_notification", "/VSNDK.Package;component/Resources/PostNotifications.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("run_when_backgrounded"), "Run When Backgrounded", "run_when_backgrounded", "/VSNDK.Package;component/Resources/RunBackgrounded.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_shared"), "Shared Files", "access_shared", "/VSNDK.Package;component/Resources/SharedFiles.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_sms_mms"), "Text Messages", "access_sms_mms", "/VSNDK.Package;component/Resources/TextMessages.bmp");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("_sys_use_consumer_push"), "Consumer Push", "_sys_use_consumer_push", "");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("narrow_landscape_exit"), "Narrow Swipe Up", "narrow_landscape_exit", "");
            PermissionList.Add(permissionItem);
            permissionItem = new PermissionItemClass(isPermissionChecked("access_phone"), "Phone", "access_phone", "");
            PermissionList.Add(permissionItem);
            _permissionList = new CollectionView(PermissionList);
        }

        /// <summary>
        /// Called on idle time. This is when we check if the designer is out of sync with the underlying text buffer.
        /// </summary>
        public void DoIdle()
        {
            if (BufferDirty || DesignerDirty)
            {
                int delay = 100;

                if ((Environment.TickCount - _dirtyTime) > delay)
                {
                    // Must not try and sync while XML editor is parsing otherwise we just confuse matters.
                    if (IsXmlEditorParsing)
                    {
                        _dirtyTime = System.Environment.TickCount;
                        return;
                    }

                    //If there is contention, give the preference to the designer.
                    if (DesignerDirty)
                    {
                        SaveModelToXmlModel(Resources.SynchronizeBuffer);
                        //We don't do any merging, so just overwrite whatever was in the buffer.
                        BufferDirty = false;
                    }
                    else if (BufferDirty)
                    {
                        LoadModelFromXmlModel();
                    }
                }
            }
        }

        /// <summary>
        /// We must not try and update the XDocument while the XML Editor is parsing as this may cause
        /// a deadlock in the XML Editor!
        /// </summary>
        /// <returns></returns>
        bool IsXmlEditorParsing
        {
            get
            {
                LanguageService langsvc = GetXmlLanguageService();
                return langsvc != null ? langsvc.IsParsing : false;
            }
        }

        /// <summary>
        /// Get the XML Editor language service
        /// </summary>
        /// <returns></returns>
        LanguageService GetXmlLanguageService()
        {
            if (_xmlLanguageService == null)
            {
                IOleServiceProvider vssp = _serviceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider;
                Guid xmlEditorGuid = new Guid("f6819a78-a205-47b5-be1c-675b3c7f0b8e");
                Guid iunknown = new Guid("00000000-0000-0000-C000-000000000046");
                IntPtr ptr;
                if (ErrorHandler.Succeeded(vssp.QueryService(ref xmlEditorGuid, ref iunknown, out ptr)))
                {
                    try
                    {
                        _xmlLanguageService = Marshal.GetObjectForIUnknown(ptr) as LanguageService;
                    }
                    finally
                    {
                        Marshal.Release(ptr);
                    }
                }
            }
            return _xmlLanguageService;
        }

        /// <summary>
        /// This method is called when it is time to save the designer values to the
        /// underlying buffer.
        /// </summary>
        /// <param name="undoEntry"></param>
        void SaveModelToXmlModel(string undoEntry)
        {
            LanguageService langsvc = GetXmlLanguageService();

            try
            {
                //We can't edit this file (perhaps the user cancelled a SCC prompt, etc...)
                if (!CanEditFile())
                {
                    DesignerDirty = false;
                    BufferDirty = true;
                    throw new Exception();
                }

                XmlSerializer serializer = new XmlSerializer(typeof(qnx));
                XDocument documentFromDesignerState = new XDocument();
                using (XmlWriter w = documentFromDesignerState.CreateWriter())
                {
                    serializer.Serialize(w, _qnxSchema);
                }

                _synchronizing = true;
                XDocument document = GetParseTree();
                Source src = GetSource();
                if (src == null || langsvc == null)
                {
                    return;
                }

                langsvc.IsParsing = true; // lock out the background parse thread.

                // Wrap the buffer sync and the formatting in one undo unit.
                using (CompoundAction ca = new CompoundAction(src, Resources.SynchronizeBuffer))
                {
                    using (XmlEditingScope scope = _xmlStore.BeginEditingScope(Resources.SynchronizeBuffer, this))
                    {
                        //Replace the existing XDocument with the new one we just generated.
                        document.Root.ReplaceWith(documentFromDesignerState.Root);
                        scope.Complete();
                    }
                    ca.FlushEditActions();
                    FormatBuffer(src);
                }
                DesignerDirty = false;
            }
            catch (Exception)
            {
                // if the synchronization fails then we'll just try again in a second.
                _dirtyTime = Environment.TickCount;
            }
            finally
            {
                langsvc.IsParsing = false;
                _synchronizing = false;

                LoadPermissions();
            }
        }

        /// <summary>
        /// Reformat the text buffer
        /// </summary>
        void FormatBuffer(Source src)
        {
            using (EditArray edits = new EditArray(src, null, false, Resources.ReformatBuffer))
            {
                TextSpan span = src.GetDocumentSpan();
                src.ReformatSpan(edits, span);
            }
        }

        /// <summary>
        /// Get the XML Editor Source object for this document.
        /// </summary>
        /// <returns></returns>
        Source GetSource()
        {
            LanguageService langsvc = GetXmlLanguageService();
            if (langsvc == null)
            {
                return null;
            }
            Source src = langsvc.GetSource(_buffer);
            return src;
        }

        /// <summary>
        /// Get an up to date XML parse tree from the XML Editor.
        /// </summary>
        XDocument GetParseTree()
        {
            LanguageService langsvc = this.GetXmlLanguageService();

            // don't crash if the language service is not available
            if (langsvc != null)
            {
                Source src = langsvc.GetSource(_buffer);

                // We need to access this method to get the most up to date parse tree.
                // public virtual XmlDocument GetParseTree(Source source, IVsTextView view, int line, int col, ParseReason reason) {
                MethodInfo mi = langsvc.GetType().GetMethod("GetParseTree");
                int line = 0, col = 0;
                mi.Invoke(langsvc, new object[] { src, null, line, col, ParseReason.Check });
            }

            // Now the XmlDocument should be up to date also.
            return _xmlModel.Document;
        }

        /// <summary>
        /// This function asks the QueryEditQuerySave service if it is possible to edit the file.
        /// This can result in an automatic checkout of the file and may even prompt the user for
        /// permission to checkout the file.  If the user says no or the file cannot be edited 
        /// this returns false.
        /// </summary>
        private bool CanEditFile()
        {
            // Cache the value so we don't keep asking the user over and over.
            if (_canEditFile.HasValue)
            {
                return (bool)_canEditFile;
            }

            // Check the status of the recursion guard
            if (_gettingCheckoutStatus)
                return false;

            _canEditFile = false; // assume the worst
            try
            {
                // Set the recursion guard
                _gettingCheckoutStatus = true;

                // Get the QueryEditQuerySave service
                IVsQueryEditQuerySave2 queryEditQuerySave = _serviceProvider.GetService(typeof(SVsQueryEditQuerySave)) as IVsQueryEditQuerySave2;

                string filename = _xmlModel.Name;

                // Now call the QueryEdit method to find the edit status of this file
                string[] documents = { filename };
                uint result;
                uint outFlags;

                // Note that this function can popup a dialog to ask the user to checkout the file.
                // When this dialog is visible, it is possible to receive other request to change
                // the file and this is the reason for the recursion guard
                int hr = queryEditQuerySave.QueryEditFiles(
                    0,              // Flags
                    1,              // Number of elements in the array
                    documents,      // Files to edit
                    null,           // Input flags
                    null,           // Input array of VSQEQS_FILE_ATTRIBUTE_DATA
                    out result,     // result of the checkout
                    out outFlags    // Additional flags
                );
                if (ErrorHandler.Succeeded(hr) && (result == (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    // In this case (and only in this case) we can return true from this function
                    _canEditFile = true;
                }
            }
            finally
            {
                _gettingCheckoutStatus = false;
            }
            return (bool)_canEditFile;
        }

        /// <summary>
        /// Load the model from the underlying text buffer.
        /// </summary>
        private void LoadModelFromXmlModel()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(qnx));

                using (XmlReader reader = GetParseTree().CreateReader())
                {
                    _qnxSchema = (qnx)serializer.Deserialize(reader);
                }

                if (_qnxSchema == null)
                {
                    throw new Exception(Resources.InvalidVsTemplateData);
                }
            }
            catch (Exception e)
            {
                //Display error message
                ErrorHandler.ThrowOnFailure(VsShellUtilities.ShowMessageBox(_serviceProvider,
                    Resources.InvalidVsTemplateData + e.Message,
                    Resources.ErrorMessageBoxTitle,
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST));
            }

            BufferDirty = false;

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());

                LoadPermissions();
            }
        }

        /// <summary>
        /// BufferDirty Property
        /// </summary>
        public bool BufferDirty { get; set; }

        /// <summary>
        /// DesignerDirty Property
        /// </summary>
        public bool DesignerDirty { get; set; }


        /// <summary>
        /// Fired when all controls should be re-bound.
        /// </summary>
        public event EventHandler ViewModelChanged;

        /// <summary>
        /// Handle edit scope completion event.  This happens when the XML editor buffer decides to update
        /// it's XDocument parse tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnUnderlyingEditCompleted(object sender, XmlEditingScopeEventArgs e)
        {
            if (e.EditingScope.UserState != this && !_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// Handle undo/redo completion event.  This happens when the user invokes Undo/Redo on a buffer edit operation.
        /// We need to resync when this happens.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnUndoRedoCompleted(object sender, XmlEditingScopeEventArgs e)
        {
            if (!_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// BufferReloaded event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BufferReloaded(object sender, EventArgs e)
        {
            if (!_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        /// <summary>
        /// Read the author information from the debug token and update the appropriate boxes.
        /// </summary>
        public void setAuthorInfo()
        {
            if (!File.Exists(_localRIMFolder + "DebugToken.bar"))
            {
                // Create the dialog instance without Help support.
                var DebugTokenDialog = new DebugToken.DebugTokenDialog();
                // Show the dialog.
                if (!DebugTokenDialog.IsClosing)
                    DebugTokenDialog.ShowModal();
            }

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = p.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(p_OutputDataReceived);


            /// Get Device PIN
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = string.Format(@"/C blackberry-airpackager.bat -listManifest ""{0}""", _localRIMFolder + "DebugToken.bar");

            try
            {
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                p.Close();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(startInfo.Arguments);
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Get the author ID
        /// </summary>
        /// <returns>REturns the author ID</returns>
        public string getAuthorID()
        {
            return _tmpAuthorID;
        }

        /// <summary>
        /// Get the author ID
        /// </summary>
        /// <returns>REturns the author ID</returns>
        public string getAuthor()
        {
           return _tmpAuthor;
        }

        /// <summary>
        /// On Data Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.Contains("Package-Author-Id:"))
                    AuthorID = e.Data.Substring(e.Data.LastIndexOf(": ") + 2);
                else if (e.Data.Contains("Package-Author:"))
                    Author = e.Data.Substring(e.Data.LastIndexOf(": ") + 2);
            }
        }

        #region IViewModel

        /// <summary>
        /// Name property
        /// </summary>
        public string Name
        {
            get
            {
                return _qnxSchema.id;
            }
            set
            {
                if (_qnxSchema.id != value)
                {
                    _qnxSchema.id = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// AppName property
        /// </summary>
        public string AppName
        {
            get
            {
                return _qnxSchema.name;
            }
            set
            {
                if (_qnxSchema.name != value)
                {
                    _qnxSchema.name = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("AppName");
                }
            }
        }

        /// <summary>
        /// Description property
        /// </summary>
        public string Description
        {
            get
            {
                return _qnxSchema.description.Trim();
            }
            set
            {
                if (_qnxSchema.description != value)
                {
                    _qnxSchema.description = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        /// <summary>
        /// Version property
        /// </summary>
        public string Version
        {
            get
            {
                return _qnxSchema.versionNumber;
            }
            set
            {
                if (_qnxSchema.versionNumber != value)
                {
                    _qnxSchema.versionNumber = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("Version");
                }
            }
        }

        /// <summary>
        /// Build ID property
        /// </summary>
        public string BuildID
        {
            get
            {
                return _qnxSchema.buildId;
            }
            set
            {
                if (_qnxSchema.buildId != value)
                {
                    _qnxSchema.buildId = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("BuildID");
                }
            }
        }

        /// <summary>
        /// Author property
        /// </summary>
        public string Author
        {
            get
            {
                return _qnxSchema.author;
            }
            set
            {
                if (_qnxSchema.author != value)
                {
                    _qnxSchema.author = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("Author");
                }
            }
        }

        /// <summary>
        /// Build ID property
        /// </summary>
        public string AuthorID
        {
            get
            {
                return _qnxSchema.authorId;
            }
            set
            {
                if (_qnxSchema.authorId != value)
                {
                    _qnxSchema.authorId = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("AuthorID");
                }
            }
        }

        /// <summary>
        /// Chrome property
        /// </summary>
        public string Chrome
        {
            get
            {
                return _qnxSchema.initialWindow.systemChrome;
            }
            set
            {
                if (_qnxSchema.initialWindow.systemChrome != value)
                {
                    _qnxSchema.initialWindow.systemChrome = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged("Chrome");
                }
            }
        }

        /// <summary>
        /// Return the AssetList
        /// </summary>
        public List<asset> AssetList
        {
            get
            {
                if (_config == null)
                {
                    if (_qnxSchema.asset == null)
                        return null;
                    return _qnxSchema.asset.ToList();
                }

                if (_config.Name == "All Configurations")
                {
                    if (_qnxSchema.asset == null)
                        return null;
                    return _qnxSchema.asset.ToList();
                }
                else
                {
                    foreach (qnxConfiguration config in _qnxSchema.configuration)
                    {
                        if (config.name == _config.Name)
                        {
                            if (config.asset == null)
                                return null;
                            return config.asset.ToList();
                        }
                    }

                    return null;
                }
            }
        }


        public void AddLocalAsset(string assetPath)
        {
            asset newAsset = new asset();

            FileInfo fileInfo = new FileInfo(assetPath);
            newAsset.Value = fileInfo.Name;

            string activeDir = _activeProjectDirectory;
            string back = "";

            // generating the relative path for the asset.
            do
            {
                if (assetPath.Contains(activeDir + "\\"))
                {
                    newAsset.path = back + assetPath.Replace(activeDir + "\\", "");
                    break;
                }
                else
                {
                    int pos = activeDir.LastIndexOf('\\', activeDir.Length - 1);
                    if (pos < 0)
                    { // file is located in a different driver. Copy the entire assetPath.
                        newAsset.path = assetPath;
                        break;
                    }
                    else
                    {
                        back += "..\\";
                        activeDir = activeDir.Remove(pos);
                    }
                }
            }
            while (true);

            if (_config.Name == "All Configurations")   
                _qnxSchema.AddLocalAsset(newAsset);
            else
            {
                foreach (qnxConfiguration config in _qnxSchema.configuration)
                {
                    if (config.name == _config.Name)
                    {
                        config.AddAsset(newAsset);
                    }
                }
            }

            DesignerDirty = true;

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void DeleteLocalAsset(object asset)
        {
            if (_config.Name == "All Configurations")
                _qnxSchema.DeleteLocalAsset(asset as asset);
            else
            {
                foreach (qnxConfiguration config in _qnxSchema.configuration)
                {
                    if (config.name == _config.Name)
                    {
                        config.DeleteAsset(asset as asset);
                    }
                }
            }

            DesignerDirty = true;

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void EditLocalAsset(string identifier, bool? isPublic, string assetType)
        {
            if (_config.Name == "All Configurations")
            {
                foreach (asset assetItem in _qnxSchema.asset)
                {
                    if (assetItem.Value == identifier)
                    {
                        if (isPublic == true)
                        {
                            assetItem.publicAsset = "true";
                        }
                        else if (isPublic == false)
                        {
                            assetItem.publicAsset = "false";
                        }

                        if (assetType == "Other")
                        {
                            assetItem.type = "";
                            assetItem.entry = "";
                            assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                        }
                        else if (assetType == "Entry-point")
                        {
                            assetItem.type = "Qnx/Elf";
                            assetItem.entry = "true";
                            assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                        }
                        else if (assetType == "Executable")
                        {
                            assetItem.type = "Qnx/Elf";
                            assetItem.entry = "";
                            assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                        }
                        else if (assetType == "Library")
                        {
                            assetItem.type = "";
                            assetItem.entry = "";
                            assetItem.Value = @"lib/" + assetItem.Value;
                        }
                    }
                }
            }
            else
            {
                foreach (qnxConfiguration config in _qnxSchema.configuration)
                {
                    if (config.name == _config.Name)
                    {
                        foreach (asset assetItem in config.asset)
                        {
                            if (assetItem.Value == identifier)
                            {
                                if (isPublic == true)
                                {
                                    assetItem.publicAsset = "true";
                                }
                                else if (isPublic == false)
                                {
                                    assetItem.publicAsset = "false";
                                }

                                if (assetType == "Other")
                                {
                                    assetItem.type = "";
                                    assetItem.entry = "";
                                    assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                                }
                                else if (assetType == "Entry-point")
                                {
                                    assetItem.type = "Qnx/Elf";
                                    assetItem.entry = "true";
                                    assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                                }
                                else if (assetType == "Executable")
                                {
                                    assetItem.type = "Qnx/Elf";
                                    assetItem.entry = "";
                                    assetItem.Value = assetItem.Value.Replace(@"lib/", "");
                                }
                                else if (assetType == "Library")
                                {
                                    assetItem.type = "";
                                    assetItem.entry = "";
                                    assetItem.Value = @"lib/" + assetItem.Value;
                                }
                            }
                        }
                    }
                }
            }
               
            DesignerDirty = true;

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void AddIcon(string iconName)
        {
            _qnxSchema.icon.AddIconImage(iconName);
            DesignerDirty = true;
            IList source = (IList)_iconImageList.SourceCollection;
              ImageItemClass image = new ImageItemClass(iconName, getImagePath(iconName), _activeProjectDirectory);
            source.Add(image);
            _iconImageList = new CollectionView(source);

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void DeleteIcon(object iconName)
        {
            ImageItemClass item = (ImageItemClass)iconName;
            _qnxSchema.icon.DeleteIconImage(item.ImageName);
            DesignerDirty = true;
            IList source = (IList)_iconImageList.SourceCollection;
            source.Remove(item);
            _iconImageList = new CollectionView(source);

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void AddSplashScreen(string splashScreenName)
        {
            qnxSplashScreens qnxSS;

            if (_qnxSchema.splashScreens == null)
            {
                qnxSS = new qnxSplashScreens();
                _qnxSchema.splashScreens = qnxSS;
            }
            else
            {
                qnxSS = _qnxSchema.splashScreens;
            }

            qnxSS.AddSplashScreenImage(splashScreenName);
            DesignerDirty = true;
            IList source = (IList)_splashScreenImageList.SourceCollection;
            ImageItemClass image = new ImageItemClass(splashScreenName, getImagePath(splashScreenName), _activeProjectDirectory);
            source.Add(image);
            _splashScreenImageList = new CollectionView(source);

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void DeleteSplashScreen(object splashScreenName)
        {
            ImageItemClass item = (ImageItemClass)splashScreenName;
            _qnxSchema.splashScreens.DeleteSplashScreenImage(item.ImageName);
            DesignerDirty = true;
            IList source = (IList)_splashScreenImageList.SourceCollection;
            source.Remove(item);
            _splashScreenImageList = new CollectionView(source);

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }

        public void CheckPermission(string identifier)
        {

            /// add new perm to xml
            qnxPermission perm = new qnxPermission();
            perm.Value = identifier;
            _qnxSchema.AddPermission(perm);
            DesignerDirty = true;
        }

        public void UnCheckPermission(string identifier)
        {
            /// add new perm to xml
            qnxPermission perm = new qnxPermission();
            perm.Value = identifier;
            _qnxSchema.DeletePermission(perm);
            DesignerDirty = true;
        }

        public bool isPermissionChecked(string identifier)
        {
            bool result = false;

            if (_qnxSchema.permission != null)
            {
                foreach (qnxPermission permEntry in _qnxSchema.permission)
                {
                    if (permEntry.Value == identifier)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        
        public CollectionView OrientationList
        {
            get { return _orientationList; }
        }

        public CollectionView ConfigurationList
        {
            get { return _configurationList; }
        } 

        public CollectionView PermissionList
        {
            get { return _permissionList; }
        }

        public CollectionView IconImageList
        {
            get { return _iconImageList; }
        }


        public CollectionView SplashScreenImageList
        {
            get { return _splashScreenImageList; }
        }

        public PermissionItemClass PermissionItem
        {
            get
            {
                return _permission;
            }
            set
            {
                _permission = value;
                NotifyPropertyChanged("permission");
            }
        }

        public ConfigurationItemClass ConfigurationItemClass
        {
            get
            {
                return _config;
            }
            set
            {
                if (_config == value) return;
                _config = value;


                if (ViewModelChanged != null)
                {
                    // Update the Designer View
                    ViewModelChanged(this, new EventArgs());
                }

                NotifyPropertyChanged("ConfigurationItemClass");
            }
        }

        public OrientationItemClass OrientationItemClass
        {
            get { 
                return _orientationItem; }
            set
            {
                if (_orientationItem == value) return;
                _orientationItem = value;

                if (_orientationItem.Name == "Default")
                {
                    _qnxSchema.initialWindow.autoOrients = "";
                    _qnxSchema.initialWindow.aspectRatio = "";
                }
                else if (_orientationItem.Name == "Auto-orient")
                {
                    _qnxSchema.initialWindow.autoOrients = "true";
                    _qnxSchema.initialWindow.aspectRatio = "";
                }
                else if (_orientationItem.Name == "Landscape")
                {
                    _qnxSchema.initialWindow.autoOrients = "false";
                    _qnxSchema.initialWindow.aspectRatio = "landscape";
                }
                else if (_orientationItem.Name == "Portrait")
                {
                    _qnxSchema.initialWindow.autoOrients = "false";
                    _qnxSchema.initialWindow.aspectRatio = "portrait";
                }

                DesignerDirty = true;
                NotifyPropertyChanged("OrientationItemClass");
            }
        }

        public XmlModel Model
        {
            get
            {
                return _xmlModel;
            }
        }
        /// <summary>
        /// Transparent property
        /// </summary>
        public bool Transparent
        {
            get
            {
                if (_qnxSchema.initialWindow.transparent == "true")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (value)
                {
                    if (_qnxSchema.initialWindow.transparent != "true")
                    {
                        _qnxSchema.initialWindow.transparent = "true";
                        DesignerDirty = true;
                        NotifyPropertyChanged("Chrome");
                    }
                }
                else
                {
                    if (_qnxSchema.initialWindow.transparent == "true")
                    {
                        _qnxSchema.initialWindow.transparent = "false";
                        DesignerDirty = true;
                        NotifyPropertyChanged("Chrome");
                    }
                }
            }
        }

        #endregion

        #region IDataErrorInfo

        /// <summary>
        /// Error Property
        /// </summary>
        public string Error
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public string this[string columnName]
        {
            get
            {
                string error = null;
                return error;
            }
        }


        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region TreeView SelectionChanged

        private ITrackSelection trackSel;
        private ITrackSelection TrackSelection
        {
            get
            {
                if (trackSel == null)
                    trackSel = _serviceProvider.GetService(typeof(STrackSelection)) as ITrackSelection;
                return trackSel;
            }
        }

        private Microsoft.VisualStudio.Shell.SelectionContainer selContainer;
        public void OnSelectChanged(object p)
        {
            selContainer = new Microsoft.VisualStudio.Shell.SelectionContainer(true, false);
            ArrayList items = new ArrayList();
            items.Add(p);
            selContainer.SelectableObjects = items;
            selContainer.SelectedObjects = items;

            ITrackSelection track = TrackSelection;
            if (track != null)
                track.OnSelectChange((ISelectionContainer)selContainer);
        }

        #endregion

    }
}
