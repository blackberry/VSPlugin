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

namespace RIM.VSNDK_Package
{
    using System.Xml.Serialization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.qnx.com/schemas/application/1.0", IsNullable=true)]
    public partial class asset : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string pathField;

        private string publicAssetField;
        
        private string entryField;
        
        private string typeField;

        private string[] _assettypeList = {"Other", "Library", "Executable", "Entry-point"};

        private string _assettype = "Other";
        
        private string valueField;

        [System.Xml.Serialization.XmlIgnore]
        public string assettype
        {
            get
            {
                _assettype = getAssetType();
                return _assettype;
            }
            set
            {
                if (value != _assettype)
                    _assettype = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string[] assettypelist
        {
            get
            {
                return this._assettypeList;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string path {
            get {
                return this.pathField;
            }
            set {
                this.pathField = value;
                this.RaisePropertyChanged("path");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("public")]
        public string publicAsset
        {
            get 
            {
                return this.publicAssetField;
            }
            set
            {
                this.publicAssetField = value;
                this.RaisePropertyChanged("publicAsset");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string entry {
            get {
                return this.entryField;
            }
            set {
                this.entryField = value;
                this.RaisePropertyChanged("entry");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type {
            get {
                return this.typeField;
            }
            set {
                this.typeField = value;
                this.RaisePropertyChanged("type");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
                this.RaisePropertyChanged("Value");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) 
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private string getAssetType()
        {
            if (this.valueField.StartsWith("lib/"))
            {
                return "Library";
            }
            else if (this.typeField == "Qnx/Elf")
            {
                if (this.entry == "true")
                {
                    return "Entry-point";
                }
                else
                {
                    return "Executable";
                }
            }
            else
            {
                return "Other";
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.qnx.com/schemas/application/1.0", IsNullable=false)]
    public partial class qnx : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string idField;
        
        private string nameField;
        
        private string versionNumberField;
        
        private string buildIdField;
        
        private string descriptionField;
        
        private string authorField;

        private string authorIdField;
        
        private string categoryField;
        
        private qnxInitialWindow initialWindowField;
        
        private asset[] assetField;
        
        private qnxConfiguration[] configurationField;

        private qnxPermission[] permissionField;

        private qnxIcon iconField;

        private qnxSplashScreens splashScreenField;
        
        private qnxAction[] actionField;
        
        private qnxEnv[] envField;
        
        /// <remarks/>
        public string id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
                this.RaisePropertyChanged("id");
            }
        }
        
        /// <remarks/>
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
                this.RaisePropertyChanged("name");
            }
        }
        
        /// <remarks/>
        public string versionNumber {
            get {
                return this.versionNumberField;
            }
            set {
                this.versionNumberField = value;
                this.RaisePropertyChanged("versionNumber");
            }
        }
        
        /// <remarks/>
        public string buildId {
            get {
                return this.buildIdField;
            }
            set {
                this.buildIdField = value;
                this.RaisePropertyChanged("buildId");
            }
        }
        
        /// <remarks/>
        public string description {
            get {
                return this.descriptionField;
            }
            set {
                this.descriptionField = value;
                this.RaisePropertyChanged("description");
            }
        }
        
        /// <remarks/>
        public string author {
            get {
                return this.authorField;
            }
            set {
                this.authorField = value;
                this.RaisePropertyChanged("author");
            }
        }

        /// <remarks/>
        public string authorId
        {
            get
            {
                return this.authorIdField;
            }
            set
            {
                this.authorIdField = value;
                this.RaisePropertyChanged("authorId");
            }
        }
        
        /// <remarks/>
        public string category {
            get {
                return this.categoryField;
            }
            set {
                this.categoryField = value;
                this.RaisePropertyChanged("category");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("initialWindow")]
        public qnxInitialWindow initialWindow {
            get {
                return this.initialWindowField;
            }
            set {
                this.initialWindowField = value;
                this.RaisePropertyChanged("initialWindow");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("asset", IsNullable=true)]
        public asset[] asset {
            get {
                return this.assetField;
            }
            set {
                this.assetField = value;
                this.RaisePropertyChanged("asset");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("configuration")]
        public qnxConfiguration[] configuration {
            get {
                return this.configurationField;
            }
            set {
                this.configurationField = value;
                this.RaisePropertyChanged("configuration");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("permission", IsNullable = true)]
        public qnxPermission[] permission
        {
            get
            {
                return this.permissionField;
            }
            set
            {
                this.permissionField = value;
                this.RaisePropertyChanged("permission");
            }
        }

        public void AddPermission(qnxPermission newPermission)
        {
            if (permission == null)
            {
                permission = new qnxPermission[0];
            }

            var newPermissionsList = permission;
            Array.Resize(ref newPermissionsList, newPermissionsList.Length + 1);
            newPermissionsList[newPermissionsList.Length - 1] = newPermission;
            permission = newPermissionsList;
        }

        public void DeletePermission(qnxPermission oldPermission)
        {
            foreach (qnxPermission perm in permission)
            {
                if (perm.Value == oldPermission.Value)
                {
                    List<qnxPermission> permissionList = new List<qnxPermission>(permission);
                    permissionList.Remove(perm);
                    permission = permissionList.ToArray();
                }
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("icon")]
        public qnxIcon icon {
            get {
                return this.iconField;
            }
            set {
                this.iconField = value;
                this.RaisePropertyChanged("icon");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("splashScreens")]
        public qnxSplashScreens splashScreens
        {
            get
            {
                return this.splashScreenField;
            }
            set
            {
                this.splashScreenField = value;
                this.RaisePropertyChanged("splashScreens");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("action", IsNullable=true)]
        public qnxAction[] action {
            get {
                return this.actionField;
            }
            set {
                this.actionField = value;
                this.RaisePropertyChanged("action");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("env")]
        public qnxEnv[] env {
            get {
                return this.envField;
            }
            set {
                this.envField = value;
                this.RaisePropertyChanged("env");
            }
        }

        public void AddLocalAsset(asset newAsset)
        {
            if (asset == null)
            {
                asset = new asset[0];
            }

            var local_asset = asset;
            Array.Resize(ref local_asset, local_asset.Length + 1);
            local_asset[local_asset.Length - 1] = newAsset;
            asset = local_asset;
        }

        public void DeleteLocalAsset(asset removeAsset)
        {
            List<asset> assetList = new List<asset>(asset);
            assetList.Remove(removeAsset);
            asset = assetList.ToArray();
        }
        

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxInitialWindow : object, System.ComponentModel.INotifyPropertyChanged {

        private string autoOrientsField;

        private string aspectRatioField;

        private string systemChromeField;
        
        private string transparentField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("autoOrients", IsNullable = true)]
        public string autoOrients
        {
            get
            {
                return this.autoOrientsField;
            }
            set
            {
                this.autoOrientsField = value;
                this.RaisePropertyChanged("autoOrients");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("aspectRatio", IsNullable = true)]
        public string aspectRatio
        {
            get
            {
                return this.aspectRatioField;
            }
            set
            {
                this.aspectRatioField = value;
                this.RaisePropertyChanged("aspectRatio");
            }
        }
        
        /// <remarks/>
        public string systemChrome {
            get {
                return this.systemChromeField;
            }
            set {
                this.systemChromeField = value;
                this.RaisePropertyChanged("systemChrome");
            }
        }
        
        /// <remarks/>
        public string transparent {
            get {
                return this.transparentField;
            }
            set {
                this.transparentField = value;
                this.RaisePropertyChanged("transparent");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxConfiguration : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string platformArchitectureField;
        
        private asset[] assetField;
        
        private string idField;
        
        private string nameField;
        
        /// <remarks/>
        public string platformArchitecture {
            get {
                return this.platformArchitectureField;
            }
            set {
                this.platformArchitectureField = value;
                this.RaisePropertyChanged("platformArchitecture");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("asset", IsNullable=true)]
        public asset[] asset {
            get {
                return this.assetField;
            }
            set {
                this.assetField = value;
                this.RaisePropertyChanged("asset");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
                this.RaisePropertyChanged("id");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
                this.RaisePropertyChanged("name");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public void AddAsset(asset newAsset)
        {
            if (asset == null)
            {
                asset = new asset[0];
            }

            var local_asset = asset;
            Array.Resize(ref local_asset, local_asset.Length + 1);
            local_asset[local_asset.Length - 1] = newAsset;
            asset = local_asset;
        }

        public void DeleteAsset(asset removeAsset)
        {
            List<asset> assetList = new List<asset>(asset);
            assetList.Remove(removeAsset);
            asset = assetList.ToArray();
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxPermission : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string systemField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string system
        {
            get
            {
                return this.systemField;
            }
            set
            {
                this.systemField = value;
                this.RaisePropertyChanged("system");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
                this.RaisePropertyChanged("Value");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxIcon : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string[] imageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("image", IsNullable = true)]
        public string[] image {
            get {
                return this.imageField;
            }
            set {
                this.imageField = value;
                this.RaisePropertyChanged("image");
            }
        }

        public void AddIconImage(string iconImage)
        {
            if (image == null)
            {
                image = new string[0];
            }

            var iconImages = image;
            Array.Resize(ref iconImages, iconImages.Length + 1);
            iconImages[iconImages.Length - 1] = iconImage;
            image = iconImages;
        }

        public void DeleteIconImage(string iconImage)
        {
            List<string> iconList = new List<string>(image);
            iconList.Remove(iconImage);
            image = iconList.ToArray();
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxSplashScreens : object, System.ComponentModel.INotifyPropertyChanged
    {

        private string[] imageField;

        [System.Xml.Serialization.XmlElementAttribute("image", IsNullable = true)]
        public string[] image
        {
            get
            {
                return this.imageField;
            }
            set
            {
                this.imageField = value;
                this.RaisePropertyChanged("image");
            }
        }

        public void AddSplashScreenImage(string iconImage)
        {
            if (image == null)
            {
                image = new string[0];
            }

            var iconImages = image;
            Array.Resize(ref iconImages, iconImages.Length + 1);
            iconImages[iconImages.Length - 1] = iconImage;
            image = iconImages;
        }

        public void DeleteSplashScreenImage(string iconImage)
        {
            List<string> iconList = new List<string>(image);
            iconList.Remove(iconImage);
            image = iconList.ToArray();
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null))
            {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxAction : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string systemField;
        
        private string valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string system {
            get {
                return this.systemField;
            }
            set {
                this.systemField = value;
                this.RaisePropertyChanged("system");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
                this.RaisePropertyChanged("Value");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    public partial class qnxEnv : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string varField;
        
        private string valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string var {
            get {
                return this.varField;
            }
            set {
                this.varField = value;
                this.RaisePropertyChanged("var");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
                this.RaisePropertyChanged("value");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.1")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.qnx.com/schemas/application/1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.qnx.com/schemas/application/1.0", IsNullable=false)]
    public partial class NewDataSet : object, System.ComponentModel.INotifyPropertyChanged {
        
        private object[] itemsField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("asset", typeof(asset), IsNullable=true)]
        [System.Xml.Serialization.XmlElementAttribute("qnx", typeof(qnx))]
        public object[] Items {
            get {
                return this.itemsField;
            }
            set {
                this.itemsField = value;
                this.RaisePropertyChanged("Items");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
