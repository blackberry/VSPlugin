using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BlackBerry.BarDescriptor.Model
{
    /// <summary>
    /// Following are some extensions for all the classes defining bar-descriptor structure.
    /// The base bar-descriptor.cs file was automatically generated from XSD using command:
    /// 
    ///     xsd /n:BlackBerry.BarDescriptor.Model /classes /language:CS /edb bar-descriptor.xsl
    /// 
    /// Having the 'working' part here guarantees it's not lost each new bar-descriptor classes re-creation.
    /// </summary>
    partial class QnxRootType
    {
        public void AddPermission(PermissionType newPermission)
        {
            if (permission == null)
            {
                permission = new PermissionType[0];
            }

            var newPermissionsList = permission;
            Array.Resize(ref newPermissionsList, newPermissionsList.Length + 1);
            newPermissionsList[newPermissionsList.Length - 1] = newPermission;
            permission = newPermissionsList;
        }


        public void DeletePermission(PermissionType oldPermission)
        {
            foreach (var perm in permission)
            {
                if (perm.Value == oldPermission.Value)
                {
                    var permissionList = new List<PermissionType>(permission);
                    permissionList.Remove(perm);
                    permission = permissionList.ToArray();
                }
            }
        }

        public void AddLocalAsset(AssetType newAsset)
        {
            if (asset == null)
            {
                asset = new AssetType[0];
            }

            var localAsset = asset;
            Array.Resize(ref localAsset, localAsset.Length + 1);
            localAsset[localAsset.Length - 1] = newAsset;
            asset = localAsset;
        }

        public void DeleteLocalAsset(AssetType removeAsset)
        {
            var assetList = new List<AssetType>(asset);
            assetList.Remove(removeAsset);
            asset = assetList.ToArray();
        }
    }

    partial class ConfigurationType
    {
        public void AddAsset(AssetType newAsset)
        {
            if (asset == null)
            {
                asset = new AssetType[0];
            }

            var localAsset = asset;
            Array.Resize(ref localAsset, localAsset.Length + 1);
            localAsset[localAsset.Length - 1] = newAsset;
            asset = localAsset;
        }

        public void DeleteAsset(AssetType removeAsset)
        {
            var assetList = new List<AssetType>(asset);
            assetList.Remove(removeAsset);
            asset = assetList.ToArray();
        }
    }

    partial class LocalizedStringType
    {
        [XmlIgnore]
        public string Value
        {
            get { return Text != null && Text.Length > 0 ? Text[0] : null; }
            set { Text = value != null ? new[] { value } : null; }
        }
    }

    partial class AssetType
    {
        private static readonly string[] AssetTypeList = {"Other", "Library", "Executable", "Entry-point"}; 
         private string _assetType = "Other"; 


        [XmlIgnore]
        public string Value
        {
            get { return Text != null && Text.Length > 0 ? Text[0] : null; }
            set { Text = value != null ? new[] { value } : null; }
        }

        [XmlIgnore]
        public string publicAsset
        {
            get { return @public; }
            set
            {
                if (value != @public)
                {
                    @public = value;
                    this.RaisePropertyChanged("publicAsset");
                }
            }
        }

        [XmlIgnore]
        public string assettype
        {
            get
            {
                _assetType = GetAssetType();
                return _assetType;
            }
            set
            {
                if (value != _assetType)
                {
                    _assetType = value;
                    this.RaisePropertyChanged("assettype");
                }
            }
        }

        [XmlIgnore]
        public string[] assettypelist
        {
            get { return AssetTypeList; }
        }

        private string GetAssetType()
        {
            if (this.Value.StartsWith("lib/"))
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

    partial class ImageType
    {
        [XmlIgnore]
        public string defaultImage
        {
            get { return image != null && image.Length > 0 ? image[0] : null; }
            set
            {
                if (image != null && image.Length > 0)
                {
                    image[0] = value;
                }
            }
        }

        public void AddImage(string icon)
        {
            if (image == null)
            {
                image = new string[0];
            }

            var iconImages = image;
            Array.Resize(ref iconImages, iconImages.Length + 1);
            iconImages[iconImages.Length - 1] = icon;
            image = iconImages;
        }

        public void DeleteImage(string icon)
        {
            var iconList = new List<string>(image);
            iconList.Remove(icon);
            image = iconList.ToArray();
        }
    }
}
