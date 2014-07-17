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
using System.Text;
using System.Collections.Specialized;

namespace BlackBerry.Package.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class NameValueCollectionHelper
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DumpToString(NameValueCollection value)
        {
            var xSB = new StringBuilder();
            foreach (string xKey in value.Keys)
            {
                xSB.AppendFormat("{0}={1};", xKey, (string)value[xKey]);
            }
            return xSB.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        public static void LoadFromString(NameValueCollection target, string value)
        {
            if (target.Count > 0)
            {
                throw new Exception("Target is not empty!");
            }
            if (String.IsNullOrEmpty(value))
            {
                return;
            }

            string[] xPairs = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var xPair in xPairs)
            {
                string[] xParts = xPair.Split('=');
                if (xParts.Length > 1)
                {
                    target.Add(xParts[0], xParts[1]);
                }
                else
                {
                    target.Add(xParts[0], "");
                }
            }
        }
    }
}