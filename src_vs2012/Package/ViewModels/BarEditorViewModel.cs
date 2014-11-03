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
using System.Linq;
using BlackBerry.BarDescriptor.Model;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Resources;
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
using EnvDTE;

namespace BlackBerry.Package.ViewModels
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
        private readonly PermissionInfo _info;
        private readonly BarEditorViewModel _viewModel;

        public bool IsChecked
        {
            get
            {
                return _viewModel.isPermissionChecked(_info.ID);
            }
            set { 
                if (value)
                {
                    _viewModel.CheckPermission(_info.ID);
                }
                else
                {
                    if (_viewModel.isPermissionChecked(_info.ID))
                        _viewModel.UnCheckPermission(_info.ID);
                }
                OnPropertyChanged("IsChecked");
            }
        }

        public string Permission
        {
            get { return _info.Name; }
        }

        public string PermissionImagePath
        {
            get { return BarEditorViewModel.GetPermissionIcon(_info.ID); }
        }

        public string Identifier
        {
            get { return _info.ID; }
        }

        public string Description
        {
            get { return _info.Description; }
        }

        public PermissionItemClass(PermissionInfo info, BarEditorViewModel vm)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            if (vm == null)
                throw new ArgumentNullException("vm");
            _info = info;
            _viewModel = vm;
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

        /// <summary>
        /// Class to store the splashscreen and icon images data.
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="imagePath"></param>
        /// <param name="activeProjectDirectory"></param>
        public ImageItemClass(string imageName, string imagePath, string activeProjectDirectory)
        {
            _imageName = imageName;
            _imagePath = imagePath;

            if (!File.Exists(_imagePath))
            {
                if (File.Exists(activeProjectDirectory + "\\" + _imagePath))
                {
                    _imagePath = activeProjectDirectory + "\\" + _imagePath;

                }
            }

            try
            {
                System.Drawing.Image objImage = System.Drawing.Image.FromFile(_imagePath);
                _imageSize = objImage.Width + "x" + objImage.Height;
            }
            catch
            {

            }
        }
    }

    /// <summary>
    /// ViewModel is where the interesting portion of the VsTemplate Designer lives. The View binds to an instance of this class.
    /// 
    /// The View binds the various designer controls to the methods derived from IViewModel that get and set values in the XmlModel.
    /// The ViewModel and an underlying XmlModel manage how an IVsTextBuffer is shared between the designer and the XML editor (if opened).
    /// </summary>
    public sealed class BarEditorViewModel : IDataErrorInfo, INotifyPropertyChanged, IDisposable
    {
        private readonly CollectionView _orientationList;
        private OrientationItemClass _orientationItem;
        private CollectionView _iconImageList;
        private CollectionView _splashScreenImageList;
        private CollectionView _permissionList;
        private readonly CollectionView _configurationList;
        private ConfigurationItemClass _config;
        private PermissionItemClass _permission;
        private readonly string _activeProjectDirectory;

        private long _dirtyTime;
        private LanguageService _xmlLanguageService;
        private IServiceProvider _serviceProvider;
        private QnxRootType _qnxSchema;
        private bool _synchronizing;
        private XmlModel _xmlModel;
        private XmlStore _xmlStore;
        private IVsTextLines _buffer;

        private bool? _canEditFile;
        private bool _gettingCheckoutStatus;

        private DebugTokenInfoRunner _debugTokenInfoRunner;

        private readonly EventHandler<XmlEditingScopeEventArgs> _editingScopeCompletedHandler;
        private readonly EventHandler<XmlEditingScopeEventArgs> _undoRedoCompletedHandler;
        private readonly EventHandler _bufferReloadedHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xmlStore"></param>
        /// <param name="xmlModel"></param>
        /// <param name="provider"></param>
        /// <param name="buffer"></param>
        public BarEditorViewModel(XmlStore xmlStore, XmlModel xmlModel, IServiceProvider provider, IVsTextLines buffer)
        {
            // Initialize Asset Type List
            IList<AssetTypeItemClass> assetTypeListItem = new List<AssetTypeItemClass>();
            AssetTypeItemClass assetType = new AssetTypeItemClass("Other");
            assetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Entry-point");
            assetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Library");
            assetTypeListItem.Add(assetType);
            assetType = new AssetTypeItemClass("Executable");
            assetTypeListItem.Add(assetType);

            if (xmlModel == null)
                throw new ArgumentNullException("xmlModel");
            if (xmlStore == null)
                throw new ArgumentNullException("xmlStore");
            if (provider == null)
                throw new ArgumentNullException("provider");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            BufferDirty = false;
            DesignerDirty = false;

            _serviceProvider = provider;
            _buffer = buffer;

            DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE));
            Array activeProjects = (Array)dte.ActiveSolutionProjects;
            Project activeProject = (Project)activeProjects.GetValue(0);
            FileInfo activeProjectFileInfo = new FileInfo(activeProject.FullName);
            _activeProjectDirectory = activeProjectFileInfo.DirectoryName;

            _xmlStore = xmlStore;
            // OnUnderlyingEditCompleted
            _editingScopeCompletedHandler = OnUnderlyingEditCompleted;
            _xmlStore.EditingScopeCompleted += _editingScopeCompletedHandler;
            // OnUndoRedoCompleted
            _undoRedoCompletedHandler = OnUndoRedoCompleted;
            _xmlStore.UndoRedoCompleted += _undoRedoCompletedHandler;

            _xmlModel = xmlModel;
            // BufferReloaded
            _bufferReloadedHandler += BufferReloaded;
            _xmlModel.BufferReloaded += _bufferReloadedHandler;

            LoadModelFromXmlModel();

            IList<ImageItemClass> iconImageList = new List<ImageItemClass>();
            if ((_qnxSchema.icon != null) && (_qnxSchema.icon.image != null))
            {
                string iconPngPath = "";  //added to avoid duplication. That's because I didn't find the template to remove teh ICON.PNG.
                foreach (string iconImage in _qnxSchema.icon.image)
                {
                    ImageItemClass imageItem = new ImageItemClass(iconImage, GetImagePath(iconImage), _activeProjectDirectory);
                    if (imageItem.ImageName != null) //added because I didn't find the template to remove teh ICON.PNG.
                        if (imageItem.ImageName == "icon.png")
                        {
                            if (iconPngPath != imageItem.ImagePath) //added because I didn't find the template to remove teh ICON.PNG.
                            {
                                iconImageList.Add(imageItem);
                                iconPngPath = imageItem.ImagePath;
                            }
                        }
                        else
                            iconImageList.Add(imageItem);

                }
            }
            _iconImageList = new CollectionView(iconImageList);

            LoadPermissions();

            IList<ImageItemClass> splashScreenImageList = new List<ImageItemClass>();
            if ((_qnxSchema.splashScreens != null) && (_qnxSchema.splashScreens.image != null))
            {
                foreach (string splashScreenImage in _qnxSchema.splashScreens.image)
                {
                    ImageItemClass imageItem = new ImageItemClass(splashScreenImage, GetImagePath(splashScreenImage), _activeProjectDirectory);
                    splashScreenImageList.Add(imageItem);
                }
            }
            _splashScreenImageList = new CollectionView(splashScreenImageList);

            IList<ConfigurationItemClass> configurationList = new List<ConfigurationItemClass>();
            ConfigurationItemClass configItem = new ConfigurationItemClass("All Configurations");
            configurationList.Add(configItem);
            foreach (var config in _qnxSchema.configuration)
            {
                configItem = new ConfigurationItemClass(config.name);
                configurationList.Add(configItem);
            }
            _configurationList = new CollectionView(configurationList);

            IList<OrientationItemClass> orientationList = new List<OrientationItemClass>();
            OrientationItemClass orientationItem = new OrientationItemClass("Default");
            orientationList.Add(orientationItem);
            if (_qnxSchema.initialWindow.autoOrients == "") 
            {
                _orientationItem = orientationItem;
            }            

            orientationItem = new OrientationItemClass("Auto-orient");
            orientationList.Add(orientationItem);
            if (_qnxSchema.initialWindow.autoOrients == "true") 
            {
                _orientationItem = orientationItem;
            }


            orientationItem = new OrientationItemClass("Landscape");
            orientationList.Add(orientationItem);
            if (_qnxSchema.initialWindow.aspectRatio == "landscape") 
            {
                _orientationItem = orientationItem;
            }

            orientationItem = new OrientationItemClass("Portrait");
            orientationList.Add(orientationItem);
            if (_qnxSchema.initialWindow.aspectRatio == "portrait")
            {
                _orientationItem = orientationItem;
            }

            _orientationList = new CollectionView(orientationList);
        }

        ~BarEditorViewModel()
        {
            Dispose(false);
        }

        private string GetImagePath(string imgName)
        {
            string imagePath = "";

            foreach (var assetItem in _qnxSchema.asset)
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
                _xmlStore.EditingScopeCompleted -= _editingScopeCompletedHandler;
                _xmlStore.UndoRedoCompleted -= _undoRedoCompletedHandler;
            }
            if (_xmlModel != null)
            {
                _xmlModel.BufferReloaded -= _bufferReloadedHandler;
            }
        }

        /// <summary>
        /// Given the permission ID return the appropriate Icon
        /// </summary>
        internal static string GetPermissionIcon(string id)
        {
            string retVal = "";
            string prefix = "/BlackBerry.Package;component/Resources/Permissions/";

            switch (id)
            {
                case "bbm_connect":
                    retVal = prefix + "BlackBerryMessager.bmp";
                    break;
                case "access_pimdomain_calendars":
                    retVal = prefix + "Calendar.bmp";
                    break;
                case "use_camera":
                    retVal = prefix + "Camera.bmp";
                    break;
                case "access_pimdomain_contacts":
                    retVal = prefix + "Contacts.bmp";
                    break;
                case "read_device_identifying_information":
                    retVal = prefix + "DeviceIdentifyingInfo.bmp";
                    break;
                case "access_pimdomain_messages":
                    retVal = prefix + "EmailPINMessages.bmp";
                    break;
                case "access_internet":
                    retVal = prefix + "Internet.bmp";
                    break;
                case "read_geolocation":
                    retVal = prefix + "GPSLocation.bmp";
                    break;
                case "access_location_services":
                    retVal = prefix + "Location.bmp";
                    break;
                case "record_audio":
                    retVal = prefix + "Mircrophone.bmp";
                    break;
                case "access_pimdomain_notebooks":
                    retVal = prefix + "Notebooks.bmp";
                    break;
                case "post_notification":
                    retVal = prefix + "PostNotifications.bmp";
                    break;
                case "run_when_backgrounded":
                    retVal = prefix + "RunBackgrounded.bmp";
                    break;
                case "access_shared":
                    retVal = prefix + "SharedFiles.bmp";
                    break;
                case "access_sms_mms":
                    retVal = prefix + "TextMessages.bmp";
                    break;
                case "read_personally_identifiable_information":
                    retVal = prefix + "MyContactInfo.bmp";
                    break;
                case "access_phone":
                    retVal = prefix + "Phone.bmp";
                    break;
                case "control_phone":
                    retVal = prefix + "PhoneControl.bmp";
                    break;
                case "_sys_use_consumer_push":
                    retVal = prefix + "Push.bmp";
                    break;
                case "use_camera_desktop":
                    retVal = prefix + "CaptureScreen.bmp";
                    break;
                case "use_gamepad":
                    retVal = prefix + "Gamepad.bmp";
                    break;
            }

            return retVal;
        }

        /// <summary>
        /// Load the permissions list
        /// </summary>
        private void LoadPermissions()
        {
            if (_permissionList == null)
            {
                var activeNdk = PackageViewModel.Instance.ActiveNDK;
                PermissionInfo[] permissions = activeNdk != null ? activeNdk.Permissions : PermissionInfo.CreateDefaultList();

                // convert permissions to view-model model items:
                var list = new List<PermissionItemClass>();
                foreach(var permission in permissions)
                    list.Add(new PermissionItemClass(permission, this));

                _permissionList = new CollectionView(list);
            }
        }

        /// <summary>
        /// Called on idle time. This is when we check if the designer is out of sync with the underlying text buffer.
        /// </summary>
        public void DoIdle()
        {
            if (BufferDirty || DesignerDirty)
            {
                const int delay = 100;

                if ((Environment.TickCount - _dirtyTime) > delay)
                {
                    // Must not try and sync while XML editor is parsing otherwise we just confuse matters.
                    if (IsXmlEditorParsing)
                    {
                        _dirtyTime = Environment.TickCount;
                        return;
                    }

                    //If there is contention, give the preference to the designer.
                    if (DesignerDirty)
                    {
                        SaveModelToXmlModel(Strings.SynchronizeBuffer);
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

                XmlSerializer serializer = new XmlSerializer(typeof(QnxRootType));
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
                using (CompoundAction ca = new CompoundAction(src, Strings.SynchronizeBuffer))
                {
                    using (XmlEditingScope scope = _xmlStore.BeginEditingScope(Strings.SynchronizeBuffer, this))
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
            using (EditArray edits = new EditArray(src, null, false, Strings.ReformatBuffer))
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
            LanguageService langsvc = GetXmlLanguageService();

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
                XmlSerializer serializer = new XmlSerializer(typeof(QnxRootType));

                using (XmlReader reader = GetParseTree().CreateReader())
                {
                    _qnxSchema = (QnxRootType)serializer.Deserialize(reader);
                }

                if (_qnxSchema == null)
                {
                    throw new Exception(Strings.InvalidVsTemplateData);
                }
            }
            catch (Exception e)
            {
                //Display error message
                ErrorHandler.ThrowOnFailure(VsShellUtilities.ShowMessageBox(_serviceProvider,
                    Strings.InvalidVsTemplateData + e.Message,
                    Strings.ErrorMessageBoxTitle,
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
        public void SetAuthorInfoFrom(string debugTokenFileName, IEventDispatcher dispatcher, EventHandler failHandler)
        {
            if (string.IsNullOrEmpty(debugTokenFileName))
                throw new ArgumentNullException("debugTokenFileName");

            // is running...
            if (_debugTokenInfoRunner != null)
                return;

            if (!File.Exists(debugTokenFileName))
            {
                if (failHandler != null)
                    failHandler(this, EventArgs.Empty);
                return;
            }

            _debugTokenInfoRunner = new DebugTokenInfoRunner(debugTokenFileName);
            _debugTokenInfoRunner.Tag = failHandler;
            _debugTokenInfoRunner.Dispatcher = dispatcher;
            _debugTokenInfoRunner.Finished += DebugTokenInfoLoaded;
            _debugTokenInfoRunner.ExecuteAsync();
        }

        private void DebugTokenInfoLoaded(object sender, ToolRunnerEventArgs e)
        {
            var debugToken = _debugTokenInfoRunner.DebugToken;
            _debugTokenInfoRunner = null;

            if (e.IsSuccessfull && debugToken != null && debugToken.Author != null)
            {
                Author = debugToken.Author.Name;
                AuthorID = debugToken.Author.ID;

                // order cache update...
                PackageViewModel.Instance.UpdateCachedAuthor(debugToken.Author);
            }
            else
            {
                // notify about the error:
                var failHandler = e.Tag as EventHandler;
                if (failHandler != null)
                    failHandler(this, EventArgs.Empty);
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
                return _qnxSchema.name.Value;
            }
            set
            {
                if (_qnxSchema.name.Value != value)
                {
                    _qnxSchema.name.Value = value;
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
                return _qnxSchema.description.Value;
            }
            set
            {
                if (_qnxSchema.description.Value != value)
                {
                    _qnxSchema.description.Value = value;
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
        public List<AssetType> AssetList
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
                    foreach (var config in _qnxSchema.configuration)
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
            var newAsset = new AssetType();

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
                foreach (var config in _qnxSchema.configuration)
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
                _qnxSchema.DeleteLocalAsset(asset as AssetType);
            else
            {
                foreach (var config in _qnxSchema.configuration)
                {
                    if (config.name == _config.Name)
                    {
                        config.DeleteAsset(asset as AssetType);
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
                foreach (var assetItem in _qnxSchema.asset)
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
                foreach (var config in _qnxSchema.configuration)
                {
                    if (config.name == _config.Name)
                    {
                        foreach (var assetItem in config.asset)
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

        public void AddIcon(FileInfo icon)
        {
            _qnxSchema.icon.AddImage(icon.Name);
            DesignerDirty = true;
            IList source = (IList)_iconImageList.SourceCollection;
              ImageItemClass image = new ImageItemClass(icon.Name, icon.ToString(), _activeProjectDirectory);
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
            _qnxSchema.icon.DeleteImage(item.ImageName);
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

        public void AddSplashScreen(FileInfo splashScreen)
        {
            ImageType qnxSS;

            if (_qnxSchema.splashScreens == null)
            {
                qnxSS = new ImageType();
                _qnxSchema.splashScreens = qnxSS;
            }
            else
            {
                qnxSS = _qnxSchema.splashScreens;
            }

            qnxSS.AddImage(splashScreen.Name);
            DesignerDirty = true;
            IList source = (IList)_splashScreenImageList.SourceCollection;
            ImageItemClass image = new ImageItemClass(splashScreen.Name, splashScreen.ToString(), _activeProjectDirectory);
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
            _qnxSchema.splashScreens.DeleteImage(item.ImageName);
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

            if (!isPermissionChecked(identifier))
            {
                /// add new perm to xml
                var perm = new PermissionType();
                perm.Value = identifier;
                _qnxSchema.AddPermission(perm);
            }
            DesignerDirty = true;
        }

        public void UnCheckPermission(string identifier)
        {
            /// add new perm to xml
            var perm = new PermissionType();
            perm.Value = identifier;
            _qnxSchema.DeletePermission(perm);
            DesignerDirty = true;
        }

        public bool isPermissionChecked(string identifier)
        {
            if (_qnxSchema.permission != null)
            {
                foreach (var permEntry in _qnxSchema.permission)
                {
                    if (permEntry.Value == identifier)
                    {
                        return true;
                    }
                }
            }
            return false;
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
            get { return string.Empty; }
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

        private void NotifyPropertyChanged(string propertyName)
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

        #region IDisposable Implementation

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_debugTokenInfoRunner != null)
                {
                    _debugTokenInfoRunner.Dispose();
                    _debugTokenInfoRunner = null;
                }
            }
        }

        #endregion
    }
}
