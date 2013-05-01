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
using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VSNDK.Tasks
{
    public class CheckFlagFile : Task
    {
        private string _action;
        private string _flagFile;
        private bool _isFlagSet;

        public override bool Execute()
        {
            //System.Diagnostics.Debugger.Launch();
            _isFlagSet = false;
            
            if (String.Equals(_action, "check", StringComparison.OrdinalIgnoreCase)) 
            {            
			    if (File.Exists(_flagFile))
                {
				    _isFlagSet = true;				    
			    }
                return true;
            } 
            else if (String.Equals(_action, "remove", StringComparison.OrdinalIgnoreCase)) 
            {
                try
                {
                    File.Delete(_flagFile);
                    return true;
                }
                catch (DirectoryNotFoundException dirNotFound)
                {
                    Console.WriteLine(dirNotFound.Message);
                    return false;
                }                
            } 
            
            // Unrecognized action
            return false;
        }

        [Required]
        public string FlagFile
        {
            set
            {
                _flagFile = value;
            }
        }

        [Required]
        public string Action
        {
            set
            {
                _action = value;
            }
        }

        [Output]
        public bool IsFlagSet
        {
            get
            {
                return _isFlagSet;
            }
        }
    }
}
