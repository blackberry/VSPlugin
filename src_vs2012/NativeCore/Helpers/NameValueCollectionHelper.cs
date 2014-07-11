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
using System.Text;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class for managing key-value collection.
    /// </summary>
    public static class CollectionHelper
    {
        /// <summary>
        /// Dumps given collection into a single string.
        /// All values are converted to Base64 (just to avoid possible collisions with special separator chars: ';' and '=').
        /// </summary>
        public static string Serialize(Dictionary<string, string> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            var result = new StringBuilder();
            foreach (var pair in dictionary)
            {
                if (pair.Key.IndexOf('=') >= 0 || pair.Key.IndexOf(';') >= 0)
                    throw new ArgumentOutOfRangeException("dictionary", "Forbidden char found in key name (" + pair.Key + ")");

                result.Append(pair.Key).Append('=');
                if (pair.Value == null)
                    result.Append('~');
                else
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                        result.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(pair.Value)));
                }
                result.Append(';');
            }
            return result.ToString();
        }

        /// <summary>
        /// Updates the collection with a value parsed out from a given string.
        /// </summary>
        public static Dictionary<string, string> Deserialize(string data)
        {
            var result = new Dictionary<string, string>();

            // if there was any input data:
            if (!string.IsNullOrEmpty(data))
            {
                string[] entryDescriptor = data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in entryDescriptor)
                {
                    int valueAt = item.IndexOf('=');
                    if (valueAt < 0)
                        throw new ArgumentOutOfRangeException("data", "Missing value definition in entry: \"" + item + "\"");

                    var key = item.Substring(0, valueAt);
                    var value = item.Substring(valueAt + 1);
                    if (string.IsNullOrEmpty(value))
                    {
                        result.Add(key, string.Empty);
                    }
                    else
                    {
                        if (value == "~")
                            result.Add(key, null);
                        else
                            result.Add(key, Encoding.UTF8.GetString(Convert.FromBase64String(value)));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Appends info about the device into the dictionary.
        /// </summary>
        public static void AppendDevice(Dictionary<string, string> data, DeviceDefinition device)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (device == null)
                throw new ArgumentNullException("device");

            data["target.name"] = device.Name;
            data["target.IP"] = device.IP;
            data["target.password"] = device.Password;
            data["target.type"] = DeviceHelper.GetTypeToString(device.Type);
        }

        /// <summary>
        /// Gets the info about target device from the specified dictionary.
        /// </summary>
        public static DeviceDefinition GetDevice(Dictionary<string, string> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // data about the device is missing:
            if (!data.ContainsKey("target.IP") || !data.ContainsKey("target.type") || !data.ContainsKey("target.password"))
                return null;

            var name = data.ContainsKey("target.name") ? data["target.name"] : "Ad-hoc device";

            return new DeviceDefinition(name, data["target.IP"], data["target.password"], DeviceHelper.GetTypeFromString(data["target.type"]));
        }

        /// <summary>
        /// Appends info about the NDK into the dictionary.
        /// </summary>
        public static void AppendNDK(Dictionary<string, string> data, NdkDefinition ndk)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (ndk == null)
                throw new ArgumentNullException("ndk");

            data["ndk.type"] = DeviceHelper.GetFamilyTypeToString(ndk.Type); 
            data["ndk.path.host"] = ndk.HostPath;
            data["ndk.path.target"] = ndk.TargetPath;
        }

        /// <summary>
        /// Gets the info about NDK from the specified dictionary.
        /// </summary>
        public static NdkDefinition GetNDK(Dictionary<string, string> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // data about the NDK is missing:
            if (!data.ContainsKey("ndk.type") || !data.ContainsKey("ndk.path.host") || !data.ContainsKey("ndk.path.target"))
                return null;

            return new NdkDefinition(data["ndk.path.host"], data["ndk.path.target"], DeviceHelper.GetFamilyTypeFromString(data["ndk.type"]));
        }
    }
}
