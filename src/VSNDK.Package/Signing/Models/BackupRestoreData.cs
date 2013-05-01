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
using PkgResources = RIM.VSNDK_Package.Resources;
using System.IO;
using System.IO.Packaging;

namespace RIM.VSNDK_Package.Signing.Models
{
    class BackupRestoreData : NotifyPropertyChanged
    {
        private string _info;

        private static string p12 = "/author.p12";
        private static string csk = "/barsigner.csk";
        private static string db = "/barsigner.db";

        public BackupRestoreData()
        {
            _info = PkgResources.BackupRestoreInfo;
        }

        public string Info { get { return _info; } }

        public void Backup(string certPath, string toZipFile)
        {
            using (Package pkg = Package.Open(toZipFile, FileMode.Create))
            {
                AddUriToPackage(certPath, p12, pkg);
                AddUriToPackage(certPath, csk, pkg);
                AddUriToPackage(certPath, db, pkg);
            }
        }

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
            contentFilePath =contentFile.Uri.OriginalString.Replace('/', 
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
            //contentFilePath =  System.IO.Path.Combine(rootFolder, contentFilePath); 

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
                    System.IO.File.Create( contentFilePath );
            newFileStream.Close(); 
            byte[] content = new byte[contentFile.GetStream().Length];
            contentFile.GetStream().Read(content, 0, content.Length );
            System.IO.File.WriteAllBytes(contentFilePath, content);

        } 

        private void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }
    }
}
