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
using System.IO;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VSNDK.Tasks
{
    public class GetAppInfo : Task
    {
        private string _barDescriptorPath;
        private string _appId;

        public override bool Execute()
        {
            XmlReader reader = XmlReader.Create(_barDescriptorPath);
            reader.ReadToFollowing("id");
            _appId = reader.ReadElementContentAsString();

            return true;
        }

        [Required]
        public string ApplicationDescriptorXml
        {
            set
            {
                _barDescriptorPath = value;
            }
        }

        [Output]
        public string AppId
        {
            get
            {
                return _appId;
            }
        }
    }
}
