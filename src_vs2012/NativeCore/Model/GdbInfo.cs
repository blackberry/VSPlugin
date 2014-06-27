using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Description of locally installed GDB.
    /// </summary>
    public sealed class GdbInfo
    {
        public GdbInfo(NdkDefinition ndk, DeviceDefinitionType deviceType, RuntimeDefinition runtime, IEnumerable<string> additionalLibraryPaths)
        {
            if (ndk == null)
                throw new ArgumentNullException("ndk");
            if (string.IsNullOrEmpty(ndk.HostPath))
                throw new ArgumentOutOfRangeException("ndk");
            if (string.IsNullOrEmpty(ndk.TargetPath))
                throw new ArgumentOutOfRangeException("ndk");

            NDK = ndk;
            DeviceType = deviceType;
            Runtime = runtime;
            LibraryPaths = CreateLibraryPaths(ndk, deviceType, additionalLibraryPaths);
            Executable = Path.Combine(NDK.HostPath, "usr", "bin", GetName(deviceType));
            Arguments = "--interpreter=mi2";
        }

        public GdbInfo(NdkInfo ndk, DeviceDefinitionType deviceType, RuntimeDefinition runtime, IEnumerable<string> additionalLibraryPaths)
            : this(ndk != null ? ndk.ToDefinition() : null, deviceType, runtime, additionalLibraryPaths)
        {
        }

        #region Properties

        public NdkDefinition NDK
        {
            get;
            private set;
        }

        public DeviceDefinitionType DeviceType
        {
            get;
            private set;
        }

        public string Architecture
        {
            get
            {
                switch (DeviceType)
                {
                    case DeviceDefinitionType.Device:
                        return "ARM";
                    case DeviceDefinitionType.Simulator:
                        return "x86";
                    default:
                        throw new InvalidEnumArgumentException("Unsupported enum value (" + DeviceType + ")");
                }
            }
        }

        public RuntimeDefinition Runtime
        {
            get;
            private set;
        }

        public string Executable
        {
            get;
            private set;
        }

        public string Arguments
        {
            get;
            private set;
        }

        public string[] LibraryPaths
        {
            get;
            private set;
        }

        #endregion

        private string GetName(DeviceDefinitionType type)
        {
            switch (type)
            {
                case DeviceDefinitionType.Device:
                    return "ntoarm-gdb.exe";
                case DeviceDefinitionType.Simulator:
                    return "ntox86-gdb.exe";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private static string[] CreateLibraryPaths(NdkDefinition ndk, DeviceDefinitionType deviceType, IEnumerable<string> additionalLibraryPaths)
        {
            if (ndk == null)
                throw new ArgumentNullException("ndk");

            // create list of folders with installed libraries:
            var libraries = new List<string>();

            // developer-specified paths go first:
            if (additionalLibraryPaths != null)
            {
                libraries.AddRange(additionalLibraryPaths);
            }

            // then the default ones, that belong to current NDK:
            libraries.Add(Path.Combine(ndk.TargetPath, GetArchitectureFolder(deviceType), "lib"));
            libraries.Add(Path.Combine(ndk.TargetPath, GetArchitectureFolder(deviceType), "usr", "lib"));
            return libraries.ToArray();
        }

        private static string GetArchitectureFolder(DeviceDefinitionType type)
        {
            switch (type)
            {
                case DeviceDefinitionType.Device:
                    return "armle-v7";
                case DeviceDefinitionType.Simulator:
                    return "x86";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}
