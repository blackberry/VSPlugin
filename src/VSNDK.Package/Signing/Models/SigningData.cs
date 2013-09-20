using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;
using System.IO.Packaging;

namespace RIM.VSNDK_Package.Signing.Models
{
    /// <summary>
    /// Common Class for the signing dialogs
    /// </summary>
    class SigningData : INotifyPropertyChanged
    {
        /// <summary>
        /// Private Constants
        /// </summary>
        private const string _colRegistered = "Registered";
        private const string _colUnregistered = "Unregistered";
        private const string _colUnregisterdText = "UnregisteredText";
        private const string p12 = @"/author.p12";
        private const string csk = @"/bbidtoken.csk";

        /// <summary>
        /// Private Members
        /// </summary>
        private bool _registered;
        private string _certPath;
        private string _bbidtokenPath;

        /// <summary>
        /// Constructor
        /// </summary>
        public SigningData()
        {
            _certPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\author.p12";
            _bbidtokenPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion\bbidtoken.csk";

            RefreshScreen();
        }

        /// <summary>
        /// Refresh the screen data
        /// </summary>
        public void RefreshScreen()
        {

            Registered = File.Exists(_certPath); 
        }

        /// <summary>
        /// Unregistered propert 
        /// For setting the enabled property for the Unregistered button
        /// </summary>
        public bool Unregistered
        {
            get
            {
                return !_registered;
            }
        }

        /// <summary>
        /// Property for setting the registered label
        /// </summary>
        public string UnregisteredText
        {
            get
            {
                return _registered ? "Registered" : "Not Registered";
            }
        }

        /// <summary>
        /// Function to backup the signing certs at the specified path
        /// </summary>
        /// <param name="certPath">Path to the signing keys</param>
        /// <param name="toZipFile">Path to destination zip file</param>
        public void Backup(string certPath, string toZipFile)
        {
            using (Package pkg = Package.Open(toZipFile, FileMode.Create))
            {
                AddUriToPackage(certPath, p12, pkg);
                AddUriToPackage(certPath, csk, pkg);
            }
        }

        /// <summary>
        /// Function to add a file to the zip
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="file">File name</param>
        /// <param name="pkg">Package to add file to</param>
        private void AddUriToPackage(string path, string file, Package pkg)
        {
            Uri uri = null;
            PackagePart pkgPart = null;
            uri = PackUriHelper.CreatePartUri(new Uri(file, UriKind.Relative));
            pkgPart = pkg.CreatePart(uri, string.Empty);
            using (FileStream fileStream = new FileStream(path + file, FileMode.Open, FileAccess.Read))
            {
                CopyStream(fileStream, pkgPart.GetStream());
            }
        }

        /// <summary>
        /// Function to unzip and restore a set of singing keys
        /// </summary>
        /// <param name="fromZipFile"></param>
        /// <param name="certPath"></param>
        public void Restore(string fromZipFile, string certPath)
        {

            Package zipFilePackage = ZipPackage.Open(fromZipFile, FileMode.Open, FileAccess.ReadWrite);

            //Iterate through the all the files that 
            //is added within the collection and 
            foreach (ZipPackagePart contentFile in zipFilePackage.GetParts())
            {
                createFile(certPath, contentFile);
            }

            zipFilePackage.Close();

        }


        /// <summary>
        /// Method to create file at the temp folder
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="contentFileURI"></param>
        /// <returns></returns>
        private void createFile(string rootFolder, ZipPackagePart contentFile)
        {
            // Initially create file under the folder specified
            string contentFilePath = string.Empty;
            contentFilePath = contentFile.Uri.OriginalString.Replace('/',
                             System.IO.Path.DirectorySeparatorChar);

            if (contentFilePath.StartsWith(
                System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                contentFilePath = contentFilePath.TrimStart(
                                         System.IO.Path.DirectorySeparatorChar);
            }
            else
            {
                //do nothing
            }

            contentFilePath = System.IO.Path.Combine(rootFolder, contentFilePath);

            //Check for the folder already exists. If not then create that folder

            if (System.IO.Directory.Exists(
                System.IO.Path.GetDirectoryName(contentFilePath)) != true)
            {
                System.IO.Directory.CreateDirectory(
                          System.IO.Path.GetDirectoryName(contentFilePath));
            }
            else
            {
                //do nothing
            }

            System.IO.FileStream newFileStream =
                    System.IO.File.Create(contentFilePath);
            newFileStream.Close();
            byte[] content = new byte[contentFile.GetStream().Length];
            contentFile.GetStream().Read(content, 0, content.Length);
            System.IO.File.WriteAllBytes(contentFilePath, content);

        }

        /// <summary>
        /// Function to copy a stream from one stream to another
        /// </summary>
        /// <param name="source">Source Stream</param>
        /// <param name="target">Target Stream</param>
        private void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }

        /// <summary>
        /// Registered Property
        /// </summary>
        public bool Registered
        {
            get 
            { 
                return _registered; 
            }
            set 
            { 
                _registered = value;
                OnPropertyChanged(_colRegistered);
                OnPropertyChanged(_colUnregisterdText);
                OnPropertyChanged(_colUnregistered);
            }
        }

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire the PropertyChnaged event handler on change of property
        /// </summary>
        /// <param name="propName"></param>
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
